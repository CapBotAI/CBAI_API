using System;
using System.Linq.Expressions;
using App.BLL.Interfaces;
using App.Commons.Extensions;
using App.Commons.Paging;
using App.Commons.ResponseModel;
using App.DAL.Interfaces;
using App.DAL.Queries.Implementations;
using App.DAL.UnitOfWork;
using App.Entities.DTOs.Submissions;
using App.Entities.Entities.App;
using App.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.BLL.Implementations;

public class SubmissionService : ISubmissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityRepository _identityRepository;

    public SubmissionService(IUnitOfWork unitOfWork, IIdentityRepository identityRepository)
    {
        _unitOfWork = unitOfWork;
        _identityRepository = identityRepository;
    }

    public async Task<BaseResponseModel<SubmissionDetailDTO>> CreateSubmission(CreateSubmissionDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == dto.TopicVersionId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Topic)
                .WithTracking(false)
                .Build());

            if (topicVersion == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Phiên bản chủ đề không tồn tại"
                };
            }

            var phaseRepo = _unitOfWork.GetRepo<Phase>();
            var phase = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                .WithPredicate(x => x.Id == dto.PhaseId && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Semester)
                .WithTracking(false)
                .Build());

            if (phase == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Giai đoạn không tồn tại"
                };
            }

            if (topicVersion.Status != TopicStatus.Draft)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ có thể tạo submission cho phiên bản ở trạng thái Draft"
                };
            }

            // Ràng buộc semester của phase và topic
            if (topicVersion.Topic.SemesterId != phase.SemesterId)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Giai đoạn không thuộc cùng học kỳ với chủ đề"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = dto.GetEntity();
            submission.SubmittedBy = userId;

            await submissionRepo.CreateAsync(submission);

            topicVersion.Status = TopicStatus.SubmissionPending;
            await versionRepo.UpdateAsync(topicVersion);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new BaseResponseModel<SubmissionDetailDTO>
            {
                Data = new SubmissionDetailDTO(submission),
                IsSuccess = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo submission thành công"
            };
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }

    public async Task<BaseResponseModel<SubmissionDetailDTO>> UpdateSubmission(UpdateSubmissionDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Phase)
                .WithTracking(true)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            if (submission.SubmittedBy != userId)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền cập nhật submission này"
                };
            }

            if (submission.Status != SubmissionStatus.Pending && submission.Status != SubmissionStatus.RevisionRequired)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ được cập nhật khi submission đang ở trạng thái Pending hoặc RevisionRequired"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            if (submission.TopicVersionId != dto.TopicVersionId)
            {
                var versionRepo = _unitOfWork.GetRepo<TopicVersion>();

                // old version (hiện đang gắn với submission)
                var oldVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                    .WithPredicate(x => x.Id == submission.TopicVersionId)
                    .WithTracking(true)
                    .Build());

                // new version (chuẩn bị gắn)
                var topicVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                    .WithPredicate(x => x.Id == dto.TopicVersionId && x.IsActive && x.DeletedAt == null)
                    .WithInclude(x => x.Topic)
                    .WithTracking(true)
                    .Build());

                if (topicVersion == null)
                {
                    await _unitOfWork.RollBackAsync();
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Phiên bản chủ đề không tồn tại"
                    };
                }

                if (topicVersion.Status != TopicStatus.Draft)
                {
                    await _unitOfWork.RollBackAsync();
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        Message = "Chỉ có thể đổi phiên bản ở trạng thái Draft"
                    };
                }

                topicVersion.Status = TopicStatus.SubmissionPending;
                await versionRepo.UpdateAsync(topicVersion);

                if (oldVersion != null)
                {
                    if (oldVersion.Status == TopicStatus.SubmissionPending)
                        oldVersion.Status = TopicStatus.Draft;
                    else if (oldVersion.Status == TopicStatus.Submitted)
                        oldVersion.Status = TopicStatus.Archived;

                    await versionRepo.UpdateAsync(oldVersion);
                }

                submission.TopicVersionId = dto.TopicVersionId;
            }

            if (submission.PhaseId != dto.PhaseId)
            {
                var phaseRepo = _unitOfWork.GetRepo<Phase>();
                var phase = await phaseRepo.GetSingleAsync(new QueryBuilder<Phase>()
                    .WithPredicate(x => x.Id == dto.PhaseId && x.IsActive && x.DeletedAt == null)
                    .WithInclude(x => x.Semester)
                    .WithTracking(false)
                    .Build());

                if (phase == null)
                {
                    await _unitOfWork.RollBackAsync();
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status404NotFound,
                        Message = "Giai đoạn không tồn tại"
                    };
                }

                // Ensure semester consistency
                // Lấy topicVersion hiện tại để đối chiếu
                var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
                var currentVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                    .WithPredicate(x => x.Id == submission.TopicVersionId)
                    .WithInclude(x => x.Topic)
                    .WithTracking(false)
                    .Build());

                if (currentVersion == null || currentVersion.Topic.SemesterId != phase.SemesterId)
                {
                    await _unitOfWork.RollBackAsync();
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "Giai đoạn không thuộc cùng học kỳ với chủ đề"
                    };
                }

                submission.PhaseId = dto.PhaseId;
            }

            submission.DocumentUrl = dto.DocumentUrl?.Trim();
            submission.AdditionalNotes = dto.AdditionalNotes?.Trim();

            await submissionRepo.UpdateAsync(submission);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new BaseResponseModel<SubmissionDetailDTO>
            {
                Data = new SubmissionDetailDTO(submission),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật submission thành công"
            };
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }

    /// <summary>
    ///Submit submission
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<BaseResponseModel> SubmitSubmission(SubmitSubmissionDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Phase)
                .WithInclude(x => x.TopicVersion)
                .WithTracking(true)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            if (submission.SubmittedBy != userId)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền submit submission này"
                };
            }

            if (submission.Status != SubmissionStatus.Pending)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ được submit khi submission đang ở trạng thái Pending"
                };
            }

            //check status topic version
            if (submission.TopicVersion.Status != TopicStatus.SubmissionPending)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ được submit khi topic version đang ở trạng thái SubmissionPending"
                };
            }


            // Check deadline
            if (submission.Phase.SubmissionDeadline.HasValue &&
                DateTime.Now > submission.Phase.SubmissionDeadline.Value)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Đã quá hạn nộp cho giai đoạn này"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            submission.Status = SubmissionStatus.UnderReview;
            submission.SubmittedAt = DateTime.Now;
            await submissionRepo.UpdateAsync(submission);

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            submission.TopicVersion.Status = TopicStatus.Submitted;
            await versionRepo.UpdateAsync(submission.TopicVersion);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Submit submission thành công"
            };
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }

    public async Task<BaseResponseModel> ResubmitSubmission(ResubmitSubmissionDTO dto, int userId)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == dto.Id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.Phase)
                .WithTracking(true)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            if (submission.SubmittedBy != userId)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền resubmit submission này"
                };
            }

            if (submission.Status != SubmissionStatus.RevisionRequired)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ được resubmit khi submission đang ở trạng thái RevisionRequired"
                };
            }

            // Check deadline
            if (submission.Phase.SubmissionDeadline.HasValue &&
                DateTime.Now > submission.Phase.SubmissionDeadline.Value)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Đã quá hạn nộp lại cho giai đoạn này"
                };
            }

            // Ensure TopicVersion is prepared (SubmissionPending)
            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = await versionRepo.GetSingleAsync(new QueryBuilder<TopicVersion>()
                .WithPredicate(x => x.Id == submission.TopicVersionId)
                .WithTracking(true)
                .Build());

            if (topicVersion == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Phiên bản chủ đề không tồn tại"
                };
            }

            if (topicVersion.Status != TopicStatus.SubmissionPending)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "TopicVersion phải ở trạng thái SubmissionPending trước khi resubmit"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            submission.Status = SubmissionStatus.UnderReview;
            submission.SubmittedAt = DateTime.Now;
            submission.SubmissionRound += 1;
            await submissionRepo.UpdateAsync(submission);

            topicVersion.Status = TopicStatus.Submitted;
            await versionRepo.UpdateAsync(topicVersion);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Resubmit submission thành công"
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<SubmissionDetailDTO>> GetSubmissionDetail(int id)
    {
        try
        {
            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.TopicVersion)
                .WithInclude(x => x.Phase)
                .WithInclude(x => x.SubmittedByUser)
                .WithTracking(false)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            return new BaseResponseModel<SubmissionDetailDTO>
            {
                Data = new SubmissionDetailDTO(submission),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel<PagingDataModel<SubmissionOverviewResDTO, GetSubmissionsQueryDTO>>> GetSubmissions(GetSubmissionsQueryDTO query)
    {
        try
        {
            var submissionRepo = _unitOfWork.GetRepo<Submission>();

            Expression<Func<Submission, bool>> predicate = x => true;

            if (query.TopicVersionId.HasValue)
            {
                var vId = query.TopicVersionId.Value;
                predicate = predicate.AndAlso(x => x.TopicVersionId == vId);
            }

            if (query.PhaseId.HasValue)
            {
                var pId = query.PhaseId.Value;
                predicate = predicate.AndAlso(x => x.PhaseId == pId);
            }

            if (query.Status.HasValue)
            {
                var st = query.Status.Value;
                predicate = predicate.AndAlso(x => x.Status == st);
            }

            var qb = new QueryBuilder<Submission>()
                .WithPredicate(predicate)
                .WithInclude(x => x.SubmittedByUser)
                .WithTracking(false);

            // Lọc theo SemesterId qua join Phase.SemesterId nếu có
            if (query.SemesterId.HasValue)
            {
                qb.WithInclude(x => x.Phase);
            }

            var baseQuery = submissionRepo.Get(qb.Build());

            if (query.SemesterId.HasValue)
            {
                var semId = query.SemesterId.Value;
                baseQuery = baseQuery.Where(x => x.Phase.SemesterId == semId);
            }

            query.TotalRecord = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderByDescending(x => x.SubmittedAt)
                .ToPagedList(query.PageNumber, query.PageSize)
                .ToListAsync();

            return new BaseResponseModel<PagingDataModel<SubmissionOverviewResDTO, GetSubmissionsQueryDTO>>
            {
                Data = new PagingDataModel<SubmissionOverviewResDTO, GetSubmissionsQueryDTO>(
                    items.Select(x => new SubmissionOverviewResDTO(x)).ToList(), query),
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<BaseResponseModel> DeleteSubmission(int id, int userId, bool isAdmin)
    {
        try
        {
            var user = await _identityRepository.GetByIdAsync((long)userId);
            if (user == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Người dùng không tồn tại"
                };
            }

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = await submissionRepo.GetSingleAsync(new QueryBuilder<Submission>()
                .WithPredicate(x => x.Id == id && x.IsActive && x.DeletedAt == null)
                .WithInclude(x => x.TopicVersion)
                .WithTracking(true)
                .Build());

            if (submission == null)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Submission không tồn tại"
                };
            }

            if (submission.SubmittedBy != userId && !isAdmin)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status403Forbidden,
                    Message = "Bạn không có quyền hủy/xóa submission này"
                };
            }

            if (submission.Status == SubmissionStatus.UnderReview || submission.Status == SubmissionStatus.Completed)
            {
                return new BaseResponseModel
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Chỉ được hủy/xóa khi submission ở trạng thái Pending hoặc RevisionRequired"
                };
            }

            await _unitOfWork.BeginTransactionAsync();

            var versionRepo = _unitOfWork.GetRepo<TopicVersion>();
            var topicVersion = submission.TopicVersion;

            if (topicVersion != null)
            {
                if (topicVersion.Status == TopicStatus.SubmissionPending)
                {
                    topicVersion.Status = TopicStatus.Draft;
                    await versionRepo.UpdateAsync(topicVersion);
                }
                else if (topicVersion.Status == TopicStatus.Submitted)
                {
                    topicVersion.Status = TopicStatus.Archived;
                    await versionRepo.UpdateAsync(topicVersion);
                }
            }

            submission.IsActive = false;
            submission.DeletedAt = DateTime.Now;
            await submissionRepo.UpdateAsync(submission);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Hủy/Xóa submission thành công"
            };
        }
        catch (Exception)
        {
            await _unitOfWork.RollBackAsync();
            throw;
        }
    }
}

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

            if (topicVersion.Status != TopicStatus.Approved)
            {
                return new BaseResponseModel<SubmissionDetailDTO>
                {
                    IsSuccess = false,
                    StatusCode = StatusCodes.Status409Conflict,
                    Message = "Chỉ được tạo submission cho phiên bản chủ đề đã được phê duyệt"
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

            var submissionRepo = _unitOfWork.GetRepo<Submission>();
            var submission = dto.GetEntity();
            submission.SubmittedBy = userId;
            // Lưu thời gian tạo bản nộp (SubmittedAt có thể cập nhật lại khi submit)
            submission.SubmittedAt = DateTime.Now;

            await submissionRepo.CreateAsync(submission);
            await _unitOfWork.SaveChangesAsync();

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
                .WithPredicate(x => x.Id == dto.Id)
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

            // Cho phép đổi Phase và TopicVersion nếu cần, kèm validate
            if (submission.TopicVersionId != dto.TopicVersionId)
            {
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

                if (topicVersion.Status != TopicStatus.Approved)
                {
                    return new BaseResponseModel<SubmissionDetailDTO>
                    {
                        IsSuccess = false,
                        StatusCode = StatusCodes.Status409Conflict,
                        Message = "Chỉ được đổi sang phiên bản chủ đề đã được phê duyệt"
                    };
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
            throw;
        }
    }

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
                .WithPredicate(x => x.Id == dto.Id)
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

            submission.Status = SubmissionStatus.UnderReview;
            submission.SubmittedAt = DateTime.Now;

            await submissionRepo.UpdateAsync(submission);
            await _unitOfWork.SaveChangesAsync();

            return new BaseResponseModel
            {
                IsSuccess = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Submit submission thành công"
            };
        }
        catch (Exception)
        {
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
                .WithPredicate(x => x.Id == dto.Id)
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

            submission.Status = SubmissionStatus.UnderReview;
            submission.SubmittedAt = DateTime.Now;
            submission.SubmissionRound += 1;

            await submissionRepo.UpdateAsync(submission);
            await _unitOfWork.SaveChangesAsync();

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
                .WithPredicate(x => x.Id == id)
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
}

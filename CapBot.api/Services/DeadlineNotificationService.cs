using App.BLL.Interfaces;
using App.DAL.UnitOfWork;
using App.DAL.Queries;
using App.Entities.DTOs.Notifications;
using App.Entities.Entities.App;
using App.Entities.Enums;

namespace CapBot.api.Services;

public class DeadlineNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeadlineNotificationService> _logger;

    public DeadlineNotificationService(
        IServiceProvider serviceProvider,
        ILogger<DeadlineNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.Now;
                
                // Chỉ chạy vào 12:00 AM (00:00)
                if (now.Hour == 0 && now.Minute < 5)
                {
                    _logger.LogInformation("Bắt đầu kiểm tra deadline notifications lúc {Time}", now);
                    await CheckAndSendDeadlineNotifications();
                }
                
                // Chờ 1 giờ trước khi kiểm tra lại
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi kiểm tra deadline notifications");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task CheckAndSendDeadlineNotifications()
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            var tomorrow = DateTime.Today.AddDays(1);
            var dayAfterTomorrow = DateTime.Today.AddDays(2);

            // Assignments sắp đến hạn (1-2 ngày)
            var upcomingDeadlineAssignments = await unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(
                new QueryOptions<ReviewerAssignment>
                {
                    Predicate = ra => ra.Deadline.HasValue &&
                                     ra.Deadline.Value.Date >= tomorrow &&
                                     ra.Deadline.Value.Date <= dayAfterTomorrow &&
                                     (ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress),
                    IncludeProperties = new List<System.Linq.Expressions.Expression<Func<ReviewerAssignment, object>>>
                    {
                        ra => ra.Reviewer,
                        ra => ra.Submission,
                        ra => ra.Submission.TopicVersion,
                        ra => ra.Submission.TopicVersion.Topic
                    }
                });

            // Assignments đã quá hạn
            var overdueAssignments = await unitOfWork.GetRepo<ReviewerAssignment>().GetAllAsync(
                new QueryOptions<ReviewerAssignment>
                {
                    Predicate = ra => ra.Deadline.HasValue &&
                                     ra.Deadline.Value.Date < DateTime.Today &&
                                     (ra.Status == AssignmentStatus.Assigned || ra.Status == AssignmentStatus.InProgress),
                    IncludeProperties = new List<System.Linq.Expressions.Expression<Func<ReviewerAssignment, object>>>
                    {
                        ra => ra.Reviewer,
                        ra => ra.Submission,
                        ra => ra.Submission.TopicVersion,
                        ra => ra.Submission.TopicVersion.Topic
                    }
                });

            // Gửi thông báo sắp đến hạn
            foreach (var assignment in upcomingDeadlineAssignments)
            {
                var daysUntilDeadline = (assignment.Deadline!.Value.Date - DateTime.Today).Days;
                var topicTitle = assignment.Submission.TopicVersion?.Topic?.EN_Title ?? 
                               assignment.Submission.Topic?.EN_Title ?? "Không xác định";

                await notificationService.CreateAsync(new CreateNotificationDTO
                {
                    UserId = assignment.ReviewerId,
                    Title = $"Sắp đến hạn review - {daysUntilDeadline} ngày",
                    Message = $"Assignment cho đề tài '{topicTitle}' sẽ đến hạn vào {assignment.Deadline:dd/MM/yyyy HH:mm}. Vui lòng hoàn thành review trước deadline.",
                    Type = NotificationTypes.Warning,
                    RelatedEntityType = "ReviewerAssignment",
                    RelatedEntityId = assignment.Id
                });

                _logger.LogInformation("Đã gửi thông báo sắp đến hạn cho reviewer {ReviewerId}, assignment {AssignmentId}", 
                    assignment.ReviewerId, assignment.Id);
            }

            // Gửi thông báo quá hạn
            foreach (var assignment in overdueAssignments)
            {
                var daysOverdue = (DateTime.Today - assignment.Deadline!.Value.Date).Days;
                var topicTitle = assignment.Submission.TopicVersion?.Topic?.EN_Title ?? 
                               assignment.Submission.Topic?.EN_Title ?? "Không xác định";

                await notificationService.CreateAsync(new CreateNotificationDTO
                {
                    UserId = assignment.ReviewerId,
                    Title = $"Đã quá hạn review - {daysOverdue} ngày",
                    Message = $"Assignment cho đề tài '{topicTitle}' đã quá hạn {daysOverdue} ngày (deadline: {assignment.Deadline:dd/MM/yyyy HH:mm}). Vui lòng liên hệ quản trị viên.",
                    Type = NotificationTypes.Error,
                    RelatedEntityType = "ReviewerAssignment",
                    RelatedEntityId = assignment.Id
                });

                _logger.LogInformation("Đã gửi thông báo quá hạn cho reviewer {ReviewerId}, assignment {AssignmentId}", 
                    assignment.ReviewerId, assignment.Id);
            }

            _logger.LogInformation("Hoàn thành kiểm tra deadline: {UpcomingCount} sắp đến hạn, {OverdueCount} quá hạn",
                upcomingDeadlineAssignments.Count(), overdueAssignments.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý deadline notifications");
        }
    }
}
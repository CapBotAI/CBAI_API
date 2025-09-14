using App.BLL.Interfaces;
using App.Commons.Interfaces;
using App.Commons;
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
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

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
                await SendUpcomingDeadlineNotification(assignment, notificationService, emailService);
            }

            // Gửi thông báo quá hạn
            foreach (var assignment in overdueAssignments)
            {
                await SendOverdueNotification(assignment, notificationService, emailService);
            }

            _logger.LogInformation("Hoàn thành kiểm tra deadline: {UpcomingCount} sắp đến hạn, {OverdueCount} quá hạn",
                upcomingDeadlineAssignments.Count(), overdueAssignments.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý deadline notifications");
        }
    }

    private async Task SendUpcomingDeadlineNotification(
        ReviewerAssignment assignment,
        INotificationService notificationService,
        IEmailService emailService)
    {
        try
        {
            var daysUntilDeadline = (assignment.Deadline!.Value.Date - DateTime.Today).Days;
            var topicTitle = assignment.Submission.TopicVersion?.Topic.EN_Title ??
                           assignment.Submission.Topic.EN_Title ?? "Không xác định";

            var title = $"Sắp đến hạn review - {daysUntilDeadline} ngày";
            var message = $"Assignment cho đề tài '{topicTitle}' sẽ đến hạn vào {assignment.Deadline:dd/MM/yyyy HH:mm}. Vui lòng hoàn thành review trước deadline.";

            // 1. Gửi thông báo qua SignalR (realtime + lưu DB)
            await notificationService.CreateAsync(new CreateNotificationDTO
            {
                UserId = assignment.ReviewerId,
                Title = title,
                Message = message,
                Type = NotificationTypes.Warning,
                RelatedEntityType = "ReviewerAssignment",
                RelatedEntityId = assignment.Id
            });

            // 2. Gửi email
            if (!string.IsNullOrEmpty(assignment.Reviewer.Email))
            {
                var emailBody = CreateUpcomingDeadlineEmailBody(
                    assignment.Reviewer.UserName ?? "Reviewer",
                    topicTitle,
                    assignment.Deadline.Value,
                    daysUntilDeadline);

                var emailModel = new EmailModel(
                    new[] { assignment.Reviewer.Email },
                    $"[CapBot] {title}",
                    emailBody
                );

                var emailSent = await emailService.SendEmailAsync(emailModel);

                if (emailSent)
                {
                    _logger.LogInformation("Đã gửi email sắp đến hạn cho reviewer {ReviewerEmail}, assignment {AssignmentId}",
                        assignment.Reviewer.Email, assignment.Id);
                }
                else
                {
                    _logger.LogWarning("Không thể gửi email sắp đến hạn cho reviewer {ReviewerEmail}, assignment {AssignmentId}",
                        assignment.Reviewer.Email, assignment.Id);
                }
            }

            _logger.LogInformation("Đã gửi thông báo sắp đến hạn cho reviewer {ReviewerId}, assignment {AssignmentId}",
                assignment.ReviewerId, assignment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo sắp đến hạn cho assignment {AssignmentId}", assignment.Id);
        }
    }

    private async Task SendOverdueNotification(
        ReviewerAssignment assignment,
        INotificationService notificationService,
        IEmailService emailService)
    {
        try
        {
            var daysOverdue = (DateTime.Today - assignment.Deadline!.Value.Date).Days;
            var topicTitle = assignment.Submission.TopicVersion?.Topic.EN_Title ??
                           assignment.Submission.Topic.EN_Title ?? "Không xác định";

            var title = $"Đã quá hạn review - {daysOverdue} ngày";
            var message = $"Assignment cho đề tài '{topicTitle}' đã quá hạn {daysOverdue} ngày (deadline: {assignment.Deadline:dd/MM/yyyy HH:mm}). Vui lòng liên hệ quản trị viên.";

            // 1. Gửi thông báo qua SignalR (realtime + lưu DB)
            await notificationService.CreateAsync(new CreateNotificationDTO
            {
                UserId = assignment.ReviewerId,
                Title = title,
                Message = message,
                Type = NotificationTypes.Error,
                RelatedEntityType = "ReviewerAssignment",
                RelatedEntityId = assignment.Id
            });

            // 2. Gửi email
            if (!string.IsNullOrEmpty(assignment.Reviewer.Email))
            {
                var emailBody = CreateOverdueEmailBody(
                    assignment.Reviewer.UserName ?? "Reviewer",
                    topicTitle,
                    assignment.Deadline.Value,
                    daysOverdue);

                var emailModel = new EmailModel(
                    new[] { assignment.Reviewer.Email },
                    $"[CapBot] {title} - URGENT",
                    emailBody
                );

                var emailSent = await emailService.SendEmailAsync(emailModel);

                if (emailSent)
                {
                    _logger.LogInformation("Đã gửi email quá hạn cho reviewer {ReviewerEmail}, assignment {AssignmentId}",
                        assignment.Reviewer.Email, assignment.Id);
                }
                else
                {
                    _logger.LogWarning("Không thể gửi email quá hạn cho reviewer {ReviewerEmail}, assignment {AssignmentId}",
                        assignment.Reviewer.Email, assignment.Id);
                }
            }

            _logger.LogInformation("Đã gửi thông báo quá hạn cho reviewer {ReviewerId}, assignment {AssignmentId}",
                assignment.ReviewerId, assignment.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi gửi thông báo quá hạn cho assignment {AssignmentId}", assignment.Id);
        }
    }

    private string CreateUpcomingDeadlineEmailBody(string reviewerName, string topicTitle, DateTime deadline, int daysUntilDeadline)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Thông báo sắp đến hạn review</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;"">
        <h2 style=""color: #f39c12; text-align: center;"">⚠️ THÔNG BÁO SẮP ĐẾN HẠN REVIEW</h2>
        
        <p>Xin chào <strong>{reviewerName}</strong>,</p>
        
        <div style=""background-color: #fff3cd; padding: 15px; border-left: 4px solid #f39c12; margin: 20px 0;"">
            <p><strong>Đề tài:</strong> {topicTitle}</p>
            <p><strong>Deadline:</strong> {deadline:dd/MM/yyyy HH:mm}</p>
            <p><strong>Thời gian còn lại:</strong> <span style=""color: #f39c12; font-weight: bold;"">{daysUntilDeadline} ngày</span></p>
        </div>
        
        <p>Assignment của bạn sắp đến hạn. Vui lòng hoàn thành review trước thời hạn để đảm bảo tiến độ chung của dự án.</p>
        
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""#"" style=""background-color: #f39c12; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">
                Vào hệ thống review
            </a>
        </div>
        
        <p style=""font-size: 12px; color: #666; margin-top: 30px; border-top: 1px solid #eee; padding-top: 15px;"">
            Đây là email tự động từ hệ thống CapBot. Vui lòng không reply email này.<br>
            Nếu có thắc mắc, vui lòng liên hệ quản trị viên.
        </p>
    </div>
</body>
</html>";
    }

    private string CreateOverdueEmailBody(string reviewerName, string topicTitle, DateTime deadline, int daysOverdue)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Thông báo quá hạn review</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333;"">
    <div style=""max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;"">
        <h2 style=""color: #e74c3c; text-align: center;"">🚨 THÔNG BÁO QUÁ HẠN REVIEW</h2>
        
        <p>Xin chào <strong>{reviewerName}</strong>,</p>
        
        <div style=""background-color: #f8d7da; padding: 15px; border-left: 4px solid #e74c3c; margin: 20px 0;"">
            <p><strong>Đề tài:</strong> {topicTitle}</p>
            <p><strong>Deadline:</strong> {deadline:dd/MM/yyyy HH:mm}</p>
            <p><strong>Đã quá hạn:</strong> <span style=""color: #e74c3c; font-weight: bold;"">{daysOverdue} ngày</span></p>
        </div>
        
        <p style=""color: #e74c3c; font-weight: bold;"">Assignment của bạn đã quá hạn {daysOverdue} ngày!</p>
        
        <p>Vui lòng liên hệ ngay với quản trị viên hoặc moderator để được hỗ trợ và xử lý tình huống này.</p>
        
        <div style=""text-align: center; margin: 30px 0;"">
            <a href=""#"" style=""background-color: #e74c3c; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; font-weight: bold;"">
                Liên hệ quản trị viên
            </a>
        </div>
        
        <p style=""font-size: 12px; color: #666; margin-top: 30px; border-top: 1px solid #eee; padding-top: 15px;"">
            Đây là email tự động từ hệ thống CapBot. Vui lòng không reply email này.<br>
            Nếu có thắc mắc, vui lòng liên hệ quản trị viên ngay lập tức.
        </p>
    </div>
</body>
</html>";
    }
}
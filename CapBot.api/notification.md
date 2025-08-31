## Hướng dẫn sử dụng Notification (FE + BE)

### Mục tiêu

- Frontend: kết nối SignalR để nhận thông báo realtime, gọi REST API để lấy danh sách/đánh dấu đọc.
- Backend: dùng `INotificationService` để bắn noti ở bất kỳ service nào (đã tích hợp mẫu ở `TopicService` và `SubmissionService`).

---

## Kiến trúc tổng quan

- Bảng dữ liệu: `system_notifications` (entity `SystemNotification`), index theo `UserId`, `IsRead`, `CreatedAt`.
- Service nghiệp vụ: gọi `INotificationService` để:
  - Lưu thông báo vào DB.
  - Broadcast realtime qua `INotificationBroadcaster` (SignalR).
- Realtime: Hub `NotificationHub` map tại `/hubs/notifications`, group theo `user:{userId}`.
- Sự kiện client lắng nghe:
  - `notification` → payload: `NotificationResponseDTO`
  - `notificationUnreadCount` → payload: number
  - `notificationMarkedAsRead` → payload: notificationId
  - `notificationAllMarkedAsRead` → payload: {}
  - `notificationBulkCreated` → payload: {}

Lưu ý: Default `IsRead` phải là `false` trong EF (`MyDbContext`) để thông báo mới là “chưa đọc”.

---

## Dành cho Frontend

### 1) Kết nối SignalR (TypeScript/JavaScript)

Cài đặt:

```bash
npm i @microsoft/signalr
```

Notification method: NotificationMarkedAsRead, NotificationBulkCreated, NotificationUnreadCount

Khởi tạo kết nối (có JWT):

```ts
import * as signalR from "@microsoft/signalr";

const tokenProvider = () => localStorage.getItem("access_token") ?? "";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications", {
    accessTokenFactory: tokenProvider, // Backend đọc claims -> group theo user
    transport: signalR.HttpTransportType.WebSockets, // ưu tiên WS
    skipNegotiation: true,
  })
  .withAutomaticReconnect([0, 2000, 5000, 10000])
  .configureLogging(signalR.LogLevel.Information)
  .build();

// Handlers
connection.on("notification", (noti) => {
  // noti: NotificationResponseDTO
  console.log("notification", noti);
});

connection.on("notificationUnreadCount", (count) => {
  console.log("unreadCount", count);
});

connection.on("notificationMarkedAsRead", (notificationId) => {
  console.log("markedAsRead", notificationId);
});

connection.on("notificationAllMarkedAsRead", () => {
  console.log("allMarkedAsRead");
});

connection.on("notificationBulkCreated", () => {
  console.log("bulkCreated");
});

// Start
async function start() {
  try {
    await connection.start();
    console.log("SignalR connected");
  } catch (err) {
    console.error("SignalR connect failed", err);
    setTimeout(start, 3000);
  }
}
start();
```

### 2) REST API hỗ trợ

- Lấy danh sách thông báo của tôi (paging + filter):
  - GET `api/notifications?PageNumber=1&PageSize=10&Keyword=&IsRead=&Type=&From=&To=`
  - Params:
    - `PageNumber`, `PageSize`
    - `Keyword` (search `Title`/`Message`)
    - `IsRead` (true/false)
    - `Type` (1-Info, 2-Warning, 3-Error, 4-Success)
    - `From`/`To` (ISO date)
- Đếm số chưa đọc:
  - GET `api/notifications/unread-count`
- Đánh dấu đã đọc 1 thông báo:
  - PUT `api/notifications/{id}/read`
- Đánh dấu đã đọc tất cả:
  - PUT `api/notifications/read-all`
- (Admin) Tạo thông báo:
  - POST `api/notifications`
- (Admin) Tạo hàng loạt:
  - POST `api/notifications/bulk`

Ví dụ gọi bằng fetch:

```ts
const authHeaders = () => ({
  Authorization: `Bearer ${localStorage.getItem("access_token")}`,
  "Content-Type": "application/json",
});

// List
fetch("/api/notifications?PageNumber=1&PageSize=10", { headers: authHeaders() })
  .then((r) => r.json())
  .then(console.log);

// Unread count
fetch("/api/notifications/unread-count", { headers: authHeaders() })
  .then((r) => r.json())
  .then(console.log);

// Mark as read
fetch("/api/notifications/123/read", { method: "PUT", headers: authHeaders() });

// Mark all as read
fetch("/api/notifications/read-all", { method: "PUT", headers: authHeaders() });
```

### 3) Payload nhận được (NotificationResponseDTO)

```json
{
  "id": 123,
  "userId": 45,
  "title": "Tiêu đề",
  "message": "Nội dung",
  "type": 1,
  "relatedEntityType": "Submission",
  "relatedEntityId": 678,
  "isRead": false,
  "createdAt": "2025-08-31T00:00:00",
  "readAt": null
}
```

### 4) Lưu ý FE

- 401 khi connect hub: kiểm tra header JWT (accessTokenFactory), CORS, map hub đúng `/hubs/notifications`.
- Badge đếm: lắng nghe `notificationUnreadCount` để cập nhật ngay.
- Reconnect: đã bật `withAutomaticReconnect`, nên đăng ký handler trước `start()`.

---

## Dành cho Backend

### 1) Đăng ký DI + Hub

- Đã đăng ký trong `CapBot.api/ServiceConfiguration/ServiceConfig.cs`:
  - `AddSignalR()`
  - `AddScoped<INotificationBroadcaster, SignalRNotificationBroadcaster>()`
  - `AddScoped<INotificationService, NotificationService>()`
- Đảm bảo map Hub trong `CapBot.api/Program.cs`:

```csharp
app.MapHub<CapBot.api.Hubs.NotificationHub>("/hubs/notifications");
```

### 2) Cách sử dụng `INotificationService`

Inject vào service của bạn:

```csharp
private readonly INotificationService _notificationService;

public SomeService(..., INotificationService notificationService)
{
    _notificationService = notificationService;
}
```

Tạo thông báo 1 người dùng:

```csharp
await _notificationService.CreateAsync(new CreateNotificationDTO {
    UserId = targetUserId,
    Title = "Tiêu đề",
    Message = "Nội dung",
    Type = NotificationTypes.Info,
    RelatedEntityType = "Submission",
    RelatedEntityId = submissionId
});
```

Tạo hàng loạt (ví dụ cho Moderator):

```csharp
var moderators = await _identityRepository.GetUsersInRoleAsync(SystemRoleConstants.Moderator);
var moderatorIds = moderators.Select(m => (int)m.Id).Distinct().ToList();

if (moderatorIds.Count > 0)
{
    await _notificationService.CreateBulkAsync(new CreateBulkNotificationsDTO {
        UserIds = moderatorIds,
        Title = "Submission mới cần xử lý",
        Message = $"Supervisor {user.UserName} vừa submit submission #{submission.Id}",
        Type = NotificationTypes.Info,
        RelatedEntityType = "Submission",
        RelatedEntityId = submission.Id
    });
}
```

Đếm số chưa đọc (nếu cần dùng nội bộ):

```csharp
var unread = await _notificationService.CountUnreadAsync(userId);
// Data là List<int> với phần tử đầu tiên là count (theo kiểu trả về hiện tại)
```

Đánh dấu đã đọc/tất cả (nên để REST API làm; service có sẵn nếu cần dùng nội bộ):

```csharp
await _notificationService.MarkAsReadAsync(userId, notificationId);
await _notificationService.MarkAllAsReadAsync(userId);
```

Khuyến nghị:

- Gọi tạo thông báo sau khi `CommitTransactionAsync()` để đảm bảo entity liên quan đã tồn tại.
- Ghi `RelatedEntityType/Id` để FE có thể deep-link.

### 3) Điểm gợi ý gắn Notification

- Sau khi Supervisor tạo Topic mới (`TopicService.CreateTopic`) → notify Moderator (đã tích hợp).
- Sau khi Supervisor `Submit`/`Resubmit` Submission (`SubmissionService.SubmitSubmission`, `ResubmitSubmission`) → notify Moderator (đã tích hợp).
- Khi phân công reviewer (`ReviewerAssignmentService`) → notify reviewer.
- Khi moderator/administrator phê duyệt/ra quyết định → notify Supervisor/Student phù hợp.

---

## Kiểm thử nhanh

1. FE:

- Đăng nhập lấy JWT, lưu vào `localStorage`.
- Kết nối tới `/hubs/notifications`, lắng nghe events.
- Mở trang badge đếm, xác nhận nhận `notificationUnreadCount`.

2. BE:

- Tạo Topic (role Supervisor) → Moderator nhận `notification`.
- Submit/Resubmit Submission → Moderator nhận `notification`.
- Gọi GET `api/notifications` → thấy record đúng user.
- PUT `api/notifications/{id}/read` → FE nhận `notificationUnreadCount` giảm.

3. Swagger/Postman:

- Dùng `api/notifications` endpoints để kiểm tra list/mark read.

---

## Troubleshooting

- 401 khi connect Hub: thiếu JWT/expired; kiểm tra `accessTokenFactory`.
- Không nhận realtime:
  - Chưa `app.MapHub("/hubs/notifications")`
  - CORS/WebSocket bị chặn (proxy/nginx).
  - User không có claim `NameIdentifier` hợp lệ (Hub nhóm theo `user:{id}`).
- Badge không đúng:
  - Kiểm tra default `IsRead` trong EF: cần `HasDefaultValue(false)`.
  - Kiểm tra event `notificationUnreadCount` có tới client.

---

## Phụ lục

- Hub: `CapBot.api/Hubs/NotificationHub.cs`
- Service: `App.BLL/Implementations/NotificationService.cs`
- Interface: `App.BLL/Interfaces/INotificationService.cs`
- Broadcaster: `App.Commons/Interfaces/INotificationBroadcaster.cs` + `CapBot.api/Services/SignalRNotificationBroadcaster.cs`
- DTOs:
  - `CreateNotificationDTO`
  - `CreateBulkNotificationsDTO`
  - `GetNotificationsQueryDTO`
  - `NotificationResponseDTO`
- Enum: `NotificationTypes` (1 Info, 2 Warning, 3 Error, 4 Success)
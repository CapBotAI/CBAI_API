## User Profile API Guide

### Tổng quan

- **Auth**: Tất cả endpoints yêu cầu Bearer JWT.
- **Phân quyền**:
  - Non-admin: chỉ được thao tác hồ sơ của chính mình.
  - Admin (claim Role = "Administrator"): được thao tác hồ sơ của bất kỳ `UserId` nào.
- **Quy tắc dữ liệu**:
  - Mỗi `UserId` chỉ có tối đa 1 hồ sơ đang hoạt động (IsActive = true, DeletedAt = null).
  - Xóa là soft delete: `IsActive=false`, set `DeletedAt`.

### Base URL

- Mặc định: `https://{host}/api/user-profiles`

### Response Envelope

- Tất cả endpoints trả về dạng `FSResponse`:
  - `statusCode`: HTTP code (ví dụ 200, 201, 403, 404, 409, 500)
  - `success`: bool
  - `message`: string
  - `data`: dữ liệu (nếu có)

Ví dụ:

```json
{
  "statusCode": 200,
  "success": true,
  "message": "Cập nhật hồ sơ thành công",
  "data": {
    "id": 12,
    "userId": 34,
    "fullName": "John Doe",
    "address": "HCM",
    "avatar": "https://...",
    "coverImage": "https://...",
    "createdAt": "2025-08-31T10:00:00Z",
    "createdBy": "john",
    "lastModifiedAt": "2025-08-31T11:11:11Z",
    "lastModifiedBy": "john"
  }
}
```

### Schemas

- CreateUserProfileDTO (POST body)

```json
{
  "userId": 0, // optional; admin có thể truyền để tạo cho user khác; non-admin bỏ qua hoặc trùng với chính mình
  "fullName": "string?", // <= 255 ký tự
  "address": "string?", // <= 512 ký tự
  "avatar": "string?", // <= 1024 ký tự (URL)
  "coverImage": "string?" // <= 1024 ký tự (URL)
}
```

- UpdateUserProfileDTO (PUT body)

```json
{
  "id": 0, // bắt buộc
  "fullName": "string?",
  "address": "string?",
  "avatar": "string?",
  "coverImage": "string?"
}
```

- UserProfileResponseDTO (data)

```json
{
  "id": 0,
  "userId": 0,
  "fullName": "string?",
  "address": "string?",
  "avatar": "string?",
  "coverImage": "string?",
  "createdAt": "string? (ISO)",
  "createdBy": "string?",
  "lastModifiedAt": "string? (ISO)",
  "lastModifiedBy": "string?"
}
```

### Endpoints

#### 1) Tạo hồ sơ

- POST `/api/user-profiles`
- Quyền:
  - Non-admin: tạo cho chính mình (bỏ `userId` hoặc `userId` phải trùng `UserId` của token).
  - Admin: có thể truyền `userId` bất kỳ hợp lệ.
- Trả về:
  - 201 Created: thành công
  - 403 Forbidden: không có quyền tạo cho user khác
  - 409 Conflict: hồ sơ đã tồn tại
  - 404 Not Found: user trong token không tồn tại
  - 500: lỗi hệ thống

cURL:

```bash
curl -X POST https://{host}/api/user-profiles \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName":"John Doe",
    "address":"HCM",
    "avatar":"https://...",
    "coverImage":"https://..."
  }'
```

fetch:

```js
await fetch("/api/user-profiles", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  },
  body: JSON.stringify({ fullName, address, avatar, coverImage }),
});
```

#### 2) Cập nhật hồ sơ

- PUT `/api/user-profiles`
- Quyền:
  - Non-admin: chỉ cập nhật hồ sơ có `userId` trùng `UserId` của token.
  - Admin: cập nhật bất kỳ hồ sơ.
- Trả về:
  - 200 OK: thành công
  - 403 Forbidden: không có quyền
  - 404 Not Found: không tìm thấy hồ sơ
  - 500: lỗi hệ thống

cURL:

```bash
curl -X PUT https://{host}/api/user-profiles \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "id": 12,
    "fullName":"John Updated",
    "address":"HN",
    "avatar":"https://new...",
    "coverImage":"https://new..."
  }'
```

#### 3) Xóa hồ sơ (soft delete)

- DELETE `/api/user-profiles/{id}`
- Quyền:
  - Non-admin: chỉ xóa hồ sơ của chính mình.
  - Admin: xóa bất kỳ.
- Trả về:
  - 200 OK: thành công
  - 403 Forbidden
  - 404 Not Found
  - 500: lỗi hệ thống

cURL:

```bash
curl -X DELETE https://{host}/api/user-profiles/12 \
  -H "Authorization: Bearer {token}"
```

#### 4) Lấy hồ sơ theo Id

- GET `/api/user-profiles/{id}`
- Trả về:
  - 200 OK + `UserProfileResponseDTO`
  - 404 Not Found
  - 500: lỗi hệ thống

#### 5) Lấy hồ sơ theo UserId

- GET `/api/user-profiles/by-user/{userId}`
- Trả về:
  - 200 OK + `UserProfileResponseDTO`
  - 404 Not Found
  - 500: lỗi hệ thống

#### 6) Lấy hồ sơ của chính mình

- GET `/api/user-profiles/me`
- Trả về:
  - 200 OK + `UserProfileResponseDTO`
  - 404 Not Found (chưa có hồ sơ)
  - 500: lỗi hệ thống

### Luồng gợi ý cho FE

- On login/profile page:
  1. Gọi `GET /api/user-profiles/me`.
  2. Nếu 200: hiển thị dữ liệu.
  3. Nếu 404: cho phép người dùng tạo mới với `POST /api/user-profiles`.
- Khi lưu form:
  - Nếu đã có `id`: dùng `PUT /api/user-profiles`.
  - Nếu chưa có: dùng `POST /api/user-profiles`.

### Lưu ý tích hợp

- Bổ sung header `Authorization: Bearer {token}` cho mọi request.
- Kiểm tra cả HTTP status code và `FSResponse.success/message` để hiển thị thông báo phù hợp.
- Ràng buộc độ dài trường theo mô tả trong DTO (255/512/1024).
- Non-admin không cần (và không nên) gửi `userId` trong POST; backend sẽ mặc định lấy từ token.

### Ví dụ xử lý FE (Axios)

```js
import axios from "axios";

const api = axios.create({
  baseURL: "/api",
  headers: { Authorization: `Bearer ${token}` },
});

// Get my profile
const { data: res } = await api.get("/user-profiles/me");
if (res.statusCode === 200 && res.success) {
  // res.data là UserProfileResponseDTO
} else if (res.statusCode === 404) {
  // show create form
}

// Create
await api.post("/user-profiles", { fullName, address, avatar, coverImage });

// Update
await api.put("/user-profiles", { id, fullName, address, avatar, coverImage });

// Delete
await api.delete(`/user-profiles/${id}`);
```

### Ghi chú kỹ thuật

- Admin được nhận diện qua claim `Role = "Administrator"` trong token.
- Truy vấn lấy hồ sơ luôn lọc `IsActive = true` và `DeletedAt = null`.
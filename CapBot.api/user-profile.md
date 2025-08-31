## User Profile API Guide

### T?ng quan

- **Auth**: T?t c? endpoints yêu c?u Bearer JWT.
- **Phân quy?n**:
  - Non-admin: ch? ???c thao tác h? s? c?a chính mình.
  - Admin (claim Role = "Administrator"): ???c thao tác h? s? c?a b?t k? `UserId` nào.
- **Quy t?c d? li?u**:
  - M?i `UserId` ch? có t?i ?a 1 h? s? ?ang ho?t ??ng (IsActive = true, DeletedAt = null).
  - Xóa là soft delete: `IsActive=false`, set `DeletedAt`.

### Base URL

- M?c ??nh: `https://{host}/api/user-profiles`

### Response Envelope

- T?t c? endpoints tr? v? d?ng `FSResponse`:
  - `statusCode`: HTTP code (ví d? 200, 201, 403, 404, 409, 500)
  - `success`: bool
  - `message`: string
  - `data`: d? li?u (n?u có)

Ví d?:

```json
{
  "statusCode": 200,
  "success": true,
  "message": "C?p nh?t h? s? thành công",
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
  "userId": 0, // optional; admin có th? truy?n ?? t?o cho user khác; non-admin b? qua ho?c trùng v?i chính mình
  "fullName": "string?", // <= 255 ký t?
  "address": "string?", // <= 512 ký t?
  "avatar": "string?", // <= 1024 ký t? (URL)
  "coverImage": "string?" // <= 1024 ký t? (URL)
}
```

- UpdateUserProfileDTO (PUT body)

```json
{
  "id": 0, // b?t bu?c
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

#### 1) T?o h? s?

- POST `/api/user-profiles`
- Quy?n:
  - Non-admin: t?o cho chính mình (b? `userId` ho?c `userId` ph?i trùng `UserId` c?a token).
  - Admin: có th? truy?n `userId` b?t k? h?p l?.
- Tr? v?:
  - 201 Created: thành công
  - 403 Forbidden: không có quy?n t?o cho user khác
  - 409 Conflict: h? s? ?ã t?n t?i
  - 404 Not Found: user trong token không t?n t?i
  - 500: l?i h? th?ng

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

#### 2) C?p nh?t h? s?

- PUT `/api/user-profiles`
- Quy?n:
  - Non-admin: ch? c?p nh?t h? s? có `userId` trùng `UserId` c?a token.
  - Admin: c?p nh?t b?t k? h? s?.
- Tr? v?:
  - 200 OK: thành công
  - 403 Forbidden: không có quy?n
  - 404 Not Found: không tìm th?y h? s?
  - 500: l?i h? th?ng

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

#### 3) Xóa h? s? (soft delete)

- DELETE `/api/user-profiles/{id}`
- Quy?n:
  - Non-admin: ch? xóa h? s? c?a chính mình.
  - Admin: xóa b?t k?.
- Tr? v?:
  - 200 OK: thành công
  - 403 Forbidden
  - 404 Not Found
  - 500: l?i h? th?ng

cURL:

```bash
curl -X DELETE https://{host}/api/user-profiles/12 \
  -H "Authorization: Bearer {token}"
```

#### 4) L?y h? s? theo Id

- GET `/api/user-profiles/{id}`
- Tr? v?:
  - 200 OK + `UserProfileResponseDTO`
  - 404 Not Found
  - 500: l?i h? th?ng

#### 5) L?y h? s? theo UserId

- GET `/api/user-profiles/by-user/{userId}`
- Tr? v?:
  - 200 OK + `UserProfileResponseDTO`
  - 404 Not Found
  - 500: l?i h? th?ng

#### 6) L?y h? s? c?a chính mình

- GET `/api/user-profiles/me`
- Tr? v?:
  - 200 OK + `UserProfileResponseDTO`
  - 404 Not Found (ch?a có h? s?)
  - 500: l?i h? th?ng

### Lu?ng g?i ý cho FE

- On login/profile page:
  1. G?i `GET /api/user-profiles/me`.
  2. N?u 200: hi?n th? d? li?u.
  3. N?u 404: cho phép ng??i dùng t?o m?i v?i `POST /api/user-profiles`.
- Khi l?u form:
  - N?u ?ã có `id`: dùng `PUT /api/user-profiles`.
  - N?u ch?a có: dùng `POST /api/user-profiles`.

### L?u ý tích h?p

- B? sung header `Authorization: Bearer {token}` cho m?i request.
- Ki?m tra c? HTTP status code và `FSResponse.success/message` ?? hi?n th? thông báo phù h?p.
- Ràng bu?c ?? dài tr??ng theo mô t? trong DTO (255/512/1024).
- Non-admin không c?n (và không nên) g?i `userId` trong POST; backend s? m?c ??nh l?y t? token.

### Ví d? x? lý FE (Axios)

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

### Ghi chú k? thu?t

- Admin ???c nh?n di?n qua claim `Role = "Administrator"` trong token.
- Truy v?n l?y h? s? luôn l?c `IsActive = true` và `DeletedAt = null`.
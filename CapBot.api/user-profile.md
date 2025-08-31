## User Profile API Guide

### T?ng quan

- **Auth**: T?t c? endpoints y�u c?u Bearer JWT.
- **Ph�n quy?n**:
  - Non-admin: ch? ???c thao t�c h? s? c?a ch�nh m�nh.
  - Admin (claim Role = "Administrator"): ???c thao t�c h? s? c?a b?t k? `UserId` n�o.
- **Quy t?c d? li?u**:
  - M?i `UserId` ch? c� t?i ?a 1 h? s? ?ang ho?t ??ng (IsActive = true, DeletedAt = null).
  - X�a l� soft delete: `IsActive=false`, set `DeletedAt`.

### Base URL

- M?c ??nh: `https://{host}/api/user-profiles`

### Response Envelope

- T?t c? endpoints tr? v? d?ng `FSResponse`:
  - `statusCode`: HTTP code (v� d? 200, 201, 403, 404, 409, 500)
  - `success`: bool
  - `message`: string
  - `data`: d? li?u (n?u c�)

V� d?:

```json
{
  "statusCode": 200,
  "success": true,
  "message": "C?p nh?t h? s? th�nh c�ng",
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
  "userId": 0, // optional; admin c� th? truy?n ?? t?o cho user kh�c; non-admin b? qua ho?c tr�ng v?i ch�nh m�nh
  "fullName": "string?", // <= 255 k� t?
  "address": "string?", // <= 512 k� t?
  "avatar": "string?", // <= 1024 k� t? (URL)
  "coverImage": "string?" // <= 1024 k� t? (URL)
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
  - Non-admin: t?o cho ch�nh m�nh (b? `userId` ho?c `userId` ph?i tr�ng `UserId` c?a token).
  - Admin: c� th? truy?n `userId` b?t k? h?p l?.
- Tr? v?:
  - 201 Created: th�nh c�ng
  - 403 Forbidden: kh�ng c� quy?n t?o cho user kh�c
  - 409 Conflict: h? s? ?� t?n t?i
  - 404 Not Found: user trong token kh�ng t?n t?i
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
  - Non-admin: ch? c?p nh?t h? s? c� `userId` tr�ng `UserId` c?a token.
  - Admin: c?p nh?t b?t k? h? s?.
- Tr? v?:
  - 200 OK: th�nh c�ng
  - 403 Forbidden: kh�ng c� quy?n
  - 404 Not Found: kh�ng t�m th?y h? s?
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

#### 3) X�a h? s? (soft delete)

- DELETE `/api/user-profiles/{id}`
- Quy?n:
  - Non-admin: ch? x�a h? s? c?a ch�nh m�nh.
  - Admin: x�a b?t k?.
- Tr? v?:
  - 200 OK: th�nh c�ng
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

#### 6) L?y h? s? c?a ch�nh m�nh

- GET `/api/user-profiles/me`
- Tr? v?:
  - 200 OK + `UserProfileResponseDTO`
  - 404 Not Found (ch?a c� h? s?)
  - 500: l?i h? th?ng

### Lu?ng g?i � cho FE

- On login/profile page:
  1. G?i `GET /api/user-profiles/me`.
  2. N?u 200: hi?n th? d? li?u.
  3. N?u 404: cho ph�p ng??i d�ng t?o m?i v?i `POST /api/user-profiles`.
- Khi l?u form:
  - N?u ?� c� `id`: d�ng `PUT /api/user-profiles`.
  - N?u ch?a c�: d�ng `POST /api/user-profiles`.

### L?u � t�ch h?p

- B? sung header `Authorization: Bearer {token}` cho m?i request.
- Ki?m tra c? HTTP status code v� `FSResponse.success/message` ?? hi?n th? th�ng b�o ph� h?p.
- R�ng bu?c ?? d�i tr??ng theo m� t? trong DTO (255/512/1024).
- Non-admin kh�ng c?n (v� kh�ng n�n) g?i `userId` trong POST; backend s? m?c ??nh l?y t? token.

### V� d? x? l� FE (Axios)

```js
import axios from "axios";

const api = axios.create({
  baseURL: "/api",
  headers: { Authorization: `Bearer ${token}` },
});

// Get my profile
const { data: res } = await api.get("/user-profiles/me");
if (res.statusCode === 200 && res.success) {
  // res.data l� UserProfileResponseDTO
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

### Ghi ch� k? thu?t

- Admin ???c nh?n di?n qua claim `Role = "Administrator"` trong token.
- Truy v?n l?y h? s? lu�n l?c `IsActive = true` v� `DeletedAt = null`.
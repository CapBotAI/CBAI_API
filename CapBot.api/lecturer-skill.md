## API Tài liệu — LecturerSkill

### Tổng quan

- Thực thể: `LecturerSkill` (kỹ năng của giảng viên)
- Quy ước phản hồi: Bọc trong `FSResponse` với các trường: `data`, `statusCode`, `message`, `success`.
- Yêu cầu xác thực: Tất cả endpoints đều cần JWT (`Authorization: Bearer <token>`).
- Phân quyền:
  - Tạo/Cập nhật/Xóa: Admin hoặc chính giảng viên sở hữu skill.
  - Xem theo `lecturerId`: ai cũng gọi được (đã đăng nhập).
  - Lấy kỹ năng của chính mình: dùng `/me`.

### Base URL

- Local (ví dụ): `https://localhost:7190/api/lecturer-skills`

### Model

- `LecturerSkillResponseDTO`

  - `id` (number)
  - `lecturerId` (number)
  - `skillTag` (string)
  - `proficiencyLevel` (number: 1=Beginner, 2=Intermediate, 3=Advanced, 4=Expert)
  - `proficiencyLevelName` (string: "Beginner" | "Intermediate" | "Advanced" | "Expert")
  - `createdAt` (string, ISO datetime)
  - `lastModifiedAt` (string | null, ISO datetime)

- `CreateLecturerSkillDTO`

  - `lecturerId?` (number, optional; Admin có thể chỉ định, giảng viên thường sẽ để trống để mặc định là chính mình)
  - `skillTag` (string, required, max 100)
  - `proficiencyLevel` (number, optional, default 2)

- `UpdateLecturerSkillDTO`

  - `id` (number, required)
  - `skillTag` (string, required, max 100)
  - `proficiencyLevel` (number, required)

- Enum `ProficiencyLevels`
  - 1: Beginner
  - 2: Intermediate
  - 3: Advanced
  - 4: Expert

### Phân trang

- Query params chung: `PageNumber` (default 1), `PageSize` (default 10)
- Response phân trang:

```json
{
  "data": {
    "paging": {
      "pageNumber": 1,
      "pageSize": 10,
      "keyword": null,
      "totalRecord": 25
    },
    "listObjects": [
      {
        "id": 1,
        "lecturerId": 12,
        "skillTag": "AI",
        "proficiencyLevel": 3,
        "proficiencyLevelName": "Advanced",
        "createdAt": "...",
        "lastModifiedAt": "..."
      }
    ]
  },
  "statusCode": 200,
  "message": null,
  "success": true
}
```

---

### 1) Tạo kỹ năng

- Method: POST
- Path: `/api/lecturer-skills`
- Quyền:
  - Admin: tạo cho bất kỳ `lecturerId` hoặc để trống (nếu để trống sẽ gán theo người gọi).
  - Giảng viên: chỉ tạo cho chính mình (bỏ `lecturerId` hoặc `lecturerId` phải bằng `UserId`).
- Body (JSON):

```json
{
  "lecturerId": 12,
  "skillTag": "AI",
  "proficiencyLevel": 3
}
```

- Response (201):

```json
{
  "data": {
    "id": 101,
    "lecturerId": 12,
    "skillTag": "AI",
    "proficiencyLevel": 3,
    "proficiencyLevelName": "Advanced",
    "createdAt": "2025-08-30T07:00:00Z",
    "lastModifiedAt": "2025-08-30T07:00:00Z"
  },
  "statusCode": 201,
  "message": "Tạo kỹ năng thành công",
  "success": true
}
```

- Lỗi thường gặp:
  - 403: tạo cho người khác khi không phải Admin.
  - 409: trùng `(lecturerId, skillTag)`.

Ví dụ cURL:

```bash
curl -X POST "https://localhost:7190/api/lecturer-skills" \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"skillTag":"AI","proficiencyLevel":3}'
```

Ví dụ fetch:

```javascript
await fetch(`/api/lecturer-skills`, {
  method: "POST",
  headers: {
    Authorization: `Bearer ${token}`,
    "Content-Type": "application/json",
  },
  body: JSON.stringify({ skillTag: "AI", proficiencyLevel: 3 }),
});
```

---

### 2) Cập nhật kỹ năng

- Method: PUT
- Path: `/api/lecturer-skills`
- Quyền: Admin hoặc chủ sở hữu skill.
- Body:

```json
{
  "id": 101,
  "skillTag": "AI & ML",
  "proficiencyLevel": 4
}
```

- Response (200):

```json
{
  "data": {
    "id": 101,
    "lecturerId": 12,
    "skillTag": "AI & ML",
    "proficiencyLevel": 4,
    "proficiencyLevelName": "Expert",
    "createdAt": "2025-08-30T07:00:00Z",
    "lastModifiedAt": "2025-08-30T07:05:00Z"
  },
  "statusCode": 200,
  "message": "Cập nhật kỹ năng thành công",
  "success": true
}
```

- Lỗi thường gặp:
  - 404: không tìm thấy `id`.
  - 403: không phải Admin và không phải chủ sở hữu.
  - 409: đổi `skillTag` gây trùng với skill khác của cùng `lecturerId`.

---

### 3) Xóa kỹ năng (soft delete)

- Method: DELETE
- Path: `/api/lecturer-skills/{id}`
- Quyền: Admin hoặc chủ sở hữu skill.
- Response (200):

```json
{
  "data": null,
  "statusCode": 200,
  "message": "Xóa kỹ năng thành công",
  "success": true
}
```

- Lỗi thường gặp:
  - 404, 403.

---

### 4) Lấy kỹ năng theo ID

- Method: GET
- Path: `/api/lecturer-skills/{id}`
- Response (200):

```json
{
  "data": {
    "id": 101,
    "lecturerId": 12,
    "skillTag": "AI",
    "proficiencyLevel": 3,
    "proficiencyLevelName": "Advanced",
    "createdAt": "2025-08-30T07:00:00Z",
    "lastModifiedAt": "2025-08-30T07:00:00Z"
  },
  "statusCode": 200,
  "message": null,
  "success": true
}
```

- Lỗi: 404.

---

### 5) Lấy danh sách kỹ năng theo giảng viên (phân trang)

- Method: GET
- Path: `/api/lecturer-skills`
- Query:
  - `lecturerId` (number, required)
  - `PageNumber`, `PageSize`
- Response: dạng phân trang (xem mục Phân trang).
- Ví dụ:

```bash
curl -G "https://localhost:7190/api/lecturer-skills" \
  -H "Authorization: Bearer <TOKEN>" \
  --data-urlencode "lecturerId=12" \
  --data-urlencode "PageNumber=1" \
  --data-urlencode "PageSize=10"
```

---

### 6) Lấy danh sách kỹ năng của chính mình (phân trang)

- Method: GET
- Path: `/api/lecturer-skills/me`
- Query: `PageNumber`, `PageSize`
- Response: dạng phân trang.

---

### Headers chuẩn

- `Authorization: Bearer <token>`
- `Content-Type: application/json` (với POST/PUT)

### Mẫu xử lý response (frontend)

```typescript
type FSResponse<T> = {
  data: T | null;
  statusCode: number;
  message: string | null;
  success: boolean;
};

async function api<T>(input: RequestInfo, init?: RequestInit): Promise<T> {
  const res = await fetch(input, init);
  const payload = (await res.json()) as FSResponse<T>;
  if (!payload.success) {
    throw new Error(payload.message ?? "Có lỗi xảy ra");
  }
  return payload.data as T;
}
```

### Gợi ý UI/Validation

- Chặn tạo/cập nhật nếu `skillTag` rỗng hoặc quá 100 ký tự.
- Map enum `proficiencyLevel` => label theo `proficiencyLevelName`.
- Bắt lỗi 409 để hiển thị “Kỹ năng đã tồn tại”.
- Phân trang: đọc `data.paging.totalRecord` để hiển thị tổng và điều khiển trang.

### Mã lỗi thường gặp

- 400: body không hợp lệ (thiếu trường, sai format).
- 401: thiếu/invalid token.
- 403: không đủ quyền (không phải Admin/Owner).
- 404: không tìm thấy resource.
- 409: xung đột dữ liệu (trùng `(lecturerId, skillTag)`).
- 500: lỗi hệ thống.
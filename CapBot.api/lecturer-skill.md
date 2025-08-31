## API Tài li?u — LecturerSkill

### T?ng quan

- Th?c th?: `LecturerSkill` (k? n?ng c?a gi?ng viên)
- Quy ??c ph?n h?i: B?c trong `FSResponse` v?i các tr??ng: `data`, `statusCode`, `message`, `success`.
- Yêu c?u xác th?c: T?t c? endpoints ??u c?n JWT (`Authorization: Bearer <token>`).
- Phân quy?n:
  - T?o/C?p nh?t/Xóa: Admin ho?c chính gi?ng viên s? h?u skill.
  - Xem theo `lecturerId`: ai c?ng g?i ???c (?ã ??ng nh?p).
  - L?y k? n?ng c?a chính mình: dùng `/me`.

### Base URL

- Local (ví d?): `https://localhost:7190/api/lecturer-skills`

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

  - `lecturerId?` (number, optional; Admin có th? ch? ??nh, gi?ng viên th??ng s? ?? tr?ng ?? m?c ??nh là chính mình)
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

### 1) T?o k? n?ng

- Method: POST
- Path: `/api/lecturer-skills`
- Quy?n:
  - Admin: t?o cho b?t k? `lecturerId` ho?c ?? tr?ng (n?u ?? tr?ng s? gán theo ng??i g?i).
  - Gi?ng viên: ch? t?o cho chính mình (b? `lecturerId` ho?c `lecturerId` ph?i b?ng `UserId`).
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
  "message": "T?o k? n?ng thành công",
  "success": true
}
```

- L?i th??ng g?p:
  - 403: t?o cho ng??i khác khi không ph?i Admin.
  - 409: trùng `(lecturerId, skillTag)`.

Ví d? cURL:

```bash
curl -X POST "https://localhost:7190/api/lecturer-skills" \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"skillTag":"AI","proficiencyLevel":3}'
```

Ví d? fetch:

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

### 2) C?p nh?t k? n?ng

- Method: PUT
- Path: `/api/lecturer-skills`
- Quy?n: Admin ho?c ch? s? h?u skill.
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
  "message": "C?p nh?t k? n?ng thành công",
  "success": true
}
```

- L?i th??ng g?p:
  - 404: không tìm th?y `id`.
  - 403: không ph?i Admin và không ph?i ch? s? h?u.
  - 409: ??i `skillTag` gây trùng v?i skill khác c?a cùng `lecturerId`.

---

### 3) Xóa k? n?ng (soft delete)

- Method: DELETE
- Path: `/api/lecturer-skills/{id}`
- Quy?n: Admin ho?c ch? s? h?u skill.
- Response (200):

```json
{
  "data": null,
  "statusCode": 200,
  "message": "Xóa k? n?ng thành công",
  "success": true
}
```

- L?i th??ng g?p:
  - 404, 403.

---

### 4) L?y k? n?ng theo ID

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

- L?i: 404.

---

### 5) L?y danh sách k? n?ng theo gi?ng viên (phân trang)

- Method: GET
- Path: `/api/lecturer-skills`
- Query:
  - `lecturerId` (number, required)
  - `PageNumber`, `PageSize`
- Response: d?ng phân trang (xem m?c Phân trang).
- Ví d?:

```bash
curl -G "https://localhost:7190/api/lecturer-skills" \
  -H "Authorization: Bearer <TOKEN>" \
  --data-urlencode "lecturerId=12" \
  --data-urlencode "PageNumber=1" \
  --data-urlencode "PageSize=10"
```

---

### 6) L?y danh sách k? n?ng c?a chính mình (phân trang)

- Method: GET
- Path: `/api/lecturer-skills/me`
- Query: `PageNumber`, `PageSize`
- Response: d?ng phân trang.

---

### Headers chu?n

- `Authorization: Bearer <token>`
- `Content-Type: application/json` (v?i POST/PUT)

### M?u x? lý response (frontend)

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
    throw new Error(payload.message ?? "Có l?i x?y ra");
  }
  return payload.data as T;
}
```

### G?i ý UI/Validation

- Ch?n t?o/c?p nh?t n?u `skillTag` r?ng ho?c quá 100 ký t?.
- Map enum `proficiencyLevel` => label theo `proficiencyLevelName`.
- B?t l?i 409 ?? hi?n th? “K? n?ng ?ã t?n t?i”.
- Phân trang: ??c `data.paging.totalRecord` ?? hi?n th? t?ng và ?i?u khi?n trang.

### Mã l?i th??ng g?p

- 400: body không h?p l? (thi?u tr??ng, sai format).
- 401: thi?u/invalid token.
- 403: không ?? quy?n (không ph?i Admin/Owner).
- 404: không tìm th?y resource.
- 409: xung ??t d? li?u (trùng `(lecturerId, skillTag)`).
- 500: l?i h? th?ng.
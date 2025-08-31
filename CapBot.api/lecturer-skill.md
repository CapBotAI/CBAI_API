## API T�i li?u � LecturerSkill

### T?ng quan

- Th?c th?: `LecturerSkill` (k? n?ng c?a gi?ng vi�n)
- Quy ??c ph?n h?i: B?c trong `FSResponse` v?i c�c tr??ng: `data`, `statusCode`, `message`, `success`.
- Y�u c?u x�c th?c: T?t c? endpoints ??u c?n JWT (`Authorization: Bearer <token>`).
- Ph�n quy?n:
  - T?o/C?p nh?t/X�a: Admin ho?c ch�nh gi?ng vi�n s? h?u skill.
  - Xem theo `lecturerId`: ai c?ng g?i ???c (?� ??ng nh?p).
  - L?y k? n?ng c?a ch�nh m�nh: d�ng `/me`.

### Base URL

- Local (v� d?): `https://localhost:7190/api/lecturer-skills`

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

  - `lecturerId?` (number, optional; Admin c� th? ch? ??nh, gi?ng vi�n th??ng s? ?? tr?ng ?? m?c ??nh l� ch�nh m�nh)
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

### Ph�n trang

- Query params chung: `PageNumber` (default 1), `PageSize` (default 10)
- Response ph�n trang:

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
  - Admin: t?o cho b?t k? `lecturerId` ho?c ?? tr?ng (n?u ?? tr?ng s? g�n theo ng??i g?i).
  - Gi?ng vi�n: ch? t?o cho ch�nh m�nh (b? `lecturerId` ho?c `lecturerId` ph?i b?ng `UserId`).
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
  "message": "T?o k? n?ng th�nh c�ng",
  "success": true
}
```

- L?i th??ng g?p:
  - 403: t?o cho ng??i kh�c khi kh�ng ph?i Admin.
  - 409: tr�ng `(lecturerId, skillTag)`.

V� d? cURL:

```bash
curl -X POST "https://localhost:7190/api/lecturer-skills" \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"skillTag":"AI","proficiencyLevel":3}'
```

V� d? fetch:

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
  "message": "C?p nh?t k? n?ng th�nh c�ng",
  "success": true
}
```

- L?i th??ng g?p:
  - 404: kh�ng t�m th?y `id`.
  - 403: kh�ng ph?i Admin v� kh�ng ph?i ch? s? h?u.
  - 409: ??i `skillTag` g�y tr�ng v?i skill kh�c c?a c�ng `lecturerId`.

---

### 3) X�a k? n?ng (soft delete)

- Method: DELETE
- Path: `/api/lecturer-skills/{id}`
- Quy?n: Admin ho?c ch? s? h?u skill.
- Response (200):

```json
{
  "data": null,
  "statusCode": 200,
  "message": "X�a k? n?ng th�nh c�ng",
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

### 5) L?y danh s�ch k? n?ng theo gi?ng vi�n (ph�n trang)

- Method: GET
- Path: `/api/lecturer-skills`
- Query:
  - `lecturerId` (number, required)
  - `PageNumber`, `PageSize`
- Response: d?ng ph�n trang (xem m?c Ph�n trang).
- V� d?:

```bash
curl -G "https://localhost:7190/api/lecturer-skills" \
  -H "Authorization: Bearer <TOKEN>" \
  --data-urlencode "lecturerId=12" \
  --data-urlencode "PageNumber=1" \
  --data-urlencode "PageSize=10"
```

---

### 6) L?y danh s�ch k? n?ng c?a ch�nh m�nh (ph�n trang)

- Method: GET
- Path: `/api/lecturer-skills/me`
- Query: `PageNumber`, `PageSize`
- Response: d?ng ph�n trang.

---

### Headers chu?n

- `Authorization: Bearer <token>`
- `Content-Type: application/json` (v?i POST/PUT)

### M?u x? l� response (frontend)

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
    throw new Error(payload.message ?? "C� l?i x?y ra");
  }
  return payload.data as T;
}
```

### G?i � UI/Validation

- Ch?n t?o/c?p nh?t n?u `skillTag` r?ng ho?c qu� 100 k� t?.
- Map enum `proficiencyLevel` => label theo `proficiencyLevelName`.
- B?t l?i 409 ?? hi?n th? �K? n?ng ?� t?n t?i�.
- Ph�n trang: ??c `data.paging.totalRecord` ?? hi?n th? t?ng v� ?i?u khi?n trang.

### M� l?i th??ng g?p

- 400: body kh�ng h?p l? (thi?u tr??ng, sai format).
- 401: thi?u/invalid token.
- 403: kh�ng ?? quy?n (kh�ng ph?i Admin/Owner).
- 404: kh�ng t�m th?y resource.
- 409: xung ??t d? li?u (tr�ng `(lecturerId, skillTag)`).
- 500: l?i h? th?ng.
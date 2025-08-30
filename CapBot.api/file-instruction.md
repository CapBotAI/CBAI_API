## Hướng dẫn sử dụng API File và tích hợp với Topic/TopicVersion/Submission

### Tổng quan

- Hệ thống dùng một model thống nhất `AppFile` để lưu metadata cho cả ảnh và file.
- Liên kết file với thực thể (Topic, TopicVersion, Submission, ...) qua bảng đa hình `entity_files` với `EntityType` và `EntityId`.
- Frontend quy trình chuẩn:
  1. Upload file/ảnh → nhận `FileId`, `Url`, `ThumbnailUrl`.
  2. Gọi API domain (create/update) kèm `FileId` để backend liên kết file vào entity.

---

## 1) Upload file/ảnh

### 1.1) Upload ảnh

- Endpoint: `POST /api/file/upload-image`
- Auth: Bearer token (bắt buộc)
- Content-Type: `multipart/form-data`
- Body:
  - `file`: File ảnh hợp lệ (.jpg, .jpeg, .png, .svg, .dng, .webp)
- Response mẫu:

```json
{
  "success": true,
  "fileId": 123,
  "url": "https://cdn.example.com/images/entities/abc.jpg",
  "thumbnailUrl": "https://cdn.example.com/images/entities/abc_thumb.jpg"
}
```

- Ghi chú:
  - Ảnh non-SVG sẽ được tạo `thumbnail` kích thước max-width 150px.
  - Giới hạn kích thước: theo `AppSettings:FileSizeLimit` (mặc định 20MB).

### 1.2) Upload file (tổng quát)

- Endpoint: `POST /api/file/upload`
- Auth: Bearer token (bắt buộc)
- Content-Type: `multipart/form-data`
- Body:
  - `file`: File hợp lệ (.pdf, .docx, .xlsx, .pptx, .txt, .csv, .zip, .rar, .7z, .mp3, .mp4, .mov, .avi, .png, .jpg, .jpeg, .svg, .webp, ...)
- Response mẫu:

```json
{
  "success": true,
  "fileId": 456,
  "url": "https://cdn.example.com/files/entities/xyz.pdf",
  "thumbnailUrl": null
}
```

- Ghi chú:
  - Không tạo thumbnail cho file không phải ảnh.
  - Hệ thống tự phân loại `fileType` dựa trên đuôi/mime.

### Lỗi thường gặp

- 400: Định dạng không hỗ trợ hoặc kích thước vượt quá giới hạn.
- 401: Thiếu/invalid token.
- 500: Lỗi xử lý server.

---

## 2) Tích hợp vào các thực thể

### 2.1) Topic

- Tạo Topic: gửi `FileId` (optional) để gắn file chính.
- API (request body) ví dụ:

```json
{
  "title": "Tiêu đề",
  "description": "Mô tả",
  "objectives": "Mục tiêu",
  "categoryId": 1,
  "semesterId": 1,
  "maxStudents": 3,
  "fileId": 123
}
```

- Cập nhật Topic: có thể đổi `fileId` để thay file chính.

- Detail: API trả về thông tin topic kèm file chính (nếu có). Trường trả về liên quan file:

  - `fileId`
  - `url`
  - `thumbnailUrl`

- Quy tắc: File phải do chính user đang gọi API upload (kiểm tra theo `CreatedBy`). Nếu không, 409 sẽ được trả về.

### 2.2) TopicVersion

- Tạo/Cập nhật: gửi `fileId` (optional) tương tự Topic.
- Detail: trả thông tin file chính (nếu có) với các trường:
  - `fileId`
  - `url`
  - `thumbnailUrl`
- Quy tắc sở hữu file tương tự Topic.

### 2.3) Submission

- Tạo/Cập nhật: có thể gửi `fileId` (optional) để gắn file chính cho submission.
- Ví dụ tạo:

```json
{
  "topicId": 10,
  "phaseId": 5,
  "additionalNotes": "Ghi chú",
  "fileId": 456
}
```

- Detail: trả file chính (nếu có) với các trường:

  - `fileId`
  - `url`
  - `thumbnailUrl`

- Lưu ý:
  - Hệ thống vẫn có trường `documentUrl` trong `Submission`; nhưng nên ưu tiên cơ chế `fileId` để đồng bộ và kiểm soát quyền sở hữu.
  - File phải do user hiện tại upload.

---

## 3) Best practices cho Frontend

- **Chọn API đúng loại**:
  - Ảnh cần thumbnail → `upload-image`.
  - Tài liệu/video/âm thanh/zip → `upload`.
- **Luồng thao tác đề xuất**:
  1. Người dùng chọn file → gọi API upload → lấy `fileId`.
  2. Gọi API domain (create/update) kèm `fileId`.
  3. Render ảnh từ `thumbnailUrl` (nếu có), click xem bản gốc từ `url`.
- **Quyền sở hữu**:
  - Chỉ sử dụng `fileId` do chính user hiện tại upload. Nếu chuyển user khác, backend sẽ từ chối.
- **Xử lý UI/UX**:
  - Hiển thị tiến trình upload, validate size/đuôi trước khi gọi API.
  - Với ảnh: ưu tiên hiển thị `thumbnailUrl` để tối ưu tốc độ.
  - Lưu lại `fileId` trong state/form trước khi submit API domain.

---

## 4) Tham chiếu nhanh

- Upload ảnh: `POST /api/file/upload-image` → trả `{ fileId, url, thumbnailUrl }`
- Upload file: `POST /api/file/upload` → trả `{ fileId, url, thumbnailUrl }`
- Gắn file:
  - Topic: `fileId` trong Create/Update
  - TopicVersion: `fileId` trong Create/Update
  - Submission: `fileId` trong Create/Update
- Chi tiết thực thể: payload có phần file chính (nếu có) gồm: `fileId`, `url`, `thumbnailUrl`.

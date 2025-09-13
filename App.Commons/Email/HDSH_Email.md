# Hướng dẫn sử dụng Email Template System

Đoạn code trên thực hiện việc cấu hình và cung cấp đường dẫn cho các email template thông qua một vài thành phần chính sau:

## 1. Cấu hình EmailTemplateOptions

**EmailTemplateOptions** là một lớp cấu hình (POCO) chứa các thuộc tính liên quan đến đường dẫn:
- **RootPath**: Mặc định được thiết lập là `"wwwroot"`, dùng để chỉ thư mục chứa các file template.
- **BasePath**: Là một chuỗi có thể null, dùng để chỉ định một đường dẫn tuyệt đối. Nếu được cung cấp, đường dẫn này sẽ được ưu tiên sử dụng thay vì đường dẫn gốc của ứng dụng.

## 2. Định nghĩa giao diện IPathProvider và triển khai PathProvider

**IPathProvider** định nghĩa hai phương thức:
- `GetEmailTemplatePath(string templatePath)`: Trả về đường dẫn đầy đủ đến file email template dựa trên một đường dẫn tương đối được truyền vào.
- `GetBasePath()`: Trả về đường dẫn gốc được sử dụng.

**PathProvider** là lớp thực thi giao diện này:

### Constructor:
- Nhận vào `IOptions<EmailTemplateOptions>` để lấy cấu hình từ `EmailTemplateOptions`.
- Nhận thêm `IHostingEnvironment` để lấy thông tin về môi trường chạy của ứng dụng, cụ thể là `ContentRootPath` (đường dẫn gốc của ứng dụng).
- Thiết lập trường `_basePath`: Nếu `BasePath` từ cấu hình không rỗng, sử dụng nó; ngược lại, dùng `env.ContentRootPath`.

### Phương thức GetEmailTemplatePath:
- Sử dụng `Path.Combine` để nối chuỗi theo thứ tự: `BasePath`, `RootPath` và `templatePath` nhằm tạo ra đường dẫn đầy đủ đến email template.

### Phương thức GetBasePath:
- Trả về giá trị của `_basePath` đã được thiết lập trong constructor.

## 3. Cấu hình Dependency Injection (DI)

### Đăng ký cấu hình:
```csharp
services.Configure<EmailTemplateOptions>(configuration.GetSection("EmailTemplates"));
```
Dòng này thực hiện việc ánh xạ (bind) phần cấu hình từ file cấu hình (ví dụ: *appsettings.json*) với key `"EmailTemplates"` vào đối tượng `EmailTemplateOptions`.

### Đăng ký dịch vụ:
```csharp
services.AddScoped<IPathProvider, PathProvider>();
```
Đăng ký `PathProvider` là implementation của `IPathProvider` theo phạm vi Scoped, nghĩa là mỗi request sẽ có một instance riêng của `PathProvider`.

## 4. Cấu hình thực tế từ file cấu hình

Phần cấu hình JSON:
```json
{
  "EmailTemplates": {
    "RootPath": "wwwroot",
    "BasePath": "/Users/tamnguyen/Documents/FPT_Semester_7/PRN222/SP25_PRN222_NET1708_GaQuay/Final_Assignment/GaQuay_Net1708_FinalProject/ApiTesting"
  }
}
```

Tại đây:
- `RootPath` được đặt là `"wwwroot"`
- `BasePath` được chỉ định rõ ràng thành một đường dẫn tuyệt đối

Nhờ đó, khi ứng dụng cần lấy đường dẫn của một email template cụ thể, nó sẽ kết hợp `BasePath`, `RootPath` và tên file template đã cho để tạo ra đường dẫn đầy đủ.

## Tổng kết

- **EmailTemplateOptions** lưu trữ thông tin cấu hình về đường dẫn.
- **PathProvider** sử dụng thông tin này cùng với thông tin môi trường để xây dựng đường dẫn đầy đủ cho email template.
- **DI Container** được cấu hình để inject các giá trị cấu hình và đối tượng `PathProvider` vào các thành phần cần thiết trong ứng dụng.

Nhờ cấu trúc này, ứng dụng có thể dễ dàng thay đổi đường dẫn email template thông qua file cấu hình mà không cần thay đổi code, đồng thời đảm bảo tính linh hoạt và mở rộng cho dự án.

---

## Lưu ý về BasePath rỗng

Nếu trong file cấu hình appSettings, bạn để BasePath rỗng hoặc không khai báo thuộc tính này, thì trong constructor của **PathProvider** đoạn kiểm tra:

```csharp
_basePath = string.IsNullOrEmpty(_options.BasePath) ? env.ContentRootPath : _options.BasePath;
```

sẽ đánh giá điều kiện `string.IsNullOrEmpty(_options.BasePath)` là **true**. Điều này có nghĩa là giá trị của `_basePath` sẽ được gán là `env.ContentRootPath`, tức là đường dẫn gốc của ứng dụng (ContentRootPath).

Sau đó, khi gọi `GetEmailTemplatePath`, đường dẫn được tạo thành sẽ là sự kết hợp của:

- **ContentRootPath** (đường dẫn gốc của ứng dụng)
- **RootPath** (mặc định là "wwwroot" nếu không thay đổi trong cấu hình)
- và tên file template được truyền vào

Như vậy, khi không có BasePath hoặc BasePath rỗng, ứng dụng sẽ sử dụng đường dẫn gốc của ứng dụng làm cơ sở cho các
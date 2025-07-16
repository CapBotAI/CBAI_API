namespace App.Commons;

public class ConstantModel
{
    #region Common
    public const string ErrorMessage = "Đã xảy ra lỗi trong quá trình thực hiện. Vui lòng thử lại trong ít phút.";
    public const string ModelInvalid = "Dữ liệu đầu vào không hợp lệ.";
    public const string GetDataSuccess = "Get data succeeded.";
    public const string GetDataFailed = "Get data failed.";
    public const string GetUnAuthorized = "Bạn đã hết phiên đăng nhập. Vui lòng đăng nhập lại để tiếp tục.";
    public const string GetNotFound = "Oops!!! Không tìm thấy.";
    public const string GetForbidden = "Oops!!! Không đủ quyền hạn.";
    public const string SaveDataSuccess = "Lưu thành công.";
    public const string SaveDataFailed = "Lưu thất bại.";
    public const string Required = "Dữ liệu không được để trống.";
    public const string EmailAddressFormatError = "Email không đúng định dạng.";
    public const string PasswordStringLengthError = "{0} phải có tối thiểu {2} và tối đa {1} kí tự";
    public const string ConfirmPasswordError = "Mật khẩu nhập lại không đúng.";
    public const string MaxlengthError = "{0} độ dài tối đa {1} ký tự.";
    public const string APIURL = "https://localhost:7190/";
    public const string POLICY_VERIFY_EMAIL = "VerifyEmail";
    public const string CLAIM_USER_TYPE = "user_type";
    public const string CLAIM_FULL_NAME = "full_name";
    public const string CLAIM_EMAIL = "Email";
    public const string CLAIM_ID = "id";
    public const string IS_ADMIN = "is_admin";
    public const string IS_MODERATOR = "is_moderator";
    public const string IS_SUPERVISOR = "is_supervisor";
    public const string IS_REVIEWER = "is_reviewer";
    public const string AVATAR = "avatar";
    public const string USERNAME = "username";
    public const string IS_REMEMBER = "is_remember";
    public const string FormatDateTime = "dd/MM/yyyy HH:mm";
    public const string FormatFullDateTime = "dd/MM/yyyy HH:mm:ss";
    public const string FormatDate = "dd/MM/yyyy";
    public const string UserRole = "user";
    public const string PhoneNumberVietNam = "+84";
    public const string SomeThingWentWrong = "Có lỗi xảy ra trong quá trình thực hiện, vui lòng thử lại sau ít phút!";
    public const string UserNotSame = "Người dùng không giống nhau.";
    public const string DefaultAvatar = "https://static.vecteezy.com/system/resources/previews/009/734/564/non_2x/default-avatar-profile-icon-of-social-media-user-vector.jpg";
    public const string EXPIRED_SESSION = "Phiên đăng nhập đã hết hạn.";
    public const string FULL_NAME = "fullName";
    #endregion

    #region Password
    public const string REGEX_PASSWORD = @"^[A-Za-z0-9!@#?$%^&*()_[\]{}|:"",.<>+=-]*$";
    public const string PasswordInvalidFormat = "Mật khẩu chỉ được chứa ký tự chữ cái, chữ số, ký tự đặc biệt.";
    public const string PasswordIsInCorrect = "Tài khoản hoặc mật khẩu không đúng";
    #endregion

    #region JWT
    public const string JWT_ISSUER = "CapBot";
    public const string JWT_AUDIENCE = "CapBot-Users";
    #endregion
}

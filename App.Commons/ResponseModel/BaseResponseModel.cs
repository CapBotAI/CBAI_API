namespace App.Commons.ResponseModel;

public class BaseResponseModel
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string? Message { get; set; }
}

public class BaseResponseModel<T> : BaseResponseModel where T : class
{
    public T Data { get; set; }
}

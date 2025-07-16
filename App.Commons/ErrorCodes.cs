using System;

namespace App.Commons;

public class ErrorCodes
{
    // 400 - Bad Request
    public const string HB40001 = "HB40001"; // Missing/invalid input

    // 401 - Unauthorized
    public const string HB40101 = "HB40101"; // Token missing/invalid

    // 403 - Forbidden
    public const string HB40301 = "HB40301"; // Permission denied

    // 404 - Not Found
    public const string HB40401 = "HB40401"; // Resource not found

    // 500 - Internal Server Error
    public const string HB50001 = "HB50001"; // Internal server error
    public const string HB20001 = "HB20001"; // OK
    public const string HB20101 = "HB20101"; // Created
}

public static class ErrorMessages
{
    // 400 Messages
    public const string MissingInvalidInput = "Missing/invalid input";
    public const string ModelNameRequired = "modelName is required";
    public const string MaterialRequired = "material is required";
    public const string PriceRequired = "price is required";
    public const string StockRequired = "stock is required";
    public const string BrandIdRequired = "brandId is required";
    public const string InvalidEmailPassword = "Invalid email or password";

    // 401 Messages
    public const string TokenMissingInvalid = "Token missing/invalid";
    public const string UnauthorizedAccess = "Unauthorized access";

    // 403 Messages
    public const string PermissionDenied = "Permission denied";
    public const string InsufficientPrivileges = "Access denied. Insufficient privileges";

    // 404 Messages
    public const string ResourceNotFound = "Resource not found";
    public const string HandbagNotFound = "Handbag not found";
    public const string BrandNotFound = "Brand not found";

    // 500 Messages
    public const string InternalServerError = "Internal server error";
    public const string UnexpectedError = "An unexpected error occurred";
}

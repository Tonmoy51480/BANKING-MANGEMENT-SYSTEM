namespace BankingManagementSystem.Core.DTOs;

public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ServiceResult Ok(string message = "Operation completed successfully.") =>
        new() { Success = true, Message = message };

    public static ServiceResult Failed(string error) =>
        new() { Success = false, Message = error, Errors = new() { error } };

    public static ServiceResult Failed(List<string> errors, string message = "Operation failed.") =>
        new() { Success = false, Message = message, Errors = errors };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data, string message = "Operation completed successfully.") =>
        new() { Success = true, Message = message, Data = data };

    public new static ServiceResult<T> Failed(string error) =>
        new() { Success = false, Message = error, Errors = new() { error } };

    public new static ServiceResult<T> Failed(List<string> errors, string message = "Operation failed.") =>
        new() { Success = false, Message = message, Errors = errors };
}

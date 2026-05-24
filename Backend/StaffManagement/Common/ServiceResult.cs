namespace StaffManagement.Common;

public class ServiceResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ServiceResult Ok()
    {
        return new ServiceResult { Success = true };
    }

    public static ServiceResult Fail(string message)
    {
        return new ServiceResult
        {
            Success = false,
            Message = message
        };
    }
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data)
    {
        return new ServiceResult<T>
        {
            Success = true,
            Data = data
        };
    }

    public static new ServiceResult<T> Fail(string message)
    {
        return new ServiceResult<T>
        {
            Success = false,
            Message = message
        };
    }
}

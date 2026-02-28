namespace AmieLife.Application.Common.Models;

/// <summary>
/// Generic discriminated union result type.
/// Avoids throwing exceptions for expected business outcomes (login failure, etc.).
/// Use domain exceptions only for unexpected / unrecoverable situations.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }

    private Result() { }

    public static Result<T> Success(T data) => new() { IsSuccess = true, Data = data };

    public static Result<T> Failure(string error) => new() { IsSuccess = false, Error = error };
}

/// <summary>Non-generic result for operations that return no data on success.</summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public string? Error { get; private set; }

    private Result() { }

    public static Result Success() => new() { IsSuccess = true };
    public static Result Failure(string error) => new() { IsSuccess = false, Error = error };
}

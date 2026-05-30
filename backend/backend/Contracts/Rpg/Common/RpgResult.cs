namespace backend.Contracts.Rpg.Common;

public enum RpgResultStatus
{
    Ok,
    NotFound,
    BadRequest,
    Conflict,
    ServiceUnavailable
}

public sealed record RpgResult<T>(RpgResultStatus Status, T? Value, string? Message)
{
    public static RpgResult<T> Ok(T value) => new(RpgResultStatus.Ok, value, null);

    public static RpgResult<T> NotFound(string message) => new(RpgResultStatus.NotFound, default, message);

    public static RpgResult<T> BadRequest(string message) => new(RpgResultStatus.BadRequest, default, message);

    public static RpgResult<T> Conflict(string message) => new(RpgResultStatus.Conflict, default, message);

    public static RpgResult<T> ServiceUnavailable(T? value, string message) => new(RpgResultStatus.ServiceUnavailable, value, message);
}

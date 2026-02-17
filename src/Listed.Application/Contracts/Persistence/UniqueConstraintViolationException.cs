namespace Listed.Application.Contracts.Persistence;

public sealed class UniqueConstraintViolationException(string constraintCode, string? constraintName = null) : Exception
{
    public string ConstraintCode { get; } = constraintCode;
    public string? ConstraintName { get; } = constraintName;
}

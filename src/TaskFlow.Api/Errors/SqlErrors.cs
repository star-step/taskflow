namespace TaskFlow.Api.Errors;

public enum SqlErrorKind
{
    Other,
    UniqueViolation,
    ConstraintViolation,
    Transient
}

/// <summary>
/// Classifies SQL Server error numbers, so a duplicate key reads as a conflict rather than
/// "database unavailable", and only genuine connectivity/transient failures become 503.
/// </summary>
public static class SqlErrors
{
    private static readonly HashSet<int> TransientNumbers = new()
    {
        -2,     // client timeout
        20, 53, 64, 121, 233, 10053, 10054, 10060, // connection/transport failures
        1205,   // deadlock victim
        4060,   // cannot open database
        40197, 40501, 40613, 49918, 49919, 49920 // Azure SQL service busy/unavailable
    };

    public static SqlErrorKind Classify(int number) => number switch
    {
        2601 or 2627 => SqlErrorKind.UniqueViolation,
        547 => SqlErrorKind.ConstraintViolation,
        _ when TransientNumbers.Contains(number) => SqlErrorKind.Transient,
        _ => SqlErrorKind.Other
    };
}

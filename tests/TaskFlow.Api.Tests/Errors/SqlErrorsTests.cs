using TaskFlow.Api.Errors;

namespace TaskFlow.Api.Tests.Errors;

public sealed class SqlErrorsTests
{
    [Theory]
    [InlineData(2601, SqlErrorKind.UniqueViolation)]  // duplicate key in unique index
    [InlineData(2627, SqlErrorKind.UniqueViolation)]  // unique/primary key constraint
    [InlineData(547, SqlErrorKind.ConstraintViolation)] // CHECK / FOREIGN KEY
    [InlineData(-2, SqlErrorKind.Transient)]          // timeout
    [InlineData(1205, SqlErrorKind.Transient)]        // deadlock victim
    [InlineData(40613, SqlErrorKind.Transient)]       // database unavailable
    [InlineData(2, SqlErrorKind.Transient)]           // server not found / not accessible
    [InlineData(26, SqlErrorKind.Transient)]          // error locating server/instance
    [InlineData(40, SqlErrorKind.Transient)]          // could not open a connection
    [InlineData(10061, SqlErrorKind.Transient)]       // connection refused
    [InlineData(11001, SqlErrorKind.Transient)]       // host not known
    [InlineData(208, SqlErrorKind.Other)]             // invalid object name: a bug, not an outage
    public void Classify_MapsSqlErrorNumbers(int number, SqlErrorKind expected) =>
        Assert.Equal(expected, SqlErrors.Classify(number));

    [Fact]
    public void Classify_TreatsFatalSeverityAsTransient_EvenForUnknownNumbers()
    {
        Assert.Equal(SqlErrorKind.Transient, SqlErrors.Classify(99999, severity: 20));
        Assert.Equal(SqlErrorKind.Other, SqlErrors.Classify(99999, severity: 16));
    }
}

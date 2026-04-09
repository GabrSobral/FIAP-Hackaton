using fiap_hackaton.Domain.Core;

namespace fiap_hackaton.Tests.Unit.Domain;

public class ResultTests
{
    [Fact]
    public void Success_ShouldSetIsSuccess_True()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void SuccessT_ShouldSetValueAndIsSuccess_True()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_ShouldSetIsFailure_True()
    {
        var ex     = new Exception("something failed");
        var result = Result.Failure(ex);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Same(ex, result.Error);
    }

    [Fact]
    public void FailureT_ShouldSetIsFailure_True_AndDefaultValue()
    {
        var ex     = new Exception("oops");
        var result = Result.Failure<string>(ex);

        Assert.True(result.IsFailure);
        Assert.Same(ex, result.Error);
    }

    [Fact]
    public void Success_WithError_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new TestResult(true, new Exception("bad")));
    }

    [Fact]
    public void Failure_WithoutError_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new TestResult(false, null));
    }

    // Helper to test protected constructor
    private sealed class TestResult : Result
    {
        public TestResult(bool isSuccess, Exception? error) : base(isSuccess, error) { }
    }
}

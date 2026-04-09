namespace fiap_hackaton.Domain.Exceptions;

public class DomainError : Exception
{
    public DomainError() : base("A domain error occurred.") { }
    public DomainError(string message) : base(message) { }
    public DomainError(string message, Exception innerException) : base(message, innerException) { }
}

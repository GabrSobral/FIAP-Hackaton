namespace fiap_hackaton.Domain.Exceptions;

public class AnalysisNotFoundException : Exception
{
    public AnalysisNotFoundException(Guid id)
        : base($"Analysis with ID '{id}' was not found.") { }
}

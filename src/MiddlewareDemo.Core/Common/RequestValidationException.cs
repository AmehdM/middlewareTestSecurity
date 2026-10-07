namespace MiddlewareDemo.Core.Common;

public sealed class RequestValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public RequestValidationException(IReadOnlyList<string> errors)
        : base(string.Join("; ", errors))
    {
        Errors = errors;
    }

    public RequestValidationException(string error)
        : this(new[] { error })
    {
    }
}

public sealed class ValidationOutcome
{
    public List<string> Errors { get; } = new();
    public bool IsValid => Errors.Count == 0;

    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new RequestValidationException(Errors);
        }
    }
}

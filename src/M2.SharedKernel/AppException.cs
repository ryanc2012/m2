namespace M2.SharedKernel;

public class AppException : Exception
{
    public string Code { get; }

    public AppException(string code, string message) : base(message)
    {
        Code = code;
    }

    public AppException(string code, string message, Exception inner) : base(message, inner)
    {
        Code = code;
    }
}

namespace NetatmoTrueTempSync;

public sealed class NetatmoException : Exception
{
    public NetatmoException(string message) : base(message)
    {
    }

    public NetatmoException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

namespace RifeZPhoneBridge.App;

public sealed class SimpleFileLogger
{
    private readonly object _sync = new();
    private readonly string _logPath;

    public SimpleFileLogger(string logsDirectory)
    {
        Directory.CreateDirectory(logsDirectory);
        _logPath = Path.Combine(logsDirectory, $"bridge-{DateTime.Now:yyyyMMdd}.log");
    }

    public void Info(string message) => Write("INFO", message);
    public void Error(string message) => Write("ERROR", message);

    public void Write(string level, string message)
    {
        lock (_sync)
        {
            File.AppendAllText(_logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
        }
    }
}

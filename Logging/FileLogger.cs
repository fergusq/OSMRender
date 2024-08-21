namespace OSMRender.Logging;

class FileLogger : ILogger, IDisposable {

    private readonly StreamWriter Writer;
    public LoggingLevel Level { get; set; } = LoggingLevel.Info;

    public enum LoggingLevel {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    public FileLogger(StreamWriter writer) {
        Writer = writer;
    }

    public FileLogger(string path) {
        Writer = new StreamWriter(path);
    }

    public void Debug(string message) {
        if (Level >= LoggingLevel.Debug) {
            Writer.WriteLine($"DEBUG: {message}");
        }
    }

    public void Error(string message) {
        if (Level >= LoggingLevel.Error) {
            Writer.WriteLine($"ERROR: {message}");
        }
    }

    public void Info(string message) {
        if (Level >= LoggingLevel.Info) {
            Writer.WriteLine($"INFO: {message}");
        }
    }

    public void Warning(string message) {
        if (Level >= LoggingLevel.Warning) {
            Writer.WriteLine($"WARNING: {message}");
        }
    }

    public void Dispose()
    {
        Writer.Flush();
        Writer.Dispose();
    }
}
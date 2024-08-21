namespace OSMRender.Logging;

public abstract class Logger : IDisposable {

    public LoggingLevel Level { get; set; } = LoggingLevel.Info;

    public enum LoggingLevel {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
    }

    public void Debug(string message) {
        if (Level <= LoggingLevel.Debug) {
            DebugImpl(message);
        }
    }

    public void Error(string message) {
        if (Level <= LoggingLevel.Error) {
            ErrorImpl(message);
        }
    }

    public void Info(string message) {
        if (Level <= LoggingLevel.Info) {
            InfoImpl(message);
        }
    }

    public void Warning(string message) {
        if (Level <= LoggingLevel.Warning) {
            WarningImpl(message);
        }
    }

    protected abstract void ErrorImpl(string message);
    protected abstract void WarningImpl(string message);
    protected abstract void InfoImpl(string message);
    protected abstract void DebugImpl(string message);
    public abstract void Dispose();
}
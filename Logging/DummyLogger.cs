namespace OSMRender.Logging;

public class DummyLogger : ILogger {
    public void Debug(string message) {}

    public void Error(string message) {}

    public void Info(string message) {}

    public void Warning(string message) {}
}
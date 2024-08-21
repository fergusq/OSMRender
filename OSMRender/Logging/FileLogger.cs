namespace OSMRender.Logging;

public class FileLogger : Logger {

    private readonly StreamWriter Writer;

    public FileLogger(StreamWriter writer) {
        Writer = writer;
    }

    public FileLogger(string path) {
        Writer = new StreamWriter(path);
    }

    protected override void DebugImpl(string message) {
        Writer.WriteLine($"DEBUG: {message}");
    }

    protected override void ErrorImpl(string message) {
        Writer.WriteLine($"ERROR: {message}");
    }

    protected override void InfoImpl(string message) {
        Writer.WriteLine($"INFO: {message}");
    }

    protected override void WarningImpl(string message) {
        Writer.WriteLine($"WARNING: {message}");
    }

    public override void Dispose()
    {
        Writer.Flush();
        Writer.Dispose();
        GC.SuppressFinalize(this);
    }
}
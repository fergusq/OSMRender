namespace OSMRender.Logging;

public class ConsoleLogger : Logger {

    protected override void DebugImpl(string message) {
        Console.WriteLine($"DEBUG: {message}");
    }

    protected override void ErrorImpl(string message) {
        Console.WriteLine($"ERROR: {message}");
    }

    protected override void InfoImpl(string message) {
        Console.WriteLine($"INFO: {message}");
    }

    protected override void WarningImpl(string message) {
        Console.WriteLine($"WARNING: {message}");
    }

    public override void Dispose() {
        GC.SuppressFinalize(this);
    }
}
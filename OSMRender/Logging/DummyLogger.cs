namespace OSMRender.Logging;

public class DummyLogger : Logger {
    protected override void DebugImpl(string message) {}

    protected override void ErrorImpl(string message) {}

    protected override void InfoImpl(string message) {}

    protected override void WarningImpl(string message) {}

    public override void Dispose() {
        GC.SuppressFinalize(this);
    }
}
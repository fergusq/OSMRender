// This file is part of OSMRender.
//
// OSMRender is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OSMRender is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OSMRender. If not, see <https://www.gnu.org/licenses/>.

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
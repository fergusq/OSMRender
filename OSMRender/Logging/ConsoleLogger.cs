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
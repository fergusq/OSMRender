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

namespace OSMRender.Rules;

/// <summary>
/// Represents the currently set property values.
/// </summary>
public class State {
    public IDictionary<string, string> Properties { get; set; }

    public State(Ruleset ruleset) {
        Properties = new Dictionary<string, string>();
        ruleset.Properties.ToList().ForEach(p => Properties[p.Key] = p.Value);
    }
}
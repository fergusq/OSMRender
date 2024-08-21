namespace OSMRender.Rules;

public class State {
    public IDictionary<string, string> Properties { get; set; }

    public State(Ruleset ruleset) {
        Properties = new Dictionary<string, string>();
        ruleset.Properties.ToList().ForEach(p => Properties[p.Key] = p.Value);
    }
}
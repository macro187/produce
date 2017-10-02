using System;
using System.Collections.Generic;
using System.Linq;
using MacroGuards;


namespace
produce
{


public sealed class
Graph
{


public
Graph()
{
    Targets = new HashSet<Target>();
    Rules = new Dictionary<Target, Rule>();
    Dependencies = new HashSet<Dependency>();
}


// TODO Readonly
public ISet<Target>
Targets { get; }


// TODO Readonly
public IDictionary<Target, Rule>
Rules { get; }


// TODO Readonly
public ISet<Dependency>
Dependencies { get; }


/// <summary>
/// Get or add a <see cref="CommandTarget"/> named <paramref name="name"/>
/// </summary>
///
public CommandTarget
Command(string name)
{
    Guard.Required(name, nameof(name));
    var t = FindCommand(name);
    if (t != null) return t;
    t = new CommandTarget(name);
    Targets.Add(t);
    return t;
}


/// <summary>
/// Find a <see cref="CommandTarget"/> named <paramref name="name"/>
/// </summary>
///
public CommandTarget
FindCommand(string name)
{
    Guard.Required(name, nameof(name));
    return Targets
        .OfType<CommandTarget>()
        .FirstOrDefault(t => t.Name == name);
}


/// <summary>
/// Add a rule to the graph
/// </summary>
///
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "1#",
    Justification = "Supposed to feel declarative")]
public void
Rule(Target target, Rule rule)
{
    Guard.NotNull(target, nameof(target));
    Guard.NotNull(rule, nameof(rule));
    if (!Targets.Contains(target))
        throw new ArgumentException(
            FormattableString.Invariant($"Target {target} is not in this graph"),
            nameof(target));
    if (RuleFor(target) != null)
        throw new InvalidOperationException(
            FormattableString.Invariant($"Graph already contains a rule for target {target}"));
    Rules.Add(target, rule);
}


/// <summary>
/// Add a regular dependency to the graph
/// </summary>
///
public void
Dependency(Target from, Target to)
{
    Dependency(new Dependency(from, to));
}


/// <summary>
/// Add a dependency to the graph
/// </summary>
///
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#",
    Justification = "Supposed to feel declarative")]
public void
Dependency(Dependency dependency)
{
    Guard.NotNull(dependency, nameof(dependency));
    Dependencies.Add(dependency);
}


public Rule
RuleFor(Target target)
{
    Guard.NotNull(target, nameof(target));
    if (!Rules.TryGetValue(target, out var rule)) return null;
    return rule;
}


public IEnumerable<Target>
RequiredBy(Target target)
{
    Guard.NotNull(target, nameof(target));
    return Dependencies.Where(d => d.To == target).Select(d => d.From);
}


public IEnumerable<Target>
Requiring(Target target)
{
    Guard.NotNull(target, nameof(target));
    return Dependencies.Where(d => d.From == target).Select(d => d.To);
}


}
}

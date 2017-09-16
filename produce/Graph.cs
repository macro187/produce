using MacroGuards;
using System;
using System.Collections.Generic;
using System.Linq;


namespace
produce
{


public class
Graph
{


public
Graph()
{
    Rules = new HashSet<Rule>();
    Targets = new HashSet<Target>();
}


// TODO Need ReadOnlySetProxy<T>
public ISet<Rule>
Rules
{
    get;
}


// TODO Need ReadOnlySetProxy<T>
public ISet<Target>
Targets
{
    get;
}


/// <summary>
/// Get or add a <see cref="CommandTarget"/> named <paramref name="name"/>
/// </summary>
///
public CommandTarget
Command(string name)
{
    Guard.Required(name, nameof(name));
    var t = FindCommandTarget(name);
    if (t != null) return t;
    t = new CommandTarget(name);
    Targets.Add(t);
    return t;
}


public CommandTarget
FindCommandTarget(string name)
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
public void
Add(Rule rule)
{
    Guard.NotNull(rule, nameof(rule));
    if (!Targets.Contains(rule.Target))
        throw new ArgumentException(
            FormattableString.Invariant($"Rule's target {rule.Target} is not in this graph"),
            nameof(rule));
    foreach (var source in rule.Required)
        if (!Targets.Contains(source))
            throw new ArgumentException(
                FormattableString.Invariant($"Rule's source {source} is not in this Graph"),
                nameof(rule));
    if (FindRuleFor(rule.Target) != null)
        throw new InvalidOperationException(
            FormattableString.Invariant($"Graph already contains a rule for target {rule.Target}"));
    Rules.Add(rule);
}


public Rule
FindRuleFor(Target target)
{
    Guard.NotNull(target, nameof(target));
    return Rules.SingleOrDefault(r => r.Target == target);
}


public ISet<Target>
RequiredBy(Target target)
{
    var rule = FindRuleFor(target);
    var requiredTargets = rule != null ? rule.Required : Enumerable.Empty<Target>();
    var requiredByTargets = Rules.Where(r => r.RequiredBy.Contains(target)).Select(r => r.Target);
    return new HashSet<Target>(requiredTargets.Concat(requiredByTargets));
}


public ISet<Target>
Requiring(Target target)
{
    var rule = FindRuleFor(target);
    var requiredTargets = Rules.Where(r => r.Required.Contains(target)).Select(r => r.Target);
    var requiredByTargets = rule != null ? rule.RequiredBy : Enumerable.Empty<Target>();
    return new HashSet<Target>(requiredTargets.Concat(requiredByTargets));
}


}
}

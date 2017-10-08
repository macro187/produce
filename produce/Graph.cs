using System;
using System.Collections.Generic;
using System.IO;
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
    Dependencies = new HashSet<Dependency>();
}


public ISet<Target>
Targets { get; }


public ISet<Dependency>
Dependencies { get; }


/// <summary>
/// Add or retrieve a <see cref="CommandTarget"/>
/// </summary>
///
public CommandTarget
Command(string name)
{
    return Command(name, () => {});
}


/// <summary>
/// Add or retrieve a <see cref="CommandTarget"/>
/// </summary>
///
public CommandTarget
Command(string name, Action build)
{
    Guard.Required(name, nameof(name));
    Guard.NotNull(build, nameof(build));
    return FindCommand(name) ?? AddTarget(new CommandTarget(this, name, build));
}


/// <summary>
/// Find a <see cref="CommandTarget"/> by name
/// </summary>
///
public CommandTarget
FindCommand(string name)
{
    Guard.Required(name, nameof(name));
    return Targets.OfType<CommandTarget>().FirstOrDefault(t => t.Name == name);
}


/// <summary>
/// Add a <see cref="ListTarget"/>
/// </summary>
///
public ListTarget
List(string name, string firstValue, params string[] moreValues)
{
    var values = Enumerable.Empty<string>();
    if (firstValue != null) values = values.Concat(new []{ firstValue });
    if (moreValues != null) values = values.Concat(moreValues);
    return List(name, values);
}


/// <summary>
/// Add a <see cref="ListTarget"/>
/// </summary>
///
public ListTarget
List(string name, IEnumerable<string> values)
{
    Guard.NotNull(values, nameof(values));
    return List(name, _ => values);
}


/// <summary>
/// Add a <see cref="ListTarget"/>
/// </summary>
///
public ListTarget
List(string name, Func<ListTarget, IEnumerable<string>> getValues)
{
    Guard.Required(name, nameof(name));
    if (Targets.OfType<ListTarget>().Any(t => t.Name == name))
        throw new InvalidOperationException(
            FormattableString.Invariant($"Graph already contains a ListTarget named {name}"));
    return AddTarget(new ListTarget(this, name, getValues));
}


/// <summary>
/// Get a <see cref="ListTarget"/>
/// </summary>
///
public ListTarget
List(string name)
{
    Guard.Required(name, nameof(name));
    var target = Targets.OfType<ListTarget>().Where(t => t.Name == name).FirstOrDefault();
    if (target == null)
        throw new InvalidOperationException(
            FormattableString.Invariant($"Graph contains no ListTarget named {name}"));
    return target;
}


/// <summary>
/// Add or retreive a <see cref="FileTarget"/>
/// </summary>
///
public FileTarget
File(string path)
{
    Guard.Required(path, nameof(path));
    if (!Path.IsPathRooted(path)) throw new ArgumentException("Not an absolute path", nameof(path));
    path = Path.GetFullPath(path);
    return
        Targets.OfType<FileTarget>().FirstOrDefault(t => t.Path == path)
        ?? AddTarget(new FileTarget(this, path));
}


/// <summary>
/// Add or retreive a <see cref="FileSetTarget"/>
/// </summary>
///
public FileSetTarget
FileSet(string name)
{
    return
        Targets.OfType<FileSetTarget>().FirstOrDefault(t => t.Name == name)
        ?? AddTarget(new FileSetTarget(this, name));
}


/// <summary>
/// Add a dependency to the graph
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
    if (Dependencies.Any(d => d.From == dependency.From && d.To == dependency.To))
        throw new InvalidOperationException(
            FormattableString.Invariant(
                $"Graph already contains a dependency from {dependency.From} to {dependency.To}"));
    Dependencies.Add(dependency);
}


public void
RemoveDependency(Target from, Target to)
{
    var dep = Dependencies.SingleOrDefault(d => d.From == from && d.To == to);
    if (dep == null) throw new ArgumentException("Specified dependency doesn't exist");
    Dependencies.Remove(dep);
}


public IEnumerable<Target>
RequiredBy(Target target)
{
    Guard.NotNull(target, nameof(target));
    return Dependencies.Where(d => d.To == target).Select(d => d.From).ToList();
}


public IEnumerable<Target>
Requiring(Target target)
{
    Guard.NotNull(target, nameof(target));
    return Dependencies.Where(d => d.From == target).Select(d => d.To).ToList();
}


T
AddTarget<T>(T target)
    where T : Target
{
    Guard.NotNull(target, nameof(target));
    Targets.Add(target);
    return target;
}


}
}

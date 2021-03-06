using System;
using static System.FormattableString;
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
Graph(ProduceWorkspace workspace)
{
    Guard.NotNull(workspace, nameof(workspace));
    Workspace = workspace;
    Targets = new HashSet<Target>();
    Dependencies = new HashSet<Dependency>();
}


public ProduceWorkspace
Workspace { get; }


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
    return Command(name, _ => {});
}


/// <summary>
/// Add or retrieve a <see cref="CommandTarget"/>
/// </summary>
///
public CommandTarget
Command(string name, Action<CommandTarget> build)
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
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypesInMemberSignatures")]
public ListTarget
List(string name, Func<ListTarget, IEnumerable<string>> getValues)
{
    Guard.Required(name, nameof(name));
    if (Targets.OfType<ListTarget>().Any(t => t.Name == name))
        throw new InvalidOperationException(Invariant($"Graph already contains a ListTarget named {name}"));
    return AddTarget(new ListTarget(this, name, getValues));
}


/// <summary>
/// Retrieve a <see cref="ListTarget"/>
/// </summary>
///
public ListTarget
List(string name)
{
    Guard.Required(name, nameof(name));
    var target = Targets.OfType<ListTarget>().Where(t => t.Name == name).FirstOrDefault();
    if (target == null)
        throw new InvalidOperationException(Invariant($"Graph contains no ListTarget named {name}"));
    return target;
}


/// <summary>
/// Add or retrieve a <see cref="FileTarget"/>
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
/// Add or retrieve a <see cref="FileSetTarget"/>
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
/// Remove a target from the graph along with any dependencies to or from it
/// </summary>
///
public void
RemoveTarget(Target target)
{
    Guard.NotNull(target, nameof(target));
    if (!Targets.Contains(target)) throw new ArgumentException("Target not in graph", nameof(target));
    var requiring = Requiring(target).ToList();
    foreach (var t in requiring) RemoveDependency(target, t);
    var required = Requiring(target).ToList();
    foreach (var t in required) RemoveDependency(t, target);
    Targets.Remove(target);
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
            Invariant($"Graph already contains a dependency from {dependency.From} to {dependency.To}"));
    Dependencies.Add(dependency);
}


/// <summary>
/// Remove a dependency from the graph
/// </summary>
///
public void
RemoveDependency(Target from, Target to)
{
    var dep = Dependencies.SingleOrDefault(d => d.From == from && d.To == to);
    if (dep == null) throw new ArgumentException("Specified dependency doesn't exist");
    Dependencies.Remove(dep);
}


/// <summary>
/// Find all targets directly required by a specified target
/// </summary>
///
public IEnumerable<Target>
RequiredBy(Target target)
{
    Guard.NotNull(target, nameof(target));
    return Dependencies.Where(d => d.To == target).Select(d => d.From).ToList();
}


/// <summary>
/// Find all targets directly or indirectly required by a specified target
/// </summary>
///
public IEnumerable<Target>
AllRequiredBy(Target target)
{
    var requiredBy = RequiredBy(target).ToList();
    return requiredBy.Concat(requiredBy.SelectMany(t => AllRequiredBy(t)));
}


/// <summary>
/// Find all targets that directly require a specified target
/// </summary>
///
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

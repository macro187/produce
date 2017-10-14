using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using MacroCollections;
using MacroDiagnostics;
using MacroGuards;


namespace
produce
{


public class
Builder
{


public
Builder(Graph graph)
{
    Guard.NotNull(graph, nameof(graph));
    Graph = graph;
    IDGenerator = new ObjectIDGenerator();
}


Graph
Graph { get; }


ObjectIDGenerator
IDGenerator { get; }


public void
Build(Target target)
{
    Guard.NotNull(target, nameof(target));

    while (true)
    {
        //var dotFile = Path.Combine(Graph.Workspace.GetDebugDirectory(), "graph.dot");
        //File.WriteAllLines(dotFile, ToDot());
        var targetSubset = new HashSet<Target>();
        targetSubset.Add(target);
        targetSubset.AddRange(AllRequiredBy(target));
        var targetToBuild = targetSubset.FirstOrDefault(t => IsBuildable(t));
        if (targetToBuild == null) break;
        using (LogicalOperation.Start(FormattableString.Invariant($"Building {targetToBuild}")))
        {
            targetToBuild.Build();
        }
    }

}


IEnumerable<Target>
AllRequiredBy(Target target)
{
    var requiredBy = Graph.RequiredBy(target).ToList();
    return requiredBy.Concat(requiredBy.SelectMany(t => AllRequiredBy(t)));
}


bool
IsBuildable(Target target)
{
    var requiredBy = Graph.RequiredBy(target).ToList();
    if (requiredBy.Any(t => !IsUpToDate(t))) return false;
    if (target.Timestamp != null && requiredBy.All(t => t.Timestamp <= target.Timestamp)) return false;
    return true;
}


bool
IsUpToDate(Target target)
{
    return
        target.Timestamp != null &&
        Graph.RequiredBy(target).All(t =>
            t.Timestamp != null &&
            t.Timestamp <= target.Timestamp &&
            IsUpToDate(t));
}


IEnumerable<string>
ToDot()
{
    yield return "digraph G {";
    foreach (var t in Graph.Targets)
        yield return $"{GetID(t)} [label=\"{t.ToString().Replace("\\", "\\\\")}\"];";
    foreach (var d in Graph.Dependencies)
        yield return $"{GetID(d.To)} -> {GetID(d.From)};";
    yield return "}";
}


string
GetID(Target target)
{
    return "target" + IDGenerator.GetId(target, out var firstTime).ToString();
}


}
}

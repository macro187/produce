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


int
DotCount;


public void
Build(Target target)
{
    Guard.NotNull(target, nameof(target));

    ClearDots();
    while (true)
    {
        WriteDot(null);

        var targetSubset = new HashSet<Target>();
        targetSubset.Add(target);
        targetSubset.AddRange(AllRequiredBy(target));
        var targetToBuild = targetSubset.FirstOrDefault(t => IsBuildable(t));
        if (targetToBuild == null) break;

        WriteDot(targetToBuild);

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


void
ClearDots()
{
    DotCount = 0;
    var debugDir = Graph.Workspace.GetDebugDirectory();
    foreach (var file in Directory.GetFiles(debugDir, "*.dot")) File.Delete(file);
    foreach (var file in Directory.GetFiles(debugDir, "*.dot.png")) File.Delete(file);
}


void
WriteDot(Target targetToBuild)
{
    var debugDir = Graph.Workspace.GetDebugDirectory();
    var dotFile = Path.Combine(debugDir, $"graph{DotCount:d2}.dot");
    var pngFile = Path.Combine(debugDir, $"graph{DotCount:d2}.dot.png");
    var dot = "C:\\Program Files (x86)\\Graphviz2.38\\bin\\dot.exe";
    File.WriteAllLines(dotFile, ToDot(targetToBuild));
    ProcessExtensions.Execute(true, true, null, dot, "-Tpng", "-o" + pngFile, dotFile);
    DotCount++;
}


IEnumerable<string>
ToDot(Target targetToBuild)
{
    yield return "digraph G {";
    foreach (var t in Graph.Targets)
    {
        var building = t == targetToBuild;
        var built = t.Timestamp != null;
        var color = IsBuildable(t) ? "limegreen" : "black";
        var label = t.ToString().Replace("\\", "\\\\");
        var style = building ? "filled" : built ? "filled" : "solid";
        var fillcolor = building ? "limegreen" : built ? "gray50" : "white";
        yield return $"{GetID(t)} [label=\"{label}\", style={style}, color={color}, fillcolor={fillcolor}];";
    }
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

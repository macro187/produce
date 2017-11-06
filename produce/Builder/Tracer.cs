using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using MacroDiagnostics;
using MacroGuards;


namespace
produce
{


/// <summary>
/// Draws the dependency graph in Graphviz format at each build step
/// </summary>
///
/// <remarks>
/// Graphs are written to <c>(workspace)/_produce/_trace/</c>
/// </remarks>
///
public class
Tracer
{


public static bool
Enabled { get; set; }


public
Tracer(Graph graph)
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
ClearDots()
{
    if (!Enabled) return;
    using (LogicalOperation.Start("Deleting existing graph drawings"))
    {
        DotCount = 0;
        var debugDir = Graph.Workspace.GetDebugDirectory();
        foreach (var file in Directory.GetFiles(debugDir, "*.dot")) File.Delete(file);
        foreach (var file in Directory.GetFiles(debugDir, "*.dot.png")) File.Delete(file);
    }
}


public void
WriteDot(Target targetToBuild)
{
    if (!Enabled) return;
    var debugDir = Graph.Workspace.GetDebugDirectory();
    var dotFile = Path.Combine(debugDir, $"graph{DotCount:d2}.dot");
    var pngFile = Path.Combine(debugDir, $"graph{DotCount:d2}.dot.png");
    var dot = "C:\\Program Files (x86)\\Graphviz2.38\\bin\\dot.exe";

    using (LogicalOperation.Start($"Drawing graph {dotFile}"))
    {
        File.WriteAllLines(dotFile, ToDot(targetToBuild));
        ProcessExtensions.Execute(true, true, null, dot, "-Tpng", "-o" + pngFile, dotFile);
    }

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
        var color = t.IsBuildable ? "limegreen" : "black";
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

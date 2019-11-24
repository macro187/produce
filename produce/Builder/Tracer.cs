using System;
using static System.FormattableString;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using MacroDiagnostics;
using MacroGuards;
using MacroSystem;

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
    DotCount = 0;
    var debugDir = Graph.Workspace.GetTraceDirectory();
    foreach (var file in Directory.GetFiles(debugDir, "*.dot")) File.Delete(file);
    foreach (var file in Directory.GetFiles(debugDir, "*.dot.png")) File.Delete(file);
}


public void
WriteDot(Target targetToBuild)
{
    if (!Enabled) return;
    var debugDir = Graph.Workspace.GetTraceDirectory();
    var dotFile = Path.Combine(debugDir, Invariant($"graph{DotCount:d2}.dot"));
    var pngFile = Path.Combine(debugDir, Invariant($"graph{DotCount:d2}.dot.png"));
    var dot =
        EnvironmentExtensions.IsWindows
            ? "C:\\Program Files (x86)\\Graphviz2.38\\bin\\dot.exe"
            : "dot";
    File.WriteAllLines(dotFile, ToDot(targetToBuild));
    ProcessExtensions.Execute(false, false, null, dot, "-Tpng", "-o" + pngFile, dotFile);
    DotCount++;
}


IEnumerable<string>
ToDot(Target targetToBuild)
{
    yield return "digraph G {";
    yield return "rankdir = RL;";
    yield return "dpi = 192;";
    foreach (var t in Graph.Targets)
    {
        var type = t.GetType().Name.Replace("Target", "");
        var isBuilding = t == targetToBuild;
        var isBuilt = t.Timestamp != null;
        var shape = "box";
        var color = t.IsBuildable ? "limegreen" : "black";
        var style = isBuilding ? "filled" : isBuilt ? "filled" : "solid";
        var fillcolor = isBuilding ? "limegreen" : isBuilt ? "gray50" : "white";
        var label = "";
        label += t.ToString().Replace("\\", "\\\\");
        label += "\\n";
        label += "(" + type + ")";
        if (t.Timestamp != null)
        {
            label += "\\n";
            label += t.Timestamp.Value.ToString("yyyy-MM-dd");
            label += "\\n";
            label += t.Timestamp.Value.ToString("HH:mm:ss.fff");
        }
        var fontname = "Helvetica";
        yield return $"{GetID(t)} [label=\"{label}\", fontname=\"{fontname}\", shape=\"{shape}\", style={style}, color={color}, fillcolor={fillcolor}];";
    }
    foreach (var d in Graph.Dependencies)
        yield return $"{GetID(d.To)} -> {GetID(d.From)};";
    yield return "}";
}


string
GetID(Target target)
{
    return "target" + IDGenerator.GetId(target, out var firstTime).ToString(CultureInfo.InvariantCulture);
}


}
}

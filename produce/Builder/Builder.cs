using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
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

    foreach (var requirement in Graph.RequiredBy(target)) Build(requirement);

    using (LogicalOperation.Start(FormattableString.Invariant($"Building {target}")))
    {
        target.Build();
    }

    var dotFile = Path.Combine(Graph.Workspace.GetDebugDirectory(), "graph.dot");
    File.WriteAllLines(dotFile, ToDot());
}


IEnumerable<string>
ToDot()
{
    yield return "digraph G {";
    foreach (var t in Graph.Targets)
        //yield return $"{GetID(t)} [label=\"{t.ToString().Replace("\\", "\\\\")}\\n{t.GetType().Name}\"];";
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

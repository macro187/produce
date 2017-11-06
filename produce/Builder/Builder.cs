using System;
using System.Collections.Generic;
using System.Linq;
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
    Tracer = new Tracer(graph);
}


Graph
Graph { get; }


Tracer
Tracer { get; }


public void
Build(Target target)
{
    Guard.NotNull(target, nameof(target));

    Tracer.ClearDots();
    while (true)
    {
        Tracer.WriteDot(null);

        var targetSubset = new HashSet<Target>();
        targetSubset.Add(target);
        targetSubset.AddRange(AllRequiredBy(target));
        var targetToBuild = targetSubset.FirstOrDefault(t => t.IsBuildable);
        if (targetToBuild == null) break;

        Tracer.WriteDot(targetToBuild);

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


}
}

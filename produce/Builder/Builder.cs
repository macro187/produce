using System;
using MacroDiagnostics;
using MacroGuards;


namespace
produce
{


public static class
Builder
{


public static void
Build(Graph graph, Target target)
{
    Guard.NotNull(target, nameof(target));
    Guard.NotNull(graph, nameof(graph));

    foreach (var requirement in graph.RequiredBy(target)) Build(graph, requirement);

    using (LogicalOperation.Start(FormattableString.Invariant($"Building {target}")))
    {
        target.Build();
    }
}


}
}

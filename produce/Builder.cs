using MacroDiagnostics;
using MacroGuards;
using System;
using System.Diagnostics;


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

    using (LogicalOperation.Start(target.Description))
    {
        var rule = graph.FindRuleFor(target);
        if (rule != null) rule.Build();
    }
}


}
}

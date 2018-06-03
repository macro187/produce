using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;


namespace
produce
{


public class
NuGitModule : Module
{


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var nugitRestore = graph.Command("nugit-restore", _ => Restore(repository));
    graph.Dependency(nugitRestore, graph.Command("restore"));

    var nugitUpdate = graph.Command("nugit-update", _ => Update(repository));
    graph.Dependency(nugitUpdate, graph.Command("update"));
}


static void
Restore(ProduceRepository repository)
{
    using (LogicalOperation.Start("Restoring NuGit dependencies"))
        if (ProcessExtensions.ExecuteAny(true, true, repository.Path, "nugit", "restore") != 0)
            throw new UserException("nugit failed");
}


static void
Update(ProduceRepository repository)
{
    using (LogicalOperation.Start("Updating NuGit dependencies"))
        if (ProcessExtensions.ExecuteAny(true, true, repository.Path, "nugit", "update") != 0)
            throw new UserException("nugit failed");
}


}
}

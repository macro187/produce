using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;


namespace
produce
{


public class
NuGitPlugin : Plugin
{


public override void
DetectRepositoryRules(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    graph.Add(
        new Rule(
            graph.Command("nugit-restore"),
            null,
            new[]{ graph.Command("restore") },
            () => Restore(repository)));
    graph.Add(
        new Rule(
            graph.Command("nugit-update"),
            null,
            new[]{ graph.Command("update") },
            () => Update(repository)));
}


static void
Restore(ProduceRepository repository)
{
    if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "nugit", "restore") != 0)
        throw new UserException("nugit failed");
}


static void
Update(ProduceRepository repository)
{
    if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "nugit", "update") != 0)
        throw new UserException("nugit failed");
}


}
}

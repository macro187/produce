using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;
using MacroSln;
using System.IO;


namespace
produce
{


public class
DotNetPlugin : Plugin
{


public override void
DetectRepositoryRules(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var dotNuGitDir = Path.Combine(repository.Path, ".nugit");
    VisualStudioSolution sln = null;
    // TODO Add a `findsln` command to `nugit` and use that (or similar)
    if (Directory.Exists(dotNuGitDir)) sln = VisualStudioSolution.Find(dotNuGitDir);
    if (sln == null) sln = VisualStudioSolution.Find(repository.Path);
    if (sln == null) return;

    graph.Add(
        new Rule(
            graph.Command("dotnet-build"),
            null,
            new[]{ graph.Command("build") },
            () => Sln(repository, sln, "build")));
    graph.Add(
        new Rule(
            graph.Command("dotnet-rebuild"),
            null,
            new[]{ graph.Command("rebuild") },
            () => Sln(repository, sln, "rebuild")));
    graph.Add(
        new Rule(
            graph.Command("dotnet-clean"),
            null,
            new[]{ graph.Command("clean") },
            () => Sln(repository, sln, "clean")));
}


static void
Sln(ProduceRepository repository, VisualStudioSolution sln, string command)
{
    var verb = command[0].ToString().ToUpperInvariant() + command.Substring(1) + "ing";
    using (LogicalOperation.Start(verb + " " + repository.Name))
    {
        if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "sln", command, sln.Path) != 0)
            throw new UserException("Failed");
    }
}


}
}

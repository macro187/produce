using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;
using MacroSln;
using System.IO;


namespace
produce
{


public class
SlnModule : Module
{


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var dotNuGitDir = Path.Combine(repository.Path, ".nugit");
    VisualStudioSolution sln = null;
    // TODO Add a `findsln` command to `nugit` and use that (or similar)
    if (Directory.Exists(dotNuGitDir)) sln = VisualStudioSolution.Find(dotNuGitDir);
    if (sln == null) sln = VisualStudioSolution.Find(repository.Path);
    if (sln == null) return;

    graph.Rule(
        graph.Command("sln-build"),
        _ => Sln(repository, sln, "build"));
    graph.Dependency(
        new Dependency(
            graph.Command("sln-build"),
            graph.Command("build")));

    graph.Rule(
        graph.Command("sln-rebuild"),
        _ => Sln(repository, sln, "rebuild"));
    graph.Dependency(
        new Dependency(
            graph.Command("sln-rebuild"),
            graph.Command("rebuild")));

    graph.Rule(
        graph.Command("sln-clean"),
        _ => Sln(repository, sln, "clean"));
    graph.Dependency(
        new Dependency(
            graph.Command("sln-clean"),
            graph.Command("clean")));
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

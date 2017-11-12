using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;
using MacroSln;
using System.IO;
using System.Linq;

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

    // Candidate .sln locations
    // TODO Use patterns e.g. **/*.sln once supported
    var slnPaths = graph.List("sln-paths", 
        Path.Combine(repository.Path, ".nugit", repository.Name + ".sln"),
        Path.Combine(repository.Path, repository.Name + ".sln"));

    // Candidate .sln files 
    var slnFiles = graph.FileSet("sln-files");
    graph.Dependency(slnPaths, slnFiles);

    // Path to selected .sln file
    var slnPath = graph.List("sln-path", _ => slnFiles.Files.Select(f => f.Path).Take(1));
    graph.Dependency(slnFiles, slnPath);

    var slnBuild = graph.Command("sln-build", () => Sln(repository, "build", slnPath.Values.SingleOrDefault()));
    graph.Dependency(slnPath, slnBuild);
    graph.Dependency(slnBuild, graph.Command("build"));

    var slnRebuild = graph.Command("sln-rebuild", () => Sln(repository, "rebuild", slnPath.Values.SingleOrDefault()));
    graph.Dependency(slnPath, slnRebuild);
    graph.Dependency(slnRebuild, graph.Command("rebuild"));

    var slnClean = graph.Command("sln-clean", () => Sln(repository, "clean", slnPath.Values.SingleOrDefault()));
    graph.Dependency(slnPath, slnClean);
    graph.Dependency(slnClean, graph.Command("clean"));
}


static void
Sln(ProduceRepository repository, string command, string slnPath)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.Required(command, nameof(command));
    if (slnPath == null) return;

    var verb = command[0].ToString().ToUpperInvariant() + command.Substring(1) + "ing";
    using (LogicalOperation.Start(verb + " " + repository.Name))
    {
        if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "sln", command, slnPath) != 0)
            throw new UserException("Failed");
    }
}


}
}

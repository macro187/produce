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

    // Selected .sln file
    var slnFile = graph.FileSet("sln-file");
    graph.Dependency(slnPath, slnFile);

    var slnBuild = graph.Command("sln-build", _ =>
        Sln(repository, "build", slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, slnBuild);
    graph.Dependency(slnBuild, graph.Command("build"));

    var slnRebuild = graph.Command("sln-rebuild", _ =>
        Sln(repository, "rebuild", slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, slnRebuild);
    graph.Dependency(slnRebuild, graph.Command("rebuild"));

    var slnClean = graph.Command("sln-clean", _ =>
        Sln(repository, "clean", slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, slnClean);
    graph.Dependency(slnClean, graph.Command("clean"));

    var slnPublishPath = graph.List("sln-publish-path", repository.GetWorkSubdirectory("sln-publish"));
    graph.Dependency(slnPublishPath, graph.Command("publish"));

    var slnPublish = graph.Command("sln-publish", _ =>
        Publish(repository, slnPath.Values.SingleOrDefault(), slnPublishPath.Values?.Single()));
    graph.Dependency(slnPublishPath, slnPublish);
    graph.Dependency(slnBuild, slnPublish);
    graph.Dependency(slnPublish, graph.Command("publish"));
}


static void
Sln(ProduceRepository repository, string command, string slnPath)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.Required(command, nameof(command));
    if (slnPath == null) return;

    var verb = command[0].ToString().ToUpperInvariant() + command.Substring(1) + "ing";
    using (LogicalOperation.Start(verb + " " + slnPath))
    {
        if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "sln", command, slnPath) != 0)
            throw new UserException("Failed");
    }
}


static void
Publish(ProduceRepository repository, string slnPath, string destinationPath)
{
    Guard.NotNull(repository, nameof(repository));
    if (slnPath == null) return;

    using (LogicalOperation.Start("Publishing " + slnPath + " to " + destinationPath))
    {
        if (Directory.Exists(destinationPath))
        using (LogicalOperation.Start("Deleting " + destinationPath))
            Directory.Delete(destinationPath, true);

        using (LogicalOperation.Start("Creating " + destinationPath))
            Directory.CreateDirectory(destinationPath);

        if (ProcessExtensions.Execute(
            true, true, repository.Path, "cmd", "/c",
            "sln", "publish", slnPath, destinationPath
        ) != 0)
            throw new UserException("Failed");
    }
}


}
}

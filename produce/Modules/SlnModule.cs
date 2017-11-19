using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;
using MacroSln;
using System.IO;
using System.Linq;
using System;

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

    // Solution paths
    // TODO Use patterns e.g. **/*.sln once supported
    var slnPaths = graph.List("sln-paths", 
        Path.Combine(repository.Path, ".nugit", repository.Name + ".sln"),
        Path.Combine(repository.Path, repository.Name + ".sln"));

    // Solution files 
    var slnFiles = graph.FileSet("sln-files");
    graph.Dependency(slnPaths, slnFiles);

    // Primary solution path
    var slnPath = graph.List("sln-path", _ => slnFiles.Files.Select(f => f.Path).Take(1));
    graph.Dependency(slnFiles, slnPath);

    // Primary solution file
    var slnFile = graph.FileSet("sln-file");
    graph.Dependency(slnPath, slnFile);

    // Project paths
    var slnProjPaths = graph.List("sln-proj-paths", _ =>
        slnFile.Files.Any()
            ? new VisualStudioSolution(slnFile.Files.Single().Path)
                .ProjectReferences
                .Where(r => r.TypeId == VisualStudioProjectTypeIds.CSharp)
                .Select(r => r.GetProject().Path)
            : Enumerable.Empty<string>());
    graph.Dependency(slnFile, slnProjPaths);

    // Project files
    var slnProjFiles = graph.FileSet("sln-proj-files");
    graph.Dependency(slnProjPaths, slnProjFiles);

    // Primary project path
    var slnProjPath = graph.List("sln-proj-path", _ =>
        slnProjFiles.Files
            .Select(f => f.Path)
            .Where(p =>
                string.Equals(
                    Path.GetFileNameWithoutExtension(p),
                    repository.Name,
                    StringComparison.OrdinalIgnoreCase))
            .Take(1));
    graph.Dependency(slnProjFiles, slnProjPath);

    // Primary project file
    var slnProjFile = graph.FileSet("sln-proj-file");
    graph.Dependency(slnProjPath, slnProjFile);

    // Publish destination
    var slnPublishPath = graph.List("sln-publish-path", repository.GetWorkSubdirectory("sln-publish"));

    var slnBuild = graph.Command("sln-build", _ =>
        Sln(repository, "build", slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, slnBuild);
    graph.Dependency(slnProjFiles, slnBuild);
    graph.Dependency(slnBuild, graph.Command("build"));

    var slnRebuild = graph.Command("sln-rebuild", _ =>
        Sln(repository, "rebuild", slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, slnRebuild);
    graph.Dependency(slnProjFiles, slnRebuild);
    graph.Dependency(slnRebuild, graph.Command("rebuild"));

    var slnClean = graph.Command("sln-clean", _ =>
        Sln(repository, "clean", slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, slnClean);
    graph.Dependency(slnClean, graph.Command("clean"));

    var slnPublish = graph.Command("sln-publish", _ =>
        Publish(
            repository,
            slnFile.Files.SingleOrDefault()?.Path,
            slnProjFile.Files.SingleOrDefault()?.Path,
            slnPublishPath.Values.Single()));
    graph.Dependency(slnFile, slnPublish);
    graph.Dependency(slnProjFile, slnPublish);
    graph.Dependency(slnPublishPath, slnPublish);
    graph.Dependency(slnPublish, graph.Command("publish"));
    graph.Dependency(slnPublishPath, graph.Command("publish"));
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
Publish(ProduceRepository repository, string slnPath, string projPath, string destinationPath)
{
    Guard.NotNull(repository, nameof(repository));

    if (Directory.Exists(destinationPath))
    using (LogicalOperation.Start("Deleting " + destinationPath))
        Directory.Delete(destinationPath, true);

    if (slnPath == null) return;
    if (projPath == null) return;

    using (LogicalOperation.Start("Creating " + destinationPath))
        Directory.CreateDirectory(destinationPath);

    using (LogicalOperation.Start("Publishing " + slnPath + " to " + destinationPath))
    {
        var projName = Path.GetFileNameWithoutExtension(projPath);
        if (ProcessExtensions.Execute(
            true, true, repository.Path, "cmd", "/c",
            "sln", "publish", slnPath, projName, destinationPath ) != 0)
            throw new UserException("Failed");
    }
}


}
}

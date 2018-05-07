using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;
using MacroSln;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;


namespace
produce
{


public class
DotnetModule : Module
{


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var build = graph.Command("build");
    var rebuild = graph.Command("rebuild");
    var clean = graph.Command("clean");
    var distfiles = graph.Command("distfiles");

    // Solution paths
    // TODO Use patterns e.g. **/*.sln once supported
    var dotnetSlnPaths = graph.List("dotnet-sln-paths",
        Path.Combine(repository.Path, ".nugit", repository.Name + ".sln"),
        Path.Combine(repository.Path, repository.Name + ".sln"));

    // Solution files
    var dotnetSlnFiles = graph.FileSet("dotnet-sln-files");
    graph.Dependency(dotnetSlnPaths, dotnetSlnFiles);

    // Primary solution path
    var dotnetSlnPath = graph.List("dotnet-sln-path", _ => dotnetSlnFiles.Files.Select(f => f.Path).Take(1));
    graph.Dependency(dotnetSlnFiles, dotnetSlnPath);

    // Primary solution file
    var dotnetSlnFile = graph.FileSet("dotnet-sln-file");
    graph.Dependency(dotnetSlnPath, dotnetSlnFile);

    // Project paths
    var dotnetProjPaths = graph.List("dotnet-proj-paths", _ =>
        dotnetSlnFile.Files.Any()
            ? new VisualStudioSolution(dotnetSlnFile.Files.Single().Path)
                .ProjectReferences
                .Where(r =>
                    r.TypeId == VisualStudioProjectTypeIds.CSharp ||
                    r.TypeId == VisualStudioProjectTypeIds.CSharpNew)
                .Select(r => r.GetProject().Path)
            : Enumerable.Empty<string>());
    graph.Dependency(dotnetSlnFile, dotnetProjPaths);

    // Project files
    var dotnetProjFiles = graph.FileSet("dotnet-proj-files");
    graph.Dependency(dotnetProjPaths, dotnetProjFiles);

    // Primary project path
    var dotnetProjPath = graph.List("dotnet-proj-path", _ =>
        dotnetProjFiles.Files
            .Select(f => f.Path)
            .Where(p =>
                string.Equals(
                    Path.GetFileNameWithoutExtension(p),
                    repository.Name,
                    StringComparison.OrdinalIgnoreCase))
            .Take(1));
    graph.Dependency(dotnetProjFiles, dotnetProjPath);

    // Primary project file
    var dotnetProjFile = graph.FileSet("dotnet-proj-file");
    graph.Dependency(dotnetProjPath, dotnetProjFile);

    var dotnetBuild = graph.Command("dotnet-build", _ =>
        Dotnet(repository, "build", dotnetSlnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(dotnetSlnFile, dotnetBuild);
    graph.Dependency(dotnetProjFiles, dotnetBuild);
    graph.Dependency(dotnetBuild, build);

    var dotnetRebuild = graph.Command("dotnet-rebuild", _ => {
        Dotnet(repository, "clean", dotnetSlnFile.Files.SingleOrDefault()?.Path);
        Dotnet(repository, "build", dotnetSlnFile.Files.SingleOrDefault()?.Path);
    });
    graph.Dependency(dotnetSlnFile, dotnetRebuild);
    graph.Dependency(dotnetProjFiles, dotnetRebuild);
    graph.Dependency(dotnetRebuild, rebuild);

    var dotnetClean = graph.Command("dotnet-clean", _ =>
        Dotnet(repository, "clean", dotnetSlnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(dotnetSlnFile, dotnetClean);
    graph.Dependency(dotnetClean, clean);

    var dotnetDistfilesPath = graph.List("dotnet-distfiles-path", _ =>
        dotnetProjPath.Values
            .Select(p =>
                Path.GetFullPath(
                    Path.Combine(
                        Path.GetDirectoryName(p),
                        "bin", "Debug", "net461", "publish"))));
    graph.Dependency(dotnetProjPath, dotnetDistfilesPath);

    var dotnetDistfiles = graph.Command("dotnet-distfiles", _ =>
        Dotnet(
            repository,
            "publish",
            dotnetProjFile.Files.SingleOrDefault()?.Path,
            "net461"));
    graph.Dependency(dotnetSlnFile, dotnetDistfiles);
    graph.Dependency(dotnetProjFile, dotnetDistfiles);
    graph.Dependency(dotnetDistfilesPath, dotnetDistfiles);
    graph.Dependency(dotnetDistfiles, distfiles);
    graph.Dependency(dotnetDistfilesPath, distfiles);
}


static void
Dotnet(ProduceRepository repository, string command, string projPath, string framework = null)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.Required(command, nameof(command));
    if (projPath == null) return;

    var verb = command[0].ToString().ToUpperInvariant() + command.Substring(1) + "ing";

    var args = new List<string>() {
        "/c", "dotnet", command, projPath
    };

    if (!string.IsNullOrWhiteSpace(framework))
    {
        args.Add("-f");
        args.Add(framework);
    }

    using (LogicalOperation.Start(verb + " " + projPath))
    {
        if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", args.ToArray()) != 0)
            throw new UserException("Failed");
    }
}


}
}

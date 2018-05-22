using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;
using MacroSln;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using MacroIO;

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
    var dotnetSlnPath = graph.List("dotnet-sln-path", _ =>
        dotnetSlnFiles.Files.Take(1)
            .Select(f => f.Path));
    graph.Dependency(dotnetSlnFiles, dotnetSlnPath);

    // Primary solution file
    var dotnetSlnFile = graph.FileSet("dotnet-sln-file");
    graph.Dependency(dotnetSlnPath, dotnetSlnFile);

    // Project paths
    var dotnetProjPaths = graph.List("dotnet-proj-paths", _ =>
        dotnetSlnFile.Files.Take(1)
            .Select(f => f.Path)
            .Select(p => new VisualStudioSolution(p))
            .SelectMany(sln => FindLocalBuildableProjects(repository, sln))
            .Select(r => r.AbsoluteLocation));
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
        Build(repository, dotnetSlnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(dotnetProjFiles, dotnetBuild);
    graph.Dependency(dotnetBuild, build);

    var dotnetClean = graph.Command("dotnet-clean", _ =>
        Clean(repository, dotnetSlnFile.Files.SingleOrDefault()?.Path));
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
            "-f", "net461"));
    graph.Dependency(dotnetSlnFile, dotnetDistfiles);
    graph.Dependency(dotnetProjFile, dotnetDistfiles);
    graph.Dependency(dotnetDistfilesPath, dotnetDistfiles);
    graph.Dependency(dotnetDistfiles, distfiles);
    graph.Dependency(dotnetDistfilesPath, distfiles);
}


static IEnumerable<VisualStudioSolutionProjectReference>
FindLocalBuildableProjects(ProduceRepository repository, VisualStudioSolution sln) =>
    sln.ProjectReferences
        .Where(r =>
            r.TypeId == VisualStudioProjectTypeIds.CSharp ||
            r.TypeId == VisualStudioProjectTypeIds.CSharpNew)
        .Where(r => PathExtensions.IsDescendantOf(r.AbsoluteLocation, repository.Path))
        .ToList();


static void
Build(ProduceRepository repository, string slnPath)
{
    if (slnPath == null) return;

    var sln = new VisualStudioSolution(slnPath);

    var projs = FindLocalBuildableProjects(repository, sln);

    var frameworksByProj =
        projs.ToDictionary(p => p, p => p.GetProject().AllTargetFrameworks.ToList());

    var allFrameworks =
        frameworksByProj.Values
            .SelectMany(list => list)
            .Distinct();

    foreach (var framework in allFrameworks)
    {
        var projsTargetingThisFramework = projs.Where(p => frameworksByProj[p].Contains(framework)).ToList();
        Build(repository, sln, projsTargetingThisFramework, framework);
    }
}


static void
Build(
    ProduceRepository repository,
    VisualStudioSolution sln,
    IList<VisualStudioSolutionProjectReference> projs,
    string framework)
{
    var msbuildArgs = new List<string>() {
        "/nr:false", $"/p:TargetFramework={framework}"
    };

    msbuildArgs.AddRange(projs.Select(p => $"/t:{p.MSBuildTargetName}:Publish"));

    using (LogicalOperation.Start($"Building for {framework}"))
        Dotnet(repository, "msbuild", sln.Path, msbuildArgs.ToArray());
}


static void
Clean(ProduceRepository repository, string slnPath)
{
    if (slnPath == null) return;

    var sln = new VisualStudioSolution(slnPath);

    var projs = FindLocalBuildableProjects(repository, sln);

    var msbuildArgs = new List<string>() {
        "/nr:false",
    };

    msbuildArgs.AddRange(projs.Select(p => $"/t:{p.MSBuildTargetName}:Clean"));

    Dotnet(repository, "msbuild", sln.Path, msbuildArgs.ToArray());
}


static void
Dotnet(ProduceRepository repository, string command, string projPath, params string[] args)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.Required(command, nameof(command));
    if (projPath == null) return;

    var cmdArgs = new List<string>() {
        "/c", "dotnet", command, projPath
    };

    cmdArgs.AddRange(args);

    if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", cmdArgs.ToArray()) != 0)
        throw new UserException("Failed");
}


}
}

using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;
using MacroSln;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using MacroIO;
using MacroSystem;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace
produce
{


public class
DotnetModule : Module
{


static
DotnetModule()
{
    CanBuildNetFramework = false;
    BuildNetFrameworkUsingMSBuild = false;

    //
    // On Windows we can always build for .NET Frameworks
    //
    if (EnvironmentExtensions.IsWindows)
    {
        CanBuildNetFramework = true;
    }

    //
    // On UNIX we can build for .NET Frameworks using Mono, which we assume is available if a standalone `msbuild` is
    // present
    //
    else
    {
        if (ProcessExtensions.ExecuteAny(false, false, null, "msbuild", "/version") == 0)
        {
            CanBuildNetFramework = true;
            BuildNetFrameworkUsingMSBuild = true;
        }
    }
}


static bool CanBuildNetFramework;
static bool BuildNetFrameworkUsingMSBuild;


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var restore = graph.Command("restore");
    var build = graph.Command("build");
    var clean = graph.Command("clean");
    var package = graph.Command("package");
    var publish = graph.Command("publish");

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

    // Path to .nupkg output directory
    var nupkgDir = repository.GetWorkSubdirectory("nupkg");

    // Path to .nupkg file
    var nupkgFilePath = graph.List("dotnet-nupkg-path", _ => {
        if (!Directory.Exists(nupkgDir)) return new string[0];
        return
            Directory.EnumerateFiles(nupkgDir, "*.nupkg")
                .Select(f => Path.Combine(nupkgDir, f))
                .Take(1);
    });

    // .nupkg file
    var nupkgFile = graph.FileSet("dotnet-nupkg-file");
    graph.Dependency(nupkgFilePath, nupkgFile);

    // Restore
    var dotnetRestore = graph.Command("dotnet-restore", _ =>
        Restore(repository, dotnetSlnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(dotnetProjFiles, dotnetRestore);  // Should be all projects in sln, not just local?
    graph.Dependency(dotnetRestore, restore);

    // Build
    var dotnetBuild = graph.Command("dotnet-build", _ =>
        Build(repository, dotnetSlnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(dotnetProjFiles, dotnetBuild);
    graph.Dependency(dotnetBuild, build);

    // Pack
    var dotnetPack = graph.Command("dotnet-pack", _ =>
        Pack(
            repository,
            dotnetSlnFile.Files.SingleOrDefault()?.Path,
            dotnetProjFile.Files.SingleOrDefault()?.Path,
            nupkgDir));
    graph.Dependency(dotnetProjFile, dotnetPack);
    graph.Dependency(dotnetPack, package);
    
    // NuGet Push
    var dotnetNugetPush = graph.Command("dotnet-nuget-push", _ =>
        NugetPush(repository, nupkgFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(nupkgFile, dotnetNugetPush);
    graph.Dependency(dotnetNugetPush, publish);

    // Clean
    var dotnetClean = graph.Command("dotnet-clean", _ =>
        Clean(repository, dotnetSlnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(dotnetSlnFile, dotnetClean);
    graph.Dependency(dotnetClean, clean);
}


static void
Restore(ProduceRepository repository, string slnPath)
{
    if (slnPath == null) return;

    var sln = new VisualStudioSolution(slnPath);

    using (LogicalOperation.Start("Restoring NuGet packages"))
        Dotnet(repository, "restore", sln);
}


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
    var properties = new Dictionary<string,String>() {
        { "TargetFramework", framework },
    };

    var targets = projs.Select(p => $"{p.MSBuildTargetName}:Publish");

    using (LogicalOperation.Start($"Building .NET for {framework}"))
    {
        var isNetFramework = Regex.IsMatch(framework, @"^net\d+$");

        if (isNetFramework && !CanBuildNetFramework)
        {
            Trace.TraceInformation("This system can't build for .NET Framework");
            return;
        }

        if (isNetFramework && BuildNetFrameworkUsingMSBuild)
        {
            MSBuild(repository, sln, properties, targets);
            return;
        }

        DotnetMSBuild(repository, sln, properties, targets);
    }
}


static void
Pack(ProduceRepository repository, string slnPath, string projPath, string nupkgDir)
{
    if (slnPath == null) return;
    if (projPath == null) return;
    Guard.Required(nupkgDir, nameof(nupkgDir));
    if (!Path.IsPathRooted(nupkgDir))
        throw new ArgumentException("nupkgDir must be an absolute path", nameof(nupkgDir));

    var sln = new VisualStudioSolution(slnPath);
    var proj = sln.ProjectReferences.Single(p => p.AbsoluteLocation == projPath);

    var properties = new Dictionary<string,string>() {
        { "PackageOutputPath", nupkgDir },
    };

    var targets = new[]{ $"{proj.MSBuildTargetName}:Pack" };

    using (LogicalOperation.Start("Building nupkg"))
    {
        if (Directory.Exists(nupkgDir)) Directory.Delete(nupkgDir, true);
        Directory.CreateDirectory(nupkgDir);
        DotnetMSBuild(repository, sln, properties, targets);
    }
}


static void
NugetPush(ProduceRepository repository, string nupkgPath)
{
    Guard.NotNull(repository, nameof(repository));
    if (nupkgPath == null) return;

    using (LogicalOperation.Start("Publishing nupkg"))
    {
        Dotnet(repository, "nuget", "push", nupkgPath, "-s", "https://api.nuget.org/v3/index.json");
    }
}


static void
Clean(ProduceRepository repository, string slnPath)
{
    if (slnPath == null) return;

    var sln = new VisualStudioSolution(slnPath);
    var projs = FindLocalBuildableProjects(repository, sln);

    var targets = projs.Select(p => $"{p.MSBuildTargetName}:Clean");

    using (LogicalOperation.Start("Cleaning .NET artifacts"))
        DotnetMSBuild(repository, sln, targets);
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
DotnetMSBuild(
    ProduceRepository repository,
    VisualStudioSolution sln,
    string target)
{
    DotnetMSBuild(repository, sln, new []{ target });
}


static void
DotnetMSBuild(
    ProduceRepository repository,
    VisualStudioSolution sln,
    IEnumerable<string> targets)
{
    var properties = new Dictionary<string,string>();
    DotnetMSBuild(repository, sln, properties, targets);
}


static void
DotnetMSBuild(
    ProduceRepository repository,
    VisualStudioSolution sln,
    IDictionary<string,string> properties,
    IEnumerable<string> targets)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(sln, nameof(sln));
    Guard.NotNull(properties, nameof(properties));
    Guard.NotNull(targets, nameof(targets));

    var args = new List<string>() {
        "/nr:false",
    };
    args.AddRange(properties.Select(p => $"/p:{p.Key}=\"{p.Value}\""));
    args.AddRange(targets.Select(t => $"/t:{t}"));

    Dotnet(repository, "msbuild", sln, args.ToArray());
}


static void
MSBuild(
    ProduceRepository repository,
    VisualStudioSolution sln,
    IDictionary<string,string> properties,
    IEnumerable<string> targets)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(sln, nameof(sln));
    Guard.NotNull(properties, nameof(properties));
    Guard.NotNull(targets, nameof(targets));

    var args = new List<string>() {
        "/nr:false",
        "/v:m",
    };
    args.AddRange(properties.Select(p => $"/p:{p.Key}=\"{p.Value}\""));
    args.AddRange(targets.Select(t => $"/t:{t}"));

    if (ProcessExtensions.ExecuteAny(true, true, repository.Path, "msbuild", args.ToArray()) != 0)
        throw new UserException("Failed");
}


static void
Dotnet(ProduceRepository repository, string command, VisualStudioSolution sln, params string[] args)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.Required(command, nameof(command));
    Guard.NotNull(sln, nameof(sln));

    var dotnetArgs = new List<string>() {
        command, sln.Path
    };

    dotnetArgs.AddRange(args);

    Dotnet(repository, dotnetArgs.ToArray());
}


static void
Dotnet(ProduceRepository repository, params string[] args)
{
    Guard.NotNull(repository, nameof(repository));

    if (ProcessExtensions.ExecuteAny(true, true, repository.Path, "dotnet", args.ToArray()) != 0)
        throw new UserException("Failed");
}


}
}

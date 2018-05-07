using static System.FormattableString;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;


namespace
produce
{


public class
NuGetModule : Module
{


const string
SOURCE = "https://www.nuget.org/api/v2/package";


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var dotnetSlnFile = graph.FileSet("dotnet-sln-file");
    var dotnetProjFile = graph.FileSet("dotnet-proj-file");
    var restore = graph.Command("restore");
    var update = graph.Command("update");
    var dist = graph.Command("dist");
    var publish = graph.Command("publish");

    var nuspecPath = graph.List("nuget-nuspec-path", _ =>
        dotnetProjFile.Files
            .Select(p => Path.ChangeExtension(dotnetProjFile.Files.Single().Path, ".nuspec"))
            .Take(1));
    graph.Dependency(dotnetProjFile, nuspecPath);

    var nuspecFile = graph.FileSet("nuget-nuspec-file");
    graph.Dependency(nuspecPath, nuspecFile);

    var nugetRestore = graph.Command("nuget-restore", _ =>
        Restore(repository, dotnetSlnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(dotnetSlnFile, nugetRestore);
    graph.Dependency(nugetRestore, restore);

    var nugetUpdate = graph.Command("nuget-update", _ =>
        Update(repository, dotnetSlnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(dotnetSlnFile, nugetUpdate);
    graph.Dependency(nugetUpdate, update);

    var nupkgDir = graph.List("nuget-nupkg-dir", repository.GetWorkSubdirectory("nuget-nupkg"));

    var nugetPack = graph.Command("nuget-pack", _ =>
        Pack(
            repository,
            dotnetProjFile.Files.SingleOrDefault()?.Path,
            nuspecFile.Files.SingleOrDefault()?.Path,
            nupkgDir.Values.Single()));
    graph.Dependency(nupkgDir, nugetPack);
    graph.Dependency(dotnetProjFile, nugetPack);
    graph.Dependency(nuspecFile, nugetPack);
    graph.Dependency(nugetPack, dist);

    var nupkgPath = graph.List("nuget-nupkg-path", _ =>
        nupkgDir.Values
            .Where(p => Directory.Exists(p))
            .SelectMany(p => Directory.EnumerateFiles(p, "*.nupkg"))
            .Take(1));
    graph.Dependency(nugetPack, nupkgPath);

    var nupkgFile = graph.FileSet("nuget-nupkg-file");
    graph.Dependency(nupkgPath, nupkgFile);

    var nugetPush = graph.Command("nuget-push", _ =>
        Push(repository, nupkgFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(nupkgFile, nugetPush);
    graph.Dependency(nugetPush, publish);
}


static void
Restore(ProduceRepository repository, string slnPath)
{
    if (slnPath == null) return;
    if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "nuget", "restore", slnPath) != 0)
        throw new UserException("nuget restore failed");
}


static void
Update(ProduceRepository repository, string slnPath)
{
    if (slnPath == null) return;
    if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "nuget", "update", slnPath) != 0)
        throw new UserException("nuget update failed");
}


static void
Pack(ProduceRepository repository, string projPath, string nuspecPath, string outputDir)
{
    Guard.NotNull(repository, nameof(repository));
    if (projPath == null) return;
    if (nuspecPath == null) return;
    Guard.Required(outputDir, nameof(outputDir));

    if (Directory.Exists(outputDir))
    using (LogicalOperation.Start("Deleting " + outputDir))
        Directory.Delete(outputDir, true);

    using (LogicalOperation.Start("Creating " + outputDir))
        Directory.CreateDirectory(outputDir);

    using (LogicalOperation.Start("Building .nupkg"))
    {
        if (
            ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c",
                "nuget", "pack", projPath, "-outputdirectory", outputDir)
            != 0
        )
            throw new UserException("nuget pack failed");
    }
}


static void
Push(ProduceRepository repository, string nupkgPath)
{
    Guard.NotNull(repository, nameof(repository));
    if (nupkgPath == null) return;

    using (LogicalOperation.Start(Invariant($"Pushing {nupkgPath} to {SOURCE}")))
    {
        Trace.TraceInformation(Invariant($"nuget push {nupkgPath} -Source {SOURCE}"));
        /*
        if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c",
            "nuget", "push", nupkgPath, "-Source", SOURCE)
            != 0
        )
            throw new UserException("nuget push failed");
        */
    }
}


}
}

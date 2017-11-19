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

    var slnFile = graph.FileSet("sln-file");
    var slnProjFile = graph.FileSet("sln-proj-file");
    var restore = graph.Command("restore");
    var update = graph.Command("update");
    var publish = graph.Command("publish");

    var nuspecPath = graph.List("nuget-nuspec-path", _ =>
        slnProjFile.Files
            .Select(p => Path.ChangeExtension(slnProjFile.Files.Single().Path, ".nuspec"))
            .Take(1));
    graph.Dependency(slnProjFile, nuspecPath);

    var nuspecFile = graph.FileSet("nuget-nuspec-file");
    graph.Dependency(nuspecPath, nuspecFile);

    var nugetRestore = graph.Command("nuget-restore", _ =>
        Restore(repository, slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, nugetRestore);
    graph.Dependency(nugetRestore, restore);

    var nugetUpdate = graph.Command("nuget-update", _ =>
        Update(repository, slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, nugetUpdate);
    graph.Dependency(nugetUpdate, update);

    var nupkgDir = graph.List("nuget-nupkg-dir", repository.GetWorkSubdirectory("nuget-nupkg"));

    var nugetPack = graph.Command("nuget-pack", _ =>
        Pack(
            repository,
            slnProjFile.Files.SingleOrDefault()?.Path,
            nuspecFile.Files.SingleOrDefault()?.Path,
            nupkgDir.Values.Single()));
    graph.Dependency(nupkgDir, nugetPack);
    graph.Dependency(slnProjFile, nugetPack);
    graph.Dependency(nuspecFile, nugetPack);

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

    using (LogicalOperation.Start($"Pushing {nupkgPath} to {SOURCE}"))
    {
        Trace.TraceInformation($"nuget push {nupkgPath} -Source {SOURCE}");
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

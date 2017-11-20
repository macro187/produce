using static System.FormattableString;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MacroDiagnostics;
using MacroGuards;


namespace
produce
{


public class
GlobalModule : Module
{


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    graph.Command("restore");
    graph.Command("update");
    graph.Command("clean", t => Clean(repository));
    graph.Command("build");
    graph.Command("rebuild");
    graph.Command("publish");

    var distfiles = graph.Command("distfiles", t =>
        DistFiles(
            repository,
            graph.RequiredBy(t).OfType<ListTarget>().SelectMany(l => l.Values)));

    var dist = graph.Command("dist");
    graph.Dependency(distfiles, dist);
}


static void
DistFiles(ProduceRepository repository, IEnumerable<string> sourceDirs)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(sourceDirs, nameof(sourceDirs));

    var destDir = repository.GetWorkSubdirectory("distfiles");

    if (Directory.Exists(destDir))
    using (LogicalOperation.Start("Deleting " + destDir))
        Directory.Delete(destDir, true);

    if (!sourceDirs.Where(p => Directory.Exists(p)).Any()) return;

    using (LogicalOperation.Start("Creating " + destDir))
        Directory.CreateDirectory(destDir);

    foreach (var sourceDir in sourceDirs.Where(d => Directory.Exists(d)))
    using (LogicalOperation.Start("Copying distributable files from " + sourceDir))
    foreach (var sourceFile in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
    {
        var localFile = sourceFile.Substring(sourceDir.Length + 1);
        var destFile = Path.Combine(destDir, localFile);
        Trace.TraceInformation(destFile);
        File.Copy(sourceFile, destFile);
    }
}


static void
Clean(ProduceRepository repository)
{
    Guard.NotNull(repository, nameof(repository));
    var workDir = repository.WorkDirectory;
    if (Directory.Exists(workDir))
    using (LogicalOperation.Start("Deleting " + workDir))
        Directory.Delete(workDir, true);
}


}
}

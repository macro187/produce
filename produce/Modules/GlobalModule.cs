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
    graph.Command("clean");
    graph.Command("build");
    graph.Command("rebuild");
    graph.Command("publish", t =>
        Publish(
            repository,
            graph.RequiredBy(t).OfType<ListTarget>().SelectMany(l => l.Values)));
}


static void
Publish(ProduceRepository repository, IEnumerable<string> sourceDirs)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(sourceDirs, nameof(sourceDirs));

    if (!sourceDirs.Any()) return;

    using (LogicalOperation.Start("Publishing " + repository.Name))
    {
        var destDir = repository.GetWorkDirectory("publish");

        if (Directory.Exists(destDir))
        using (LogicalOperation.Start("Deleting " + destDir))
            Directory.Delete(destDir, true);

        using (LogicalOperation.Start("Creating " + destDir))
            Directory.CreateDirectory(destDir);

        foreach (var sourceDir in sourceDirs)
        foreach (var sourceFile in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var localFile = sourceFile.Substring(sourceDir.Length + 1);
            var destFile = Path.Combine(destDir, localFile);
            Trace.TraceInformation(Invariant($"{sourceFile} -> {destFile}"));
            File.Copy(sourceFile, destFile);
        }
    }
}


}
}

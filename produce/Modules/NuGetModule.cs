﻿using System.Linq;
using MacroDiagnostics;
using MacroExceptions;
using MacroGuards;


namespace
produce
{


public class
NuGetModule : Module
{


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var slnFile = graph.FileSet("sln-file");
    var restore = graph.Command("restore");
    var update = graph.Command("update");

    var nugetRestore = graph.Command("nuget-restore", _ =>
        Restore(repository, slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, nugetRestore);
    graph.Dependency(nugetRestore, restore);

    var nugetUpdate = graph.Command("nuget-update", _ =>
        Update(repository, slnFile.Files.SingleOrDefault()?.Path));
    graph.Dependency(slnFile, nugetUpdate);
    graph.Dependency(nugetUpdate, update);
}


static void
Restore(ProduceRepository repository, string slnPath)
{
    if (slnPath == null) return;
    if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "nuget", "restore", slnPath) != 0)
        throw new UserException("nuget failed");
}


static void
Update(ProduceRepository repository, string slnPath)
{
    if (slnPath == null) return;
    if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "nuget", "update", slnPath) != 0)
        throw new UserException("nuget failed");
}


}
}

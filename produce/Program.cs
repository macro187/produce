using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MacroConsole;
using MacroExceptions;
using MacroGit;
using MacroDiagnostics;


namespace
produce
{


static class
Program
{


static ProduceWorkspace
CurrentWorkspace;


static ProduceRepository
CurrentRepository;


static IEnumerable<Plugin>
Plugins = new Plugin[] {
    new ProgramsPlugin(),
    new NuGitPlugin(),
    new DotNetPlugin(),
};


static int
Main(string[] args)
{
    Trace.Listeners.Add(new ConsoleApplicationTraceListener());

    try
    {
        ShadowCopier.ShadowCopy();
        return Main2(new Queue<string>(args));
    }
    catch (UserException ue)
    {
        Trace.TraceError(ue.Message);
        return 1;
    }
    catch (Exception e)
    {
        Trace.TraceError("Internal produce error");
        Trace.TraceError(ExceptionExtensions.Format(e));
        return 1;
    }
}


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase",
    Justification = "Commands are spelled lowercase")]
static int
Main2(Queue<string> args)
{
    FindCurrentWorkspaceAndRepository();

    if (args.Count == 0) throw new UserException("Expected <command>");
    var command = args.Dequeue().ToLowerInvariant();
    if (args.Count > 0) throw new UserException("Unexpected <arguments>");

    if (CurrentRepository != null)
    {
        RunCommand(CurrentRepository, command);
    }
    else
    {
        RunCommand(CurrentWorkspace, command);
    }

    return 0;
}


static void
RunCommand(ProduceWorkspace workspace, string command)
{
    var graph = new Graph();
    foreach (var plugin in Plugins) plugin.DetectWorkspaceRules(workspace, graph);

    var target = graph.FindCommandTarget(command);
    if (target != null)
    {
        Builder.Build(graph, target);
        return;
    }

    foreach (var repository in workspace.FindRepositories()) RunCommand(repository, command);
}


static void
RunCommand(ProduceRepository repository, string command)
{
    using (LogicalOperation.Start(FormattableString.Invariant($"Running {command} command for {repository.Name}")))
    {
        var graph = new Graph();
        foreach (var plugin in Plugins) plugin.DetectRepositoryRules(repository, graph);

        var target = graph.FindCommandTarget(command);
        if (target == null)
        {
            Trace.TraceInformation(FormattableString.Invariant($"No {command} command"));
            return;
        }

        Builder.Build(graph, target);
    }
}


static void
FindCurrentWorkspaceAndRepository()
{
    var gitRepo = GitRepository.FindContainingRepository(Environment.CurrentDirectory);
    if (gitRepo != null)
    {
        CurrentWorkspace = new ProduceWorkspace(Path.GetDirectoryName(gitRepo.Path));
        CurrentRepository = CurrentWorkspace.GetRepository(new GitRepositoryName(Path.GetFileName(gitRepo.Path)));
    }
    else
    {
        CurrentWorkspace = new ProduceWorkspace(Path.GetFullPath(Environment.CurrentDirectory));
        CurrentRepository = null;
    }
}


}
}

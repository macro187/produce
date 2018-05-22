using System;
using static System.FormattableString;
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


static IEnumerable<Module>
Modules = new Module[] {
    new GlobalModule(),
    new DotProduceModule(),
    new ProgramsModule(),
    new DotnetModule(),
    new NuGitModule(),
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

    while (args.Count > 0 && args.Peek().StartsWith("--", StringComparison.Ordinal))
    {
        var s = args.Dequeue();
        switch (s)
        {
            case "--tracegraph":
                Tracer.Enabled = true;
                break;
            default:
                throw new UserException(Invariant($"Unrecognised switch {s}"));
        }
    }

    var commands = new List<string>();
    while (args.Count > 0)
    {
        var command = args.Dequeue().ToLowerInvariant();
        if (command.StartsWith("-", StringComparison.Ordinal)) throw new UserException("Expected <command>");
        commands.Add(command);
    }
    if (commands.Count == 0) throw new UserException("Expected <command>");

    if (CurrentRepository != null)
    {
        RunCommands(CurrentRepository, commands);
    }
    else
    {
        RunCommands(CurrentWorkspace, commands);
    }

    return 0;
}


static void
RunCommands(ProduceWorkspace workspace, IList<string> commands)
{
    foreach (var command in commands)
        foreach (var module in Modules)
            module.PreWorkspace(workspace, command);

    foreach (var repository in workspace.FindRepositories())
        RunCommands(repository, commands);

    foreach (var command in commands)
        foreach (var module in Modules)
            module.PostWorkspace(workspace, command);
}


static void
RunCommands(ProduceRepository repository, IList<string> commands)
{
    foreach (var command in commands)
        RunCommand(repository, command);
}


static void
RunCommand(ProduceRepository repository, string command)
{
    using (LogicalOperation.Start(Invariant($"Running {command} command for {repository.Name}")))
    {
        var graph = new Graph(repository.Workspace);
        foreach (var module in Modules) module.Attach(repository, graph);

        var target = graph.FindCommand(command);
        if (target == null)
        {
            Trace.TraceInformation(Invariant($"No {command} command"));
            return;
        }

        new Builder(graph).Build(target);
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

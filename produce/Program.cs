using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MacroConsole;
using MacroDiagnostics;
using MacroExceptions;
using MacroGit;
using MacroSln;


namespace
produce
{


static class
Program
{


static ProduceWorkspace
workspace;


static ProduceRepository
repository;


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
    FindWorkspaceAndRepository();

    if (args.Count == 0) throw new UserException("Expected <command>");
    var command = args.Dequeue().ToLowerInvariant();

    switch (command)
    {
        case "build":
        case "rebuild":
        case "clean":
            Sln(command, args);
            break;
        case "programs":
            Programs(args);
            break;
        default:
            throw new UserException("Unrecognised <command>");
    }

    return 0;
}


static void
Sln(string command, Queue<string> args)
{
    if (args.Count > 0) throw UnexpectedArgumentsException();

    var verb = command[0].ToString().ToUpperInvariant() + command.Substring(1) + "ing";

    var repositories = repository != null ? new[] { repository } : workspace.FindRepositories();
    foreach (var repo in repositories)
    using (LogicalOperation.Start(verb + " " + repo.Name))
    {
        // TODO Find .nugit/*.sln
        var sln = VisualStudioSolution.Find(repo.Path);
        if (sln == null)
        {
            Trace.TraceInformation("No .sln found");
            return;
        }

        if (ProcessExtensions.Execute(true, true, repo.Path, "cmd", "/c", "sln", command, sln.Path) != 0)
            throw new UserException(string.Concat(command, " ", repo.Path, " failed"));
    }
}


static void
Programs(Queue<string> args)
{
    if (args.Count > 0) throw UnexpectedArgumentsException();

    if (repository != null)
    {
        ProgramWrapperGenerator.GenerateProgramWrappers(repository);
    }
    else
    {
        ProgramWrapperGenerator.GenerateProgramWrappers(workspace);
    }
}


static void
FindWorkspaceAndRepository()
{
    var gitRepo = GitRepository.FindContainingRepository(Environment.CurrentDirectory);
    if (gitRepo != null)
    {
        workspace = new ProduceWorkspace(Path.GetDirectoryName(gitRepo.Path));
        repository = workspace.GetRepository(new GitRepositoryName(Path.GetFileName(gitRepo.Path)));
    }
    else
    {
        workspace = new ProduceWorkspace(Path.GetFullPath(Environment.CurrentDirectory));
        repository = null;
    }
}


static Exception
UnexpectedArgumentsException()
{
    return new UserException("Unexpected <arguments>");
}


}
}

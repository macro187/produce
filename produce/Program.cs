using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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


static DirectoryInfo
workspace;


static GitRepository
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


static int
Main2(Queue<string> args)
{
    if (args.Count == 0) throw new UserException("Expected <command>");
    var command = args.Dequeue();

    FindWorkspaceAndRepository();

    IEnumerable<GitRepository> repos =
        repository != null
            ? new[] { repository }
            : FindRepositories();

    foreach (var repo in repos)
    {
        // TODO Proceed with next repo on failure, tally and final exit code at the end
        Execute(command, repo);
    }

    return 0;
}


static void
FindWorkspaceAndRepository()
{
    repository = GitRepository.FindContainingRepository(Environment.CurrentDirectory);
    var workspacePath =
        repository != null
            ? Path.GetDirectoryName(repository.Path)
            : Path.GetFullPath(Environment.CurrentDirectory);
    workspace = new DirectoryInfo(workspacePath);
}


static IEnumerable<GitRepository>
FindRepositories()
{
    return workspace.GetDirectories()
        .Where(d => GitRepository.IsRepository(d.FullName))
        .Select(d => new GitRepository(d.FullName));
}


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase",
    Justification = "Commands are spelled lowercase")]
static void
Execute(string command, GitRepository repository)
{
    using (LogicalOperation.Start(string.Concat(command, " ", repository.Path)))
    {
        // TODO Find .nugit/*.sln
        var sln = VisualStudioSolution.Find(repository.Path);
        if (sln == null)
        {
            Trace.TraceInformation("No .sln found");
            return;
        }

        switch (command.ToLowerInvariant())
        {
            case "build":
            case "rebuild":
            case "clean":
                if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "sln", command, sln.Path) != 0)
                    throw new UserException(string.Concat(command, " ", repository.Path, " failed"));
                break;
            default:
                Trace.TraceInformation(FormattableString.Invariant($"Don't know how to ${command} this repository"));
                return;
        }
    }
}


}
}

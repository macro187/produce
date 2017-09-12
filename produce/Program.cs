using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MacroConsole;
using MacroExceptions;
using MacroGit;


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


static IEnumerable<IPlugin>
Plugins = new IPlugin[] {
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
    var rulesByCommand = new Dictionary<string, Rule>();
    foreach (var plugin in Plugins)
    foreach (var rule in plugin.DetectWorkspaceRules(workspace))
        rulesByCommand.Add(rule.Command, rule);

    Rule ruleToRun;
    rulesByCommand.TryGetValue(command, out ruleToRun);
    if (ruleToRun != null)
    {
        ruleToRun.Action();
        return;
    }

    foreach (var repository in workspace.FindRepositories()) RunCommand(repository, command);
}


static void
RunCommand(ProduceRepository repository, string command)
{
    var rulesByCommand = new Dictionary<string, Rule>();

    foreach (var plugin in Plugins)
    foreach (var rule in plugin.DetectRepositoryRules(repository))
        rulesByCommand.Add(rule.Command, rule);

    Rule ruleToRun;
    if (!rulesByCommand.TryGetValue(command, out ruleToRun))
    {
        Trace.TraceInformation(FormattableString.Invariant($"No {command} command in {repository.Name}"));
        return;
    }

    ruleToRun.Action();
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

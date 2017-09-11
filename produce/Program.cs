using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using MacroConsole;
using MacroExceptions;
using MacroGit;
using MacroGuards;


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
Plugins = new[] {
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

    // TODO Establish pattern for workspace-wide commands
    if (command == "programs")
    {
        Programs();
        return 0;
    }

    var repositories = CurrentRepository != null ? new[] { CurrentRepository } : CurrentWorkspace.FindRepositories();
    foreach (var repository in repositories) RunCommand(repository, command);
    return 0;
}


static void
RunCommand(ProduceRepository repository, string command)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.Required(command, nameof(command));

    var rulesInOrder = new List<Rule>();
    var rulesByName = new Dictionary<string, Rule>();

    foreach (var plugin in Plugins)
    {
        foreach (var rule in plugin.DetectRules(repository))
        {
            Trace.TraceInformation(FormattableString.Invariant($"Plugin {plugin} detected rule {rule.Command}"));
            rulesByName.Add(rule.Command, rule);
            rulesInOrder.Add(rule);
        }
    }

    Rule ruleToRun;
    if (!rulesByName.TryGetValue(command, out ruleToRun))
    {
        Trace.TraceInformation(FormattableString.Invariant($"No {command} command in {repository.Name}"));
        return;
    }

    ruleToRun.Action();
}


static void
Programs()
{
    if (CurrentRepository != null)
    {
        ProgramWrapperGenerator.GenerateProgramWrappers(CurrentRepository);
    }
    else
    {
        ProgramWrapperGenerator.GenerateProgramWrappers(CurrentWorkspace);
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

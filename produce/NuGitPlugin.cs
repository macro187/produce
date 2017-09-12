using MacroDiagnostics;
using MacroExceptions;
using System.Collections.Generic;


namespace
produce
{


public class
NuGitPlugin
    : IPlugin
{


public IEnumerable<Rule>
DetectWorkspaceRules(ProduceWorkspace workspace)
{
    yield break;
}


public IEnumerable<Rule>
DetectRepositoryRules(ProduceRepository repository)
{
    yield return new Rule("restore", () => Restore(repository));
    yield return new Rule("update", () => Update(repository));
}


static void
Restore(ProduceRepository repository)
{
    if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "nugit", "restore") != 0)
        throw new UserException("nugit failed");
}


static void
Update(ProduceRepository repository)
{
    if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "nugit", "update") != 0)
        throw new UserException("nugit failed");
}


}
}

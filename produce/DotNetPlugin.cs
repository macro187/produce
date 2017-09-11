using MacroDiagnostics;
using MacroExceptions;
using MacroSln;
using System.Collections.Generic;
using System.IO;


namespace
produce
{


public class
DotNetPlugin
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
    var dotNuGitDir = Path.Combine(repository.Path, ".nugit");
    VisualStudioSolution sln = null;
    // TODO Add a `findsln` command to `nugit` and use that (or similar)
    if (Directory.Exists(dotNuGitDir)) sln = VisualStudioSolution.Find(dotNuGitDir);
    if (sln == null) sln = VisualStudioSolution.Find(repository.Path);
    if (sln == null) yield break;
    yield return new Rule("build", () => Sln(repository, sln, "build"));
    yield return new Rule("rebuild", () => Sln(repository, sln, "rebuild"));
    yield return new Rule("clean", () => Sln(repository, sln, "clean"));
}


static void
Sln(ProduceRepository repository, VisualStudioSolution sln, string command)
{
    var verb = command[0].ToString().ToUpperInvariant() + command.Substring(1) + "ing";
    using (LogicalOperation.Start(verb + " " + repository.Name))
    {
        if (ProcessExtensions.Execute(true, true, repository.Path, "cmd", "/c", "sln", command, sln.Path) != 0)
            throw new UserException("Failed");
    }
}


}
}

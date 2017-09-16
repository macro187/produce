using MacroCollections;
using MacroDiagnostics;
using MacroGuards;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;


namespace
produce
{


public class
ProgramsPlugin : Plugin
{


public override void
DetectWorkspaceRules(ProduceWorkspace workspace, Graph graph)
{
    Guard.NotNull(workspace, nameof(workspace));
    Guard.NotNull(graph, nameof(graph));

    graph.Add(
        new Rule(
            graph.Command("programs"),
            null,
            null,
            () => GenerateProgramWrappers(workspace)));
}


public override void
DetectRepositoryRules(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    graph.Add(
        new Rule(
            graph.Command("programs"),
            null,
            null,
            () => GenerateProgramWrappers(repository)));
}


/// <summary>
/// Generate wrapper scripts for programs in all repositories in a workspace
/// </summary>
///
static void
GenerateProgramWrappers(ProduceWorkspace workspace)
{
    Guard.NotNull(workspace, nameof(workspace));

    var scripts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var repository in workspace.FindRepositories())
    {
        scripts.AddRange(GenerateProgramWrappers(repository));
    }

    var orphans =
        Directory.GetFiles(workspace.GetBinDirectory())
            .Where(file => !scripts.Contains(file))
            .ToList();

    if (orphans.Count > 0)
    {
        using (LogicalOperation.Start("Deleting orphan program wrapper scripts"))
        {
            foreach (var file in orphans)
            {
                Trace.WriteLine(file);
                File.Delete(file);
            }
        }
    }
}


/// <summary>
/// Generate wrapper scripts for programs in a repository
/// </summary>
///
/// <returns>
/// Paths of all generated wrapper scripts
/// </returns>
///
static IEnumerable<string>
GenerateProgramWrappers(ProduceRepository repository)
{
    Guard.NotNull(repository, nameof(repository));

    var paths = new List<string>();

    var dotProduce = repository.ReadDotProduce();
    if (dotProduce == null) return paths;
    if (dotProduce.Programs.Count == 0) return paths;

    using (LogicalOperation.Start("Writing " + repository.Name + " program wrapper script(s)"))
    {
        var programDirectory = repository.Workspace.GetBinDirectory();

        foreach (var program in dotProduce.Programs)
        {
            var programBase = Path.GetFileNameWithoutExtension(program);
            var target = Path.Combine("..", "..", repository.Name, program);
            var cmdPath = Path.Combine(programDirectory, programBase) + ".cmd";
            var shPath = Path.Combine(programDirectory, programBase);
            var cmd = GenerateCmd(target);
            var sh = GenerateSh(target);

            Trace.WriteLine(cmdPath);
            if (File.Exists(cmdPath)) File.Move(cmdPath, cmdPath); // In case only the casing has changed
            File.WriteAllText(cmdPath, cmd);
            paths.Add(cmdPath);

            Trace.WriteLine(shPath);
            if (File.Exists(shPath)) File.Move(shPath, shPath); // In case only the casing has changed
            File.WriteAllText(shPath, sh);
            paths.Add(shPath);

            if (!IsOnWindows())
            {
                Process.Start("chmod", "u+x \"" + shPath + "\"").WaitForExit();
            }
        }
    }

    return paths;
}


static string
GenerateCmd(string target)
{
    target = target.Replace("/", "\\");
    return "@\"%~dp0" + target + "\" %*\r\n";
}


static string
GenerateSh(string target)
{
    var mono = IsOnWindows() ? "" : "mono --debug ";
    target = target.Replace("\\", "/");
    return
        "#!/bin/bash\n" +
        mono + "\"$(dirname $0)/" + target + "\" \"$@\"\n";
}


static bool
IsOnWindows()
{
    switch (Environment.OSVersion.Platform)
    {
        case PlatformID.MacOSX:
        case PlatformID.Unix:
            return false;
        default:
            return true;
    }
}


}
}

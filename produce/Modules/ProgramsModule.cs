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
ProgramsModule : Module
{


public override void
Attach(ProduceWorkspace workspace, Graph graph)
{
    Guard.NotNull(workspace, nameof(workspace));
    Guard.NotNull(graph, nameof(graph));
    graph.Command("programs", () => GenerateProgramWrappers(workspace));
}


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var dotProducePrograms = graph.List("dot-produce-programs");
    var command = graph.Command("programs", () => GenerateProgramWrappers(repository, dotProducePrograms.Values));
    graph.Dependency(dotProducePrograms, command);
}


/// <summary>
/// Generate wrapper scripts for programs in all repositories in a workspace
/// </summary>
///
static void
GenerateProgramWrappers(ProduceWorkspace workspace)
{
    Guard.NotNull(workspace, nameof(workspace));

    var scripts = new HashSet<string>(
        workspace.FindRepositories()
            .Select(r => r.DotProducePath)
            .Where(p => File.Exists(p))
            .Select(p => new DotProduce(p))
            .Where(dp => dp != null)
            .SelectMany(dp => dp.Programs)
            .Select(path => Path.GetFileNameWithoutExtension(path)),
        StringComparer.OrdinalIgnoreCase);

    var orphans =
        Directory.GetFiles(workspace.GetBinDirectory())
            .Where(file => !scripts.Contains(Path.GetFileNameWithoutExtension(file)))
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
static void
GenerateProgramWrappers(ProduceRepository repository, IEnumerable<string> programs)
{
    Guard.NotNull(programs, nameof(programs));

    if (!programs.Any()) return;

    using (LogicalOperation.Start("Writing " + repository.Name + " program wrapper script(s)"))
    {
        var programDirectory = repository.Workspace.GetBinDirectory();

        foreach (var program in programs)
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

            Trace.WriteLine(shPath);
            if (File.Exists(shPath)) File.Move(shPath, shPath); // In case only the casing has changed
            File.WriteAllText(shPath, sh);

            if (!IsOnWindows())
            {
                Process.Start("chmod", "u+x \"" + shPath + "\"").WaitForExit();
            }
        }
    }
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

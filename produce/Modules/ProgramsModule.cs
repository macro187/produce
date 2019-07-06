using MacroCollections;
using MacroDiagnostics;
using MacroGuards;
using MacroSystem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace
produce
{


public class
ProgramsModule : Module
{


public override void
PostWorkspace(ProduceWorkspace workspace, string command)
{
    Guard.NotNull(workspace, nameof(workspace));
    Guard.Required(command, nameof(command));
    if (command != "programs") return;
    GenerateProgramWrappers(workspace);
}


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var dotProducePrograms = graph.List("dot-produce-programs");
    var command = graph.Command("programs", _ => GenerateProgramWrappers(repository, dotProducePrograms.Values));
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

            if (!EnvironmentExtensions.IsWindows)
            {
                Process.Start("chmod", "u+x \"" + shPath + "\"").WaitForExit();
            }
        }
    }
}


static string
GenerateCmd(string target)
{
    var driver = GetDriver(target);
    target = target.Replace("/", "\\");
    return $"@{driver}\"%~dp0{target}\" %*\r\n";
}


static string
GenerateSh(string target)
{
    var driver = GetDriver(target);
    target = target.Replace("\\", "/");
    return
        $"#!/bin/bash\n" +
        $"{driver}\"$(dirname $0)/{target}\" \"$@\"\n";
}


static string
GetDriver(string target)
{
    var isDotNetFramework = IsDotNetFramework(target);
    var isDotNetCore = IsDotNetCore(target);

    if (isDotNetCore)
    {
        return "dotnet ";
    }

    if (isDotNetFramework && !EnvironmentExtensions.IsWindows)
    {
        return "mono --debug ";
    }

    return "";
}


static bool
IsDotNetFramework(string target)
{
    return
        Regex.IsMatch(
            target.Replace('\\', '/'),
            @"/net[0123456789]+/");
}


static bool
IsDotNetCore(string target)
{
    return
        Regex.IsMatch(
            target.Replace('\\', '/'),
            @"/netcoreapp[0123456789.]+/");
}


}
}

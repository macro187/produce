using System;
using System.IO;
using MacroGuards;
using MacroGit;
using System.Collections.Generic;
using System.Linq;
using MacroDiagnostics;


namespace
produce
{


/// <summary>
/// A root directory that contains repository subdirectories
/// </summary>
///
public class
ProduceWorkspace
{


/// <summary>
/// Name of special workspace subdirectory containing wrapper scripts for running programs in the repositories
/// in the workspace
/// </summary>
///
const string
ProgramWrapperDirectoryName = ".bin";


/// <summary>
/// Initialise a new workspace
/// </summary>
///
/// <param name="path">
/// Path to the workspace
/// </param>
///
public
ProduceWorkspace(string path)
{
    Guard.Required(path, nameof(path));
    if (!Directory.Exists(path)) throw new ArgumentException("Not a directory", nameof(path));
    Path = System.IO.Path.GetFullPath(path);
}


/// <summary>
/// Full path to the workspace
/// </summary>
///
public string
Path
{
    get;
    private set;
}


/// <summary>
/// Get a repository in the workspace
/// </summary>
///
/// <param name="name">
/// Name of the repository
/// </param>
///
/// <returns>
/// The repository named <paramref name="name"/>
/// </returns>
///
/// <exception cref="ArgumentException">
/// No repository named <paramref name="name"/> exists in the workspace
/// </exception>
///
public ProduceRepository
GetRepository(GitRepositoryName name)
{
    var repository = FindRepository(name);
    if (repository == null)
        throw new ArgumentException(FormattableString.Invariant($"No '{name}' repository in workspace"), nameof(name));
    return repository;
}


/// <summary>
/// Look for a repository in the workspace
/// </summary>
///
/// <param name="name">
/// Name of the sought-after repository
/// </param>
///
/// <returns>
/// The repository in the workspace named <paramref name="name"/>
/// - OR -
/// <c>null</c> if no such repository exists
/// </returns>
///
public ProduceRepository
FindRepository(GitRepositoryName name)
{
    Guard.NotNull(name, nameof(name));
    return FindRepositories().FirstOrDefault(r => r.Name == name);
}


/// <summary>
/// Locate all repositories in the workspace
/// </summary>
///
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design",
    "CA1024:UsePropertiesWhereAppropriate",
    Justification = "Not static, this is re-read from disk each call")]
public IEnumerable<ProduceRepository>
FindRepositories()
{
    return
        Directory.EnumerateDirectories(Path)
            .Where(path => ProduceRepository.IsRepository(path))
            .Select(path => new GitRepositoryName(System.IO.Path.GetFileName(path)))
            .Select(name => new ProduceRepository(this, name));
}


/// <summary>
/// Get full path to (and if necessary create) a special workspace subdirectory for wrapper scripts that run
/// programs in the repositories in the workspace
/// </summary>
///
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design",
    "CA1024:UsePropertiesWhereAppropriate",
    Justification = "This method can have side-effects")]
public string
GetProgramWrapperDirectory()
{
    var path = System.IO.Path.Combine(Path, ProgramWrapperDirectoryName);
    
    if (!Directory.Exists(path))
    {
        using (LogicalOperation.Start("Creating " + path))
        {
            Directory.CreateDirectory(path);
        }
    }
    
    return path;
}


}
}

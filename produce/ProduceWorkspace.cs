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


const string
ProduceDirectoryName = "_produce";


const string
BinDirectoryName = "_bin";


const string
TraceDirectoryName = "_trace";


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


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design",
    "CA1024:UsePropertiesWhereAppropriate",
    Justification = "This method can have side-effects")]
public string
GetProduceDirectory()
{
    var path = System.IO.Path.Combine(Path, ProduceDirectoryName);
    
    if (!Directory.Exists(path))
    {
        using (LogicalOperation.Start("Creating " + path))
        {
            Directory.CreateDirectory(path);
        }
    }
    
    return path;
}


/// <summary>
/// Get full path to (and if necessary create) the directory containing wrapper scripts that run programs in the
/// workspace
/// </summary>
///
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Design",
    "CA1024:UsePropertiesWhereAppropriate",
    Justification = "This method can have side-effects")]
public string
GetBinDirectory()
{
    var produceDir = GetProduceDirectory();
    var path = System.IO.Path.Combine(Path, produceDir, BinDirectoryName);
    
    if (!Directory.Exists(path))
    {
        using (LogicalOperation.Start("Creating " + path))
        {
            Directory.CreateDirectory(path);
        }
    }
    
    return path;
}


public string
GetTraceDirectory()
{
    var produceDir = GetProduceDirectory();
    var path = System.IO.Path.Combine(Path, produceDir, TraceDirectoryName);
    
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

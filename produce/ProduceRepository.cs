using System.IO;
using IOPath = System.IO.Path;
using MacroGuards;
using MacroGit;


namespace
produce
{


/// <summary>
/// A Produce repository
/// </summary>
///
public class
ProduceRepository
    : GitRepository
{


public
ProduceRepository(ProduceWorkspace workspace, GitRepositoryName name)
    : base(
        IOPath.Combine(
            Guard.NotNull(workspace, nameof(workspace)).Path,
            Guard.NotNull(name, nameof(name))))
{
    Workspace = workspace;
    DotProducePath = IOPath.Combine(Path, ".produce");
}


/// <summary>
/// Workspace the repository is in
/// </summary>
///
public ProduceWorkspace
Workspace
{
    get;
}


/// <summary>
/// Full path to the repository's .produce configuration file
/// </summary>
///
public string
DotProducePath
{
    get;
}


/// <summary>
/// Get path to a work directory with the specified name for this repository
/// </summary>
///
/// <remarks>
/// The directory may or may not exist yet
/// </remarks>
///
public string
GetWorkDirectory(string name)
{
    Guard.Required(name, nameof(name));
    return IOPath.Combine(Workspace.GetProduceDirectory(), Name, name);
}


}
}

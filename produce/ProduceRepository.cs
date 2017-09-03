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
    Name = name;
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
/// Name of the repository subdirectory
/// </summary>
///
public GitRepositoryName
Name
{
    get;
}


/// <summary>
/// Read .produce information
/// </summary>
///
public DotProduce
ReadDotProduce()
{
    var path = IOPath.Combine(Path, ".produce");
    if (!File.Exists(path)) return null;
    return new DotProduce(path);
}


}
}

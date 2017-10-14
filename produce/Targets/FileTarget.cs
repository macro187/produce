using System;
using System.IO;
using IOPath = System.IO.Path;
using MacroGuards;


namespace
produce
{


public class
FileTarget : Target
{


public
FileTarget(Graph graph, string path)
    : base(graph)
{
    Guard.Required(path, nameof(path));
    if (!IOPath.IsPathRooted(path)) throw new ArgumentException("Not an absolute path", nameof(path));
    Path = IOPath.GetFullPath(path);
}


public bool
Exists => File.Exists(Path);


public string
Path { get; }


public override void Build()
{
    SetTimestamp(Exists ? File.GetLastWriteTime(Path) : DateTime.Now);
}


public override string
ToString() => Path;


}
}

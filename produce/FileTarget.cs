using System;
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


public string
Path { get; }


public new void
SetTimestamp(DateTime timestamp)
{
    base.SetTimestamp(timestamp);
}


public override string
ToString() => Path;


}
}

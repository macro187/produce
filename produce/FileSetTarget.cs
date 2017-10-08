using System.Collections.Generic;
using System.Linq;
using MacroGuards;


namespace
produce
{


public class
FileSetTarget : Target
{


public
FileSetTarget(Graph graph, string name)
    : base(graph)
{
    Guard.Required(name, nameof(name));
    Name = name;
}


public string
Name { get; }


public override void
Build()
{
    var patterns = Graph.RequiredBy(this).OfType<ListTarget>().SelectMany(t => t.Values);
    // TODO Handle patterns not just paths
    var newFiles = patterns.Select(p => Graph.File(p)).ToList();
    foreach (var file in Files) Graph.RemoveDependency(file, this);
    foreach (var file in newFiles) Graph.Dependency(file, this);
}


public IEnumerable<FileTarget>
Files => Graph.RequiredBy(this).OfType<FileTarget>().ToList();


public override string
ToString() => Name;


}
}

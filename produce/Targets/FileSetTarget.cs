using System;
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
    var toRemove = Files.Except(newFiles).ToList();
    var toAdd = newFiles.Except(Files).ToList();
    foreach (var file in toRemove) Graph.RemoveDependency(file, this);
    foreach (var file in toAdd) Graph.Dependency(file, this);
    if (toAdd.Count == 0) SetTimestamp(DateTime.Now);
}


public IEnumerable<FileTarget>
Files => Graph.RequiredBy(this).OfType<FileTarget>().ToList();


public override string
ToString() => Name;


}
}

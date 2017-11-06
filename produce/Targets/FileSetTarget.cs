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
}


public IEnumerable<FileTarget>
Files => Graph.RequiredBy(this).OfType<FileTarget>().ToList();


public new void
SetTimestamp(DateTime timeStamp)
{
    base.SetTimestamp(timeStamp);
}


public override string
ToString() => Name;


}
}

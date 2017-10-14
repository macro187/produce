using System;
using System.Collections.Generic;
using System.Linq;
using MacroGuards;


namespace
produce
{


public class
ListTarget : Target
{


public
ListTarget(Graph graph, string name, Func<ListTarget, IEnumerable<string>> getValues)
    : base(graph)
{
    Guard.Required(name, nameof(name));
    Guard.NotNull(getValues, nameof(getValues));
    Name = name;
    this.getValues = getValues;
    Values = new string[0];
}


public string
Name { get; }


public IReadOnlyList<string>
Values { get; private set; }


Func<ListTarget, IEnumerable<string>> getValues;


public override void
Build()
{
    Values = getValues(this).ToList();
    SetTimestamp(DateTime.Now);
}


public override string
ToString() => Name;


}
}

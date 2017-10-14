using System;
using MacroGuards;


namespace
produce
{


public class
CommandTarget : Target
{


public
CommandTarget(Graph graph, string name, Action build)
    : base(graph)
{
    Guard.Required(name, nameof(name));
    Guard.NotNull(build, nameof(build));
    Name = name;
    this.build = build;
}


public string
Name { get; }


Action build;


public override void
Build()
{
    build();
    SetTimestamp(DateTime.Now);
}


public override string
ToString() => Name;


}
}

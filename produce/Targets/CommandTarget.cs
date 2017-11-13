using System;
using MacroGuards;


namespace
produce
{


public class
CommandTarget : Target
{


public
CommandTarget(Graph graph, string name, Action<CommandTarget> build)
    : base(graph)
{
    Guard.Required(name, nameof(name));
    Guard.NotNull(build, nameof(build));
    Name = name;
    this.build = build;
}


public string
Name { get; }


Action<CommandTarget> build;


public override void
Build()
{
    build(this);
    SetTimestamp(DateTime.Now);
}


public override string
ToString() => Name;


}
}

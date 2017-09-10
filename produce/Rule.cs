using MacroGuards;
using System;


namespace
produce
{


public class
Rule
{


public
Rule(string command, Action action)
{
    Guard.NotNull(command, nameof(command));
    Guard.NotNull(action, nameof(action));
    Command = command;
    Action = action;
}


public string
Command
{
    get;
    private set;
}


public Action
Action
{
    get;
    private set;
}


}
}

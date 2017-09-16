using MacroGuards;


namespace
produce
{


public class
CommandTarget
    : Target
{


public
CommandTarget(string name)
    : base(name)
{
    Guard.Required(name, nameof(name));
    Name = name;
}


public string
Name
{
    get;
}


}
}

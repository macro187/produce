using MacroGuards;


namespace
produce
{


public abstract class
Target
{


protected
Target(string description)
{
    Guard.Required(description, nameof(description));
    Description = description;
}


public string
Description
{
    get;
}


public override string
ToString()
{
    return Description;
}


}
}

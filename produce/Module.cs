using System;


namespace
produce
{


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Module")]
public abstract class
Module
{


const string
TypeNameSuffix = "Module";


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
protected
Module()
{
    var name = this.GetType().Name;
    if (name.EndsWith("Module", StringComparison.OrdinalIgnoreCase))
        name = name.Substring(0, name.Length - TypeNameSuffix.Length);
    name = name.ToLowerInvariant();
    Name = name;
}


public string
Name
{
    get;
}


public virtual void
Attach(ProduceWorkspace workspace, Graph graph)
{
}


public virtual void
Attach(ProduceRepository repository, Graph graph)
{
}


}
}

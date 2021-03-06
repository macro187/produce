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


/// <summary>
/// Perform actions before workspace-wide commands are executed
/// </summary>
///
public virtual void
PreWorkspace(ProduceWorkspace workspace, string command)
{
}


/// <summary>
/// Perform actions after workspace-wide commands are executed
/// </summary>
///
public virtual void
PostWorkspace(ProduceWorkspace workspace, string command)
{
}


public virtual void
Attach(ProduceRepository repository, Graph graph)
{
}


}
}

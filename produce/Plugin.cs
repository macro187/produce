using System;
using MacroGuards;


namespace
produce
{


public abstract class
Plugin
{


const string TypeNameSuffix = "Plugin";


[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
protected
Plugin()
{
    var name = this.GetType().Name;
    if (name.EndsWith("Plugin", StringComparison.OrdinalIgnoreCase))
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
DetectWorkspaceRules(ProduceWorkspace workspace, Graph graph)
{
}


public virtual void
DetectRepositoryRules(ProduceRepository repository, Graph graph)
{
}


}
}

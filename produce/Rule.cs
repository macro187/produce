using MacroGuards;
using System;
using System.Collections.Generic;
using System.Linq;

namespace
produce
{


public class
Rule
{


public
Rule(Target target, IEnumerable<Target> required, IEnumerable<Target> requiredBy, Action build)
{
    Guard.NotNull(target, nameof(target));
    required = required ?? Enumerable.Empty<Target>();
    requiredBy = requiredBy ?? Enumerable.Empty<Target>();
    Guard.NotNull(build, nameof(build));

    // TODO Target can't require itselt or be requiredby itself
    // TODO Same target can't be required and requiredby

    Target = target;
    Required = new HashSet<Target>(required);
    RequiredBy = new HashSet<Target>(requiredBy);
    Build = build;
}


/// <summary>
/// The target that this rule builds
/// </summary>
///
public Target
Target
{
    get;
}


/// <summary>
/// Other targets that this target requires
/// </summary>
///
public ISet<Target>
Required
{
    get;
}


/// <summary>
/// Other targets that require this target
/// </summary>
///
public ISet<Target>
RequiredBy
{
    get;
}


/// <summary>
/// Build the target
/// </summary>
///
public Action
Build
{
    get;
}


}
}

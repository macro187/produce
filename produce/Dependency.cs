using System;
using MacroGuards;


namespace
produce
{


public class
Dependency
{


public
Dependency(Target from, Target to)
{
    Guard.NotNull(from, nameof(from));
    Guard.NotNull(to, nameof(to));
    From = from;
    To = to;
}


public Target
From { get; }


public Target
To { get; }


public override string
ToString() => FormattableString.Invariant($"{From} -> {To}");


}
}

using System;
using System.Linq;
using MacroGuards;


namespace
produce
{


public abstract class
Target
{


protected
Target(Graph graph)
{
    Guard.NotNull(graph, nameof(graph));
    Graph = graph;
}


protected Graph
Graph { get; }


public DateTime?
Timestamp { get; private set; }


public bool
IsBuildable
{
    get
    {
        var requiredBy = Graph.RequiredBy(this).ToList();
        if (requiredBy.Any(t => !t.IsUpToDate)) return false;
        if (Timestamp != null && requiredBy.All(t => t.Timestamp <= Timestamp)) return false;
        return true;
    }
}


public bool
IsUpToDate
{
    get
    {
        return
            Timestamp != null &&
            Graph.RequiredBy(this).All(t =>
                t.Timestamp != null &&
                t.Timestamp <= Timestamp &&
                t.IsUpToDate);
    }
}


public virtual void
Build()
{
}


public void
Invalidate()
{
    Timestamp = null;
}


public abstract override string
ToString();


protected void
SetTimestamp(DateTime timestamp)
{
    Timestamp = timestamp;
}


}
}

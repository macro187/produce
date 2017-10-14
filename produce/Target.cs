using System;
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

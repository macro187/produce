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


public virtual void
Build()
{
}


public abstract override string
ToString();


}
}

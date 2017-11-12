using MacroGuards;


namespace
produce
{


public class
GlobalModule : Module
{


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    graph.Command("restore");
    graph.Command("update");
    graph.Command("clean");
    graph.Command("build");
    graph.Command("rebuild");
}


}
}

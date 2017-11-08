using System.Collections.Generic;
using System.IO;
using System.Linq;
using MacroGuards;


namespace
produce
{


/// <summary>
/// Module that imports information from the <c>.produce</c> config file
/// </summary>
///
public class
DotProduceModule : Module
{


public override void
Attach(ProduceWorkspace workspace, Graph graph)
{
    Guard.NotNull(workspace, nameof(workspace));
    Guard.NotNull(graph, nameof(graph));
}


public override void
Attach(ProduceRepository repository, Graph graph)
{
    Guard.NotNull(repository, nameof(repository));
    Guard.NotNull(graph, nameof(graph));

    var path = graph.List("dot-produce-path", Path.GetFullPath(Path.Combine(repository.Path, ".produce")));

    var fileSet = graph.FileSet("dot-produce-fileset");
    graph.Dependency(path, fileSet);

    DotProduce dotProduce = new DotProduce();
    var command = graph.Command("dot-produce", () => {
        var file = fileSet.Files.SingleOrDefault();
        if (file == null) return;
        dotProduce = new DotProduce(file.Path);
    });
    graph.Dependency(fileSet, command);

    var programsList = graph.List("dot-produce-programs", target => dotProduce.Programs);
    graph.Dependency(command, programsList);
}


}
}

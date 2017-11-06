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
    var programs = graph.List("dot-produce-programs", target => GetProgramsValues(graph, target));
    graph.Dependency(fileSet, programs);
}


static IEnumerable<string>
GetProgramsValues(Graph graph, ListTarget target)
{
    var file =
        graph.RequiredBy(target)
            .OfType<FileSetTarget>()
            .SelectMany(fs => graph.RequiredBy(fs).OfType<FileTarget>())
            .SingleOrDefault();
    if (file == null) return Enumerable.Empty<string>();
    return new DotProduce(file.Path).Programs;
}


}
}

using System;
using System.IO;
using System.Linq;
using MacroGuards;


namespace
produce
{


public class
FileUpdater
{


public
FileUpdater(Graph graph)
{
    Guard.NotNull(graph, nameof(graph));
    Graph = graph;
}


Graph
Graph { get; }


public void
Update()
{
    var fileSets = Graph.Targets.OfType<FileSetTarget>().ToList();
    foreach (var fileSet in fileSets) UpdateFiles(fileSet);
    PruneFiles();
    UpdateFileTimestamps();
    foreach (var fileSet in fileSets) UpdateTimestamp(fileSet);
}


void
UpdateFiles(FileSetTarget fileSet)
{
    Guard.NotNull(fileSet, nameof(fileSet));

    // Get file specs from required list target(s)
    // TODO Handle patterns not just paths
    var patterns = Graph.RequiredBy(fileSet).OfType<ListTarget>().SelectMany(t => t.Values);
    var paths = patterns.Select(p => Path.GetFullPath(p)).Where(p => File.Exists(p));

    // Get current set of files
    var newFiles = paths.Select(p => Graph.File(p)).ToList();

    // Adjust dependencies
    var toRemove = fileSet.Files.Except(newFiles).ToList();
    var toAdd = newFiles.Except(fileSet.Files).ToList();
    foreach (var file in toRemove) Graph.RemoveDependency(file, fileSet);
    foreach (var file in toAdd) Graph.Dependency(file, fileSet);

    // Update timestamp if the set of files has changed
    var changed = toAdd.Any() || toRemove.Any();
    if (changed) fileSet.SetTimestamp(DateTime.Now);
}


void
PruneFiles()
{
    var files = Graph.Targets.OfType<FileTarget>().ToList();
    foreach (var file in files)
    {
        if (!Graph.Requiring(file).Any() || !File.Exists(file.Path)) Graph.RemoveTarget(file);
    }
}


void
UpdateFileTimestamps()
{
    var files = Graph.Targets.OfType<FileTarget>().ToList();
    foreach (var file in files) file.SetTimestamp(File.GetLastWriteTime(file.Path));
}


/// <summary>
/// Ensure a fileset's timestamp is at least as new as its dependencies
/// </summary>
///
void
UpdateTimestamp(FileSetTarget fileSet)
{
    Guard.NotNull(fileSet, nameof(fileSet));

    var newestDependencyTimestamp = Graph.RequiredBy(fileSet).Max(t => t.Timestamp);

    if (fileSet.Timestamp == null)
    {
        fileSet.SetTimestamp(newestDependencyTimestamp ?? DateTime.Now);
    }
    else if (newestDependencyTimestamp.HasValue && newestDependencyTimestamp.Value > fileSet.Timestamp)
    {
        fileSet.SetTimestamp(newestDependencyTimestamp.Value);
    }
}


}
}

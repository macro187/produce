﻿using System;
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

    // Get current set of files
    var newFiles = patterns.Select(p => Graph.File(p)).ToList();

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


void
UpdateTimestamp(FileSetTarget fileSet)
{
    Guard.NotNull(fileSet, nameof(fileSet));
    var files = fileSet.Files.ToList();
    var latestTimestamp = files.Max(f => f.Timestamp);
    if (!latestTimestamp.HasValue) return;
    if (fileSet.Timestamp < latestTimestamp) fileSet.SetTimestamp(latestTimestamp.Value);
}


}
}
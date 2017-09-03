using MacroExceptions;
using MacroGuards;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;


namespace
produce
{


public class
DotProduce
{


const string
PROGRAM_PREFIX = "program: ";


public
DotProduce(string path)
{
    Guard.Required(path, nameof(path));
    if (!File.Exists(path)) throw new ArgumentException("path does not exist", nameof(path));
    Parse(File.ReadLines(path));
}


public ICollection<string>
Programs
{
    get;
    private set;
}


[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Microsoft.Performance", "CA1820:TestForEmptyStringsUsingStringLength",
    Justification = "Comparing to empty string literal is more precise than string.IsNullOrEmpty()")]
void
Parse(IEnumerable<string> lines)
{
    var programs = new List<string>();
    int lineNumber = 0;
    
    foreach (var line in lines)
    {
        lineNumber++;

        //
        // Empty / whitespace-only
        //
        if (string.IsNullOrWhiteSpace(line)) continue;

        //
        // # <comment>
        //
        if (line.StartsWith("#", StringComparison.Ordinal)) continue;

        //
        // program: <program>
        //
        if (line.StartsWith(PROGRAM_PREFIX, StringComparison.OrdinalIgnoreCase))
        {
            var program = line.Substring(PROGRAM_PREFIX.Length).Trim();
            if (program == "")
                throw new TextFileParseException(
                    "Expected <program>",
                    lineNumber + 1,
                    line);
            program = program.Replace('/', '\\');
            program = program.Replace('\\', Path.DirectorySeparatorChar);
            if (!IsPathLocal(program))
                throw new TextFileParseException(
                    "Expected local path to <program>",
                    lineNumber + 1,
                    line);
            programs.Add(program);
            continue;
        }
    }

    Programs = new ReadOnlyCollection<string>(programs);
}


static bool
IsPathLocal(string path)
{
    if (string.IsNullOrWhiteSpace(path)) return false;
    if (Path.IsPathRooted(path)) return false;
    if (path.StartsWith("..", StringComparison.OrdinalIgnoreCase)) return false;
    return true;
}


}
}

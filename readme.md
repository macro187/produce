produce
=======

A software build task runner


Synopsis
========

```
produce [--tracegraph] <command>...
```


Options
=======

```
--tracegraph
    Draw a Graphviz graph of the internal dependency graph as each build
    step occurs, providing a visual record of the build.

    Graphs are written to <workspace>/_produce/_trace/.

<command>
    Command(s) to execute (see Commands).

    Multiple commands are executed one after another in the order
    specified.  When run at the workspace level, the sequence of commands
    is repeated for each repository one after another.
```


Commands
========

```
restore
    Restore build dependencies

upgrade
    Upgrade build dependencies within configured version ranges

clean
    Delete build artifacts

build
    Build

rebuild
    Clean and build

programs
    Create / update wrapper scripts that run programs exported from the
    repository in-place.

    Designate exported programs in the repository's `.produce` config
    file.  See "Configuration File" below.

    Running the command at the workspace level builds scripts for all
    repositories plus cleans up any orphan scripts that are no longer
    required.

    Scripts are maintained in `<workspace>/_produce/_bin/`.  Adding that
    directory to the system path enables use of the exported programs
    throughout the system.

    Scripts for both Windows `cmd.exe` and Unix `bash` are maintained.  On
    Windows, the `bash` scripts are usable in `Git Bash`.
```


Configuration File
==================

The `produce` program is controlled by plain-text `.produce` configuration
files located in repository root directories.

Empty lines and lines beginning with hash characters are ignored.

    #
    # Programs to be exported by the `programs` command
    #
    program: Foo/bin/Debug/Foo.exe
    program: Bar/bin/Debug/Bar.exe


License
=======

MIT License <https://github.com/macro187/produce/blob/master/license.txt>


Copyright
=========

Copyright (c) 2017-2018  
Ron MacNeil <https://github.com/macro187>


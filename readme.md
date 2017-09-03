produce
=======

    Tool for conducting software development tasks


Synopsis
========

    produce <command> [<arguments>]

        <command>
            The `produce` command to execute

        <arguments>
            Command-specific options and arguments


Commands
========

    build
        Build repository(s)

    rebuild
        Clean and rebuild repository(s)

    clean
        Clean repository(s)

    programs
        Build or update wrapper scripts that run, in-place, the programs
        produced by repository(s).

        Running the command within a repository builds scripts for programs
        produced by that repository as specified in its `.produce` config
        file.

        Running the command within a workspace builds scripts for all programs
        in all repositories, and also cleans up orphan scripts.

        Wrapper scripts are maintained in `<workspace>/.bin`.  Adding it to
        the system path enables use of the exported programs throughout the
        system.

        Scripts for both Windows `cmd.exe` and Unix `bash` are maintained.  On
        Windows, the `bash` scripts are usable in `Git Bash`.


File Format
===========

    The `produce` program is controlled by plain-text `.produce` configuration
    files located in the root directories of Git repositories.

    Empty lines and lines beginning with hash characters are ignored.

        #
        # Programs produced by this repository
        #
        program: Foo/bin/Debug/Foo.exe
        program: Bar/bin/Debug/Bar.exe


License
=======

    MIT License <https://github.com/macro187/produce/blob/master/license.txt>


Copyright
=========

    Copyright (c) 2017
    Ron MacNeil <https://github.com/macro187>

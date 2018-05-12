This example contains a Console application which uses a dynamic library and a static library.
Console application contain references to other libraries and that is enough for GCCBuild to include the output .lib or .a files, so there is no need for additionalLibrary settings.

Build it as (Windows)

    dotnet build

Or for Linux

    dotnet build /p:Configuration:LinuxDebug

Run it as (Linux)

    LD_LIBRARY_PATH=. ./ConsoleApplication.out

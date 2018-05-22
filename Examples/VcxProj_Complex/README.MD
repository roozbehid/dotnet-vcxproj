This example contains a Console application which uses a dynamic library and a static library, all in C++.

Console application contain references to other libraries and that is enough for GCCBuild to include the output .lib or .a files, so there is no need for manually updating/changing additionalLibrary options.

Windows :

    dotnet build

Linux :

    dotnet build /p:Configuration:LinuxDebug

Run it on Linux :

    LD_LIBRARY_PATH=. ./ConsoleApplication.out

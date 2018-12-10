Dotnet-Vcxproj
======

Previously GCCBuild. The reason behind multiple name changes are that many people had problem finding this project by searching, it seems I used very common names which would result in this project never showing up! I would still use gccbuild command inside dotnet and probably you will see many references to it. It is just a repo name change!

This is a project to make dotnet, build C/C++ sources by use of GCC or Clang or Emscripten, by Roozbeh GH!

Dotnet-Vcxproj will work on your current .sln or .vcxproj files. All projects will be working as before on Visual Studio.
Obviously you need to make modifications to your sources to compile on linux. You can use "LinuxDebug" and "LinuxRelease" Configurations for your linux build.

The reason behind this project is I was lazy to make, MakeFile or Cmake files for my projects as I was moving them from Windows to Linux and in my projects I was heavily using C#.Net Core mixed with C++, so this project was borned!

What it can do now?
-------------------
It works both on Windows and Linux, meaning you can use the same project file and build it in Linux or Windows! The ultimate goal is to use it more on Linux and use it with GCC, but you can specify your own compiler too! Currently it also supports Emscripten!

It does support Static, Dynamic, Console applications and understands project references. Meaning just referencing you static or dynamic libraries is enough to add them to your include and lib directories automatically. You can also use nugets with you C++ files in Linux now!

My future goal is to also include some comptability header files so with less modifications you can use and port you r applications from windows to linux.

Instructions to convert your exisitng C++ projects - Short Version
-------
        dotnet new -i GCC.Build.Template (only once in your system to install templates)
        cd mySolutionORvcxProjFolder
        dotnet new gccbuild
        
You have to answer 'Y' to allow scripts do their job!
Optional : if you are using git for source control

        git add -u
        git commit -m "I will keep these new files and modifed vcxprojs"
        
        
Now if you want to make your life easier, in Visual Studio go and duplicate "Debug" or "Release" configuration and name them "LinuxDebug" or "GCCDebug" or "EmscriptenDebug" or "LinuxRelease". Following command works if you are into command line (both in Linux and Windows)
     
        dotnet build /p:Configuration=EmscriptenDebug
        
You can obviously use Visual Studio to build and compile files within those configuration (to use your new compiler) or use previous configuration which will default to MSVC! Debugging also works!

So the very very short version instruction is you only need to run `dotnet new gccbuild` in your solution folder and you are golden!
        
Instructions to convert your exisitng C++ projects - Long Version
-------
All instructions should work on Windows and Linux the same way! Assumption is you have installed GCC and it is working!

 1. Install GCC templates. You have to do this only once in your system.

        dotnet new -i GCC.Build.Template
 2. Now that you have installed the template you can use it where you have .sln file or .vcxproj
 
        dotnet new gccbuild
    You have to answer 'Y' to running script. This command will iterate through all projects included in the .sln file, copy required files into those directories and slightly modify .vcxproj files. The modifications/changes are as follows :

        - Copy project.json file into each .vcxproj directory
    
        - Copy a custom made Microsoft.Cpp.Default.props into each .vcxproj directory
    
        - Modify .vcxproj to conditionally include some .target files from installed nuget folder.
    Now is a good time to add modified and newly added files to your version control system ie git.
    
    NOTE: Somethimes after answering Yes to running action command, nothing happens. Use CTRL+C to break script execution, run it again. It will list some files that will be overwritten (from previous run), do delete those files manually (or use --force) and rerun the command. You should see "Command succeded".
 3. Now your projects are ready. In order to build them with dotnet build in Linux, it is suggested that you create "LinuxDebug" and "LinuxRelease" configurations based on your current "Debug" and "Release" configurations. If you do so or create any platform or configuration that containt "Linux" or "GCC" or "Emscripten" in them, vcxproj will pick them up and use your compiler. 
If you want to proceed without this step (meaning not using a new named platform or configuration), then you need to set the following enviroment variables to force gccbuild compiler to kick in. It is not recommended though!

         VCTargetsPath = ./ 


     Note that you can still use Visual Studio to build and modify your projects and run/debug them. Nothing should be changed from Visual Studio prespective.
 4. Build as usuall
 
         dotnet build
    MSBuild should also work. Visual studio building should also work.

5. In order to seperate Windows and Linux build artifacts, recommended method is to use "LinuxDebug", "LinuxRelease" for your configurations. You can also create a new Platform with "Linux" in it. 
6. You can use Visual Studio to Compile/Build/Debug these projects for Windows or Linux! Look at examples folder for more complex solutions.

Feature List
-------
 - Easiest way to support both Linux-Windows-WebAssembly from single project
 - Easiest way to cross compile for different OSes
 - Build your C++ Project file in Linux with simple `dotnet build` command!
 - Will automatically include all your referenced projects as external library dependency! Something that Visual Studio doesnt support!
 - Support Dynamic Library, Static Library and Executable programs.
 - Works fine with solutions having other languages in them, will only modify C++ projects.
 - Support modifying all your C++ projects if `dotnet new gccbuild` is issues in your solution folder.
 - Support for any compiler with customizable command lines, arguments and orders.
 - Full support for WSL in Windows. With a single switch you can compile and build in WSL.
 - Support for debugging your application inside Visual Studio.
 - All warning, errors and comments from your favorate tools will be displayed in correct format and double clicking will get you there!
 - Use nugets in your C++ projects in Linux!
 - You can use Appveyor or your favorate CI pipeline using the same exact files and same exact command! 
 
 
Disclaimer
-------
This is based on the work of Konrad Kruczyński and CCTask.

Licence
-------
The project uses the MIT licence as you can tell from the source code.

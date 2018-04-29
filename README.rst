GCCTask
======

MSBuild task to compile C/C++ sources by Roozbeh GH.
This is based on the work of Konrad Kruczyński and CCTask.
CCTask is built based on .proj files but GCCTask will be using the same .vcxproj file you have and tries to work with that.


What it can do now?
-------------------
Currently it works both on Windows and Linux.
Does support Static, Dynamic, Console applications and understand project references. My future goal is to also include some comptability header files so with less modifications you can use and port you r applications from windows to linux.

Example
-------
All instructions should work on Windows and Linux the same way!

 1. Create a sample project in C++ in Visual Studio or use the one provided in Test directory.
 
 2. Install GCC templates. You have to do this only once in your system.
 
        dotnet new -i GCC.Build.Template
       
 3. Now that you have installed the template you can use it where you have .sln file or .vcxproj
 
        dotnet new gccbuild
        
   This command will iterate through all projects included in the .sln file and will slightly modify .vcxproj files.
   The modifications are as folowws:
    1. Copy project.json file into .vcxproj directory
    
    2. Copy a custom made Microsoft.Cpp.Default.props into .vcxproj directory
    
    3. Modify .vcxproj to conditionally include .target files.
    
 4. Now your projects are ready. Note that you can still use Visual Studio to build and modify your projects and run/debug them. Nothing should be changed from Visual Studio prespective.
 
 5. In order to instruct usage of GCC you should issue following:
    
    On Windows
       set VCTargetsPath = .
       
    On Linux
        export VCTargetsPath = .
        
 6. Build as usuall
 
         dotnet build
    




Want more?
----------
Write an issue, do a pull request.

Licence
-------
The project uses the MIT licence as you can tell from the source code.

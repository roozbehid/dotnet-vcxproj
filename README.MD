GCCBuild
======

This project is to make dotnet build C/C++ sources by use of GCC, by Roozbeh GH. This is based on the work of Konrad Kruczyński and CCTask.

GCCBuild will work on your current .sln or .vcxproj files. All projects will be working as before on Visual Studio.
Obviously you need to make modifications to your sources to compile on linux. You can use "LinuxDebug" and "LinuxRelease" Configurations for your linux build.

The reason behind this project is I was lazy to make, MakeFile or Cmake files for my projects as I was moving them from Windows to Linux and I was using C# .Net Core mixed with C++. So this project was borned and I based it off of CCTask.

What it can do now?
-------------------
Currently it works both on Windows and Linux. The ultimate goal is to use it more on Linux and use it with GCC but you can specify your own compiler too!

It does support Static, Dynamic, Console applications and understands project references. Meaning just referencing you static or dynamic libraries is enough to add them to your include and lib directories automatically. You can also use nugets with you C++ files in Linux now!

My future goal is to also include some comptability header files so with less modifications you can use and port you r applications from windows to linux.

Instructions to convert your exisitng C++ projects
-------
All instructions should work on Windows and Linux the same way! Assumption is you have installed GCC and it is working!

 1. Install GCC templates. You have to do this only once in your system.

        dotnet new -i GCC.Build.Template
 2. Now that you have installed the template you can use it where you have .sln file or .vcxproj
 
        dotnet new gccbuild
    This command will iterate through all projects included in the .sln file, copy files to those directories and will slightly modify .vcxproj files. The command modifications are as follows :

        - Copy project.json file into .vcxproj directory
    
        - Copy a custom made Microsoft.Cpp.Default.props into .vcxproj directory
    
        - Modify .vcxproj to conditionally include .target files.
    Now is a good time to add modified and newly added files to your version control.
 3. Now your projects are ready. In order to build them with dotnet build in Linux, it is suggested that you create "LinuxDebug" and "LinuxRelease" configurations based on your current "Debug" and "Release" configurations. Then fine tune them. If you want to proceed without this step, then you need to set the following in enviroment variables.

         VCTargetsPath = ./ 


     Note that you can still use Visual Studio to build and modify your projects and run/debug them. Nothing should be changed from Visual Studio prespective.
 4. Build as usuall
 
         dotnet build
    MSBuild should also work.

5. In order to seperate Windows and Linux build artifacts, suggested method is to use "LinuxDebug", "LinuxRelease" for your configurations. You can also create a new Platform with "Linux" in it. 
6. You can use Visual Studio to Compile/Build/Debug these projects for Windows or Linux! Look at examples folder for more complex solutions.

Licence
-------
The project uses the MIT licence as you can tell from the source code.

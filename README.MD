Notice
======

Major versions change due to breaking changes in the project. 
Visual Studio 2022 and newer MSBuild did a breaking change on support of `poject.json` style nugets. So you need to use `PackageReference` instead of `project.json` style.
Because of this change, we will be using 2.x version with this new changes.

Dotnet-Vcxproj
======
[![Build Status](https://roozbehid.visualstudio.com/GCCBuild/_apis/build/status/GCCBuild-.NET%20Desktop-CI?branchName=prod)](https://roozbehid.visualstudio.com/GCCBuild/_build/latest?definitionId=1&branchName=prod)
[![Nuget](https://img.shields.io/nuget/v/GCC.Build.Template.svg)](https://www.nuget.org/packages/GCC.Build.Template/)
[![Nuget](https://img.shields.io/nuget/v/GCCBuildTargets.svg)](https://www.nuget.org/packages/GCCBuildTargets/)

Also checkout my [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=RoozbehGh.webassemblyconsoleapptemplate) for creating WebAssembly in Visual Studio!

Previously GCCBuild. The reason behind multiple name changes are that many people had problem finding this project by searching, it seems I used very common names which would result in this project never showing up! I would still use gccbuild command inside dotnet and probably you will see many references to it. It is just a repo name change!

This is a project to make dotnet, build C/C++ sources by use of GCC or Clang or Emscripten, by Roozbeh GH!

Dotnet-Vcxproj will work on your current .sln or .vcxproj files. All projects will be working as before on Visual Studio.
Obviously you need to make modifications to your sources to compile on linux. You can use "LinuxDebug" and "LinuxRelease" Configurations for your linux build.

The reason behind this project is I was lazy to make, MakeFile or Cmake files for my projects as I was moving them from Windows to Linux and in my projects I was heavily using C#.Net Core mixed with C++, so this project was borned!

What it can do now?
-------------------
It works both on Windows and Linux, meaning you can use the same project file and build it in Linux or Windows! The ultimate goal is to use it more on Linux and use it with GCC, but you can specify your own compiler too! Currently it also supports WebAssembly!

It does support Static, Dynamic, Console applications and understands project references. Meaning just referencing you static or dynamic libraries is enough to add them to your include and lib directories automatically. You can also use nugets with you C++ files in Linux now!

My future goal is to also include some comptability header files so with less modifications you can use and port you r applications from windows to linux.

Requirements
-------
- You have [dotnet core SDK](https://dotnet.microsoft.com/download) installed for your platform.
- You have a Visual Studio C++ project file or you have a Visual Studio solution file with VC++ project in it.
- There is no need for Visual Studio software being installed. But you do need Visual Studio VC++ project. Meaning if you have a Visual Studio VC++ project, you can continue building in Linux, Mac or Windows.

There is no need to clone,compile, build or install this repository. It was already pushed to nuget.org.


Instructions to convert your existing C++ projects - Short Version
-------
        dotnet new -i GCC.Build.Template (only once in your system to install templates)
        cd mySolutionORvcxProjFolder
        dotnet new gccbuild
        
You have to answer 'Y' to allow scripts do their job!
Optional : if you are using git for source control

        git add -u
        git commit -m "I will keep these new files and modifed vcxprojs"
        
        
Now if you want to make your life easier, in Visual Studio go and duplicate "Debug" or "Release" configuration and name them "LinuxDebug" or "GCCDebug" or "WasmDebug" or "LinuxRelease". Following command works if you are into command line (both in Linux and Windows)
     
        dotnet build /p:Configuration=WasmDebug
        
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
 3. Now your projects are ready. In order to build them with dotnet build in Linux, it is suggested that you create "LinuxDebug" and "LinuxRelease" configurations based on your current "Debug" and "Release" configurations. If you do so or create any configuration that contains "Linux" or "GCC" or "Wasm" in them, dotnet-vcxproj will pick them up and use your definitions for compiler. 
If you want to proceed without this step (meaning not using a new named platform or configuration), then you need to set the following enviroment variables to force gccbuild compiler to kick in. It is not recommended though!

         VCTargetsPath = ./ 


     Note that you can still use Visual Studio to build and modify your projects and run/debug them. Nothing should be changed from Visual Studio prespective.
 4. Build as usuall
 
         dotnet build
    MSBuild should also work. Visual studio building should also work.

5. In order to seperate Windows and Linux build artifacts, recommended method is to use "LinuxDebug", "LinuxRelease" for your configurations. You can also create a new Platform with "Linux" in it. 
6. You can use Visual Studio to Compile/Build/Debug these projects for Windows or Linux! Look at examples folder for more complex solutions.

Instructions using Visual Studio 2022 or higher
-------
This project was started with VS 2017 and it is still compatible with it. But if you are using VS 2022 or higher, you can use the following steps to convert your projects to GCCBuild.
Visual Studio 2022 seems to have broken use of nugets with VC++ projects and `project.json` style. So you need to use `PackageReference` instead of `project.json` style. 
Follow previous steps as before but after running `dotnet new gccbuild` you need to do the following steps:

 - Add followings to your vcxproj files:

		<ItemGroup>
			<PackageReference Include="GCCBuildTargets" Version="2.*" />
		</ItemGroup>
- Add following section in your vcxproj, best after  `<PropertyGroup Label="NuGet">...</PropertyGroup>` section

		  <PropertyGroup Label="NuGet">
		    <!-- https://github.com/NuGet/Home/issues/10511#issuecomment-778400668 -->
		    <AssetTargetFallback>$(AssetTargetFallback);native</AssetTargetFallback>
		    <!--
		      Avoid Visual Studio error message:
		      "The project '$(MSBuildProjectName)' ran into a problem during the last operation: The value of the
		      'TargetFrameworkMoniker' and 'NuGetTargetMoniker' properties in the '$(Configuration)|$(Platform)' configuration are both
		      empty. This configuration will not contribute to NuGet restore, which may result in restore and build errors. You may
		      need to reload the solution after fixing the problem."
		    -->
		    <TargetFrameworkVersion>v0.0</TargetFrameworkVersion>
		    <TargetFramework>native</TargetFramework>
		    <TargetFrameworkIdentifier>native</TargetFrameworkIdentifier>
		    <TargetFrameworkMoniker Condition="'$(NuGetTargetMoniker)' == ''">native,Version=v0.0</TargetFrameworkMoniker>
		    <RuntimeIdentifiers Condition="'$(RuntimeIdentifiers)' == ''">win;win-x86;win-x64;win-arm;win-arm64</RuntimeIdentifiers>
		    <UseTargetPlatformAsNuGetTargetMoniker>false</UseTargetPlatformAsNuGetTargetMoniker>
		  </PropertyGroup>

- Also add `<ProjectCapability Include="PackageReferences" />` inside your `<ItemGroup Label="ProjectConfigurations">`
- Now you have all you need to enable PackageReference nugets with your VC C++ projects.
- Look into this repo for `Microsoft.Cpp.Default.props` and replace your rpevious one.

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

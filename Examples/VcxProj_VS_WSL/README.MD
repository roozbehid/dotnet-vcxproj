
This example contains a simple console application which you can open in Visual Studio and build it inside Visual Studio for Linux using GCC in your WSL. If you set up Remote connection to WSL box, you can also debug your Linux build!

Make sure you have installed build-essntial in Linux so you have all required files.

     apt install build-essential


After building you can also debug it inside Visual Studio by creating a new connection. You will need to install openssh in WSL.

Note that there is no need for setting VCTargetsPath = . as this is done inside .vcxproj.

Following lines are of importance inside .vcxproj

    <VCTargetsPath Condition="'$(DesignTimeBuild)'!='true' AND ('$(Configuration)' == 'LinuxDebug' OR '$(Configuration)' == 'LinuxRelease')">.\</VCTargetsPath>

    <MSBuildProjectExtensionsPath Condition="'$(DesignTimeBuild)'!='true' AND ('$(Configuration)' == 'LinuxDebug' OR '$(Configuration)' == 'LinuxRelease')">.\</MSBuildProjectExtensionsPath>

    <GCCBuild_UseWSL>true</GCCBuild_UseWSL>

You can still use "dotnet build" to compile and link this project in Windows or Linux.
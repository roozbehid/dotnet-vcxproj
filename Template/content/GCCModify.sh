#!/bin/bash

replace_vcxproj()
{
	if grep -q 'Condition=".$(VCTargetsPath).' $project ; then  
		echo "already patched"
	else
		darwin=false;
		case "`uname`" in
		  Darwin*) darwin=true ;;
		esac

		if $darwin; then
		  sedi="/usr/bin/sed -i ''"
		else
		  sedi="sed -i"
		fi

		$sedi '/<ItemGroup Label="ProjectConfigurations">/a\
				<ProjectCapability Include="PackageReferences" />' $project

		$sedi 's/<Import Project="$(VCTargetsPath)\\Microsoft.Cpp.props" \/>/<Import Project="$(VCTargetsPath)\\Microsoft.Cpp.props" Condition="\x27$(VCTargetsPath)\x27 != \x27.\x27 AND \x27$(VCTargetsPath)\x27 != \x27.\\\x27 AND \x27$(VCTargetsPath)\x27 != \x27.\/\x27" \/>/g' $project
		$sedi 's/<Import Project="$(VCTargetsPath)\\Microsoft.Cpp.targets" \/>/<Import Project="$(VCTargetsPath)\\Microsoft.Cpp.targets" Condition="\x27$(VCTargetsPath)\x27 != \x27.\x27 AND \x27$(VCTargetsPath)\x27 != \x27.\\\x27 AND \x27$(VCTargetsPath)\x27 != \x27.\/\x27" \/>/g' $project
		$sedi 's/Label="Globals">/Label="Globals">\n    <VCTargetsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND ($(Configuration.Contains(\x27GCC\x27)) OR $(Platform.Contains(\x27GCC\x27)))">.\/<\/VCTargetsPath>\n    <MSBuildProjectExtensionsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND ($(Configuration.Contains(\x27GCC\x27)) OR $(Platform.Contains(\x27GCC\x27)))">.\/<\/MSBuildProjectExtensionsPath>/g' $project
		$sedi 's/Label="Globals">/Label="Globals">\n    <VCTargetsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND $(Configuration.Contains(\x27Wasm\x27))">.\/<\/VCTargetsPath>\n    <MSBuildProjectExtensionsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND $(Configuration.Contains(\x27Wasm\x27))">.\/<\/MSBuildProjectExtensionsPath>/g' $project
		$sedi 's/Label="Globals">/Label="Globals">\n    <GCCToolCompilerStyle Condition="$(Configuration.Contains(\x27Wasm\x27))">llvm<\/GCCToolCompilerStyle>/g' $project
		$sedi 's/Label="Globals">/Label="Globals">\n    <VCTargetsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND ($(Configuration.Contains(\x27Linux\x27)) OR $(Platform.Contains(\x27Linux\x27)))">.\/<\/VCTargetsPath>\n    <MSBuildProjectExtensionsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND ($(Configuration.Contains(\x27Linux\x27)) OR $(Platform.Contains(\x27Linux\x27)))">.\/<\/MSBuildProjectExtensionsPath>\n    <GCCBuild_UseWSL>false<\/GCCBuild_UseWSL>/g' $project
		
		$sedi '/<Import Project="$(VCTargetsPath)\\Microsoft.Cpp.Default.props" \/>/i\
		<PropertyGroup Label="NuGet">\
			<AssetTargetFallback>$(AssetTargetFallback);native</AssetTargetFallback>\
			<TargetFrameworkVersion>v0.0</TargetFrameworkVersion>\
			<TargetFramework>native</TargetFramework>\
			<TargetFrameworkIdentifier>native</TargetFrameworkIdentifier>\
			<TargetFrameworkMoniker Condition="\x27$(NuGetTargetMoniker)\x27 == \x27\x27">native,Version=v0.0</TargetFrameworkMoniker>\
			<RuntimeIdentifiers Condition="\x27$(RuntimeIdentifiers)\x27 == \x27\x27">win;win-x86;win-x64;win-arm;win-arm64</RuntimeIdentifiers>\
			<UseTargetPlatformAsNuGetTargetMoniker>false</UseTargetPlatformAsNuGetTargetMoniker>\
		</PropertyGroup>' $project

		$sedi ':a;N;$!ba;s/\(.*\)\(<\/Project>\)/\1  <ItemGroup>\n    <PackageReference Include="GCCBuildTargets" Version="2.*"\/>\n  <\/ItemGroup>\n\2/' $project



        dirr=`dirname "$project"`;
		cp ./Microsoft.Cpp.Default.props.GCCBuild "$dirr/Microsoft.Cpp.Default.props"		
	fi
}

count=`find . -maxdepth 1 -name "*.sln" | wc -l`;
count2=`find . -maxdepth 1 -name "*.vcxproj" | wc -l`;

if [ $count -eq 1 ] 
 then
       projects=`dotnet sln list`
       for project in $projects; do
            if [ -f $project ] && [[ $project = *"vcxproj" ]]; then
                echo "processing $project"
                replace_vcxproj
            fi
       done
 elif [ $count2 -eq 1 ] 
   then
       for project in ./*.vcxproj; do
           echo "processing $project"
           replace_vcxproj
       done
 fi

rm ./Microsoft.Cpp.Default.props.GCCBuild 
rm ./GCCModify.sh
rm ./GCCModify.ps1

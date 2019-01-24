#!/bin/bash

replace_vcxproj()
{
	if grep -q 'Condition=".$(VCTargetsPath).' $project ; then  
		echo "already patched"
	else
		sed -i 's/<Import Project="$(VCTargetsPath)\\Microsoft.Cpp.props" \/>/<Import Project="$(VCTargetsPath)\\Microsoft.Cpp.props" Condition="\x27$(VCTargetsPath)\x27 != \x27.\x27 AND \x27$(VCTargetsPath)\x27 != \x27.\\\x27 AND \x27$(VCTargetsPath)\x27 != \x27.\/\x27" \/>/g' $project
		sed -i 's/<Import Project="$(VCTargetsPath)\\Microsoft.Cpp.targets" \/>/<Import Project="$(VCTargetsPath)\\Microsoft.Cpp.targets" Condition="\x27$(VCTargetsPath)\x27 != \x27.\x27 AND \x27$(VCTargetsPath)\x27 != \x27.\\\x27 AND \x27$(VCTargetsPath)\x27 != \x27.\/\x27" \/>/g' $project
		sed -i 's/Label="Globals">/Label="Globals">\n    <VCTargetsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND ($(Configuration.Contains(\x27GCC\x27)) OR $(Platform.Contains(\x27GCC\x27)))">.\/<\/VCTargetsPath>\n    <MSBuildProjectExtensionsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND ($(Configuration.Contains(\x27GCC\x27)) OR $(Platform.Contains(\x27GCC\x27)))">.\/<\/MSBuildProjectExtensionsPath>\n/g' $project
		sed -i 's/Label="Globals">/Label="Globals">\n    <VCTargetsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND $(Configuration.Contains(\x27Wasm\x27))">.\/<\/VCTargetsPath>\n    <MSBuildProjectExtensionsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND $(Configuration.Contains(\x27Wasm\x27))">.\/<\/MSBuildProjectExtensionsPath>\n/g' $project
		sed -i 's/Label="Globals">/Label="Globals">\n    <GCCToolCompilerStyle Condition="$(Configuration.Contains(\x27Wasm\x27))">llvm<\/GCCToolCompilerStyle>\n/g' $project
		sed -i 's/Label="Globals">/Label="Globals">\n    <VCTargetsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND ($(Configuration.Contains(\x27Linux\x27)) OR $(Platform.Contains(\x27Linux\x27)))">.\/<\/VCTargetsPath>\n    <MSBuildProjectExtensionsPath Condition="\x27$(DesignTimeBuild)\x27!=\x27true\x27 AND ($(Configuration.Contains(\x27Linux\x27)) OR $(Platform.Contains(\x27Linux\x27)))">.\/<\/MSBuildProjectExtensionsPath>\n    <GCCBuild_UseWSL>false<\/GCCBuild_UseWSL>/g' $project
        dirr=`dirname "$project"`;
		cp ./Microsoft.Cpp.Default.props.GCCBuild "$dirr/Microsoft.Cpp.Default.props"
        cp ./project.json.GCCBuild "$dirr/project.json"

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
 
rm ./GCCModify.sh.GCCBuild
rm ./GCCModify.ps1.GCCBuild
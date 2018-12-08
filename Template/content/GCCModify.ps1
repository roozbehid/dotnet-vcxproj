function ModifyVcx {
    Param($VcxPath)
    Write-Output "Modifying $VcxPath"
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" Condition="''$(VCTargetsPath)'' != ''.'' AND ''$(VCTargetsPath)'' != ''.\'' AND ''$(VCTargetsPath)'' != ''./''" />') | Set-Content $VcxPath
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" Condition="''$(VCTargetsPath)'' != ''.'' AND ''$(VCTargetsPath)'' != ''.\'' AND ''$(VCTargetsPath)'' != ''./''" />') | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND (`$(Configuration.Contains('Linux')) OR `$(Platform.Contains('Linux')))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND (`$(Configuration.Contains('Linux')) OR `$(Platform.Contains('Linux')))"">./</MSBuildProjectExtensionsPath>`n") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND (`$(Configuration.Contains('GCC')) OR `$(Platform.Contains('GCC')))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND (`$(Configuration.Contains('GCC')) OR `$(Platform.Contains('GCC')))"">./</MSBuildProjectExtensionsPath>`n") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <GCCToolLinkerStyle Condition=""(`$(Configuration.Contains('Emscripten')) OR `$(Platform.Contains('Emscripten')))"">llvm</GCCToolLinkerStyle>`n") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND (`$(Configuration.Contains('Emscripten')) OR `$(Platform.Contains('Emscripten')))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND (`$(Configuration.Contains('Emscripten')) OR `$(Platform.Contains('Emscripten')))"">./</MSBuildProjectExtensionsPath>`n    <GCCBuild_UseWSL>false</GCCBuild_UseWSL>") | Set-Content $VcxPath
}

if ((Get-ChildItem .\* -Include *.sln).Count -eq 1){
    $projects = (dotnet sln list)
    foreach ($proj in $projects) {
        if ((Test-Path $proj) -And ($proj -like "*vcxproj")){
            ModifyVcx $proj
            Copy-Item .\project.json (Split-Path $proj)
            Copy-Item .\Microsoft.Cpp.Default.props (Split-Path $proj)
        }
    }
    if ((Get-ChildItem .\* -Include *.vcxproj).Count -eq 0){
        Remove-Item .\project.json
        Remove-Item .\Microsoft.Cpp.Default.props
    }
}
elseIf ((Get-ChildItem .\* -Include *.vcxproj).Count -eq 1){
    ModifyVcx (Get-ChildItem .\* -Include *.vcxproj)
}

Write-Output "Cleaning up...."
Remove-Item .\GCCModify.ps1
Remove-Item .\GCCModify.sh
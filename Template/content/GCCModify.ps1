function ModifyVcx {
    Param($VcxPath)
    Write-Host "Modifying $VcxPath"
	
	 if ((Get-Content $VcxPath) | Select-String 'Condition="''\$\(VCTargetsPath\)''') 
	{
		Write-Host "$VcxPath is already patched. If you want to repair it you have to manually remove all fields added by GCCBuild"
		return $false
	}
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" Condition="''$(VCTargetsPath)'' != ''.'' AND ''$(VCTargetsPath)'' != ''.\'' AND ''$(VCTargetsPath)'' != ''./''" />') | Set-Content $VcxPath
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" Condition="''$(VCTargetsPath)'' != ''.'' AND ''$(VCTargetsPath)'' != ''.\'' AND ''$(VCTargetsPath)'' != ''./''" />') | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Linux'))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Linux'))"">./</MSBuildProjectExtensionsPath>`n") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('GCC'))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('GCC'))"">./</MSBuildProjectExtensionsPath>`n") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <GCCToolCompilerStyle Condition=""`$(Configuration.Contains('Emscripten'))"">llvm</GCCToolCompilerStyle>`n") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Emscripten'))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Emscripten'))"">./</MSBuildProjectExtensionsPath>`n    <GCCBuild_UseWSL>false</GCCBuild_UseWSL>") | Set-Content $VcxPath
	return $true
}

if ((Get-ChildItem .\* -Include *.sln).Count -eq 1){
    $projects = (dotnet sln list)
    foreach ($proj in $projects) {
        if ((Test-Path $proj) -And ($proj -like "*vcxproj")){
            if (ModifyVcx $proj)
			{
				Copy-Item .\project.json.GCCBuild (Split-Path $proj)\project.json
				Copy-Item .\Microsoft.Cpp.Default.props.GCCBuild (Split-Path $proj)\Microsoft.Cpp.Default.props
			}
        }
    }
        Remove-Item .\project.json.GCCBuild
        Remove-Item .\Microsoft.Cpp.Default.props.GCCBuild
}
elseIf ((Get-ChildItem .\* -Include *.vcxproj).Count -eq 1){
    ModifyVcx (Get-ChildItem .\* -Include *.vcxproj)
}

Write-Host "Cleaning up...."
Remove-Item .\GCCModify.ps1
Remove-Item .\GCCModify.sh
function ModifyVcx {
    Param($VcxPath)
    Write-Host "Modifying $VcxPath"
	
	 if ((Get-Content $VcxPath) | Select-String 'Condition="''\$\(VCTargetsPath\)''') 
	{
		Write-Host "$VcxPath is already patched. If you want to repair it you have to manually remove all fields added by GCCBuild`n"
		return $false
	}
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" Condition="''$(VCTargetsPath)'' != ''.'' AND ''$(VCTargetsPath)'' != ''.\'' AND ''$(VCTargetsPath)'' != ''./''" />') | Set-Content $VcxPath
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" Condition="''$(VCTargetsPath)'' != ''.'' AND ''$(VCTargetsPath)'' != ''.\'' AND ''$(VCTargetsPath)'' != ''./''" />') | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Linux'))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Linux'))"">./</MSBuildProjectExtensionsPath>`n") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('GCC'))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('GCC'))"">./</MSBuildProjectExtensionsPath>`n") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <GCCToolCompilerStyle Condition=""`$(Configuration.Contains('Wasm'))"">llvm</GCCToolCompilerStyle>`n") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Wasm'))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Wasm'))"">./</MSBuildProjectExtensionsPath>`n    <GCCBuild_UseWSL>false</GCCBuild_UseWSL>") | Set-Content $VcxPath
	$split_Path = Split-Path (Get-ChildItem $VcxPath)
	Copy-Item .\project.json.GCCBuild "$split_Path\project.json"
	Copy-Item .\Microsoft.Cpp.Default.props.GCCBuild "$split_Path\Microsoft.Cpp.Default.props"

	return $true
}

if ((Get-ChildItem .\* -Include *.sln).Count -eq 1){
	$timeoutSeconds = 5
	$cwd = (Resolve-Path .\).Path
	$code = {
		param($cwd)
		dotnet sln $cwd list
	}
	$myjob = Start-Job -ScriptBlock $code -arg $cwd 
	if (Wait-Job $myjob -Timeout $timeoutSeconds){
		Write-Host "dotnet sln timed out`n`n"
	}
    $projects = Receive-Job $myjob
	Remove-Job -force $myjob
    foreach ($proj in $projects) {
        if ((Test-Path $proj) -And ($proj -like "*vcxproj")){
            ModifyVcx $proj
        }
    }
}
elseIf ((Get-ChildItem .\* -Include *.vcxproj).Count -eq 1){
    ModifyVcx (Get-ChildItem .\* -Include *.vcxproj)
}

Write-Host "Cleaning up...."
Remove-Item .\project.json.GCCBuild
Remove-Item .\Microsoft.Cpp.Default.props.GCCBuild
Remove-Item .\GCCModify.ps1
Remove-Item .\GCCModify.sh
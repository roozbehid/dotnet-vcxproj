function ModifyVcx {
    Param($VcxPath)
    Write-Host "Modifying $VcxPath"
	
	 if ((Get-Content $VcxPath) | Select-String 'Condition="''\$\(VCTargetsPath\)''') 
	{
		Write-Host "$VcxPath is already patched. If you want to repair it you have to manually remove all fields added by GCCBuild`n"
		return $false
	}

	(Get-Content $VcxPath) -replace '(<ItemGroup Label="ProjectConfigurations">)', "`$1`n    <ProjectCapability Include='PackageReferences' />" | Set-Content $VcxPath
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" Condition="''$(VCTargetsPath)'' != ''.'' AND ''$(VCTargetsPath)'' != ''.\'' AND ''$(VCTargetsPath)'' != ''./''" />') | Set-Content $VcxPath
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" Condition="''$(VCTargetsPath)'' != ''.'' AND ''$(VCTargetsPath)'' != ''.\'' AND ''$(VCTargetsPath)'' != ''./''" />') | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Linux'))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Linux'))"">./</MSBuildProjectExtensionsPath>") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('GCC'))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('GCC'))"">./</MSBuildProjectExtensionsPath>") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <GCCToolCompilerStyle Condition=""`$(Configuration.Contains('Wasm'))"">llvm</GCCToolCompilerStyle>") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace("Label=""Globals"">","Label=""Globals"">`n    <VCTargetsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Wasm'))"">./</VCTargetsPath>`n    <MSBuildProjectExtensionsPath Condition=""'`$(DesignTimeBuild)'!='true' AND `$(Configuration.Contains('Wasm'))"">./</MSBuildProjectExtensionsPath>`n    <GCCBuild_UseWSL>false</GCCBuild_UseWSL>") | Set-Content $VcxPath
	(Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.Default.props" />',"<Import Project=""`$(VCTargetsPath)\Microsoft.Cpp.Default.props"" />`n  <PropertyGroup Label=""NuGet"">`n    <AssetTargetFallback>`$(AssetTargetFallback);native</AssetTargetFallback>`n    <TargetFrameworkVersion>v0.0</TargetFrameworkVersion>`n    <TargetFramework>native</TargetFramework>`n    <TargetFrameworkIdentifier>native</TargetFrameworkIdentifier>`n    <TargetFrameworkMoniker Condition=""'`$(NuGetTargetMoniker)' == ''"">native,Version=v0.0</TargetFrameworkMoniker>`n    <RuntimeIdentifiers Condition=""'`$(RuntimeIdentifiers)' == ''"">win;win-x86;win-x64;win-arm;win-arm64</RuntimeIdentifiers>`n    <UseTargetPlatformAsNuGetTargetMoniker>false</UseTargetPlatformAsNuGetTargetMoniker>`n  </PropertyGroup>") | Set-Content $VcxPath
	(Get-Content $VcxPath -Raw) -replace '(?s)(.*)(<\/Project>)', "`$1  <ItemGroup>`r`n    <PackageReference Include=`"GCCBuildTargets`" Version=`"2.*`"/>`r`n  </ItemGroup>`r`n`$2" | Set-Content $VcxPath


	$split_Path = Split-Path (Get-ChildItem $VcxPath)
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
Remove-Item .\Microsoft.Cpp.Default.props.GCCBuild
Remove-Item .\GCCModify.ps1
Remove-Item .\GCCModify.sh
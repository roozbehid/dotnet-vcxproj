function ModifyVcx {
    Param($VcxPath)
    Write-Output "Modifying $VcxPath"
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" Condition="''$(VCTargetsPath)'' != ''.''" />') | Set-Content $VcxPath
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" Condition="''$(VCTargetsPath)'' != ''.''" />') | Set-Content $VcxPath
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
    Remove-Item .\project.json
    Remove-Item .\Microsoft.Cpp.Default.props
}
elseIf ((Get-ChildItem .\* -Include *.vcxproj).Count -eq 1){
    ModifyVcx (Get-ChildItem .\* -Include *.vcxproj)
}

Remove-Item .\GCCModify.ps1
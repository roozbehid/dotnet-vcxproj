function ModifyVcx {
    Param($VcxPath)
    Write-Output "Modifying $VcxPath"
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.props" Condition="''$(VCTargetsPath)'' != ''.''" />') | Set-Content $VcxPath
    (Get-Content $VcxPath).Replace('<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />','<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" Condition="''$(VCTargetsPath)'' != ''.''" />') | Set-Content $VcxPath
}

if ((Get-ChildItem .\* -Include *.sln).Count -eq 1){
    $projects = (dotnet sln list)
    foreach ($proj in $projects) {
        if (Test-Path $proj){
            ModifyVcx $proj
        }
    }

}
elseIf ((Get-ChildItem .\* -Include *.vcxproj).Count -eq 1){
    ModifyVcx (Get-ChildItem .\* -Include *.vcxproj)
}

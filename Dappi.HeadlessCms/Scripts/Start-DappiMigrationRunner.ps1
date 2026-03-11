param (
    [string]$ProjectPath,
    [string]$Csproj,
    [string]$DotnetPath = "dotnet",
    [string]$ProcessId,
    [string]$MigrationName
    [string]$ContextName
)


Stop-Process -Id $ProcessId -Force

Write-Host "Generating EF migration: $MigrationName"
Push-Location $ProjectPath

& $DotnetPath ef migrations add $MigrationName --project $Csproj --context $ContextName
& $DotnetPath ef database update --project $Csproj --context $ContextName 

Write-Host "Restarting application..."
& $DotnetPath run --project $Csproj 
Write-Host "Done. Migration applied and app restarted."
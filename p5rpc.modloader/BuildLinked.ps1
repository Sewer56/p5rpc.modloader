# Set Working Directory
Split-Path $MyInvocation.MyCommand.Path | Push-Location
[Environment]::CurrentDirectory = $PWD

Remove-Item "$env:RELOADEDIIMODS/p5rpc.modloader/*" -Force -Recurse
dotnet publish "./p5rpc.modloader.csproj" -c Release -o "$env:RELOADEDIIMODS/p5rpc.modloader" /p:OutputPath="./bin/Release" /p:ReloadedILLink="true"

# Restore Working Directory
Pop-Location
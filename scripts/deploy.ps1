param(
    [string]$ProjectPath = "H:\dev\LobotomyMacroMod2\src\abcdcode_Macro_MOD\abcdcode_Macro_MOD.csproj",
    [string]$BuildConfiguration = "Release",
    [string]$BuiltDllPath = "H:\dev\LobotomyMacroMod2\src\abcdcode_Macro_MOD\bin\Release\net461\abcdcode_Macro_MOD.dll",
    [string]$WorkspaceModPath = "H:\dev\LobotomyMacroMod2\mod\Main File-365-1-0-1707710717 (3)",
    [string]$ModFolderName = "Main File-365-1-0-1707710717 (3)",
    [string]$KrInfoXmlPath = "H:\dev\LobotomyMacroMod2\mod\Main File-365-1-0-1707710717 (3)\Info\kr\Info.xml",
    [string]$EnInfoXmlPath = "H:\dev\LobotomyMacroMod2\mod\Main File-365-1-0-1707710717 (3)\Info\en\Info.xml",
    [string]$BaseModsPath = "I:\SteamLibrary\steamapps\common\LobotomyCorp\LobotomyCorp_Data\BaseMods",
    [string]$BaseModListPath = "I:\SteamLibrary\steamapps\common\LobotomyCorp\LobotomyCorp_Data\BaseMods\BaseModList_v2.xml",
    [string]$ReleaseRoot = "H:\dev\LobotomyMacroMod2\release"
)

$ErrorActionPreference = "Stop"

& (Join-Path $PSScriptRoot "..\tools\build_deploy_package.ps1") `
    -ProjectPath $ProjectPath `
    -BuildConfiguration $BuildConfiguration `
    -BuiltDllPath $BuiltDllPath `
    -WorkspaceModPath $WorkspaceModPath `
    -ModFolderName $ModFolderName `
    -KrInfoXmlPath $KrInfoXmlPath `
    -EnInfoXmlPath $EnInfoXmlPath `
    -BaseModsPath $BaseModsPath `
    -BaseModListPath $BaseModListPath `
    -ReleaseRoot $ReleaseRoot

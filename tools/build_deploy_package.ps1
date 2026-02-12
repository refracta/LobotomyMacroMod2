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

function Save-XmlUtf8([xml]$xml, [string]$path) {
    $settings = New-Object System.Xml.XmlWriterSettings
    $settings.Indent = $true
    $settings.Encoding = New-Object System.Text.UTF8Encoding($false)
    $writer = [System.Xml.XmlWriter]::Create($path, $settings)
    try {
        $xml.Save($writer)
    } finally {
        $writer.Close()
    }
}

function Get-NextDisplayName([string]$currentName) {
    $trimmed = $currentName.Trim()
    $match = [regex]::Match($trimmed, "^(?<base>.+?)\s+v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$")
    if ($match.Success) {
        $baseName = $match.Groups["base"].Value.Trim()
        $major = [int]$match.Groups["major"].Value
        $minor = [int]$match.Groups["minor"].Value
        $patch = [int]$match.Groups["patch"].Value + 1
        return "$baseName v$major.$minor.$patch"
    }
    if ([string]::IsNullOrWhiteSpace($trimmed)) {
        $trimmed = "Command Macro Mod 2"
    }
    return "$trimmed v0.0.1"
}

function Set-XmlChildValue([xml]$xml, [System.Xml.XmlElement]$parent, [string]$name, [string]$value) {
    $node = $parent.SelectSingleNode($name)
    if ($null -eq $node) {
        $node = $xml.CreateElement($name)
        [void]$parent.AppendChild($node)
    }
    $node.InnerText = $value
}

if (-not (Test-Path $KrInfoXmlPath)) {
    throw "KR Info.xml not found: $KrInfoXmlPath"
}

[xml]$krInfoXml = Get-Content -Path $KrInfoXmlPath -Raw -Encoding UTF8
$currentKrName = $krInfoXml.info.name
if ([string]::IsNullOrWhiteSpace($currentKrName)) {
    throw "KR Info.xml <name> is empty: $KrInfoXmlPath"
}
$newKrName = Get-NextDisplayName -currentName $currentKrName
$krInfoXml.info.name = $newKrName
Save-XmlUtf8 -xml $krInfoXml -path $KrInfoXmlPath

$versionMatch = [regex]::Match($newKrName, "v\d+\.\d+\.\d+")
if (-not $versionMatch.Success) {
    throw "Version not found in KR display name: $newKrName"
}
$newVersion = $versionMatch.Value

if (Test-Path $EnInfoXmlPath) {
    [xml]$enInfoXml = Get-Content -Path $EnInfoXmlPath -Raw -Encoding UTF8
    $currentEnName = [string]$enInfoXml.info.name
    $enBaseName = ($currentEnName -replace "\s+v\d+\.\d+\.\d+$", "").Trim()
    if ([string]::IsNullOrWhiteSpace($enBaseName)) {
        $enBaseName = "Command Macro Mod 2"
    }
    $enInfoXml.info.name = "$enBaseName $newVersion"
    Save-XmlUtf8 -xml $enInfoXml -path $EnInfoXmlPath
}

& dotnet build $ProjectPath -c $BuildConfiguration
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

if (-not (Test-Path $BuiltDllPath)) {
    throw "Built DLL not found: $BuiltDllPath"
}

if (-not (Test-Path $WorkspaceModPath)) {
    throw "Workspace mod path not found: $WorkspaceModPath"
}

$workspaceDllPath = Join-Path $WorkspaceModPath "abcdcode_Macro_MOD.dll"
Copy-Item -Path $BuiltDllPath -Destination $workspaceDllPath -Force

$deployModPath = Join-Path $BaseModsPath $ModFolderName
if (Test-Path $deployModPath) {
    Remove-Item -Path $deployModPath -Recurse -Force
}
Copy-Item -Path $WorkspaceModPath -Destination $deployModPath -Recurse -Force

if (-not (Test-Path $BaseModListPath)) {
    throw "BaseModList_v2.xml not found: $BaseModListPath"
}

[xml]$modListXml = Get-Content -Path $BaseModListPath -Raw -Encoding UTF8
$listNode = $modListXml.ModListXml.list
if ($null -eq $listNode) {
    throw "Invalid BaseModList_v2.xml structure: missing ModListXml/list"
}

$entry = $null
foreach ($item in $listNode.ModInfoXml) {
    if ($item.modfoldername -eq $ModFolderName) {
        $entry = $item
        break
    }
}

if ($null -eq $entry) {
    $entry = $modListXml.CreateElement("ModInfoXml")
    [void]$listNode.AppendChild($entry)
}

Set-XmlChildValue -xml $modListXml -parent $entry -name "modfoldername" -value $ModFolderName
Set-XmlChildValue -xml $modListXml -parent $entry -name "Useit" -value "true"
Set-XmlChildValue -xml $modListXml -parent $entry -name "IsWorkShop" -value "false"
Set-XmlChildValue -xml $modListXml -parent $entry -name "IsNexus" -value "false"
Set-XmlChildValue -xml $modListXml -parent $entry -name "IsGitHub" -value "false"
Set-XmlChildValue -xml $modListXml -parent $entry -name "modid" -value "-1"
Set-XmlChildValue -xml $modListXml -parent $entry -name "fileid" -value "-1"
Set-XmlChildValue -xml $modListXml -parent $entry -name "g_modid" -value ""
Set-XmlChildValue -xml $modListXml -parent $entry -name "g_fileid" -value "-1"
Save-XmlUtf8 -xml $modListXml -path $BaseModListPath

New-Item -ItemType Directory -Path $ReleaseRoot -Force | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$safeVersion = $newVersion
$packageRoot = Join-Path $ReleaseRoot ($ModFolderName + "_" + $safeVersion + "_" + $timestamp)
if (Test-Path $packageRoot) {
    Remove-Item -Path $packageRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
Copy-Item -Path $WorkspaceModPath -Destination $packageRoot -Recurse -Force

$zipPath = $packageRoot + ".zip"
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}
Compress-Archive -Path (Join-Path $packageRoot $ModFolderName) -DestinationPath $zipPath -Force

Write-Output "DisplayName=$newKrName"
Write-Output "Version=$newVersion"
Write-Output "WorkspaceDll=$workspaceDllPath"
Write-Output "DeployPath=$deployModPath"
Write-Output "BaseModList=$BaseModListPath"
Write-Output "Zip=$zipPath"

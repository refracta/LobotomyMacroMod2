# LobotomyMacroMod2

This repository contains the source code for `abcdcode_Macro_MOD`, an AgentCTRL-style command macro mod for Lobotomy Corporation.
It is based on decompiled code from the original `Main File-365-1-0-1707710717 (3)` command macro mod and then modified.

## Quick Usage

- `Shift + Work command`: use the original repeating-work macro behavior.
- `Insert`: save current moving/working agent -> abnormality -> work-type assignments.
- `Shift + Insert`: print saved Insert assignments in the system log.
- `Home`: apply saved Insert assignments to dispatchable agents.
- `End`: cancel en-route assignments for agents saved by Insert.

## 1. Source

- Core patch logic: `src/abcdcode_Macro_MOD/Harmony_Patch.cs`
- Macro state helpers: `src/abcdcode_Macro_MOD/CreatureCheck.cs`, `src/abcdcode_Macro_MOD/MacroBurf.cs`, `src/abcdcode_Macro_MOD/MacroInfo.cs`
- Assembly metadata: `src/abcdcode_Macro_MOD/AssemblyInfo.cs`
- Project file: `src/abcdcode_Macro_MOD/abcdcode_Macro_MOD.csproj`
- Mod info XML: `mod/Main File-365-1-0-1707710717 (3)/Info/kr/Info.xml`, `mod/Main File-365-1-0-1707710717 (3)/Info/en/Info.xml`

## 2. Build

### Requirements

- .NET SDK 8+
- PowerShell

### Local build

```powershell
./scripts/build.ps1 -Configuration Release
```

### Build + Deploy to game

```powershell
./scripts/deploy.ps1
```

### Output

- DLL (build): `out/Release/abcdcode_Macro_MOD.dll`
- ZIP package: `dist/Main File-365-1-0-1707710717 (3).zip`
- Deploy archive (versioned): `release/*.zip`


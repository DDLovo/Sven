# Development

This repository contains the Sven runtime C# source and the public mod documentation. The original Dota/Unity model, animation and audio asset workspace is not included. This is not a standalone rebuild of the complete mod package.

## Runtime build

Use a .NET SDK supporting netstandard2.1. The project restores BepInEx, Unity module references, RoR2 game libraries and MMHOOK from NuGet. R2API assemblies must come from a matching local mod profile; put them under `local/R2API` or pass `-p:R2APIPlugins=...` to MSBuild.

The project embeds the decoded Sven audio clips from `unity/DotaHero/Assets/Sven/Bundle/Audio/*.wav`. Supply those files from your own asset workspace before building if you want working source sound playback. The runtime also requires the matching `sven` AssetBundle beside the installed DLL under `AssetBundles/sven`; that bundle is distributed in the Thunderstore package and is not built by this C# project.

```powershell
dotnet build src/Sven/Sven.csproj -c Release
```

Gameplay constants are in `src/Sven/SvenBalance.cs`; bilingual in-game text is in `src/Sven/SvenTokens.cs`. The root README is the public description, and the manifest's `website_url` points to this repository. Screenshots are actual gameplay captures cropped for presentation.

## Asset attribution

Valve retains ownership of the Dota character assets. The survivor integration derives from the HenryTutorial project foundation, as credited in the README. This repository does not grant a license to Valve's original content or ship game/R2API assemblies.

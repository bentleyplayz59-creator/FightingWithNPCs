# FightingWithNPCs (Stardew Valley 1.6 port)

Port of EduAnttunes' *Fighting With NPCs* v1.2.0 (Nexus 5865) to Stardew Valley 1.6 / SMAPI 4.
Swing a Scythe or Golden Scythe near Lewis, Pierre or Pam and they end up in the Hospital.
Behavior is unchanged except the 1.6 API updates (AssetRequested instead of IAssetLoader,
`Position`/`Name` properties, string item IDs; Iridium Scythe also counts).

## Build on GitHub Actions
Push to `main`/`master` or run the workflow manually. `Pathoschild/SMAPI-ModBuildWorkflow/add-build-environment@v1`
supplies the game/SMAPI reference assemblies, so no game files are needed. The mod zip is written to `_releases/`
and uploaded by `upload-release-artifacts@v1`.

## Build locally
`dotnet build FightingWithNPCs.csproj -c Release` (needs a Stardew Valley install, or the same reference assemblies).
Auto-deploy to `Mods/` is disabled in `Directory.Build.props`.

Status: not yet compiled or tested in-game. Android unverified.

# Pokemon Platinum Together Open Sourced - Editors

Windows authoring tools for map projects, validated `.ptpack` exports and custom
trainer sprite projects. This preview does not yet install maps or trainers in
the native game. Game/ROM assets and the official creator source are not bundled.

## Download

**[Download the standalone Windows editors â€” dev11 preview](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-editors/releases/download/v0.1.0-dev.11/PokemonPlatinumTogetherOpenSourced-Editors-Windows-x64-dev11.zip)**

Extract the complete ZIP and open `MapStudio/InvernaMapStudio.exe`. Keep the folder
together; its .NET runtime is included. For sprite authoring, use the sprite tools
or start the executable with `--sprites`. The ZIP includes the map, sprite and
verification guides in `docs/`.

[Release notes and SHA-256 checksum](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-editors/releases/tag/v0.1.0-dev.11).
[All five projects and downloads](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-launcher#downloads).
The public dev11 build is the **Latest release** and remains a **development preview**.
Native map/story/sprite installation remains unfinished. The [full Windows bundle](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-launcher#downloads)
also includes these editors.

## Related projects

- [Launcher, mail app and full documentation](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-launcher)
- [Server and relay backend](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-server)
- [Map Studio and trainer sprite tools](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-editors)

## Build and run

Use Windows x64 and .NET 10:

```powershell
dotnet build MapStudio.csproj -c Release
dotnet run --project MapStudio.csproj -c Release -- --selftest
dotnet run --project MapStudio.csproj -c Release -- --sprites
```

- [Map authoring and pack guide](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-launcher/blob/main/PrivateServer/docs/maps.md)
- [Trainer sprite guide](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-launcher/blob/main/PrivateServer/docs/trainer-sprites.md)
- [Official Sprite Creator](https://platinumtogether.online/creators/)
- [Status and native integration limits](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-launcher/blob/main/PrivateServer/docs/status.md)

Creator imports preserve your original version 3 `.ptdraft.json` and export only
your saved artwork. Empty frames stay transparent. Give artists credit before
sharing their work. No community upload happens automatically.

Developed with OpenAI Codex assistance. GPL-3.0-or-later; see [LICENSE](LICENSE).


## Separate hosting projects

Server settings live in server config files and host administration tools. The
[Inverna server](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-inverna-server) and [world server](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-world-server) projects
contain distinct hosting guides and deployment configs for shared-city and co-op
protocol services. These repositories are development source previews. Both depend on the
[shared backend](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-server).
See the [configuration reference](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-launcher/blob/main/PrivateServer/docs/server-config.md)
for roles, independent data folders and current native/federation limitations.


## Preview timing

Scene particles, floating objects and sprite poses derive their animation from
monotonic elapsed time. UI timer callbacks request redraws; missed callbacks do
not slow the animation or queue a burst of old poses. Starting a pose restarts its
timeline, and selecting a different frame stops playback. Tests cover frame
boundaries, callback frequencies of 30/60/120/144 Hz and skipped presentation frames.
This concerns editor previews; native game simulation timing is separate.


## Game target selection

New map projects target 0.1.7. Use **Pack details → Target** to select 0.1.5,
0.1.6 or 0.1.7; target metadata survives project and pack roundtrips. Older
projects retain their target. This is authoring compatibility only: the retail
0.1.7 player disables Studio/import entrypoints, and native installation remains
unfinished. No game executable or imported artwork is redistributed.

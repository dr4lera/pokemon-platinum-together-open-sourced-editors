# Plat Together Map Studio

Windows authoring tools for map projects, validated `.ptpack` exports and custom
trainer sprite projects. This preview does not yet install maps or trainers in
the native game. Game/ROM assets and the official creator source are not bundled.

## Related projects

- [Launcher, mail app and full documentation](https://github.com/dr4lera/plat-together-open-sourced)
- [Server and relay backend](https://github.com/dr4lera/plat-together-server)
- [Map Studio and trainer sprite tools](https://github.com/dr4lera/plat-together-map-studio)

## Build and run

Use Windows x64 and .NET 10:

```powershell
dotnet build MapStudio.csproj -c Release
dotnet run --project MapStudio.csproj -c Release -- --selftest
dotnet run --project MapStudio.csproj -c Release -- --sprites
```

- [Map authoring and pack guide](https://github.com/dr4lera/plat-together-open-sourced/blob/main/PrivateServer/docs/maps.md)
- [Trainer sprite guide](https://github.com/dr4lera/plat-together-open-sourced/blob/main/PrivateServer/docs/trainer-sprites.md)
- [Official Sprite Creator](https://platinumtogether.online/creators/)
- [Status and native integration limits](https://github.com/dr4lera/plat-together-open-sourced/blob/main/PrivateServer/docs/status.md)

Creator imports preserve your original version 3 `.ptdraft.json` and export only
your saved artwork. Empty frames stay transparent. Give artists credit before
sharing their work. No community upload happens automatically.

Developed with OpenAI Codex assistance. GPL-3.0-or-later; see [LICENSE](LICENSE).

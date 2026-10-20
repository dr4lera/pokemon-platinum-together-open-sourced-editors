# Pokemon Platinum Together Open Sourced - Editors

Windows authoring tools for map projects, validated `.ptpack` exports and custom
trainer sprite projects. This preview does not yet install maps or trainers in
the native game. Game/ROM assets and the official creator source are not bundled.

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
`pokemon-platinum-together-open-sourced-inverna-server` and `pokemon-platinum-together-open-sourced-world-server` sibling projects
contain distinct hosting guides and deployment configs for shared-city and co-op
protocol services. These new repositories are still local development work;
public links will be added when published. Both depend on the
[shared backend](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-server).
See the [configuration reference](https://github.com/dr4lera/pokemon-platinum-together-open-sourced-launcher/blob/main/PrivateServer/docs/server-config.md)
for roles, independent data folders and current native/federation limitations.

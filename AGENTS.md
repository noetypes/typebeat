# Development builds

The user runs the desktop Debug executable at `typebeat.Desktop/bin/Debug/net8.0/type!beat`.

After code changes, rebuild the desktop app before reporting completion:

```sh
dotnet build typebeat.Desktop/typebeat.Desktop.csproj --configuration Debug --framework net8.0
```

Verify that the updated game and ruleset assemblies are copied into that executable's output directory. Keep this requirement for every future code change in this project.

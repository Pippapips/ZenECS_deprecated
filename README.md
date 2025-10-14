# ZenECS Mono-Repo Template

This repository hosts **ZenECS Core** and **ZenECS Adapter for Unity** in a single Git mono-repo.

- UPM (Git URL): `Packages/com.zenecs.core`, `Packages/com.zenecs.adapter.unity`
- NuGet (Core): `src/ZenECS.Core/ZenECS.Core.csproj` links Core sources from UPM runtime
- Split tags (optional): `upm-core-x.y.z`, `upm-adapter-x.y.z`

## Install (Unity via Git URL)

```jsonc
{
  "dependencies": {
    "com.zenecs.core": "https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.core#v1.0.0",
    "com.zenecs.adapter.unity": "https://github.com/Pippapips/ZenECS.git?path=Packages/com.zenecs.adapter.unity#v1.0.0"
  }
}
```

## Install (Unity via split tags)

```jsonc
{
  "dependencies": {
    "com.zenecs.core": "https://github.com/Pippapips/ZenECS.git#upm-core-1.0.0",
    "com.zenecs.adapter.unity": "https://github.com/Pippapips/ZenECS.git#upm-adapter-1.0.0"
  }
}
```

## Install (NuGet)

```bash
dotnet add package ZenECS.Core --version 1.0.0
```

## Release
- Tag `core-vX.Y.Z` to publish NuGet (CI).
- Run the **UPM Split & Tag** workflow with versions to create `upm-core-*` / `upm-adapter-*` tags.

MIT © Pippapips Limited

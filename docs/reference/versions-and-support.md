---
title: Versions and support
description: The release cadence, what LTS means, and how to upgrade a project.
order: 10
---

## The cadence

A major .NET version ships every November.

| Track | Released | Supported for |
| --- | --- | --- |
| LTS (even-numbered: 8, 10, …) | November | 3 years |
| STS (odd-numbered: 9, 11, …) | November | 18 months |

Both tracks are production quality. LTS means a longer support window, not a more stable product. Choose LTS when upgrades are expensive to schedule; choose the latest when you want the newest runtime and language features and can upgrade yearly.

Running an unsupported version means no security patches — an audit finding, not just a preference.

## Target framework monikers

| TFM | Means |
| --- | --- |
| `net10.0` | .NET 10, any OS |
| `net10.0-windows` | .NET 10 plus the Windows-specific APIs |
| `net10.0-android`, `net10.0-ios` | MAUI targets |
| `netstandard2.0` | The compatibility target for libraries that must also serve .NET Framework |

Applications target a single, current TFM. **Libraries** may multi-target:

```xml
<TargetFrameworks>net10.0;netstandard2.0</TargetFrameworks>
```

Only do this when you genuinely have consumers on the older platform — every extra target multiplies your conditional compilation and your test matrix.

## Upgrading

1. Change `<TargetFramework>` and update `global.json`.
2. Update packages to versions built for the new release (`dotnet list package --outdated`).
3. Build with `-warnaserror` and read the new analyser warnings — they usually point at real issues.
4. Run the tests, including [integration tests](/docs/testing/integration-testing).
5. Read the breaking-changes list for the release; most affect few applications, but the ones that hit you hit hard.

`dotnet tool install -g upgrade-assistant` automates the mechanical parts, including .NET Framework migrations.

## Runtime roll-forward

A framework-dependent application built for `net10.0` runs on the newest installed 10.x patch by default. Control it when you need to:

```json
{ "rollForward": "latestMinor", "version": "10.0.0" }
```

in `runtimeconfig.template.json`, or with the `DOTNET_ROLL_FORWARD` environment variable.

## Checking what is installed

```bash
dotnet --info                     # SDKs, runtimes, RID
dotnet --list-sdks
dotnet --list-runtimes
```

## Further reading

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [Breaking changes](https://learn.microsoft.com/dotnet/core/compatibility/)

---
uid: Uno.Features.SkiaLibrariesTrimming
---

# Skia Native Libraries Trimming [Desktop only]

Starting with Uno 6.2, we are trimming rare Skia/HarfBuzz build variants by default.

Skia/HarfBuzz build variants were taking over 75% of the total published application size when doing a generic build.

By default, we now bundle win-x64, win-arm64, osx (includes arm64 + x64), linux-x64, linux-arm64 and linux-arm.

You may disable this behavior by adding a `<UnoIncludeAllSkiaNativeLibraries>true</UnoIncludeAllSkiaNativeLibraries>` property to your csproj.

You can also customize the build variant list by adding a `<UnoSkiaNativeLibraries>...</UnoSkiaNativeLibraries>` property to your csproj.

Where `...` is a semi-colon delimited list of the following values:

- win-arm64
- win-x64
- win-x86
- osx
- linux-arm
- linux-arm64
- linux-loongarch64
- linux-musl-arm
- linux-musl-arm64
- linux-musl-loongarch64
- linux-musl-riscv64
- linux-musl-x64
- linux-riscv64
- linux-x64
- linux-x86

For example, you may use: `<UnoSkiaNativeLibraries>win-arm64;win-x64</UnoSkiaNativeLibraries>`.

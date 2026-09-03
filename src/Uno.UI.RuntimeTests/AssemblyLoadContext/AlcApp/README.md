# ALC Test App

This is a simple test application designed to be loaded into a secondary AssemblyLoadContext (ALC) for testing Uno Platform's ALC hosting capabilities.

## Purpose

This app validates the following scenarios:

1. Loading an Uno application into a secondary ALC from a host application
2. Window.ContentHostOverride functionality - redirecting secondary ALC window content to a host ContentControl
3. Resource inheritance from the secondary ALC's Application.Current.Resources
4. Cross-ALC type sharing and isolation

## Important: Build Dependencies

**This app builds against the current binaries from the Uno.UI.RuntimeTests project**, not against published NuGet packages. This ensures that:

- ALC-related changes in the Uno Platform framework are immediately testable
- The test app uses the same Uno framework binaries as the host runtime tests
- Type identity is maintained across ALC boundaries (shared Uno assemblies from default ALC)

The test infrastructure uses a custom `TestAssemblyLoadContext` that:

- Loads all assemblies from the AlcApp output directory **except** Uno framework assemblies
- Allows Uno.*, Microsoft.UI.*, Windows.*, SkiaSharp, and HarfBuzzSharp to resolve from the default ALC
- Ensures the secondary app shares the same Uno framework types with the host app

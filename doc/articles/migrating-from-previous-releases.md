## Migrating from Previous Releases of Uno Platform

This article details the migration steps required to migrate from one version to the next.

# Uno 2.x to Uno 3.0

Migrating from Uno 2.x to Uno 3.0 requires a small set of changes in the code and configuration.

- **Android 8.0** is not supported anymore, you'll need to update to **Android 9.0** or **10.0**.
- For Android, you'll need to update the `Main.cs` file from:
    ```csharp
	: base(new App(), javaReference, transfer)
    ```
    to
    ```csharp
	: base(() => new App(), javaReference, transfer)
    ```
- For WebAssembly, in the `YourProject.Wasm.csproj`:
    - Change `<PackageReference Include="Uno.UI" Version="2.4.4" />` to `<PackageReference Include="Uno.UI.WebAssembly" Version="3.0.12" />`
    - Remove `<WasmHead>true</WasmHead>`
    - You can remove `__WASM__` in `DefineConstants`
- The symbols font has been updated, and the name needs to be updated. For more information, see [this article](uno-fluent-assets.md).

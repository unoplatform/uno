---
uid: uno.publishing.desktop.macos.advanced
---

# Publishing Your App for macOS - Advanced Topics

## App Bundle (.app) Customization

### Custom `Info.plist`

If your application requires extraneous permissions from macOS to execute some operations, e.g. using the camera, then you need to customize the `Info.plist` file of your app bundle.

You can create a basic `Info.plist` file yourself, using any text editor. The content should be like:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <!-- If a `LanguageId` is not provided it will default to `en-US` -->
  <key>CFBundleDevelopmentRegion</key>
  <string></string>

  <!-- If not specified the value of `ApplicationTitle` will be used -->
  <key>CFBundleDisplayName</key>
  <string></string>

  <!-- If not specified the value of `AssemblyName` will be used -->
  <key>CFBundleExecutable</key>
  <string></string>

  <!-- If not specified an `icon.icns` file will be created from the app icons and referenced here -->
  <key>CFBundleIconFile</key>
  <string></string>

  <!-- If not specified the value of `ApplicationId` will be used -->
  <key>CFBundleIdentifier</key>
  <string></string>

  <!-- If not specified the first 16 characters of `ApplicationTitle` will be used -->
  <key>CFBundleName</key>
  <string></string>

  <!-- If not specified the value of `ApplicationDisplayVersion` will be used -->
  <key>CFBundleShortVersionString</key>
  <string></string>

  <!-- If not specified the value of `ApplicationVersion` will be used -->
  <key>CFBundleVersion</key>
  <string></string>

  <!--
	The entries for CFBundleInfoDictionaryVersion, CFBundlePackageType and
	NSPrincipalClass will be added automatically and should not be changed.

	The comments will be removed from this file when saved inside the bundle.

	You can add other entries, like permissions, as needed.
	-->
</dict>
</plist>
```

You can edit the `Info.plist` file, add any required entries (for permissions), and leave other fields empty. The basic, empty fields will be filled automatically by the `msbuild` task based on your project.

Then from the CLI run:

```bash
dotnet publish -f net10.0-desktop -p:SelfContained=true -p:PackageFormat=app -p:UnoMacOSCustomInfoPlist=path/to/Info.plist
```

### Hardened Runtime

[Hardened Runtime](https://developer.apple.com/documentation/security/hardened-runtime) is `true` by default as it is **required** for [notarization](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution).

If needed you can turn it off by providing `-p:UnoMacOSHardenedRuntime=false` on the CLI.

```bash
dotnet publish -f net10.0-desktop -p:SelfContained=true -p:PackageFormat=app -p:UnoMacOSHardenedRuntime=false
```

### Custom Entitlements

[Entitlements](https://developer.apple.com/documentation/bundleresources/entitlements) grants permission to your executable. If nothing is specified the default entitlements required for a notarization-compatible dotnet-based application will be included automatically.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>com.apple.security.cs.allow-jit</key>
    <true/>
  </dict>
</plist>
```

You can provide your own entitlement file if needed using `-p:UnoMacOSEntitlements=/path/to/entitlements.plist` on the CLI.

```bash
dotnet publish -f net10.0-desktop -p:SelfContained=true -p:PackageFormat=app -p:UnoMacOSEntitlements=/path/to/entitlements.plist
```

### Trimming

App bundles that are distributed should be self-contained applications that depend only on the OS to execute. However bundling the dotnet runtime, base class libraries, and Uno Platform libraries produce a rather large application size.

To reduce the size of the app bundle you can enable dotnet's [trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options#enable-trimming) when publishing the app, using `-p:PublishTrimmed=true`. The full command from the CLI would be:

```bash
dotnet publish -f net10.0-desktop -p:SelfContained=true -p:PackageFormat=app -p:PublishTrimmed=true
```

> [!IMPORTANT]
> Your code and dependencies need to be [trimmer-aware](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) and the trimmed app bundle should be carefully tested to ensure the code removed by the trimmer does not affect its functionality.

### Optional files

`dotnet publish` includes several files that are not strictly required to execute your application. To reduce the app bundle size most of those files are **not** included, by default, inside the app bundles.

#### Including dotnet `createdump` tool

Although useful for debugging, the `createdump` executable is rarely used by the application's consumers and, by default, is not included in the app bundle.

If you wish to include `createdump` inside your app bundle add the `-p:UnoMacOSIncludeCreateDump=true` on the CLI.

```bash
dotnet publish -f net10.0-desktop -p:SelfContained=true -p:PackageFormat=app -p:UnoMacOSIncludeCreateDump=true
```

#### Including `libclrgc.dylib` extraneous GC

An alternate garbage collection (GC) library is included by `dotnet publish`. It is, by default, removed from the app bundle since it would not be used at runtime.

If you wish to include this extra GC library inside your app bundle add the `-p:UnoMacOSIncludeExtraClrGC=true` on the CLI.

```bash
dotnet publish -f net10.0-desktop -p:SelfContained=true -p:PackageFormat=app -p:UnoMacOSIncludeExtraClrGC=true
```

#### Including `libmscordaccore.dylib` and `libmscordbi.dylib` debugging support

Extraneous debugging support libraries are included by `dotnet publish`. They are, by default, removed from the app bundle since it's unlikely to be used for debugging.

If you wish to include the extra debugging libraries inside your app bundle add the `-p:UnoMacOSIncludeNativeDebugging=true` on the CLI.

```bash
dotnet publish -f net10.0-desktop -p:SelfContained=true -p:PackageFormat=app -p:UnoMacOSIncludeNativeDebugging=true
```

#### Including assemblies debugging symbols (.pdb) files

dotnet debugging symbols (`.pdb`) are generally included in released applications since they help to provide better stack traces and help developers resolve issues. As such, they are, by default, included inside the app bundle.

If you wish to remove them anyway, you can do so by adding the `-p:UnoMacOSIncludeDebugSymbols=false` on the CLI.

```bash
dotnet publish -f net10.0-desktop -p:SelfContained=true -p:PackageFormat=app -p:UnoMacOSIncludeDebugSymbols=false
```

## Disk Image (.dmg) Customization

### Symlink to /Applications

By default, the produced disk image will contain a symlink to `/Applications` so users can drag-and-drop the app bundle inside it. If you do not want the symlink to be present inside the disk image you can add `-p:UnoMacOSIncludeSymlinkToApplications=false` on the command line.

```bash
dotnet publish -f net10.0-desktop -p:SelfContained=true -p:PackageFormat=dmg -p:UnoMacOSIncludeSymlinkToApplications=false -p:CodesignKey={{identity}} -p:DiskImageSigningKey={{identity}}
```

### Additional Customization

Further disk image customization is possible but can be tricky since it requires modification to the `.DS_Store` binary file inside the disk image (many trials and errors). If more control is required (e.g. icon positioning, background image...) we recommend using 3rd party tools created specifically for this purpose. Some free/open-source examples are:

- [create-dmg](https://github.com/sindresorhus/create-dmg)
- [dmgbuild](https://dmgbuild.readthedocs.io/en/latest/)

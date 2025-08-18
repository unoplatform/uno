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
dotnet publish -f net9.0-desktop -p:PackageFormat=app -p:UnoMacOSCustomInfoPlist=path/to/Info.plist
```

### Hardened Runtime

[Hardened Runtime](https://developer.apple.com/documentation/security/hardened-runtime) is `true` by default as it is **required** for [notarization](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution).

If needed you can turn it off by providing `-p:UnoMacOSHardenedRuntime=false` on the CLI.

```bash
dotnet publish -f net9.0-desktop -p:PackageFormat=app -p:UnoMacOSHardenedRuntime=false
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
dotnet publish -f net9.0-desktop -p:PackageFormat=app -p:UnoMacOSEntitlements=/path/to/entitlements.plist
```

### Trimming

App bundles that are distributed should be self-contained applications that depend only on the OS to execute. However bundling the dotnet runtime, base class libraries, and Uno Platform libraries produce a rather large application size.

To reduce the size of the app bundle you can enable dotnet's [trimming](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options#enable-trimming) when publishing the app, using `-p:PublishTrimmed=true`. The full command from the CLI would be:

```bash
dotnet publish -f net9.0-desktop -p:PackageFormat=app -p:PublishTrimmed=true
```

> [!IMPORTANT]
> Your code and dependencies need to be [trimmer-aware](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/prepare-libraries-for-trimming) and the trimmed app bundle should be carefully tested to ensure the code removed by the trimmer does not affect its functionality.

### Removing other platforms

Since app bundles will **only** run on macOS, you can remove the unused platforms from the app bundle to reduce its size. This can be done by modifying the `./Platforms/Desktop/Program.cs` file of your project, like this (diff format):

```diff
         var host = UnoPlatformHostBuilder.Create()
             .App(() => new App())
+#if !UNO_MACOS_APPBUNDLE
             .UseX11()
             .UseLinuxFrameBuffer()
-            .UseMacOS()
             .UseWin32()
+#endif
+            .UseMacOS()
             .Build();

         host.Run();
```

and the build (or publish) with and additional `-p:DefineConstants=UNO_MACOS_APPBUNDLE` on the command line. This works best when trimming is also enabled.

### Optional files

`dotnet publish` includes several files that are not strictly required to execute your application. To reduce the app bundle size most of those files are **not** included, by default, inside the app bundles.

#### Including dotnet `createdump` tool

Although useful for debugging, the `createdump` executable is rarely used by the application's consumers and, by default, is not included in the app bundle.

If you wish to include `createdump` inside your app bundle add the `-p:UnoMacOSIncludeCreateDump=true` on the CLI.

```bash
dotnet publish -f net9.0-desktop -p:PackageFormat=app -p:UnoMacOSIncludeCreateDump=true
```

#### Including `libclrgc.dylib` extraneous GC

An alternate garbage collection (GC) library is included by `dotnet publish`. It is, by default, removed from the app bundle since it would not be used at runtime.

If you wish to include this extra GC library inside your app bundle add the `-p:UnoMacOSIncludeExtraClrGC=true` on the CLI.

```bash
dotnet publish -f net9.0-desktop -p:PackageFormat=app -p:UnoMacOSIncludeExtraClrGC=true
```

#### Including `libmscordaccore.dylib` and `libmscordbi.dylib` debugging support

Extraneous debugging support libraries are included by `dotnet publish`. They are, by default, removed from the app bundle since it's unlikely to be used for debugging.

If you wish to include the extra debugging libraries inside your app bundle add the `-p:UnoMacOSIncludeNativeDebugging=true` on the CLI.

```bash
dotnet publish -f net9.0-desktop -p:PackageFormat=app -p:UnoMacOSIncludeNativeDebugging=true
```

#### Including assemblies debugging symbols (.pdb) files

dotnet debugging symbols (`.pdb`) are generally included in released applications since they help to provide better stack traces and help developers resolve issues. As such, they are, by default, included inside the app bundle.

If you wish to remove them anyway, you can do so by adding the `-p:UnoMacOSIncludeDebugSymbols=false` on the CLI.

```bash
dotnet publish -f net9.0-desktop -p:PackageFormat=app -p:UnoMacOSIncludeDebugSymbols=false
```

## Disk Image (.dmg) Customization

### Symlink to /Applications

By default, the produced disk image will contain a symlink to `/Applications` so users can drag-and-drop the app bundle inside it. If you do not want the symlink to be present inside the disk image you can add `-p:UnoMacOSIncludeSymlinkToApplications=false` on the command line.

```bash
dotnet publish -f net9.0-desktop -p:PackageFormat=dmg -p:UnoMacOSIncludeSymlinkToApplications=false -p:CodesignKey={{identity}} -p:DiskImageSigningKey={{identity}}
```

### Additional Customization

Further disk image customization is possible but can be tricky since it requires modification to the `.DS_Store` binary file inside the disk image (many trials and errors). If more control is required (e.g. icon positioning, background image...) we recommend using 3rd party tools created specifically for this purpose. Some free/open-source examples are:

- [create-dmg](https://github.com/sindresorhus/create-dmg)
- [dmgbuild](https://dmgbuild.readthedocs.io/en/latest/)

## Re-using the app packaging tasks individually

If you already have an app bundle built, then you can use those commands to (re)sign, package, create a disk image, and notarize it. It's useful for fat app bundles, which require an extra merge step, as well as allowing further customization (that would break a digital signature) after an app bundle is created.

### Create a fat app bundle from arch-specific app bundles

This approach is useful when you want to provide some custom build options when creating the arch-specific app bundles, e.g. enabling trimming, or if you want to create a fat app bundle from existing arch-specific app bundles.

If you want to create a fat app bundle that can run on both Intel and Apple Silicon Macs, you need to merge the arch-specific app bundles for `x64` and `arm64` architectures together.

First, create the `x64` app bundle using the `-r osx-x64` runtime identifier (RID) and any other options you need.

```bash
dotnet publish project.csproj -f net9.0-desktop -r osx-x64 -p:PackageFormat=app ...
```

> [!NOTE]
> There is no need to sign the _thin_ app bundles since the merge process will invalidate any digital signature.

Next, create the `arm64` app bundle using the `-r osx-arm64` runtime identifier (RID), again specifying any other options you need.

```bash
dotnet publish project.csproj -f net9.0-desktop -r osx-arm64 -p:PackageFormat=app ...
```

Finally, merge both arch-specific app bundles into a fat (multi-arch) app bundle.

```bash
dotnet build project.csproj -t:UnoMergeBundles -p:_IsPublishing=true -restore:false -f:net9.0-desktop -p:UnoX64Bundle=bin/Release/net9.0-desktop/osx-x64/publish/UnoApp.app -p:UnoArm64Bundle=bin/Release/net9.0-desktop/osx-arm64/publish/UnoApp.app -p:UnoFatBundle=UnoApp.app
```

Where `UnoX64Bundle` and `UnoArm64Bundle` are the (input) paths to the arch-specific app bundles created in the previous steps and `UnoFatBundle` is the (output) path where the resulting fat app bundle will be created (replacing any existing one).

> [!NOTE]
> You can sign the fat app bundle at this stage by providing `-p:CodesignKey={{identity}}`. However you can also [sign it later](https://platform.uno/docs/articles/uno-publishing-desktop-macos-advanced.html#(re)signing-an-app-bundle) if you have to customize the app bundle.

### (Re)signing an app bundle

Most changes done to an app bundle will break the digital signature, if present. If you need to customize the app bundle after it has been created, you will need to re-sign it using this command:

```bash
dotnet build project.csproj -t:UnoSignAppBundle -p:_IsPublishing=true -restore:false -f:net9.0-desktop -p:AppBundlePath=UnoApp.app -p:CodesignKey={{identity}}
```

where `{{identity}}` is the name of the signing identity to use for signing the app bundle, more details in [How to find your identity](https://platform.uno/docs/articles/uno-publishing-desktop-macos.html#how-to-find-your-identity)

### Creating a package for an app bundle

If you did not create a package (.pkg) while building the app bundle, you can create one using the following command:

```bash
dotnet build project.csproj -t:UnoPackageAppBundle -p:_IsPublishing=true -restore:false -f:net9.0-desktop -p:AppBundlePath=UnoApp.app
```

### Creating a disk image for an app bundle

If you did not create a disk image (.dmg) while building the app bundle, you can create one using the following command:

```bash
dotnet build project.csproj -t:UnoCreateDiskImage -p:_IsPublishing=true -restore:false -f:net9.0-desktop -p:AppBundlePath=UnoApp.app
```

### Notarizing an app bundle, package or disk image

It's possible to notarize an app bundle, package or disk image after it has been created. This is useful for fat app bundles, which require an extra merge step, as well as allowing further customization (that would break a digital signature) after an app bundle is created.

```bash
dotnet build project.csproj -t:UnoNotarize -p:_IsPublishing=true -restore:false -f:net9.0-desktop -p:InputFilePath={{file-to-notarize}} -p:PackageFormat={{file-format}} ...
```

See the following notarization section for each format.

- [`.pkg`](https://platform.uno/docs/articles/uno-publishing-desktop-macos.html#notarize-the-package)
- [`.dmg`](https://platform.uno/docs/articles/uno-publishing-desktop-macos.html#notarize-the-disk-image)

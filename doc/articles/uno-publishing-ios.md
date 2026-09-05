---
uid: uno.publishing.ios
---

# Publishing Your App for iOS

iOS apps are distributed as a signed `.ipa` package that is uploaded to [App Store Connect](https://appstoreconnect.apple.com), then released through [TestFlight](https://developer.apple.com/help/app-store-connect/test-a-beta-version/overview-of-testflight) (beta testing) or the App Store (public release).

This guide covers the full path for an Uno Platform app: prerequisites, the app configuration Apple validates at upload time, code signing, building the `.ipa`, and uploading it.

> [!IMPORTANT]
> Building and publishing for iOS is **only supported on macOS**. Windows users can build through a [paired Mac build host](https://learn.microsoft.com/dotnet/maui/ios/pair-to-mac), but the packaging and signing steps still run on the Mac.

## Prerequisites

### Apple Developer Program membership

An active [Apple Developer Program](https://developer.apple.com/programs/) membership is required to create distribution certificates, provisioning profiles, and App Store Connect records. A free Apple Account is only sufficient for local device deployment, not for distribution.

Make sure any pending agreements are signed in your [Apple Developer Account](https://developer.apple.com/account) — an unsigned agreement causes signing and upload failures that report as generic HTTP 403 errors.

### Xcode and the iOS SDK

> [!IMPORTANT]
> Since **April 28, 2026**, Apple requires that apps uploaded to App Store Connect are built with **Xcode 26 or later, using the iOS 26 SDK or later**. See [Upcoming requirements](https://developer.apple.com/news/upcoming-requirements/) on developer.apple.com. Builds produced with an older SDK are rejected at upload.

The SDK used is determined by the Xcode installation selected on the build machine, not by your project file. Verify and select it with:

```bash
# Show the currently selected Xcode
xcode-select -p

# Select a specific Xcode installation
sudo xcode-select -s /Applications/Xcode.app

# Or scope the selection to a single build (useful on CI with multiple Xcodes installed)
DEVELOPER_DIR=/Applications/Xcode_26.app dotnet publish ...
```

> [!TIP]
> On CI, point `DEVELOPER_DIR` at a **canonical (non-symlinked) path**. `actool` and some `xcrun` tools fail when Xcode is reached through a symlink.

### .NET SDK and iOS workload

The .NET for iOS workload validates the Xcode version at build time and must match the Xcode you have installed. A mismatch fails the build with:

```error
This version of .NET for iOS (26.0.xxxx) requires Xcode 26.0. The current version of Xcode is 26.1.
Either install Xcode 26.0, or use a different version of .NET for iOS.
```

To resolve this, either install the Xcode version the workload expects, or update the workload to one that supports your Xcode. The .NET 10 iOS workload builds against the SDKs shipped with Xcode 26.

Install or repair the workloads with [`uno-check`](xref:UnoCheck.UsingUnoCheck):

```bash
dotnet tool update -g uno.check
uno-check --target ios
```

See [.NET version support](xref:Uno.Development.NetVersionSupport) for the versions supported by your Uno Platform release.

## Preparing for publish

Before producing a release build:

- [Configure the IL Linker](xref:uno.articles.features.illinker) and [enable XAML and resource trimming](xref:Uno.Features.ResourcesTrimming) to reduce the package size.
- [Profile your app](xref:Uno.Tutorials.ProfilingApplications) and review [performance guidance](xref:Uno.Development.Performance).

## Configuring your app for the App Store

Most of the values Apple validates come from your project file — the Uno.Sdk forwards them into the generated `Info.plist`. The rest live in `Platforms/iOS/`.

### Bundle identifier

The bundle identifier must be **identical** in three places, or signing or upload fails:

1. The `ApplicationId` property in your `csproj` (forwarded to `CFBundleIdentifier`).
2. The App ID registered in [Certificates, Identifiers & Profiles](https://developer.apple.com/account/resources/identifiers/list) and encoded in your provisioning profile.
3. The App Store Connect record for the app.

```xml
<PropertyGroup>
  <ApplicationId>com.mycompany.myapp</ApplicationId>
  <ApplicationTitle>My App</ApplicationTitle>
</PropertyGroup>
```

### Version numbers

Two distinct values are required, and Apple validates each differently:

| Property | `Info.plist` key | Rule |
|---|---|---|
| `ApplicationDisplayVersion` | `CFBundleShortVersionString` | The user-visible version. Must be **one to three period-separated integers** (`1.0`, `1.2.3`). |
| `ApplicationVersion` | `CFBundleVersion` | The build number. Must **strictly increase** with every upload for a given display version. |

```xml
<PropertyGroup>
  <ApplicationDisplayVersion>1.0.0</ApplicationDisplayVersion>
  <ApplicationVersion>1</ApplicationVersion>
</PropertyGroup>
```

> [!WARNING]
> A SemVer string with a prerelease suffix (for example `1.0.0-dev.42`) is **rejected** at upload with `The value for key CFBundleShortVersionString ... must be composed of one to three period-separated integers (-19239)`. On CI, feed `ApplicationDisplayVersion` a plain numeric version and use a monotonic build counter for `ApplicationVersion`.

### App icon and splash screen

Uno Platform apps use [Uno.Resizetizer](xref:Uno.Resizetizer.GettingStarted) to generate the iOS app icon set and launch screen from a single SVG. See [Splash screen](xref:Uno.Development.SplashScreen) for configuration.

> [!IMPORTANT]
> The iOS app icon must be **fully opaque**. Any transparency or alpha channel in the generated icon is rejected at upload with `ERROR ITMS-90717: Invalid App Store Icon. The App Store Icon ... can't be transparent nor contain an alpha channel.`
>
> This is a common trap when the same source SVG is shared with WebAssembly or Desktop, where a transparent icon *is* wanted. Keep an opaque background shape in the iOS icon source, and note that setting `UnoIconBackgroundColor` alone is not enough — Resizetizer paints the background *file* on top of that color, so a transparent source SVG stays transparent.

### Privacy manifest

Since **May 1, 2024**, Apple requires a [privacy manifest](https://developer.apple.com/documentation/bundleresources/privacy_manifest_files) (`PrivacyInfo.xcprivacy`) declaring approved reasons for the "required reason" APIs used by your app **and by any third-party SDK it embeds**. .NET apps always reach some of these APIs through the runtime and BCL, so the file is effectively mandatory — an upload whose usage is not declared is rejected.

Projects using the Uno.Sdk 5.2 or later automatically bundle `Platforms/iOS/PrivacyInfo.xcprivacy` when the file is present. The Uno Platform templates ship a starting point covering the .NET runtime and BCL; extend it for the APIs your own code and dependencies use.

A typical baseline for an Uno Platform app declares file timestamp, system boot time, disk space, and user defaults access:

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
  <dict>
    <key>NSPrivacyAccessedAPITypes</key>
    <array>
      <dict>
        <key>NSPrivacyAccessedAPIType</key>
        <string>NSPrivacyAccessedAPICategoryFileTimestamp</string>
        <key>NSPrivacyAccessedAPITypeReasons</key>
        <array><string>C617.1</string></array>
      </dict>
      <dict>
        <key>NSPrivacyAccessedAPIType</key>
        <string>NSPrivacyAccessedAPICategorySystemBootTime</string>
        <key>NSPrivacyAccessedAPITypeReasons</key>
        <array><string>35F9.1</string></array>
      </dict>
      <dict>
        <key>NSPrivacyAccessedAPIType</key>
        <string>NSPrivacyAccessedAPICategoryDiskSpace</string>
        <key>NSPrivacyAccessedAPITypeReasons</key>
        <array><string>E174.1</string></array>
      </dict>
      <dict>
        <key>NSPrivacyAccessedAPIType</key>
        <string>NSPrivacyAccessedAPICategoryUserDefaults</string>
        <key>NSPrivacyAccessedAPITypeReasons</key>
        <array><string>CA92.1</string></array>
      </dict>
    </array>
  </dict>
</plist>
```

If your app collects data (analytics, crash reporting, account identifiers), also declare `NSPrivacyCollectedDataTypes`, and keep it consistent with the **App Privacy** answers in App Store Connect.

For details, see [Apple's documentation](https://developer.apple.com/documentation/bundleresources/privacy_manifest_files), the [Microsoft .NET guidance](https://learn.microsoft.com/dotnet/maui/ios/privacy-manifest), and [Apple Privacy Manifest support](xref:Uno.Features.Uno.Sdk#apple-privacy-manifest-support) in the Uno.Sdk documentation.

### Export compliance

App Store Connect asks for [export compliance documentation](https://developer.apple.com/help/app-store-connect/manage-builds/manage-app-encryption-documentation) for every uploaded build. Declaring it in `Platforms/iOS/Info.plist` answers the question once at build time instead of per upload — without it, TestFlight distribution stalls on a manual prompt for each release.

```xml
<key>ITSAppUsesNonExemptEncryption</key>
<false/>
```

`false` declares that the app only uses encryption exempt from U.S. export documentation requirements — such as HTTPS and standard system APIs. If your app ships custom cryptography that is not covered by the exemption, set this to `true` and provide `ITSEncryptionExportComplianceCode`. See [Complying with Encryption Export Regulations](https://developer.apple.com/documentation/security/complying-with-encryption-export-regulations).

### Entitlements and capabilities

Capabilities such as Push Notifications, Sign in with Apple, App Groups, or Associated Domains must be enabled in **two** places that have to agree:

1. On the App ID in your Apple Developer Account (which regenerates the provisioning profile).
2. In `Platforms/iOS/Entitlements.plist` in your project.

The Uno.Sdk automatically picks up `Platforms/iOS/Entitlements.plist` (or `Entitlements-$(Configuration).plist`) as `CodesignEntitlements`. An entitlement present in the app but missing from the profile fails signing; the reverse usually passes signing but breaks the feature at runtime.

If your app handles custom URL schemes — for example an OAuth redirect for [authentication](xref:Uno.Extensions.Authentication.Overview) — declare them under `CFBundleURLTypes` in `Info.plist` and keep the scheme in sync with your app configuration.

### Deployment target and device families

`SupportedOSPlatformVersion` sets the minimum iOS version your app runs on (the Uno.Sdk defaults to `14.2`). This is independent of the *build* SDK requirement above — you can build with the iOS 26 SDK while still supporting older devices.

```xml
<PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
  <SupportedOSPlatformVersion>15.0</SupportedOSPlatformVersion>
</PropertyGroup>
```

`UIDeviceFamily` in `Info.plist` declares whether the app targets iPhone (`1`), iPad (`2`), or both. If you list iPad, App Review will test on iPad — make sure the layouts hold up.

## Code signing

App Store builds require an **Apple Distribution** certificate and an **App Store** provisioning profile.

### Create a distribution certificate

If you don't already have one, create it from your [Apple Developer Account](https://developer.apple.com/help/account/create-certificates/create-distribution-certificates/). Once installed in your keychain, list the valid identities on your Mac:

```bash
security find-identity -v -p codesigning
```

```text
  1) 8C8D47A2A6F7428971A8AA5C6D8F7A30D344E93C "Apple Development: John Appleby (XXXXXXXXXX)"
  2) 0357503C3CF78B093A764EA382BF10E7D3AEDA9A "Apple Distribution: John Appleby (XXXXXXXXXX)"
     2 valid identities found
```

Use the `Apple Distribution: *` entry (the older name `iPhone Distribution` also matches these certificates) for App Store builds.

### Create a provisioning profile

Create an **App Store** distribution provisioning profile bound to your App ID, following [Apple's instructions](https://developer.apple.com/help/account/manage-profiles/create-an-app-store-provisioning-profile). Download and install it on the build machine — double-clicking the `.mobileprovision` file installs it.

### Configure signing in your project

Add the signing configuration to your `csproj`, scoped to iOS release builds so local debug builds keep using automatic provisioning:

```xml
<Choose>
  <When Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
    <PropertyGroup Condition="'$(Configuration)' == 'Release'">
      <RuntimeIdentifier>ios-arm64</RuntimeIdentifier>
      <CodesignKey>Apple Distribution</CodesignKey>
      <!-- Name of the App Store provisioning profile. Optional: when omitted,
           the iOS SDK matches an installed profile against the bundle id. -->
      <CodesignProvision>My App App Store</CodesignProvision>
      <ArchiveOnBuild>true</ArchiveOnBuild>
    </PropertyGroup>
  </When>
</Choose>
```

| Property | Purpose |
|---|---|
| `RuntimeIdentifier` | `ios-arm64` is the only device architecture the App Store accepts. |
| `CodesignKey` | The signing identity to select from the keychain. |
| `CodesignProvision` | The provisioning profile name. Omit to let the SDK match by bundle id. |
| `CodesignEntitlements` | Set automatically by the Uno.Sdk from `Platforms/iOS/Entitlements.plist`. |
| `ArchiveOnBuild` | Produces the `.ipa` package as part of the build. |

## Building the `.ipa`

From a terminal on macOS, navigate to your app's `csproj` folder and run:

```bash
dotnet publish -f net10.0-ios -c Release \
  -p:RuntimeIdentifier=ios-arm64 \
  -p:ArchiveOnBuild=true \
  -p:ApplicationDisplayVersion=1.0.0 \
  -p:ApplicationVersion=1
```

Any property already set in the `csproj` can be omitted from the command line; values passed on the command line take precedence.

The signed `.ipa` is written to `bin/Release/net10.0-ios/ios-arm64/publish/`. Add `-o <folder>` to redirect the output.

> [!IMPORTANT]
> `-p:ArchiveOnBuild=true` is what produces the `.ipa`. Without it the build only produces an `.app` bundle, and there is nothing to upload. On CI, assert that exactly one `.ipa` was produced — signing problems are sometimes logged as warnings rather than errors, leaving a build that "succeeded" with no package.

### Debug symbols

The build also produces `.dSYM` bundles alongside the `.ipa`. Keep them for the exact build you ship — they are required to symbolicate crash reports from TestFlight and the App Store. Archive them as a build artifact rather than relying on the local `bin` folder.

## Uploading to App Store Connect

> [!NOTE]
> Unlike macOS apps distributed outside the Mac App Store, iOS apps **do not need to be notarized** — Apple handles binary distribution for App Store and TestFlight builds. See [Publishing Your App for macOS](xref:uno.publishing.desktop.macos) if you also ship a desktop build.

### Create the app record first

Before the first upload, create the app record in [App Store Connect](https://appstoreconnect.apple.com) with a bundle id matching your app. See [Add a new app](https://developer.apple.com/help/app-store-connect/create-an-app-record/add-a-new-app). Uploads for a bundle id that has no record are rejected.

You will also need an [app-specific password](https://support.apple.com/en-us/102654) for command-line uploads, since Apple Accounts require two-factor authentication.

### Transporter (recommended for manual uploads)

[Transporter](https://apps.apple.com/us/app/transporter/id1450874784) is Apple's free macOS app for uploading builds. Drag the `.ipa` into it, sign in with your Apple Account, and deliver. It reports validation warnings and errors with the detail needed to diagnose rejected packages.

### Command line

`altool`, included with Xcode, validates and uploads a package without leaving the terminal:

```bash
# Validate before uploading — catches most rejections early
xcrun altool --validate-app -f ./publish/MyApp.ipa -t ios \
  -u john.appleby@example.com -p "aaaa-bbbb-cccc-dddd"

xcrun altool --upload-app -f ./publish/MyApp.ipa -t ios \
  -u john.appleby@example.com -p "aaaa-bbbb-cccc-dddd"
```

Where `-p` is the app-specific password. For unattended pipelines, prefer an App Store Connect API key (`--apiKey` / `--apiIssuer`) over an Apple Account password.

The [App Store Connect API](https://developer.apple.com/documentation/appstoreconnectapi) is also available if you need to script the surrounding release workflow.

### Continuous integration

Uno Platform templates include [CI pipelines](xref:Uno.GettingStarted.UsingWizard#11-ci-pipeline) for Azure DevOps and GitHub Actions. For an iOS App Store lane, the pipeline needs to:

1. Select the Xcode version that satisfies Apple's SDK requirement.
2. Install the distribution certificate and provisioning profile into a temporary keychain — on Azure DevOps, the `InstallAppleCertificate` and `InstallAppleProvisioningProfile` tasks.
3. Run `dotnet publish` with `ArchiveOnBuild=true`, supplying a strictly increasing `ApplicationVersion` from the build counter.
4. Publish the `.ipa` and `.dSYM` bundles as build artifacts.
5. Upload the `.ipa` — for example with the [Apple App Store extension](https://marketplace.visualstudio.com/items?itemName=ms-vsclient.app-store) for Azure DevOps, or [upload-testflight-build](https://github.com/Apple-Actions/upload-testflight-build) for GitHub Actions.

> [!TIP]
> Gate the upload step so it only runs on your release branches. Every upload consumes a build number permanently — a pull-request build that uploads burns numbers you cannot reuse.

## TestFlight and release

Once processing completes, the build appears in App Store Connect and can be distributed through TestFlight to internal testers immediately. External testers and public App Store release both require App Review approval — see the [App Review Guidelines](https://developer.apple.com/app-store/review/guidelines/).

Independently of your build, these account-level requirements must be satisfied or submission is blocked:

- **Age rating** — Apple's updated age rating questionnaire must be answered for each app.
- **Trader status** — required for apps distributed in the European Union under the Digital Services Act. See [Manage trader status](https://developer.apple.com/help/app-store-connect/manage-your-app-availability/manage-trader-status).
- **App Privacy** — the data collection answers in App Store Connect must be consistent with your privacy manifest.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| Build succeeds but no `.ipa` is produced | `ArchiveOnBuild=true` is missing, or signing silently failed. Re-run with `-bl` and inspect the binlog. |
| `requires Xcode 26.0. The current version of Xcode is …` | The .NET for iOS workload and installed Xcode disagree. Align them, or select the expected Xcode with `xcode-select` / `DEVELOPER_DIR`. |
| `ERROR ITMS-90717: Invalid App Store Icon` | The app icon contains transparency. Make the icon source opaque. |
| `CFBundleShortVersionString … one to three period-separated integers (-19239)` | `ApplicationDisplayVersion` contains a prerelease suffix. Use a plain numeric version. |
| `The bundle version must be higher than the previously uploaded version` | `ApplicationVersion` was not incremented. Use a monotonic build counter. |
| Missing privacy manifest / required reason API errors | `PrivacyInfo.xcprivacy` is absent or does not cover an API used by your code or a third-party SDK. |
| Upload fails with HTTP 403 / "a required agreement is missing" | An Apple Developer Program agreement is unsigned or expired. Sign in to your account and accept it, then retry after a few minutes. |
| No matching provisioning profile found | The profile's bundle id, capabilities, or certificate does not match the app. Regenerate the profile after changing capabilities. |

For build and deployment problems unrelated to packaging, see [Issues related to iOS projects](xref:Uno.UI.CommonIssues.Ios).

## See also

- [Publishing overview](xref:uno.publishing.overview)
- [Publishing Your App for Android](xref:uno.publishing.android)
- [Publishing Your App for macOS](xref:uno.publishing.desktop.macos)
- [Apple upcoming requirements](https://developer.apple.com/news/upcoming-requirements/)
- [Publish a .NET iOS app using the command line](https://learn.microsoft.com/dotnet/maui/ios/deployment/publish-cli)

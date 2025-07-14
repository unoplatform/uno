---
uid: uno.publishing.desktop.macos
---

# Publishing Your App for macOS

There are several options to publish your macOS application to your customers. They all start with creating an [app bundle](https://developer.apple.com/library/archive/documentation/CoreFoundation/Conceptual/CFBundles/AboutBundles/AboutBundles.html) (.app).

> [!IMPORTANT]
> Publishing for macOS is only supported on macOS

## Create an app bundle (.app)

The most basic app bundle can be created with:

```bash
dotnet publish -f net10.0-desktop -p:PackageFormat=app
```

However, this bundle would depend on the correct version of dotnet, `net10.0` in this case, to be installed on the Mac computer. In practice macOS end-users expect app bundles to be self-contained and not require anything extraneous to execute on their Mac computer.

You can create such a self-contained app bundle with:

```bash
dotnet publish -f net10.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=app
```

Where `{{RID}}` is either `osx-x64` or `osx-arm64`.

The resulting app bundle, which is a directory, will be located at `bin/Release/net10.0-desktop/{{RID}}/publish/{{APPNAME}}.app`.

> [!NOTE]
> The [structure of the app bundle](https://developer.apple.com/library/archive/documentation/CoreFoundation/Conceptual/CFBundles/BundleTypes/BundleTypes.html) requires a [custom native host](https://learn.microsoft.com/en-us/dotnet/core/tutorials/netcore-hosting) to run the app. As such, starting with Uno 6.1, the app bundles are **always** built as a self-contained executable, even if the `SelfContained` property is not set to `true` inside the project.

### Code Signing

> [!IMPORTANT]
> The code signing process requires access to the Internet to retrieve a secure timestamp.

To ensure the integrity of the app bundle Apple requires you to digitally sign your code. The key difference to producing a signed app bundle is to add `-p:CodesignKey={{identity}}` to specify which identity should be used to produce the signature.

```bash
dotnet publish -f net10.0-desktop -r osx-arm64 -p:SelfContained=true -p:PackageFormat=app -p:CodesignKey={{identity}}
```

You can use the special identity `-` to produce an ad-hoc signature. This basically tells macOS's [Gatekeeper](https://support.apple.com/en-us/102445) that the file is safe to use **locally**, however, it does not help distribute the app bundle.

> [!NOTE]
> Besides needed access to the Internet the code signing process slows down the builds. For local testing of your app, you do not need to sign the app bundle.

#### How to find your identity

If you have not already, you need to create your [developer certificates](https://developer.apple.com/help/account/create-certificates/create-developer-id-certificates/). Once you have created them, on your Mac computer, you can find your identities from the CLI, by running:

```bash
security find-identity -v
```

This will show you every **valid** identity available on your Mac.

```text
  1) 8C8D47A2A6F7428971A8AA5C6D8F7A30D344E93C "Apple Development: John Appleby (XXXXXXXXXX)"
  2) F84D25AAF30BAFA988D8B4CE8A0BA3BE891199D8 "Developer ID Installer: John Appleby (XXXXXXXXXX)"
  3) 0357503C3CF78B093A764EA382BF10E7D3AEDA9A "Apple Distribution: John Appleby (XXXXXXXXXX)"
  4) A148697E815F6090DE9698F8E2602773296E2689 "Developer ID Application: John Appleby (XXXXXXXXXX)"
     4 valid identities found
```

To properly sign an app bundle for publishing you need to use the `"Developer ID Application: *"` or its thumbprint (long hex number) entry as the identity.
Both

```bash
dotnet publish -f net10.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=app -p:CodesignKey="Developer ID Application: John Appleby (XXXXXXXXXX)"
```

and

```bash
dotnet publish -f net10.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=app -p:CodesignKey=A148697E815F6090DE9698F8E2602773296E2689
```

are functionally identical and will produce a signed app bundle.

## Distributing the app bundle

An app bundle is a directory and, as such, is not easy to distribute. Most macOS applications are distributed using one of the following methods.

### Installer (.pkg)

You can easily create an installer package for your app bundle. This will produce a single, compressed executable file that can be shared (if signed and notarized) with anyone using a Mac computer.

From the CLI run:

```bash
dotnet publish -f net10.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=pkg -p:CodesignKey={{identity}} -p:PackageSigningKey={{installer_identity}}
```

Where the following changes to the previous command are:

- modifying `PackageFormat` to `pkg` to produce the package. This package will include the app bundle inside it, so the `CodesignKey` argument is still required;
- adding `-p:PackageSigningKey={{installer_identity}}` to specify which identity should be used to sign the package. Unlike app bundles, signing requires a `Developer ID Installer: *` identity.

The resulting installer will be located at `bin/Release/net10.0-desktop/{{RID}}/publish/{{APPNAME}}.pkg`.

> [!IMPORTANT]
> The installer can behave weirdly locally (or on CI) since the app bundle name is known to macOS and it will try to update the application, where it was built or copied, instead of installing a copy of it under the `/Applications/` directory. Ensure you are testing your package installer on a different Mac or inside a clean virtual machine (VM).

#### Notarize the package

Having both the app bundle (.app) and installer (.pkg) signed is insufficient. As the package is binary and you'll share it with customers, Apple must also notarize it.

The first step is to store your Apple Account credentials inside the key store. This makes all the further commands (and notarization) much simpler. From the CLI run:

```bash
xcrun notarytool store-credentials {{notarytool-credentials}} --apple-id john.appleby@platform.uno --team-id XXXXXXXXXX --password aaaa-bbbb-cccc-dddd
```

where

- `{{notarytool-credentials}}` is the name of your credentials inside the key store.
- `--apple-id` provides the email address used for your [Apple Account](https://developer.apple.com/account).
- `--team-id` provides your team ID, a 10-character code. [How to find it](https://developer.apple.com/help/account/manage-your-team/locate-your-team-id).
- `--password` is an [app specific password](https://support.apple.com/en-us/102654) created specifically for `notarytool`

> [!NOTE]
> Since Apple Accounts requires two factors authentication (2FA) you will need to [create an app-specific password](https://support.apple.com/en-us/102654) for `notarytool`.

```text
This process stores your credentials securely in the Keychain. You reference these credentials later using a profile name.

Validating your credentials...
Success. Credentials validated.
Credentials saved to Keychain.
To use them, specify `--keychain-profile "notarytool-credentials"`
```

Once this (one-time) setup is done, you can notarize the disk image while building the app. From the CLI run:

```bash
dotnet publish -f net10.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=dmg -p:CodesignKey={{identity}} -p:PackageSigningKey={{installer_identity}} -p:UnoMacOSNotarizeKeychainProfile={{notarytool-credentials}} -bl
```

where

- `{{notarytool-credentials}}` is the name of your credentials inside the key store
- `-bl` will create a binary log of your build. This will include information about the notarization process.

> [!NOTE]
> Running this command might [take a while](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow) as it will wait for the notarization process to complete on Apple servers.

Once completed you can distribute the package installer.

### Disk Image (.dmg)

Another common way to distribute your macOS software is to create a disk image (.dmg). This will produce a single, compressed disk image that can be shared (if signed and notarized) with anyone using a Mac computer.

To create a disk image from the CLI run:

```bash
dotnet publish -f net10.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=dmg -p:CodesignKey={{identity}} -p:DiskImageSigningKey={{identity}}
```

Where the following changes to the original command are

- modifying `PackageFormat` to `dmg` to produce the disk image. This image will include the app bundle inside it, so the `CodesignKey` argument is still required;
- adding `-p:DiskImageSigningKey={{identity}}` to specify which identity should be used to sign the package. Like app bundles, the signing step requires using a `Developer ID Application: *` identity.

The resulting disk image will be located at `bin/Release/net10.0-desktop/{{RID}}/publish/{{APPNAME}}.dmg`.

#### Notarize the disk image

Like an installer (.pkg) a disk image is the outermost container that you'll share with customers and, as such, needs to be notarized by Apple.

The first step is to store your Apple Account credentials inside the key store. This makes all the further commands (and notarization) much simpler. From the CLI run:

```bash
xcrun notarytool store-credentials {{notarytool-credentials}} --apple-id john.appleby@platform.uno --team-id XXXXXXXXXX --password aaaa-bbbb-cccc-dddd
```

where

- `{{notarytool-credentials}}` is the name of your credentials inside the key store.
- `--apple-id` provides the email address used for your [Apple Account](https://developer.apple.com/account).
- `--team-id` provides your team ID, a 10-character code. [How to find it](https://developer.apple.com/help/account/manage-your-team/locate-your-team-id).
- `--password` is an [app specific password](https://support.apple.com/en-us/102654) created specifically for `notarytool`

> [!NOTE]
> Since Apple Accounts requires two factors authentication (2FA) you will need to [create an app-specific password](https://support.apple.com/en-us/102654) for `notarytool`.

```text
This process stores your credentials securely in the Keychain. You reference these credentials later using a profile name.

Validating your credentials...
Success. Credentials validated.
Credentials saved to Keychain.
To use them, specify `--keychain-profile "notarytool-credentials"`
```

Once this (one-time) setup is done, you can notarize the disk image while building the app. From the CLI run:

```bash
dotnet publish -f net10.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=dmg -p:CodesignKey={{identity}} -p:DiskImageSigningKey={{identity}} -p:UnoMacOSNotarizeKeychainProfile={{notarytool-credentials}} -bl
```

where

- `{{notarytool-credentials}}` is the name of your credentials inside the key store
- `-bl` will create a binary log of your build. This will include information about the notarization process.

> [!NOTE]
> Running this command might [take a while](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow) as it will wait for the notarization process to complete on Apple servers.

Once completed you can distribute the notarized disk image.

### Mac App Store

> [!IMPORTANT]
> Applications distributed on the Mac App Store are required to execute under a [sandbox](https://developer.apple.com/documentation/security/app-sandbox?language=objc), which imposes additional limits on how applications can interact with the computer.

An app bundle (.app) can be submitted to Apple's [App Store](https://www.apple.com/app-store/) using the [transporter app](https://developer.apple.com/help/app-store-connect/manage-builds/upload-builds) from a computer running macOS.

> [!NOTE]
> Notarization of the app bundle is **not** required as the Apple App Store will be taking care of your app binary distribution.

## Troubleshooting

### Apple Developer Account

An active Apple Developer Account is **required** for code signing and notarization.

An error, such as the one below, means that the Apple Developer Account is not active or the necessary agreements have not been signed.

```text
Uno.Sdk.Extras.Publish.MacOS.targets(75,3): error : Failed to submit tmpcZQgA4.zip to Apple's notarization service. Exit code: 1: Error: HTTP status code: 403. A required agreement is missing or has expired. This request requires an in-effect agreement that has not been signed or has expired. Ensure your team has signed the necessary legal agreements and that they are not expired.
```

Try logging into your [Apple Developer Account](https://developer.apple.com/account) to see if any action is required to activate your account.
Once re-enabled, it might take a few minutes (for the update to propagate) before you can sign or notarize your app bundle.

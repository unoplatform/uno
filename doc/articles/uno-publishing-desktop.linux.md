---
uid: uno.publishing.desktop.linux
---

# Publishing Your App for Linux

## Snap Packages

We support creating .snap packages on **Ubuntu 20.04** or later.

### Requirements

The following must be installed and configured:

- snapd
- snaps (with `snap install`):
  - core20 on Ubuntu 20.04
  - core22 on Ubuntu 22.04
  - core24 on Ubuntu 24.04
  - multipass
  - lxd
    - current user must be part of the `lxd` group
    - `lxd init --minimal` or similar should be run
  - snapcraft

> [!NOTE]
> Docker may interfere with Lxd causing network connectivity issues, for solutions see: https://documentation.ubuntu.com/lxd/en/stable-5.0/howto/network_bridge_firewalld/#prevent-connectivity-issues-with-lxd-and-docker

### Publishing A Snap

To publish a snap, run:

```shell
dotnet publish -f net8.0-desktop -r {{RID}} -p:SelfContained=true -p:PackageFormat=snap
```

Where `{{RID}}` is either `linux-x64` or `linux-arm64`.

We generate snap manifests in classic confinement mode and a .desktop file by default.

If you wish to customize your snap manifest, you will need to pass the following MSBuild properties:

- SnapManifest
- DesktopFile

The `.desktop` filename MUST conform to the Desktop File spec.

If you wish, you can generate a default snap manifest and desktop file by running the command above, then tweak them.

> [!NOTE]
> .NET 9 publishing and cross-publishing are not supported as of Uno 5.5, we will support .NET 9 publishing soon.

## Limitations

- NativeAOT is not supported
- R2R is not supported
- Single file publish is not supported

> [!NOTE]
> Publishing is a work in progress

## Links

- [Snapcraft.yaml schema](https://snapcraft.io/docs/snapcraft-yaml-schema)
- [Desktop Entry Specification](https://specifications.freedesktop.org/desktop-entry-spec/latest)

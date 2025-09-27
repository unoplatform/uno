---
uid: uno.publishing.desktop.linux
---

# Publishing Your App for Linux

## Snap Packages

We support creating .snap packages on **Ubuntu 20.04** or later.

### Requirements

The following must be installed and configured:

```bash
sudo apt-get install -y snapd
sudo snap install core22
sudo snap install multipass
sudo snap install lxd
sudo snap install snapcraft
lxd init --minimal
sudo usermod --append --groups lxd $USER # In order for the current user to use LXD
```

> [!NOTE]
> In the above script, replace `core22` with `core20` if building on Ubuntu 20.04, or `core24` if building on Ubuntu 24.04.
> [!NOTE]
> Docker may interfere with Lxd causing network connectivity issues, for solutions see:
> https://documentation.ubuntu.com/lxd/en/stable-5.0/howto/network_bridge_firewalld/#prevent-connectivity-issues-with-lxd-and-docker

### Generate a Snap file

To generate a snap file, run the following:

```shell
dotnet publish -f net9.0-desktop -p:SelfContained=true -p:PackageFormat=snap
```

The generated snap file is located in the `bin/Release/netX.0-desktop/linux-[x64|arm64]/publish` folder.

Uno Platform generates snap manifests in classic confinement mode and a `.desktop` file by default.

If you wish to customize your snap manifest, you will need to pass the following MSBuild properties:

- `SnapManifest`
- `DesktopFile`

You may use absolute or relative (to the `.csproj`) paths.

The `.desktop` filename MUST conform to the [Desktop File](https://specifications.freedesktop.org/desktop-entry-spec/latest) spec.

If you wish, you can generate a default snap manifest and desktop file by running the command above, then tweak them.

> [!NOTE]
> .NET 9 publishing and cross-publishing are not supported as of Uno 5.5, we will support .NET 9 publishing soon.

#### CI Restrictions

When building in a CI environment, security restrictions may prevent LXD and Multipass from running properly.

In such cases, and if your environment is built using single-use environments like Azure DevOps Hosted Agents, you can enable the Snap [destructive mode](https://snapcraft.io/docs/explanation-architectures#destructive-mode) with the following parameter to the `dotnet publish` command:

```bash
-p:UnoSnapcraftAdditionalParameters=--destructive-mode
```

> [!IMPORTANT]
> Using this mode will make destructive changes to your environment, make sure that you will use this mode on a single-use virtual environment (e.g. Docker or Azure DevOps Hosted Agents).

### Publish your Snap Package

You can install your app on your machine using the following:

```bash
sudo snap install MyApp_1.0_amd64.snap --dangerous --classic
```

You can also publish your app to the [Snap store](https://snapcraft.io/store).

## Limitations

- NativeAOT is not yet supported
- R2R is not yet supported
- Single file publishing is not yet supported

> [!NOTE]
> Publishing is a [work in progress](https://github.com/unoplatform/uno/issues/16440)

## Links

- [Snapcraft.yaml schema](https://snapcraft.io/docs/snapcraft-yaml-schema)
- [Desktop Entry Specification](https://specifications.freedesktop.org/desktop-entry-spec/latest)

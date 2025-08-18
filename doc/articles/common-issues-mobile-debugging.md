---
uid: Uno.UI.CommonIssues.MobileDebugging
---

# Android & iOS troubleshooting

When Android or iOS emulators are missing or fail to start in JetBrains Rider or Visual Studio Code on macOS or Windows, the tips below can help you get moving again.

## Android emulator setup

There are two common ways to manage Android Virtual Devices (AVDs):

### Option 1: Use Android Studio

1. Install the latest Android Studio.
2. Open **Tools → SDK Manager → SDK Tools** and update **Android Emulator**, **Android SDK Platform‑Tools** and any outdated components.
3. Launch **AVD Manager** and create or update an emulator image with the latest system image.
4. Start the emulator before launching your Uno Platform app and restart your IDE so it picks up the running device.

### Option 2: Use Rider with the Android Support plugin

1. Install JetBrains Rider and add the **Android Support** plugin from **Preferences → Plugins → Marketplace**.
2. From Rider's AVD Manager, create or update an emulator image, or reuse an image created in Android Studio.
3. Launch the emulator manually if Rider does not detect it automatically.
4. You can also use an emulator created in Rider from other tools like VS Code.

### Keep your AVDs up to date

Updating system images and tools helps avoid compatibility issues. Whenever you change your SDKs or devices, restart Rider or VS Code to refresh the device list.

### Launch emulators manually when needed

Sometimes IDEs only discover an emulator if it's already running. You can start it from Android Studio or with the command line:

```bash
emulator -list-avds
emulator -avd <your_avd_name>
```

### macOS note: keep SDKs out of cloud‑synced folders

Ensure your Android SDK and AVD directories are stored locally and not inside iCloud Drive, Dropbox, OneDrive, or other sync services to prevent file‑locking and permission issues. Adjust the path from **Android Studio → Preferences → Appearance & Behavior → System Settings → Android SDK** if necessary.

## iOS simulator tips (macOS only)

1. Install the latest Xcode from the App Store and launch it once to agree to the license.
2. Verify the iOS Simulator runs on its own (**Xcode → Window → Devices and Simulators**).
3. If your IDE cannot find the simulator, start it manually or restart your computer.
4. To inspect available simulators from the command line run:

```bash
xcrun simctl list devices
```

## Useful device commands

```bash
adb devices                 # Lists connected Android devices and emulators
xcrun simctl list devices   # Lists iOS simulators (macOS only)
```

## IDE‑specific tips

### Rider

- Ensure the **Android Support** plugin is installed.
- Restart Rider after updating SDKs or AVDs.
- Launch emulators manually if they are not detected.

### Visual Studio Code

- Extensions such as **Android iOS Emulator** can simplify device management.
- Reload the window or restart VS Code after starting a new emulator.
- VS Code can use emulators created in Android Studio or Rider.

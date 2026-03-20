---
uid: Uno.UI.CommonIssues.MobileDebugging
---

# Android & iOS emulators: setup & troubleshooting

If Android or iOS emulators are missing or fail to start in JetBrains Rider, Visual Studio, or Visual Studio Code on macOS or Windows, use the checklists and setup paths below.

## Quick-fix checklist (do this first)

- **Start the emulator manually** once, then reopen your IDE.
- **Update tools & images** (Android Emulator, Platform-Tools, system images).
- **Use one Android SDK location** across tools; avoid cloud-synced folders.
- **Windows:** ensure virtualization is enabled (see “Windows virtualization”).
- **ADB refresh:** `adb kill-server && adb start-server`, then `adb devices`.
- **AVD reset:** Cold Boot or Wipe Data for a flaky AVD.
- **macOS/iOS:** open the iOS Simulator from Xcode once to initialize it.
- After changing SDKs, emulator images, or virtualization settings, **restart your IDE** so it refreshes its device list.

## Android emulator setup

There are several common ways to manage Android Virtual Devices (AVDs):

### Option 1 — Android Studio (works with all IDEs)

1. Install the latest Android Studio.
2. **Tools → SDK Manager → SDK Tools**: update **Android Emulator** and **Android SDK Platform-Tools**.
3. **Tools → Device Manager (AVD Manager)**: create/update an AVD with a recent system image.
4. Launch the emulator, then run your Uno Platform app from your IDE.

### Option 2 — Visual Studio

1. Install Visual Studio 2022/2026 with the **Multi-Platform App UI development with .NET** workload.
2. Open **Tools → Android → Android Device Manager**.
3. Create/update an AVD and start it before you deploy from your IDE.

### Option 3 — JetBrains Rider

1. Install **Rider** and the **Android Support** plugin via **Preferences/Settings → Plugins → Marketplace**.
2. From Rider’s Device/AVD Manager, create/update an AVD (you can reuse AVDs made in Android Studio).
3. If Rider doesn’t list it immediately, start the AVD manually and restart Rider.

### Option 4 — Visual Studio Code

> VS Code doesn’t include a built-in AVD manager. It **uses AVDs created by Android Studio, Rider, or Visual Studio** and can start them via extensions or the terminal.

1. **Install/verify Android SDK & tools**
   - Create AVDs in **Android Studio** (recommended) or another IDE.
   - Ensure command-line tools are available in your `PATH` (e.g., `<sdk>/platform-tools` and `<sdk>/emulator`).
   - Optional: confirm the .NET Android workload is present: `dotnet workload list` (look for `android`).

2. **Start an emulator**
   - Use an extension (e.g., an “Android/iOS Emulator” helper) to start/stop AVDs from the Command Palette, **or**
   - Use the terminal:

     ```bash
     emulator -list-avds
     emulator -avd <your_avd_name>
     adb devices
     ```

3. **Make VS Code see the device**
   - **Reload Window** after the emulator starts, or restart VS Code.
   - If the emulator isn’t listed, verify `adb devices` shows it, then relaunch the debugger.

4. **Troubleshooting in VS Code**
   - Ensure `ANDROID_SDK_ROOT` (or `ANDROID_HOME`) points to the correct SDK.
   - Keep only **one** SDK location across tools to avoid a mismatch.
   - If an extension can’t find the emulator, set the SDK/emulator path in its settings.

---

## Windows virtualization (required for fast Android emulation)

Enable **Windows Hypervisor Platform** (and **Hyper-V** on supported editions), then **reboot**. Avoid conflicts with other hypervisors (e.g., VirtualBox in non-Hyper-V mode).

## iOS simulator setup (macOS only)

1. Install the latest **Xcode** from the App Store and launch it once to accept the license.
2. Verify the Simulator runs: **Xcode → Window → Devices and Simulators**.
3. If your IDE can’t find the Simulator, **start it manually** or **reboot**.
4. Inspect available simulators:

   ```bash
   xcrun simctl list devices
   ```

## SDK location

- Keep your **Android SDK** and **AVD** directories **outside** cloud-sync folders (OneDrive/iCloud/Dropbox) to avoid file-locking and partial syncs.
- Verify or change the SDK path in **Android Studio**:
  - **macOS:** *Preferences → Appearance & Behavior → System Settings → Android SDK*
  - **Windows/Linux:** *File → Settings → Appearance & Behavior → System Settings → Android SDK*
- Use a **single SDK location** across all tools (Android Studio, Rider, VS/VS Code).
- Set environment variables so CLIs and IDEs agree:
  - **`ANDROID_SDK_ROOT`** (recommended) or **`ANDROID_HOME`** (legacy).
- Typical locations:
  - **macOS:** `~/Library/Android/sdk`
  - **Windows:** `%LOCALAPPDATA%\Android\Sdk` (or a custom `C:\Android\Sdk`)
  - **Linux:** `~/Android/Sdk`
  - **AVDs:** `~/.android/avd` (macOS/Linux) or `%USERPROFILE%\.android\avd` (Windows)
- After moving or changing SDKs/AVDs, **restart your IDE** so it refreshes device lists.

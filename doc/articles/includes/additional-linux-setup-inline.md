## [**Ubuntu 20.04/22.04**](#tab/ubuntu2004)

- Install the required dependencies:

    ```bash
    sudo apt update
    sudo apt install mesa-utils libgl1-mesa-glx ttf-mscorefonts-installer dbus libfontconfig1 libxrandr2 libxi-dev libwayland-client0 libwayland-cursor0 libwayland-egl1 libxkbcommon0 libegl1
    ```

## [**ArchLinux 5.8.14 or later / Manjaro**](#tab/archlinux2004)

- Update system and packages

    ```bash
    pacman -Syu
    ```

- Install the necessary dependencies

    ```bash
    sudo pacman -S dotnet-targeting-pack dotnet-sdk dotnet-host dotnet-runtime python ninja gn aspnet-runtime dbus libxrandr libxi wayland libxkbcommon mesa
    ```

---

You may also need to [install the Microsoft fonts](https://wiki.archlinux.org/title/Microsoft_fonts) manually.

If you are using Windows Subsystem for Linux (WSL), you can find specific instructions in the following video:

> [!Video https://www.youtube-nocookie.com/embed/GGszH8PDf-w]

For native Wayland, install the Wayland client, cursor and EGL libraries and `libxkbcommon`. Accelerated rendering also requires an EGL implementation, such as Mesa. Software rendering does not require EGL.

File and folder pickers require a session D-Bus, `xdg-desktop-portal`, and a desktop-specific portal backend (for example `xdg-desktop-portal-gnome`, `xdg-desktop-portal-kde`, or `xdg-desktop-portal-gtk`). Install the backend that matches your session. IME composition requires a Wayland-capable input-method service and compositor support for `text-input-v3`.

## [**Ubuntu 20.04/22.04**](#tab/ubuntu2004)

- Install the required dependencies:

    ```bash
    sudo apt update
    sudo apt install mesa-utils libgl1-mesa-glx ttf-mscorefonts-installer dbus libfontconfig1 libxrandr2 libxi-dev
    ```

## [**ArchLinux 5.8.14 or later / Manjaro**](#tab/archlinux2004)

- Update system and packages

    ```bash
    pacman -Syu
    ```

- Install the necessary dependencies

    ```bash
    sudo pacman -S dotnet-targeting-pack dotnet-sdk dotnet-host dotnet-runtime python ninja gn aspnet-runtime dbus libxrandr libxi
    ```

---

You may also need to [install the Microsoft fonts](https://wiki.archlinux.org/title/Microsoft_fonts) manually.

If you are using Windows Subsystem for Linux (WSL), you can find specific instructions in the following video:

> [!Video https://www.youtube-nocookie.com/embed/GGszH8PDf-w]

## [**Ubuntu 20.04/22.04**](#tab/ubuntu2004)

- Install the required dependencies:

    ```bash
    sudo apt update
    sudo apt install mesa-utils libgl1-mesa-glx ttf-mscorefonts-installer dbus libfontconfig1 libxrandr2 libxi-dev
    ```

- Install ICU, used for text rendering (usually preinstalled on desktop images, but often missing from minimal ones). The package name is versioned per release:

    ```bash
    sudo apt install libicu66 # Ubuntu 20.04; use libicu70 on 22.04, libicu74 on 24.04
    ```

## [**ArchLinux 5.8.14 or later / Manjaro**](#tab/archlinux2004)

- Update system and packages

    ```bash
    pacman -Syu
    ```

- Install the necessary dependencies

    ```bash
    sudo pacman -S dotnet-targeting-pack dotnet-sdk dotnet-host dotnet-runtime python ninja gn aspnet-runtime dbus libxrandr libxi icu
    ```

---

You may also need to [install the Microsoft fonts](https://wiki.archlinux.org/title/Microsoft_fonts) manually.

If you are using Windows Subsystem for Linux (WSL), you can find specific instructions in the following video:

> [!Video https://www.youtube-nocookie.com/embed/GGszH8PDf-w]

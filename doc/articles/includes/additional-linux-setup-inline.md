## [**Ubuntu 18.04**](#tab/ubuntu1804)

- Install the required dependencies:

    ```bash
    sudo apt update
    sudo apt-get install mesa-utils libgl1-mesa-glx ttf-mscorefonts-installer dbus libfontconfig1 libxrandr
    ```

## [**Ubuntu 20.04/22.04**](#tab/ubuntu2004)

- Install the required dependencies:

    ```bash
    sudo apt update
    sudo apt install mesa-utils libgl1-mesa-glx ttf-mscorefonts-installer dbus libfontconfig1 libxrandr
    ```

## [**ArchLinux 5.8.14 or later / Manjaro**](#tab/archlinux2004)

- Update system and packages

    ```bash
    pacman -Syu
    ```

- Install the necessary dependencies

    ```bash
    sudo pacman -S dotnet-targeting-pack dotnet-sdk dotnet-host dotnet-runtime python ninja gn aspnet-runtime dbus libxrandr
    ```

***

You may also need to [install the Microsoft fonts](https://wiki.archlinux.org/title/Microsoft_fonts) manually.

If you are using Windows Subsystem for Linux (WSL), you can find specific instructions in the following video:

<div style="position: relative; width: 100%; padding-bottom: 56.25%;">
    <iframe
        src="https://www.youtube-nocookie.com/embed/GGszH8PDf-w"
        title="YouTube video player"
        frameborder="0"
        allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
        allowfullscreen
        style="position: absolute; top: 0; left: 0; width: 100%; height: 100%;">
    </iframe>
</div>

***

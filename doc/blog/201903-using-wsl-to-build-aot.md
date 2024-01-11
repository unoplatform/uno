---
uid: Uno.Blog.UsingWSLToBuildAOT
---

# How to build WebAssembly C# Apps with the Mono AOT and  Windows Subsystem for Linux

Microsoft's steady progress on WebAssembly gives an opportunity to test a lot of the new features regarding the payload size and performance balance.

Using the [AOT](https://www.mono-project.com/docs/advanced/aot/) provides a significant increase in performance over the mono interpreter, between 30x to 50x depending on the browser. This enables application such as the [Uno Gallery](https://gallery-aot.platform.uno/) or the [Xaml Controls Gallery](https://xamlcontrolsgallery.platform.uno/) to run at very interesting performance.

Since the introduction of [AOT](https://www.mono-project.com/docs/advanced/aot/), we noticed that the Wasm payload is particularly large. This is caused by multiple reasons, such as the fact that an LLVM backend is not used to generate WebAssembly or that some indirect calls are not supported by current Emscripten builds.

For those reasons, and a few others, the work that the Mono team is currently doing to support a mixed AOT/Interpreter Mode is going to play an important role to strike a good balance between payload size and performance.

At present time, the mono-wasm AOT tool chain is only compatible with Linux, and even though it will eventually support other platforms, it is required to setup a Linux machine or use the excellent [Windows 10 Subsystem for Linux](https://docs.microsoft.com/en-us/windows/wsl/install-win10).

For those of you that have yet to experience WSL, Microsoft has added the ability for Windows to run native 64-Bits Linux binaries, untouched, non-virtualized. This means that most of the linux-compatible tooling, including mono, is able to run properly as if it were running on top of an actual Linux kernel.

We're going to setup a WSL environment to build an Uno Platform application using Mono WebAssembly AOT.

## Setting the build environment under WSL

Here's what to do to, only once:

- Install [WSL](https://docs.microsoft.com/en-us/windows/wsl/install-win10) on a Windows 10 box
- Install [Ubuntu 18.04 or later](https://www.microsoft.com/en-ca/p/ubuntu-1804-lts/9n9tngvndl3q)
- Install a stable [Mono toolchain](https://www.mono-project.com/download/stable/#download-lin)
  - Run the following to add a new apt source :

  ```
  sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys    3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF
  echo "deb https://download.mono-project.com/repo/ubuntu stable-bionic main" | sudo tee /etc/apt/sources.list.d/mono-official-stable.list
  sudo apt update
  ```

  - Install python, mono and msbuild

  ```bash
  sudo apt install python mono-devel msbuild
  ```

- Install a [stable dotnet core](https://dotnet.microsoft.com/download?initial-os=linux) for [Ubuntu 18.04](https://dotnet.microsoft.com/download/linux-package-manager/ubuntu18-04/sdk-current):

    ```bash
    wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
    sudo dpkg -i packages-microsoft-prod.deb
    sudo add-apt-repository universe
    sudo apt-get install apt-transport-https
    sudo apt-get update
    sudo apt-get install dotnet-sdk-2.2
    ```

- Install ninja

  ```bash
  sudo apt install libc6 ninja-build
  ```

- Install emscripten 1.38.28, with a mono patch

    ```bash
    cd ~
    git clone https://github.com/juj/emsdk.git
    cd emsdk
    ./emsdk install sdk-1.38.28-64bit
    ./emsdk activate sdk-1.38.28-64bit
    ```

## Build an Uno Platform WebAssembly head app

For each shell you're opening afterwards, you'll have to do the following:

- Activate emscripten:

    ```bash
    source ~/emsdk/emsdk_env.sh
    ```

- Create an Cross-platform Application using the [Uno Platform VSIX](https://marketplace.visualstudio.com/items?itemName=unoplatform.uno-platform-addin-2022), follow the [getting started guide](https://github.com/unoplatform/uno/blob/master/doc/articles/get-started.md
) for more details.
- Make sure that the following property is added in the Wasm csproj :

    ```xml
    <WasmShellEnableAOT Condition="$([MSBuild]::IsOsUnixLike()) and '$(Configuration)'=='Release'">true</WasmShellEnableAOT>
    ```

- Navigate to the Wasm folder of your application using the [`wslpath`](https://blogs.msdn.microsoft.com/commandline/2018/03/07/windows10v1803/) utility:

  ```bash
  cd `wslpath "C:\Users\my_user\source\repos\MyApp\MyApp.Wasm"`
  ```

- Build the application:

    ```bash
    msbuild /r /p:Configuration=Release
    ```

- Serve the resulting application

  ```
  cd `wslpath "C:\Users\my_user\source\repos\MyApp\MyApp.Wasm\bin\Release\netstandard2.0\dist\"`
  python3 server.py
  ```

  The application will be available at http://localhost:8000

You'll notice that the `mono.wasm` is roughly between 20MB and 30MB, depending on the [linker settings you've set](https://github.com/unoplatform/uno.Wasm.Bootstrap#linker-configuration), and using packages like Json.NET can significantly increase the binary size.

You'll also probably notice that the build time can get pretty long, and the most time consuming step is Emscripten. This is known by the Mono team and will certainly be worked on in the future.

We'll be discussing the use of the Mixed mode runtime in a next blog post, so we can get a performance/size balance for the generated binary.

Let us know what you think!

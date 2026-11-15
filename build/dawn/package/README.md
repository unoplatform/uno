# Uno.WinUI.Composition.WebGpu.Dawn

Google's [Dawn](https://dawn.googlesource.com/dawn) WebGPU implementation, as the native library of the Uno Platform WebGPU backend. You don't reference this package yourself: `Uno.WinUI.Composition.WebGpu` depends on it, and Dawn is the library the backend uses unless the app sets `UnoWebGpuNative` to `WgpuNative`.

| Platform | What ships |
|---|---|
| Windows x64, arm64 | `runtimes/<rid>/native/webgpu.dll`, linked against the static C++ runtime |
| Linux x64, arm64 | `runtimes/<rid>/native/libwebgpu.so` (glibc 2.34 or later) |
| macOS x64, arm64 | `runtimes/<rid>/native/libwebgpu.dylib` (macOS 12 or later) |
| Android arm64, arm, x64, x86 | `runtimes/<rid>/native/libwebgpu.so` |
| iOS device and simulator | a static library linked into the app |

On Windows, Dawn compiles shaders with the system's `d3dcompiler_47.dll` and uses the system's Vulkan loader, so nothing else ships with it.

The libraries are linked from Dawn's release builds, or built from Dawn's source where a release does not cover the platform, by the scripts in `build/dawn` of the [Uno Platform repository](https://github.com/unoplatform/uno). Dawn is licensed under the BSD 3-Clause License; `LICENSE` in this package carries it, along with the licenses of the third-party code built into it.

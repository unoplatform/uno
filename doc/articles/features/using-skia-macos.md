---
uid: Uno.Skia.macOS
---

# Using the Desktop (Skia) on macOS

## Metal (Hardware Accelerated) Rendering

All recent Mac computers support Metal. It has been required to run macOS 10.14 (Mojave). As such macOS hardware-accelerated [Metal](https://developer.apple.com/metal/) rendering is enabled by default.

However, it's possible that under certain circumstances, like virtualization (including [GitHub CI](https://github.com/actions/runner-images/issues/1779)), Metal might not be available to applications. In this case, the host will **automatically** fall back to a software-based rendering.

> [!NOTE]
> Some Mac models released before 2012 did not support Metal. See Apple's support document [Support for Metal on Mac, iPad, and iPhone](https://support.apple.com/en-us/102894) for the list of devices that support Metal.

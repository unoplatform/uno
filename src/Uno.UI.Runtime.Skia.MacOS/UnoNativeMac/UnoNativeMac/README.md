# UnoNativeMac

## How to install `libSkiaSharp.dylib`

### Install the current (defined) version

Run the following script to download and copy `libSkiaSharp.dylib` inside the Xcode project.

```bash
./getSkiaSharpDylib.sh
```

### Install a specific version of `libSkiaSharp.dylib`

Set `VERSION` to the specific version you want then call `./getSkiaSharpDylib.sh`. E.g.

```bash
VERSION=2.88.7 ./getSkiaSharpDylib.sh
```

### Updating `libSkiaSharp.dylib`

Edit `./getSkiaSharpDylib.sh` to change the default `VERSION` and commit to the repository.

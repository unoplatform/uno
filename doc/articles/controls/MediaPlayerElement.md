---
uid: Uno.Controls.MediaPlayerElement
---

# MediaPlayerElement

See [Microsoft API reference for MediaPlayerElement](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.mediaplayerelement).

## Media formats

| Supported Formats          | iOS | Android | Wasm | Skia Desktop  | Remarks                                              |
|----------------------------|:----:|:---------:|:------:|:---------:|------------------------------------------------------------------------------|
| Local/Remote MP3 Support   |  ✅  |    ✅    |  ✅   |    ✅     |                                                                              |
| Local/Remote MPEG4 Support |  ✅  |    ✅    |  ✅   |    ✅     |                                                                            |
| HLSv3 Support              |  ✅  |    ✅    |  ✅   |    ✅     |                                                                            |
| HLSv4 Support              |  ✅  |    ✅    |  ✅   |    ✅     |                                                                            |
| 3GP Support                |  ✅  |    ✅    |  ✅   |    ✅     |3GP with AMR Narrow Band (SAMR) audio codec does not work on iOS (See notes) |
| FLV Support                |  -    |    ✅   |  ✅   |    ✅     |                                                                            |
| MOV Support                |  ✅  |    -     |  -     |    -      |                                                                           |
| MKV Support                |  -    |    ✅   |  ✅   |    ✅     |                                                                            |
| AVI Support                |  -    |    ✅   |  ✅   |    ✅     |                                                                             |
| OGG Support                |  -    |    -    |  ✅   |    ✅     |                                                                            |
| MPEG-Dash Support          |  -    |    -    |  -     |    -      |                                                                           |
| Smooth Streaming Support   |  -    |    -    |  -     |    -      |                                                                           |

### Notes

- Uno's MediaPlayerElement relies on AVPlayer for iOS and AndroidMediaPlayer for Android. Please, refer to those native players documentation for more information about supported audio and video formats.
- If you need to set source programmatically (i.e., using `_mediaPlayerElement.Source = [source]`), please note that only sources created with `MediaSource.CreateFromUri()` are currently supported.

## Features

| Section            | Feature                                        | iOS | Android | Wasm | Skia Desktop  | Remarks                                      |
|--------------------|------------------------------------------------|:---:|:-------:|:----:|:--------:|------------------------------------------------|
| MediaPlayerElement | AutoPlay                                       |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Poster image                                   |  ✅  |    ✅    |  ✅   |    ✅     | Does not show when playing music             |
|                    | Enable/Disable MediaTransportControls          |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Stretch                                        |  ✅  |    ✅    |  ✅   |    ✅     | Stretch.None behave like Stretch.Fill on iOS |
|                    | Pause media when headphones unplugged          |  ✅  |    ✅    |  -     |    -     |                                               |
| TransportControls  | Transport controls custom style                |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Play/Pause                                     |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Stop                                           |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Seek                                           |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Volume change                                  |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Mute                                           |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Show elapsed time                              |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Show remaining time                            |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Show/Hide MediaTransportControls automatically |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | MediaTransportControls compact mode            |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Show/Hide MediaTransportControls commands      |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Enable/Disable MediaTransportControls commands |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Skip forward                                   |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Skip backward                                  |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Show buffering progress                        |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Zoom mode                                      |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Full-screen mode                               |  ✅  |    ✅    |  ✅   |    ✅     |                                              |
|                    | Playlists support                              |  ✅  |    ✅    |  -     |    -      |                                          |
|                    | Change playback rate                           |  -    |    -     |  ✅   |    ✅     |                                           |
|                    | Player controls on locked screen support       |  -    |    -     |  -     |    -     |                                           |
|                    | Subtitles support                              |  -    |    -     |  -     |    -     |                                           |
|                    | Languages support                              |  -    |    -     |  -     |    -     |                                           |

## Requirement

### iOS

Add the following to your info.plist:

```xml
<key>NSAppTransportSecurity</key>
<dict>
    <key>NSAllowsArbitraryLoads</key>
    <true/>
    <key>NSExceptionMinimumTLSVersion</key>
    <string>TLSv1.2</string>
</dict>
```

__Note:__ Don't just copy/paste, but properly setup `NSAppTransportSecurity` as required by your project

### Android

Add the following to your AndroidManifest.xml

```xml
<!-- Required to play remote media -->
<uses-permission android:name="android.permission.INTERNET" />
<!-- Required to keep the screen on while playing -->
<uses-permission android:name="android.permission.WAKE_LOCK" />
```

### WebAssembly

Using the `MediaPlayerElement` on WebAssembly head requires adding the [`Uno.WinUI.MediaPlayer.WebAssembly`](https://www.nuget.org/packages/Uno.WinUI.MediaPlayer.WebAssembly) package to the `MyApp.Wasm` project.

> [!IMPORTANT]
> The `Uno.WinUI.MediaPlayer.WebAssembly` package version must use the same version as the other `Uno.WinUI.*` packages in your project.

### Skia.GTK (legacy)

Using the `MediaPlayerElement` on the Skia+GTK head requires adding the [`Uno.WinUI.MediaPlayer.Skia.Gtk`](https://www.nuget.org/packages/Uno.WinUI.MediaPlayer.Skia.Gtk) package to the `MyApp.Skia.Gtk` project.

> [!IMPORTANT]
> The `Uno.WinUI.MediaPlayer.Skia.Gtk` package version must use the same version as the other `Uno.WinUI.*` packages in your project.

#### Skia on Linux

The `MediaPlayerElement` support is based on libVLC, and needs the system to provide the appropriate libraries to work properly.

You'll need to install the following packages (Debian based distros):

```bash
sudo apt-get install libvlc-dev libx11-dev vlc libgtk2.0-0 libx11dev
```

#### Skia on Windows

Running the `MediaPlayerElement` requires adding the [`VideoLAN.LibVLC.Windows`](https://www.nuget.org/packages/VideoLAN.LibVLC.Windows) package to your application.

## Future improvement

- Support for Skia Desktop `net9.0-desktop`
- React to audio focus changes (pause/stop playback or reduce audio volume)
- Subtitles support
- Languages support
- Buffering of next playlist element when using MediaPlaybackList
- Cast to device
- Playlist for Wasm
- Download option

## Known issues

- `[iOS]` Volume flyout does not display (Uno issue)
- `[All]` Dynamic width/height not supported when playing audio
- `[All]` Sometimes flickers during resizing when using dynamic width/height

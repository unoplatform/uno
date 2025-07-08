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
- Uno's MediaPlayerElement relies on VLC and libvlc on Linux. please follow [these instructions](https://github.com/videolan/libvlcsharp/blob/3.x/docs/linux-setup.md) to get the necessary dependencies.
- If you need to set source programmatically (i.e., using `_mediaPlayerElement.Source = [source]`), please note that only sources created with `MediaSource.CreateFromUri()` are currently supported.

## Features

| Section            | Feature                                        | iOS & Android (native) | Wasm (native) | Skia | Remarks                                       |
| ------------------ | ---------------------------------------------- | :--------------------: | :-----------: | :--: | --------------------------------------------- |
| MediaPlayerElement | AutoPlay                                       |            ✅           |       ✅       |   ✅  |                                               |
|                    | Poster image                                   |            ✅           |       ✅       |   ✅  | Does not show when playing music              |
|                    | Enable/Disable MediaTransportControls          |            ✅           |       ✅       |   ✅  |                                               |
|                    | Stretch                                        |            ✅           |       ✅       |   ✅  | Stretch.None behaves like Stretch.Fill on iOS |
|                    | Pause media when headphones unplugged          |            ✅           |       -         |   -   |                                               |
| TransportControls  | Transport controls custom style                |            ✅           |       ✅       |   ✅  |                                               |
|                    | Play/Pause                                     |            ✅           |       ✅       |   ✅  |                                               |
|                    | Stop                                           |            ✅           |       ✅       |   ✅  |                                               |
|                    | Seek                                           |            ✅           |       ✅       |   ✅  |                                               |
|                    | Volume change                                  |            ✅           |       ✅       |   ✅  |                                               |
|                    | Mute                                           |            ✅           |       ✅       |   ✅  |                                               |
|                    | Show elapsed time                              |            ✅           |       ✅       |   ✅  |                                               |
|                    | Show remaining time                            |            ✅           |       ✅       |   ✅  |                                               |
|                    | Show/Hide MediaTransportControls automatically |            ✅           |       ✅       |   ✅  |                                               |
|                    | MediaTransportControls compact mode            |            ✅           |       ✅       |   ✅  |                                               |
|                    | Show/Hide MediaTransportControls commands      |            ✅           |       ✅       |   ✅  |                                               |
|                    | Enable/Disable MediaTransportControls commands |            ✅           |       ✅       |   ✅  |                                               |
|                    | Skip forward                                   |            ✅           |       ✅       |   ✅  |                                               |
|                    | Skip backward                                  |            ✅           |       ✅       |   ✅  |                                               |
|                    | Show buffering progress                        |            ✅           |       ✅       |   ✅  |                                               |
|                    | Zoom mode                                      |            ✅           |       ✅       |   ✅  |                                               |
|                    | Full-screen mode                               |            ✅           |       ✅       |   -    |                                               |
|                    | Playlists support                              |            ✅           |       -        |   -    |                                               |
|                    | Change playback rate                           |            -             |       ✅       |   ✅  |                                               |
|                    | Player controls on locked screen support       |            -             |       -        |   -   |                                               |
|                    | Subtitles support                              |            -             |       -        |   -   |                                               |
|                    | Languages support                              |            -             |       -        |   -   |                                               |

## Getting Started

To add video playback functionality, include the following XAML snippet:

```xml
<MediaPlayerElement Source="ms-appx:///Assets/SampleMedia/myfile.mp4"
                    MaxWidth="400"
                    AutoPlay="False"
                    AreTransportControlsEnabled="True" />
```

Make sure to enable the required feature in your `UnoFeatures` to include the necessary packages. Add `MediaPlayerElement;` as shown below:

```diff
<UnoFeatures>
<!-- Existing features -->
+  MediaPlayerElement;
</UnoFeatures>
```

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

### Skia

On some weaker devices, the first load of a `MediaPlayerElement` instance is extremely slow. To attempt to preload media playback resources on app startup, enable the `PreloadMediaPlayer` option in the host builder where supported.

```csharp
var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11(hostBuilder => hostBuilder.PreloadMediaPlayer(true))
            .UseWin32(hostBuilder => hostBuilder.PreloadMediaPlayer(true))
            .Build();
```

## Future improvement

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

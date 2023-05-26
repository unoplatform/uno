---
uid: Uno.Controls.MediaPlayerElement
---

# MediaPlayerElement

See [MediaPlayerElement](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.mediaplayerelement) on MSDN

## Media formats

| Supported Formats    									| iOS		| Android	| Wasm		| Skia GTK	| Remarks							|
|-------------------------------------------------------|-----------|-----------|-----------|-----------|-----------------------------------|
| Local/Remote MP3 Support								| X  		| X  		| X  		| X  		|									|
| Local/Remote MPEG4 Support							| X  		| X  		| X  		| X  		|									|
| HLSv3	Support											| X  		| X  		| X  		| X  		| 									|
| HLSv4	Support											| X  		| X  		| X  		| X  		|									|
| 3GP Support											| X  		| X  		| X  		| X  		| 3GP with AMR Narrow Band (SAMR) audio codec does not work on iOS (See notes) |
| FLV Support											| -  		| X  		| X  		| X  		|									|
| MOV Support											| X  		| -  		| -  		| -  		|									|
| MKV Support											| -  		| X  		| X  		| X  		|									|
| AVI Support											| -  		| X  		| X  		| X  		| 									|
| OGG Support											| -  		| -  		| X  		| X  		|									|
| MPEG-Dash	Support										| -  		| -  		| -  		| -  		| 									|
| Smooth Streaming Support								| -  		| -  		| -  		| -  		| 									|

#### Notes

* Uno's MediaPlayerElement relies on AVPlayer for iOS and AndroidMediaPlayer for Android. Please refer to those native players documentation for more information about supported audio and video formats
* If you need to set source programmatically (i.e., using `_mediaPlayerElement.Source = [source]`), please note that only sources created with `MediaSource.CreateFromUri()` are currently supported

## Features

| Section				| Feature    											| iOS		| Android	| Wasm		| Skia GTK	| Remarks										|
|-----------------------|-------------------------------------------------------|-----------|-----------|-----------|-----------|-----------------------------------------------|
| MediaPlayerElement	| AutoPlay  											| X  		| X  		| X  		| X  		|												|
|						| Poster image											| X  		| X  		| X  		| X  		| Does not show when playing music				|
|						| Enable/Disable MediaTransportControls			  		| X  		| X  		| X  		| X  		|												|
|						| Stretch										  		| X  		| X  		| X  		| X  		| Stretch.None behave like Stretch.Fill on iOS	|
|						| Pause media when headphones unplugged			  		| X  		| X  		| X  		| X  		| 												|
| TransportControls		| Transport controls custom style						| X  		| X  		| X  		| X  		|												|
| 			    		| Play/Pause 											| X  		| X  		| X  		| X  		|												|
|						| Stop  												| X  		| X  		| X  		| X  		|												|
| 						| Seek  												| X  		| X  		| X  		| X  		|												|
|						| Volume change											| X  		| X  		| X  		| X  		|												|
|						| Mute													| X  		| X  		| X  		| X  		|												|
|						| Show elapsed time										| X  		| X  		| X  		| X  		|												|
|						| Show remaining time									| X  		| X  		| X  		| X  		|												|
|						| Show/Hide MediaTransportControls automatically		| X  		| X  		| X  		| X  		|												|
|						| MediaTransportControls compact mode					| X  		| X  		| X  		| X  		|												|
|						| Show/Hide MediaTransportControls commands  			| X  		| X  		| X  		| X  		|												|
|						| Enable/Disable MediaTransportControls commands  		| X  		| X  		| X  		| X  		|												|
|						| Skip forward											| X  		| X  		| X  		| X  		|												|
|						| Skip backward											| X  		| X  		| X  		| X  		|												|
|						| Show buffering progress						  		| X  		| X  		| X  		| X  		|												|
|						| Zoom mode												| X  		| X  		| X  		| X  		| 												|
|						| Full-screen mode								  		| X  		| X  		| X  		| X  		|												|
|						| Playlists support		  								| X  		| X  		| X  		| X  		|												|
|						| Change playback rate									| -  		| -  		| X  		| X  		|												|
|						| Player controls on locked screen support  			| -  		| -  		| -  		| -  		|												|
|						| Subtitles	support			  							| -  		| -  		| -  		| -  		|												|
|						| Languages	support			  							| -  		| -  		| -  		| -  		|												|

## Requirement

### iOS

Add the following to your info.plist

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

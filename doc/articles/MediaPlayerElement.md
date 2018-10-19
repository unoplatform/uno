# MediaPlayerElement

See [MediaPlayerElement](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.mediaplayerelement) on MSDN

## Media formats

| Supported Formats    									| iOS	| Android	| Remarks							|
|-------------------------------------------------------|-------|-----------|-----------------------------------|
| Local/Remote MP3 Support								| X     | X  		|									|
| Local/Remote MPEG4 Support							| X     | X  		|									|
| HLSv3	Support											| X     | X  		| 									|
| HLSv4	Support											| X     | X  		|									|
| MPEG-Dash	Support										| -     | -  		| 									|
| Smooth Streaming Support								| -     | -  		| 									|

_If you need to set source programmatically (ie, using `_mediaPlayerElement.Source = [source]`), please note that only source created with `MediaSource.CreateFromUri()` are currently supported_

## Features

| Section				| Feature    											| iOS	| Android	| Remarks										|
|-----------------------|-------------------------------------------------------|-------|-----------|-----------------------------------------------|
| MediaPlayerElement	| AutoPlay  											| X     | X  		|												|
|						| Poster image											| X     | X  		| Does not show when playing music				|
|						| Enable/Disable MediaTransportControls			  		| X     | X  		|												|
|						| Stretch										  		| X     | X  		| Stretch.None behave like Stretch.Fill on iOS	|
|						| Pause media when headphones unplugged			  		| X     | X  		| 												|
| TransportControls		| Transport controls custom style						| X     | X  		|												|
| 			    		| Play/Pause 											| X     | X  		|												|
|						| Stop  												| X     | X  		|												|
| 						| Seek  												| X     | X  		|												|
|						| Volume change											| X     | X  		|												|
|						| Mute													| X     | X  		|												|
|						| Show elapsed time										| X     | X  		|												|
|						| Show remaining time									| X     | X  		|												|
|						| Show/Hide MediaTransportControls automatically		| X     | X  		|												|
|						| MediaTransportControls compact mode					| X     | X  		|												|
|						| Show/Hide MediaTransportControls commands  			| X     | X  		|												|
|						| Enable/Disable MediaTransportControls commands  		| X     | X  		|												|
|						| Skip forwoard											| X     | X  		|												|
|						| Skip backward											| X     | X  		|												|
|						| Show buffering progress						  		| X     | X  		|												|
|						| Zoom mode												| X     | X  		| 												|
|						| Fullscreen mode								  		| X     | X  		|												|
|						| Change playback rate									| -     | -  		|												|
|						| Player controls on locked screen support  			| -     | -  		|												|
|						| Playlists support		  								| -     | -  		|												|
|						| Subtitles	support			  							| -     | -  		|												|
|						| Languages	support			  							| -     | -  		|												|

## Requirement

### iOS

Add the folowwing to your info.plist

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

Add the folowwing to your AndroidManifest.xml

```xml
<!-- Required to play remote media -->
<uses-permission android:name="android.permission.INTERNET" />
<!-- Required to keep the screen on while playing -->
<uses-permission android:name="android.permission.WAKE_LOCK" />
```

## Future improvement

- Playback rate support
- React to audio focus changes (pause/stop playback or reduce audio volume)
- Subtitles support
- Languages support	
- Display poster for audio media

## Known issues

- `[iOS]` Volume flyout does not display (Uno issue)
- `[All]` Dynamic width/height not supported when playing audio
- `[All]` Sometimes flickers during resizing when using dynamic width/height
- `[All]` Zoom button does not work anymore after displaying video fullscreen
- `[Android]` Poster image disappears when putting video to fullscreen
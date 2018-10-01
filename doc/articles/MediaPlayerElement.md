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
|						| Fullscreen mode								  		| -     | -  		|												|
|						| Change playback rate									| -     | -  		|												|
|						| Player controls on locked screen support  			| -     | -  		|												|
|						| Playlists support		  								| -     | -  		|												|
|						| Subtitles	support			  							| -     | -  		|												|

## Extra features

- Pause media when headphones unplugged

## Requirement

### iOS

`NSAppTransportSecurity` needs to be defined in your info.plist in order to play remote media

### Android

`<uses-permission android:name="android.permission.INTERNET" />` needs to be defined in your AndroidManifest.xml in order to play remote media
`<uses-permission android:name="android.permission.WAKE_LOCK" />` needs to be defined in your AndroidManifest.xml to allow player to keep the screen on while playing

## Future improvement

- React to audio focus changes (pause/stop playback or reduce audio volume)

## Known issues

- Slider not draggable (Uno issue)
- Volume flyout does not display on iOS (Uno issue)
- Dynamic width/height not fully supported at this time
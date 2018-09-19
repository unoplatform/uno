# MediaPlayerElement

See [MediaPlayerElement](https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.mediaplayerelement) on MSDN

## Media formats

| Supported Formats    									| iOS	| Android	| Remarks							|
|-------------------------------------------------------|-------|-----------|-----------------------------------|
| Local MP4 Support										| X     | X  		|									|
| Remote MP4 Support									| X     | X  		|									|
| HLSv3	Support											| X     | X  		| 									|
| HLSv4	Support											| X     | X  		|									|
| MPEG-Dash	Support										| -     | -  		| 									|
| Smooth Streaming Support								| -     | -  		| 									|

_If you need to set source programmatically (ie, using `_mediaPlayerElement.Source = [source]`), please note that only source created with `MediaSource.CreateFromUri()` are currently supported_

## Features

| Section				| Feature    											| iOS	| Android	| Remarks							|
|-----------------------|-------------------------------------------------------|-------|-----------|-----------------------------------|
| MediaPlayerElement	| AutoPlay  											| X     | X  		|									|
|						| Show poster image										| X     | X  		|									|
|						| Enable/Disable MediaTransportControls			  		| X     | X  		|									|
| TransportControls		| Play/Pause 											| X     | X  		|									|
|						| Stop  												| X     | X  		|									|
| 						| Seek  												| X     | X  		|									|
|						| Volume change											| X     | X  		|									|
|						| Mute													| X     | X  		|									|
|						| Show elapsed time										| X     | X  		|									|
|						| Show remaining time									| X     | X  		|									|
|						| Show/Hide MediaTransportControls automatically		| X     | X  		|									|
|						| MediaTransportControls compact mode					| X     | X  		|									|
|						| Show/Hide MediaTransportControls commands  			| X     | X  		|									|
|						| Enable/Disable MediaTransportControls commands  		| X     | X  		|									|
|						| Skip forwoard											| X     | X  		|									|
|						| Skip backward											| X     | X  		|									|
|						| Show buffering progress						  		| -     | -  		|									|
|						| Fullscreen mode								  		| -     | -  		|									|
|						| Change playback rate									| -     | -  		|									|
|						| Zoom mode												| -     | -  		|									|
|						| Locked screen support  								| -     | -  		|									|
|						| Playlists support		  								| -     | -  		|									|


## Requirement

### iOS

`NSAppTransportSecurity` needs to be defined in your info.plist in order to play remote media

### Android

`<uses-permission android:name="android.permission.INTERNET" />` needs to be defined in your AndroidManifest.xml in order to play remote media

## Known issues
using System;

namespace Windows.Media.Playback;

public partial class MediaPlayerFailedEventArgs
{
	internal MediaPlayerFailedEventArgs(MediaPlayerError error, string? errorMessage, Exception? extendedErrorCode)
	{
		Error = error;
		ErrorMessage = errorMessage;
		ExtendedErrorCode = extendedErrorCode;
	}

	public global::Windows.Media.Playback.MediaPlayerError Error
	{
		get;
	}

	public string? ErrorMessage
	{
		get;
	}

	public global::System.Exception? ExtendedErrorCode
	{
		get;
	}
}

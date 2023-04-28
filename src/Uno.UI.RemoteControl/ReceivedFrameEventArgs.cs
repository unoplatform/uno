#nullable enable

using System;

namespace Uno.UI.RemoteControl
{
	public class ReceivedFrameEventArgs : EventArgs
	{
		public ReceivedFrameEventArgs(HotReload.Messages.Frame frame)
		{
			Frame = frame;
		}

		public HotReload.Messages.Frame Frame { get; set; }
	}
}

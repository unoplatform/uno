#if !NET461

using System;
using Uno.Helpers;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class Clipboard
	{
		private readonly static StartStopEventWrapper<object> _contentChangedWrapper =
			new StartStopEventWrapper<object>(
				() => StartContentChanged(),
				() => StopContentChanged());

#if !__SKIA__
		public static void Flush()
		{
			// Do nothing, data available automatically even after application closes.
			// Except for Skia.WPF where you do have to Flush().
		}
#endif

		public static event EventHandler<object> ContentChanged
		{
			add => _contentChangedWrapper.AddHandler(value);
			remove => _contentChangedWrapper.RemoveHandler(value);
		}

		private static void OnContentChanged() => _contentChangedWrapper.Invoke(null, null);
	}
}
#endif

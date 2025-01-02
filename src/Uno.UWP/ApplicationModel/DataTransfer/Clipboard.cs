#if !IS_UNIT_TESTS

using System;
using Uno.Helpers;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class Clipboard
	{
		private static object _syncLock = new object();

		private static StartStopEventWrapper<object> _contentChangedWrapper;

		static Clipboard()
		{
			_contentChangedWrapper = new StartStopEventWrapper<object>(
				() => StartContentChanged(),
				() => StopContentChanged(),
				_syncLock);
		}

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

#if __ANDROID__ || __IOS__ || __MACOS__ || __SKIA__ || __WASM__
		private static void OnContentChanged()
		{
			_contentChangedWrapper.Invoke(null, null);
		}
#endif
	}
}
#endif

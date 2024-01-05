#if !IS_UNIT_TESTS

using System;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class Clipboard
	{
		private static object _syncLock = new object();
		private static EventHandler<object?>? _contentChanged;

#if !__SKIA__
		public static void Flush()
		{
			// Do nothing, data available automatically even after application closes.
			// Except for Skia.WPF where you do have to Flush().
		}
#endif

		public static event EventHandler<object> ContentChanged
		{
			add
			{
				lock (_syncLock)
				{
					var firstSubscriber = _contentChanged == null;
					_contentChanged += value;
					if (firstSubscriber)
					{
						StartContentChanged();
					}
				}
			}
			remove
			{
				lock (_syncLock)
				{
					_contentChanged -= value;
					if (_contentChanged == null)
					{
						StopContentChanged();
					}
				}
			}
		}

#if __ANDROID__ || __IOS__ || __MACOS__ || __SKIA__ || __WASM__
		private static void OnContentChanged()
		{
			_contentChanged?.Invoke(null, null);
		}
#endif
	}
}
#endif

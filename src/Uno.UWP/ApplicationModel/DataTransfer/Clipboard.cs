#if !NET461

using System;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class Clipboard
    {
		private static object _syncLock = new object();
		private static EventHandler<object> _contentChanged;

		public static void Flush()
		{
			// Do nothing, data available automatically even after application closes.
		}

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

		private static void OnContentChanged()
		{
			_contentChanged?.Invoke(null, null);
		}
	}
}
#endif
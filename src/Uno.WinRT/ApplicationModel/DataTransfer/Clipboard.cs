#if !IS_UNIT_TESTS && !__TVOS__

using System;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class Clipboard
	{
		private static object _syncLock = new object();
		private static EventHandler<object> _contentChanged;

#if !__SKIA__
		public static void Flush()
		{
			// Do nothing, data available automatically even after application closes.
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

		/// <summary>
		/// Whether the clipboard holds text, answered from the clipboard's description where the
		/// platform can tell without reading it. Reading is observable to the user -- Android raises
		/// a "pasted from your clipboard" notice -- so callers that only need to decide whether a
		/// Paste affordance is enabled must ask this rather than <see cref="GetContent"/>.
		/// </summary>
		internal static bool ContainsText()
		{
			bool? containsText = null;
			TryGetContainsText(ref containsText);

			return containsText ?? GetContent()?.Contains(StandardDataFormats.Text) == true;
		}

		/// <summary>
		/// Set by platforms that can answer <see cref="ContainsText"/> without reading the clipboard.
		/// Left untouched elsewhere, which falls back to reading it.
		/// </summary>
		static partial void TryGetContainsText(ref bool? containsText);

#if __ANDROID__ || __IOS__ || __TVOS__ || __SKIA__ || __WASM__ || __TVOS__
		private static void OnContentChanged()
		{
			_contentChanged?.Invoke(null, null);
		}
#endif
	}
}
#endif

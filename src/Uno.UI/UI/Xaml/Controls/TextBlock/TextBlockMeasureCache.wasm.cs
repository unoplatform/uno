#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.Foundation;

using Uno;
using Uno.Extensions;
using Uno.UI;
using Uno.Foundation.Logging;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// A TextBlock measure cache for non-formatted text.
	/// </summary>
	internal partial class TextBlockMeasureCache
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fontFamily"></param>
		internal void Clear(FontFamily fontFamily)
		{
			List<MeasureKey> keysToRemove = new();

			foreach (var item in _queue)
			{
				if (item.FontFamily is not null
					&& item.FontFamily.CssFontName == fontFamily.CssFontName)
				{
					keysToRemove.Add(item);
				}
			}

			foreach (var keyToRemove in keysToRemove)
			{
				_queue.Remove(keyToRemove);
				_entries.Remove(keyToRemove);
			}
		}
	}
}

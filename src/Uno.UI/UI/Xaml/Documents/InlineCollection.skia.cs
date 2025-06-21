#nullable enable

using System;
using System.Buffers;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Text;
using Microsoft.UI.Composition;

namespace Microsoft.UI.Xaml.Documents
{
	partial class InlineCollection
	{
		internal void GetHyperlinkPositions(IList<(int start, int, Hyperlink hyperlink)> hyperlinks)
		{
			var start = 0;
			foreach (var inline in PreorderTree)
			{
				switch (inline)
				{
					case Hyperlink hyperlink:
						hyperlinks.Add((start, start + hyperlink.GetText().Length, hyperlink));
						break;
					case Span:
						break;
					default: // Leaf node
						start += inline.GetText().Length;
						break;
				}
			}
		}
	}
}

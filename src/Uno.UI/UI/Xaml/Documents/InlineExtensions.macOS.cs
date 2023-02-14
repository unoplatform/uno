using System;
using System.Collections.Generic;
using System.Text;
using AppKit;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Xaml.Documents
{
	internal static partial class InlineExtensions
	{
		internal static NSStringAttributes GetAttributes(this Inline inline)
		{
			return Uno.UI.NSStringAttributesHelper.GetAttributes(
				inline.FontWeight,
				inline.FontStyle,
				inline.FontFamily,
				inline.Foreground,
				inline.FontSize,
				inline.CharacterSpacing,
				inline.BaseLineAlignment,
				inline.TextDecorations
			);
		}
	}
}

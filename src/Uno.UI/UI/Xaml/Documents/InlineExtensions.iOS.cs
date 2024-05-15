using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using Microsoft.UI.Xaml.Documents;

namespace Microsoft.UI.Xaml.Documents
{
	internal static partial class InlineExtensions
	{
		internal static UIStringAttributes GetAttributes(this Inline inline)
		{
			return Uno.UI.UIStringAttributesHelper.GetAttributes(
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

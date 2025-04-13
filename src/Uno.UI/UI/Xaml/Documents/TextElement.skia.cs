using System;
using Microsoft.UI.Xaml.Documents.TextFormatting;

namespace Microsoft.UI.Xaml.Documents;

public partial class TextElement
{
	partial void OnForegroundChangedPartial()
		=> (this.GetParent() as IBlock)?.Invalidate(false);
}

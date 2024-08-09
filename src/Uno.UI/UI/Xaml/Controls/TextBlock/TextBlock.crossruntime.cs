using System;
using Windows.ApplicationModel.DataTransfer;

namespace Microsoft.UI.Xaml.Controls;

partial class TextBlock
{
	/// <summary>
	/// Occurs when the text selection has changed.
	/// </summary>
	public event RoutedEventHandler SelectionChanged;

	/// <summary>
	/// Selects the entire contents in the TextBlock.
	/// </summary>
	public void SelectAll() => Selection = new Range(0, Text.Length);

	internal override bool IsViewHit() => Text != null || base.IsViewHit();
}

#nullable enable

using Uno.UI;
using Uno.UI.Xaml.Controls.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// Native iOS shows a magnifier over the text while the caret (press-and-hold drag) or a selection handle is
// dragged. The platform draws it (see INativeTextLoupeExtension); this only feeds it the caret position. Where the
// platform has none (the browser, iOS before 17) Uno draws its own (see TextBoxTouchLoupe).
internal sealed partial class TextBoxCore
{
	private bool _isLoupeActive;
	private TextBoxTouchLoupe? _managedLoupe;
	private bool _isManagedLoupeActive;

	// Lets runtime tests observe the loupe on hosts that have none (Skia Desktop).
	internal static INativeTextLoupeExtension? LoupeOverrideForTesting { get; set; }

	private INativeTextLoupeExtension? NativeLoupe => LoupeOverrideForTesting ?? TextBoxView?.Extension as INativeTextLoupeExtension;

	// Test hook: whether the Uno-drawn loupe is painted.
	internal bool IsManagedLoupeShowing => _managedLoupe?.IsShowing is true;

	/// <summary>
	/// Shows the loupe at the caret for <paramref name="caretIndex"/>, or moves it there if already showing.
	/// </summary>
	private void UpdateCaretLoupe(int caretIndex)
	{
		if (TouchSelectionConvention != TouchTextSelectionConvention.iOS
			|| Owner.XamlRoot is not { } xamlRoot
			|| TextBoxView?.DisplayBlock is not { } displayBlock)
		{
			return;
		}

		var localCaret = displayBlock.ParsedText.GetRectForIndex(caretIndex) with { Width = TextBlock.CaretThickness };
		var toRoot = displayBlock.TransformToVisual(null);

		if (FeatureConfiguration.TextBox.UseNativeTextLoupe && NativeLoupe is { } loupe)
		{
			var caretRect = toRoot.TransformBounds(localCaret);
			var point = new Point(caretRect.X, caretRect.Y + (caretRect.Height / 2));

			if (_isLoupeActive)
			{
				loupe.MoveLoupe(point, caretRect);
			}
			else
			{
				_isLoupeActive = loupe.BeginLoupe(xamlRoot, point, caretRect);
			}

			if (_isLoupeActive)
			{
				return;
			}
		}

		// No platform loupe, or it can't show one: draw our own.
		if (!FeatureConfiguration.TextBox.UseManagedTextLoupe)
		{
			return;
		}

		_managedLoupe ??= new TextBoxTouchLoupe();
		if (_isManagedLoupeActive)
		{
			_managedLoupe.Move(xamlRoot, localCaret, toRoot);
		}
		else
		{
			_managedLoupe.Show(xamlRoot, displayBlock, localCaret, toRoot);
			_isManagedLoupeActive = true;
		}
	}

	private void EndCaretLoupe()
	{
		if (_isLoupeActive)
		{
			_isLoupeActive = false;
			NativeLoupe?.EndLoupe();

			// SelectPartial held the native selection back during the drag; bring the native input up to date now.
			TextBoxView?.Select(SelectionStart, SelectionLength);
		}

		if (_isManagedLoupeActive)
		{
			_isManagedLoupeActive = false;
			_managedLoupe?.Hide();
		}
	}
}

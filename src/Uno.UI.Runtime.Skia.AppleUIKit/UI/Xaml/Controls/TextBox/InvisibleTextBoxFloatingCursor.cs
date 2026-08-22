#nullable enable

using CoreGraphics;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

/// <summary>
/// Translates the iOS floating cursor (space-bar trackpad gesture) into cumulative offsets for the
/// managed <see cref="TextBox"/>. Shared by both invisible proxies, which derive from
/// <see cref="UIKit.UITextField"/> and <see cref="UIKit.UITextView"/> and so have no common base.
/// </summary>
internal sealed class InvisibleTextBoxFloatingCursor
{
	private CGPoint _origin;

	public bool IsActive { get; private set; }

	/// <remarks>
	/// UIKit can send unbalanced Begin/Begin/End sequences, so this re-anchors rather than counting.
	/// </remarks>
	public void Begin(InvisibleTextBoxViewExtension? extension, CGPoint point)
	{
		_origin = point;
		IsActive = extension?.ProcessCaretDragGesture(TextBox.CaretDragPhase.Begin, default) ?? false;
	}

	public void Update(InvisibleTextBoxViewExtension? extension, CGPoint point)
	{
		if (!IsActive)
		{
			return;
		}

		// iOS points map 1:1 to WinUI DIPs — no LogicalToPhysicalPixels conversion here.
		var offset = new Point(point.X - _origin.X, point.Y - _origin.Y);
		IsActive = extension?.ProcessCaretDragGesture(TextBox.CaretDragPhase.Update, offset) ?? false;
	}

	public void End(InvisibleTextBoxViewExtension? extension)
	{
		if (IsActive)
		{
			IsActive = false;
			_ = extension?.ProcessCaretDragGesture(TextBox.CaretDragPhase.End, default);
		}
	}
}

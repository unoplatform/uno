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
	/// <summary>
	/// Bounds the proxy reports while a drag is running, centred on its own coordinate origin.
	/// </summary>
	/// <remarks>
	/// UIKit derives the floating cursor point as <c>caretRectForPosition + pan translation</c> and
	/// clamps it to the input view's bounds, so the real control-sized bounds cut the gesture short
	/// well before the text ends — most visibly when dragging up through a multiline TextBox.
	/// Not made any larger: the reported point loses precision as the rect grows.
	/// </remarks>
	internal static CGRect DragBounds { get; } = new(-2500, -2500, 5000, 5000);

	/// <summary>
	/// Caret rect the proxy reports while a drag is running, at the centre of <see cref="DragBounds"/>.
	/// </summary>
	/// <remarks>
	/// Pinning it keeps the reachable range symmetric and independent of the proxy's own layout,
	/// which is the same reason the floating cursor callbacks skip their UIKit base implementations.
	/// </remarks>
	internal static CGRect DragCaretRect { get; } = new(0, 0, 1, 1);

	// Captured on the first Update rather than at Begin: UIKit measures the point against the proxy's
	// bounds and caret rect, and neither is widened yet by the time Begin arrives. Costs that first
	// callback's translation, which beats guessing which base UIKit settled on.
	private CGPoint? _origin;

	public bool IsActive { get; private set; }

	/// <remarks>
	/// UIKit can send unbalanced Begin/Begin/End sequences, so this re-anchors rather than counting.
	/// </remarks>
	public void Begin(InvisibleTextBoxViewExtension? extension)
	{
		_origin = null;
		IsActive = extension?.ProcessCaretDragGesture(TextBox.CaretDragPhase.Begin, default) ?? false;
	}

	public void Update(InvisibleTextBoxViewExtension? extension, CGPoint point)
	{
		if (!IsActive)
		{
			return;
		}

		if (_origin is not { } origin)
		{
			_origin = point;
			return;
		}

		// iOS points map 1:1 to WinUI DIPs — no LogicalToPhysicalPixels conversion here.
		var offset = new Point(point.X - origin.X, point.Y - origin.Y);
		IsActive = extension?.ProcessCaretDragGesture(TextBox.CaretDragPhase.Update, offset) ?? false;
	}

	public void End(InvisibleTextBoxViewExtension? extension)
	{
		if (IsActive)
		{
			IsActive = false;
			_origin = null;
			_ = extension?.ProcessCaretDragGesture(TextBox.CaretDragPhase.End, default);
		}
	}
}

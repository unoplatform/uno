using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Uno.UI.Xaml.Controls;

internal partial class RPNode
{
	public RPNode(DependencyObject element)
	{
		m_element = element;
		m_state = RPState.Unresolved;
		m_isHorizontalLeaf = true;
		m_isVerticalLeaf = true;
		m_constraints = RPConstraints.None;

#if HAS_UNO // Handling of native controls.
		_nativeView = element as View;
#endif
	}

	// RPState flag checks.
	internal bool IsUnresolved() { return m_state == RPState.Unresolved; }
	internal bool IsPending() { return (m_state & RPState.Pending) == RPState.Pending; }
	internal bool IsMeasured() { return (m_state & RPState.Measured) == RPState.Measured; }
	internal bool IsArrangedHorizontally() { return (m_state & RPState.ArrangedHorizontally) == RPState.ArrangedHorizontally; }
	internal bool IsArrangedVertically() { return (m_state & RPState.ArrangedVertically) == RPState.ArrangedVertically; }
	internal bool IsArranged() { return (m_state & RPState.Arranged) == RPState.Arranged; }

	// RPEdge flag checks.
	internal bool IsLeftOf() { return (m_constraints & RPConstraints.LeftOf) == RPConstraints.LeftOf; }
	internal bool IsAbove() { return (m_constraints & RPConstraints.Above) == RPConstraints.Above; }
	internal bool IsRightOf() { return (m_constraints & RPConstraints.RightOf) == RPConstraints.RightOf; }
	internal bool IsBelow() { return (m_constraints & RPConstraints.Below) == RPConstraints.Below; }
	internal bool IsAlignHorizontalCenterWith() { return (m_constraints & RPConstraints.AlignHorizontalCenterWith) == RPConstraints.AlignHorizontalCenterWith; }
	internal bool IsAlignVerticalCenterWith() { return (m_constraints & RPConstraints.AlignVerticalCenterWith) == RPConstraints.AlignVerticalCenterWith; }
	internal bool IsAlignLeftWith() { return (m_constraints & RPConstraints.AlignLeftWith) == RPConstraints.AlignLeftWith; }
	internal bool IsAlignTopWith() { return (m_constraints & RPConstraints.AlignTopWith) == RPConstraints.AlignTopWith; }
	internal bool IsAlignRightWith() { return (m_constraints & RPConstraints.AlignRightWith) == RPConstraints.AlignRightWith; }
	internal bool IsAlignBottomWith() { return (m_constraints & RPConstraints.AlignBottomWith) == RPConstraints.AlignBottomWith; }
	internal bool IsAlignLeftWithPanel() { return (m_constraints & RPConstraints.AlignLeftWithPanel) == RPConstraints.AlignLeftWithPanel; }
	internal bool IsAlignTopWithPanel() { return (m_constraints & RPConstraints.AlignTopWithPanel) == RPConstraints.AlignTopWithPanel; }
	internal bool IsAlignRightWithPanel() { return (m_constraints & RPConstraints.AlignRightWithPanel) == RPConstraints.AlignRightWithPanel; }
	internal bool IsAlignBottomWithPanel() { return (m_constraints & RPConstraints.AlignBottomWithPanel) == RPConstraints.AlignBottomWithPanel; }
	internal bool IsAlignHorizontalCenterWithPanel() { return (m_constraints & RPConstraints.AlignHorizontalCenterWithPanel) == RPConstraints.AlignHorizontalCenterWithPanel; }
	internal bool IsAlignVerticalCenterWithPanel() { return (m_constraints & RPConstraints.AlignVerticalCenterWithPanel) == RPConstraints.AlignVerticalCenterWithPanel; }


	// Represents the space that's available to an element given its set of
	// constraints. The width and height of this rect is used to measure
	// a given element.
	internal Rect m_measureRect;

	// Represents the exact space within the MeasureRect that will be used 
	// to arrange a given element.
	internal Rect m_arrangeRect;

	private RPState m_state;

	// Indicates if this is the last element in a dependency chain that is
	// formed by only connecting nodes horizontally.
	internal bool m_isHorizontalLeaf;

	// Indicates if this is the last element in a dependency chain that is
	// formed by only connecting nodes vertically.
	internal bool m_isVerticalLeaf;

	private RPConstraints m_constraints;
	internal RPNode m_leftOfNode;
	internal RPNode m_aboveNode;
	internal RPNode m_rightOfNode;
	internal RPNode m_belowNode;
	internal RPNode m_alignHorizontalCenterWithNode;
	internal RPNode m_alignVerticalCenterWithNode;
	internal RPNode m_alignLeftWithNode;
	internal RPNode m_alignTopWithNode;
	internal RPNode m_alignRightWithNode;
	internal RPNode m_alignBottomWithNode;

	private readonly DependencyObject m_element;

#if HAS_UNO
	// Uno specific: Handling of native controls.
	private readonly View _nativeView;
#endif
}

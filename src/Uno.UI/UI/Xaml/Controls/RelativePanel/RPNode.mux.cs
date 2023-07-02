using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
	internal DependencyObject GetElement() => m_element;

	internal string GetName() => m_element.GetValue(FrameworkElement.NameProperty) as string;

	internal double GetDesiredWidth()
	{
		if (m_element is UIElement element)
		{
			//TODO Uno: Layout storage not supported internally yet
			//return element.GetLayoutStorage().m_desiredSize.width;
			return element.DesiredSize.Width;
		}
		else
		{
			// Native element, use parent to get desired size
			if (_nativeView is not null && m_element.GetParent() is RelativePanel relativePanel)
			{
				return relativePanel.GetChildDesiredSize(_nativeView).Width;
			}

			return 0.0;
		}
	}

	internal double GetDesiredHeight()
	{
		if (m_element is UIElement element)
		{
			//TODO Uno: Layout storage not supported internally yet
			//return element.GetLayoutStorage().m_desiredSize.height;
			return element.DesiredSize.Height;
		}
#if HAS_UNO // Handling native controls
		else if (_nativeView is not null && m_element.GetParent() is RelativePanel relativePanel)
		{
			return relativePanel.GetChildDesiredSize(_nativeView).Height;
		}
#endif

		return 0.0;
	}

	internal void Measure(Size availableSize)
	{
		if (m_element is UIElement element)
		{
			element.Measure(availableSize);
			//TODO Uno: Layout storage not supported internally yet
			//element.EnsureLayoutStorage();
		}
#if HAS_UNO // Handling native controls
		else if (_nativeView is not null && m_element.GetParent() is RelativePanel relativePanel)
		{
			relativePanel.MeasureChild(_nativeView, availableSize);
		}
#endif
	}

	internal void Arrange(Rect finalRect)
	{
		if (m_element is UIElement element)
		{
			element.Arrange(finalRect);
		}
#if HAS_UNO // Handling native controls
		else if (_nativeView is not null && m_element.GetParent() is RelativePanel relativePanel)
		{
			relativePanel.ArrangeChild(_nativeView, finalRect);
		}
#endif
	}

	internal void GetLeftOfValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.LeftOfProperty, out pValue);
	}

	internal void GetAboveValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AboveProperty, out pValue);
	}

	internal void GetRightOfValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.RightOfProperty, out pValue);
	}

	internal void GetBelowValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.BelowProperty, out pValue);
	}

	internal void GetAlignHorizontalCenterWithValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignHorizontalCenterWithProperty, out pValue);
	}

	internal void GetAlignVerticalCenterWithValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignVerticalCenterWithProperty, out pValue);
	}

	internal void GetAlignLeftWithValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignLeftWithProperty, out pValue);
	}

	internal void GetAlignTopWithValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignTopWithProperty, out pValue);
	}

	internal void GetAlignRightWithValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignRightWithProperty, out pValue);
	}

	internal void GetAlignBottomWithValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignBottomWithProperty, out pValue);
	}

	internal void GetAlignLeftWithPanelValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignLeftWithPanelProperty, out pValue);
	}

	internal void GetAlignTopWithPanelValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignTopWithPanelProperty, out pValue);
	}

	internal void GetAlignRightWithPanelValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignRightWithPanelProperty, out pValue);
	}

	internal void GetAlignBottomWithPanelValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignBottomWithPanelProperty, out pValue);
	}

	internal void GetAlignHorizontalCenterWithPanelValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignHorizontalCenterWithPanelProperty, out pValue);
	}

	internal void GetAlignVerticalCenterWithPanelValue(out object pValue)
	{
		GetPropertyValue(RelativePanel.AlignVerticalCenterWithPanelProperty, out pValue);
	}

	internal bool IsLeftAnchored()
	{
		return (IsAlignLeftWithPanel() || IsAlignLeftWith() || (IsRightOf() && !IsAlignHorizontalCenterWith()));
	}

	internal bool IsTopAnchored()
	{
		return (IsAlignTopWithPanel() || IsAlignTopWith() || (IsBelow() && !IsAlignVerticalCenterWith()));
	}

	internal bool IsRightAnchored()
	{
		return (IsAlignRightWithPanel() || IsAlignRightWith() || (IsLeftOf() && !IsAlignHorizontalCenterWith()));
	}

	internal bool IsBottomAnchored()
	{
		return (IsAlignBottomWithPanel() || IsAlignBottomWith() || (IsAbove() && !IsAlignVerticalCenterWith()));
	}

	internal bool IsHorizontalCenterAnchored()
	{
		return ((IsAlignHorizontalCenterWithPanel() && !IsAlignLeftWithPanel() && !IsAlignRightWithPanel() && !IsAlignLeftWith() && !IsAlignRightWith() && !IsLeftOf() && !IsRightOf())
			|| (IsAlignHorizontalCenterWith() && !IsAlignLeftWithPanel() && !IsAlignRightWithPanel() && !IsAlignLeftWith() && !IsAlignRightWith()));
	}

	internal bool IsVerticalCenterAnchored()
	{
		return ((IsAlignVerticalCenterWithPanel() && !IsAlignTopWithPanel() && !IsAlignBottomWithPanel() && !IsAlignTopWith() && !IsAlignBottomWith() && !IsAbove() && !IsBelow())
			|| (IsAlignVerticalCenterWith() && !IsAlignTopWithPanel() && !IsAlignBottomWithPanel() && !IsAlignTopWith() && !IsAlignBottomWith()));
	}

	internal void SetPending(bool value)
	{
		if (value)
		{
			m_state |= RPState.Pending;
		}
		else
		{
			m_state &= ~RPState.Pending;
		}
	}

	internal void SetMeasured(bool value)
	{
		if (value)
		{
			m_state |= RPState.Measured;
		}
		else
		{
			m_state &= ~RPState.Measured;
		}
	}

	internal void SetArrangedHorizontally(bool value)
	{
		if (value)
		{
			m_state |= RPState.ArrangedHorizontally;
		}
		else
		{
			m_state &= ~RPState.ArrangedHorizontally;
		}
	}

	internal void SetArrangedVertically(bool value)
	{
		if (value)
		{
			m_state |= RPState.ArrangedVertically;
		}
		else
		{
			m_state &= ~RPState.ArrangedVertically;
		}
	}

	internal void SetLeftOfConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_leftOfNode = neighbor;
			m_constraints |= RPConstraints.LeftOf;
		}
		else
		{
			m_leftOfNode = null;
			m_constraints &= ~RPConstraints.LeftOf;
		}
	}

	internal void SetAboveConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_aboveNode = neighbor;
			m_constraints |= RPConstraints.Above;
		}
		else
		{
			m_aboveNode = null;
			m_constraints &= ~RPConstraints.Above;
		}
	}

	internal void SetRightOfConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_rightOfNode = neighbor;
			m_constraints |= RPConstraints.RightOf;
		}
		else
		{
			m_rightOfNode = null;
			m_constraints &= ~RPConstraints.RightOf;
		}
	}

	internal void SetBelowConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_belowNode = neighbor;
			m_constraints |= RPConstraints.Below;
		}
		else
		{
			m_belowNode = null;
			m_constraints &= ~RPConstraints.Below;
		}
	}

	internal void SetAlignHorizontalCenterWithConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_alignHorizontalCenterWithNode = neighbor;
			m_constraints |= RPConstraints.AlignHorizontalCenterWith;
		}
		else
		{
			m_alignHorizontalCenterWithNode = null;
			m_constraints &= ~RPConstraints.AlignHorizontalCenterWith;
		}
	}

	internal void SetAlignVerticalCenterWithConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_alignVerticalCenterWithNode = neighbor;
			m_constraints |= RPConstraints.AlignVerticalCenterWith;
		}
		else
		{
			m_alignVerticalCenterWithNode = null;
			m_constraints &= ~RPConstraints.AlignVerticalCenterWith;
		}
	}

	internal void SetAlignLeftWithConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_alignLeftWithNode = neighbor;
			m_constraints |= RPConstraints.AlignLeftWith;
		}
		else
		{
			m_alignLeftWithNode = null;
			m_constraints &= ~RPConstraints.AlignLeftWith;
		}
	}

	internal void SetAlignTopWithConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_alignTopWithNode = neighbor;
			m_constraints |= RPConstraints.AlignTopWith;
		}
		else
		{
			m_alignTopWithNode = null;
			m_constraints &= ~RPConstraints.AlignTopWith;
		}
	}

	internal void SetAlignRightWithConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_alignRightWithNode = neighbor;
			m_constraints |= RPConstraints.AlignRightWith;
		}
		else
		{
			m_alignRightWithNode = null;
			m_constraints &= ~RPConstraints.AlignRightWith;
		}
	}

	internal void SetAlignBottomWithConstraint(RPNode neighbor)
	{
		if (neighbor != null)
		{
			m_alignBottomWithNode = neighbor;
			m_constraints |= RPConstraints.AlignBottomWith;
		}
		else
		{
			m_alignBottomWithNode = null;
			m_constraints &= ~RPConstraints.AlignBottomWith;
		}
	}

	internal void SetAlignLeftWithPanelConstraint(bool value)
	{
		if (value)
		{
			m_constraints |= RPConstraints.AlignLeftWithPanel;
		}
		else
		{
			m_constraints &= ~RPConstraints.AlignLeftWithPanel;
		}
	}

	internal void SetAlignTopWithPanelConstraint(bool value)
	{
		if (value)
		{
			m_constraints |= RPConstraints.AlignTopWithPanel;
		}
		else
		{
			m_constraints &= ~RPConstraints.AlignTopWithPanel;
		}
	}

	internal void SetAlignRightWithPanelConstraint(bool value)
	{
		if (value)
		{
			m_constraints |= RPConstraints.AlignRightWithPanel;
		}
		else
		{
			m_constraints &= ~RPConstraints.AlignRightWithPanel;
		}
	}

	internal void SetAlignBottomWithPanelConstraint(bool value)
	{
		if (value)
		{
			m_constraints |= RPConstraints.AlignBottomWithPanel;
		}
		else
		{
			m_constraints &= ~RPConstraints.AlignBottomWithPanel;
		}
	}

	internal void SetAlignHorizontalCenterWithPanelConstraint(bool value)
	{
		if (value)
		{
			m_constraints |= RPConstraints.AlignHorizontalCenterWithPanel;
		}
		else
		{
			m_constraints &= ~RPConstraints.AlignHorizontalCenterWithPanel;
		}
	}

	internal void SetAlignVerticalCenterWithPanelConstraint(bool value)
	{
		if (value)
		{
			m_constraints |= RPConstraints.AlignVerticalCenterWithPanel;
		}
		else
		{
			m_constraints &= ~RPConstraints.AlignVerticalCenterWithPanel;
		}
	}

	internal void UnmarkNeighborsAsHorizontalOrVerticalLeaves()
	{
		bool isHorizontallyCenteredFromLeft = false;
		bool isHorizontallyCenteredFromRight = false;
		bool isVerticallyCenteredFromTop = false;
		bool isVerticallyCenteredFromBottom = false;

		if (!IsAlignLeftWithPanel())
		{
			if (IsAlignLeftWith())
			{
				m_alignLeftWithNode.m_isHorizontalLeaf = false;
			}
			else if (IsAlignHorizontalCenterWith())
			{
				isHorizontallyCenteredFromLeft = true;
			}
			else if (IsRightOf())
			{
				m_rightOfNode.m_isHorizontalLeaf = false;
			}
		}

		if (!IsAlignTopWithPanel())
		{
			if (IsAlignTopWith())
			{
				m_alignTopWithNode.m_isVerticalLeaf = false;
			}
			else if (IsAlignVerticalCenterWith())
			{
				isVerticallyCenteredFromTop = true;
			}
			else if (IsBelow())
			{
				m_belowNode.m_isVerticalLeaf = false;
			}
		}

		if (!IsAlignRightWithPanel())
		{
			if (IsAlignRightWith())
			{
				m_alignRightWithNode.m_isHorizontalLeaf = false;
			}
			else if (IsAlignHorizontalCenterWith())
			{
				isHorizontallyCenteredFromRight = true;
			}
			else if (IsLeftOf())
			{
				m_leftOfNode.m_isHorizontalLeaf = false;
			}
		}

		if (!IsAlignBottomWithPanel())
		{
			if (IsAlignBottomWith())
			{
				m_alignBottomWithNode.m_isVerticalLeaf = false;
			}
			else if (IsAlignVerticalCenterWith())
			{
				isVerticallyCenteredFromBottom = true;
			}
			else if (IsAbove())
			{
				m_aboveNode.m_isVerticalLeaf = false;
			}
		}

		if (isHorizontallyCenteredFromLeft && isHorizontallyCenteredFromRight)
		{
			m_alignHorizontalCenterWithNode.m_isHorizontalLeaf = false;
		}

		if (isVerticallyCenteredFromTop && isVerticallyCenteredFromBottom)
		{
			m_alignVerticalCenterWithNode.m_isVerticalLeaf = false;
		}
	}

	private void GetPropertyValue(
		DependencyProperty propertyIndex,
		out object value)
	{
		// TODO Uno: Should also recognize "Unset" value
		value = m_element?.GetValue(propertyIndex);
		//TODO Uno: DeferredElement not supported yet.
		//MUX_ASSERT(m_element is UIElement || m_element.OfTypeByIndex<KnownTypeIndex.DeferredElement>());

		//if (m_element.OfTypeByIndex<KnownTypeIndex.UIElement>())
		//{
		//	IFCFAILFAST(m_element.GetValueByIndex(propertyIndex, value));
		//}
		//else if (m_element.OfTypeByIndex<KnownTypeIndex.DeferredElement>())
		//{
		//	bool valueSet = false;
		//	CDeferredElement* element = (CDeferredElement*)(m_element);
		//	DeferredElementCustomRuntimeData* runtimeData = element.GetCustomRuntimeData();

		//	if (runtimeData)
		//	{
		//		for (var prop : runtimeData.GetNonDeferredProperties())
		//		{
		//			KnownPropertyIndex index = prop.first.get_PropertyToken().GetHandle();

		//			if (index == propertyIndex)
		//			{
		//				value.CopyConverted(prop.second);
		//				valueSet = true;
		//				break;
		//			}
		//		}
		//	}

		//	// If the value was not found, this means that the property
		//	// was not set in the first place.
		//	if (!valueSet)
		//	{
		//		value = DependencyProperty.Unset;
		//	}
		//}
	}
}

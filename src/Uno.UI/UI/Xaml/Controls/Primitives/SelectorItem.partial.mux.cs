// MUX Reference SelectorItem_Partial.cpp, tag winui3/release/1.8.1

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Input;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class SelectorItem
{
	/// <summary>
	/// Gets the parent Selector.
	/// </summary>
	internal Selector GetParentSelector()
	{
		if (m_wrParentSelector is not null && m_wrParentSelector.TryGetTarget(out var parentSelector))
		{
			return parentSelector;
		}

#if HAS_UNO
		// Uno specific: Fall back to ItemsControl hierarchy when weak reference is not set.
		// TODO Uno: Remove this fallback when SetParentSelector is called from Selector.PrepareContainerForItemOverride
		return ItemsControl.ItemsControlFromItemContainer(this) as Selector;
#else
		return null;
#endif
	}

	/// <summary>
	/// Sets the parent Selector.
	/// </summary>
	internal virtual void SetParentSelector(Selector pParentSelector)
	{
		m_wrParentSelector = pParentSelector is not null
			? new WeakReference<Selector>(pParentSelector)
			: null;
	}

	// In WinUI, OnPropertyChanged2 handles the IsSelected property change notification.
	// In Uno, this is handled by the DependencyProperty callback mechanism via OnIsSelectedChangedPartial.
	partial void OnIsSelectedChangedPartial(bool oldIsSelected, bool newIsSelected)
	{
#if HAS_UNO
		// Uno specific: Update pointer-driven visual states.
		// In WinUI, this is handled by ChangeVisualState called from OnIsSelectedChanged.
		// In Uno, UpdateCommonStates manages the delayed/batched pointer-driven states separately.
		UpdateCommonStates(useTransitions: IsLoaded);
#endif
		OnIsSelectedChanged(newIsSelected);
	}

	/// <summary>
	/// Called when IsSelected property has changed.
	/// </summary>
	private protected virtual void OnIsSelectedChanged(bool isSelected)
	{
		var parentSelector = GetParentSelector();
		if (parentSelector is not null)
		{
			parentSelector.NotifyListItemSelected(this, !isSelected, isSelected);
			// TODO Uno: RaiseIsSelectedChangedAutomationEvent is not yet ported
			// parentSelector.RaiseIsSelectedChangedAutomationEvent(this, isSelected);
		}
		ChangeVisualState(true);
	}

	/// <summary>
	/// Called when the Content property changes.
	/// </summary>
	protected override void OnContentChanged(object oldContent, object newContent)
	{
		base.OnContentChanged(oldContent, newContent);

		// Check if this SelectorItem is a data virtualized item that hasn't been
		// realized yet (or was a data virtualized item we just realized now)
		var parentSelector = GetParentSelector();
		if (parentSelector is not null)
		{
			// TODO Uno: ShowPlaceholderIfVirtualized is not yet ported
			// parentSelector.ShowPlaceholderIfVirtualized(this);
		}
	}

	/// <summary>
	/// Change to the correct visual state for the SelectorItem.
	/// </summary>
	private protected override void ChangeVisualState(bool useTransitions)
	{
#if HAS_UNO
		// !!!!!! WARNING: On Uno, pointer-driven visual states (Selected, Normal, Pressed, etc.)
		// are managed by UpdateCommonStates, not this method. See UpdateCommonStates instead.
#endif

		// Update the VisualStates of parent classes
		base.ChangeVisualState(useTransitions);

		// And batch the changes of the VisualStates for SelectorItem and derived classes.
		// Note: WinUI uses VisualStateManagerBatchContext for batching, which is not available in Uno.
		ChangeVisualStateWithContext(useTransitions);

#if HAS_UNO
		// Uno specific: Handle ListViewBaseItem visual states.
		// In WinUI, this logic lives in the internal ListViewBaseItem class
		// which overrides ChangeVisualStateWithContext. Since ListViewBaseItem is not
		// exposed in the public API, Uno handles it here.
		if (IsListViewBaseItem)
		{
			var criteria = new ListViewBaseItemVisualStatesCriteria();

			criteria.isEnabled = IsEnabled;
			criteria.isSelected = IsSelected;
			criteria.focusState = FocusState;

			// Pressed state should be handled whether it's mouse or touch
			// m_inCheckboxPressedForTouch is not used because it is part of the 8.1 template
			criteria.isPressed = IsPointerPressed;
			criteria.isPointerOver = IsPointerOver;
			//criteria.isDragVisualCaptured = m_dragVisualCaptured; // Uno TODO

			if (Selector is ListViewBase spListView)
			{
				criteria.isDragging = spListView.IsInDragDrop();
				criteria.isDraggedOver = spListView.IsDragOverItem(this);
				criteria.dragItemsCount = spListView.DragItemsCount();
				criteria.isItemDragPrimary = spListView.IsContainerDragDropOwner(this);

				// Holding gesture will show drag visual
				criteria.canDrag = spListView.CanDragItems;
				criteria.canReorder = spListView.CanReorderItems;
				if (spListView.GetIsHolding())
				{
					criteria.isHolding = true;
					// Uno TODO
					//if (m_isHolding)
					//{
					//	criteria.isItemDragPrimary = true;
					//}
				}

				criteria.isMultiSelect = spListView.IsMultiSelectCheckBoxEnabled;

				var selectionMode = spListView.SelectionMode;

				// if the ListView selection mode is None, we should appear as not Selected
				criteria.isSelected &= (selectionMode != ListViewSelectionMode.None);

				// Read-only mode
				{
					bool isItemClickEnabled = false;

					isItemClickEnabled = spListView.IsItemClickEnabled;

					if (selectionMode == ListViewSelectionMode.None && !isItemClickEnabled)
					{
						criteria.isPressed = false;
						criteria.isPointerOver = false;
					}
				}

				if (criteria.isMultiSelect)
				{

					criteria.isMultiSelect &= spListView.SelectionMode == ListViewSelectionMode.Multiple;
				}

				criteria.isInsideListView = true;

				foreach (var state in VisualStatesHelper.GetValidVisualStatesListViewBaseItem(criteria))
				{
					GoToState(useTransitions, state);
				}
			}
		}
#endif
	}

	/// <summary>
	/// Change to the correct visual state for the SelectorItem
	/// using an existing VisualStateManagerBatchContext.
	/// </summary>
	/// <remarks>
	/// In WinUI, this method takes a VisualStateManagerBatchContext parameter for batching.
	/// Since Uno does not have VisualStateManagerBatchContext, VisualStateManager.GoToState is called directly.
	/// </remarks>
	internal virtual void ChangeVisualStateWithContext(bool useTransitions)
	{
		// DataVirtualization state group
		if (m_isPlaceholder || m_isUIPlaceholder)
		{
			VisualStateManager.GoToState(this, "DataPlaceholder", useTransitions);
		}
		else
		{
			VisualStateManager.GoToState(this, "DataAvailable", useTransitions);
		}
	}

	/// <summary>
	/// If this item is unfocused, sets focus on the SelectorItem.
	/// Otherwise, sets focus to whichever element currently has focus
	/// (so focusState can be propagated).
	/// </summary>
	/// <param name="focusState">The focus state to apply.</param>
	/// <param name="animateIfBringIntoView">Whether to animate when bringing into view.</param>
	/// <param name="pFocused">Returns whether focus was successfully set.</param>
	/// <param name="focusNavigationDirection">The direction of focus navigation.</param>
	/// <param name="inputActivationBehavior">The input activation behavior. Defaults to RequestActivation to match legacy behavior.</param>
	internal void FocusSelfOrChild(
		FocusState focusState,
		bool animateIfBringIntoView,
		out bool pFocused,
		FocusNavigationDirection focusNavigationDirection,
		Uno.UI.Xaml.Input.InputActivationBehavior inputActivationBehavior)
	{
		bool isItemAlreadyFocused = false;
		DependencyObject spItemToFocus = null;

		pFocused = false;

		isItemAlreadyFocused = HasFocus();
		if (isItemAlreadyFocused)
		{
			// Re-focus the currently focused item to propagate focusState (the item might be focused
			// under a different FocusState value).
			spItemToFocus = this.GetFocusedElement();
		}
		else
		{
			spItemToFocus = this;
		}

		if (spItemToFocus is not null)
		{
			bool forceBringIntoView = false;
			pFocused = this.SetFocusedElementWithDirection(spItemToFocus, focusState, animateIfBringIntoView, focusNavigationDirection, forceBringIntoView, inputActivationBehavior);
		}
	}

	#region IsSelected data source integration
	// In WinUI, SelectorItem overrides GetValue to intercept reads of IsSelected
	// and delegates to get_IsSelectedImpl / put_IsSelectedImpl which consult the
	// parent Selector's DataSource (ISelectionInfo) when available.
	// This infrastructure is not yet ported to Uno.

	// TODO Uno: Port GetValue override for IsSelected data source integration
	// Original C++:
	// _Check_return_ HRESULT SelectorItem::GetValue(
	//     _In_ const CDependencyProperty* pDP,
	//     _Outptr_ IInspectable **ppValue)
	// {
	//     if (pDP->GetIndex() == KnownPropertyIndex::SelectorItem_IsSelected)
	//     {
	//         BOOLEAN isSelected = FALSE;
	//         IFC_RETURN(get_IsSelected(&isSelected));
	//         boxedValue.SetBool(!!isSelected);
	//         IFC_RETURN(CValueBoxer::UnboxObjectValue(&boxedValue, pDP->GetPropertyType(),
	//             __uuidof(IInspectable), reinterpret_cast<void**>(ppValue)));
	//     }
	//     else
	//     {
	//         IFC_RETURN(SelectorItemGenerated::GetValue(pDP, ppValue));
	//     }
	// }

	// TODO Uno: Port get_IsSelectedImpl for data source integration
	// Original C++:
	// _Check_return_ HRESULT SelectorItem::get_IsSelectedImpl(BOOLEAN* pValue)
	// {
	//     bool isValueSet = false;
	//     if (m_wrParentSelector)
	//     {
	//         auto spSelector = GetParentSelector();
	//         if (spSelector)
	//         {
	//             IFC_RETURN(spSelector->DataSourceGetIsSelected(this, pValue, &isValueSet));
	//         }
	//     }
	//     if (!isValueSet)
	//     {
	//         IFC_RETURN(GetValueByKnownIndex(KnownPropertyIndex::SelectorItem_IsSelected, pValue));
	//     }
	// }

	// TODO Uno: Port put_IsSelectedImpl
	// Original C++:
	// _Check_return_ HRESULT SelectorItem::put_IsSelectedImpl(BOOLEAN value)
	// {
	//     // SetValue triggers OnPropertyChanged2
	//     // OnPropertyChanged2 triggers OnIsSelectedChanged
	//     // OnIsSelectedChanged triggers NotifyListIsItemSelected
	//     // NotifyListIsItemSelected triggers DataSource->SelectRange if SelectionInfo interface is implemented
	//     return SetValueByKnownIndex(KnownPropertyIndex::SelectorItem_IsSelected, value);
	// }
	#endregion

	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns a plain text string to provide a default AutomationProperties.Name
	//      in the absence of an explicitly defined one
	//
	//---------------------------------------------------------------------------
	internal override string GetPlainText()
	{
		string strPlainText = null;

		var contentTemplateRoot = ContentTemplateRoot;

		if (contentTemplateRoot is DependencyObject doContentTemplateRoot)
		{
			// we have the first child of the content. Check whether it has an automation name

			strPlainText = AutomationProperties.GetName(doContentTemplateRoot);

			// fallback: use getplain text on it
			if (string.IsNullOrEmpty(strPlainText))
			{
				var contentTemplateRootAsIFE = contentTemplateRoot as FrameworkElement;

				strPlainText = null;

				if (contentTemplateRootAsIFE is not null)
				{
					strPlainText = contentTemplateRootAsIFE.GetPlainText();
				}
			}

			// fallback, use GetPlainText on the contentpresenter, who has some special logic to account for old templates
			if (string.IsNullOrEmpty(strPlainText))
			{
				var contentTemplateRootAsIFE = contentTemplateRoot as FrameworkElement;

				strPlainText = null;

				if (contentTemplateRootAsIFE is not null)
				{
					var pParent = contentTemplateRootAsIFE.Parent;
					if (pParent is ContentPresenter cp)
					{
						strPlainText = cp.GetTextBlockText();
					}
				}
			}
		}

		// Fallback is to call the ancestor's GetPlainText. SelectorItemGenerated doesn't have a GetPlainText
		// implementation, so it would find something in the parent. As of this writing, it should be the
		// ContentControl.
		if (string.IsNullOrEmpty(strPlainText))
		{
			return base.GetPlainText();
		}

		return strPlainText;
	}
}

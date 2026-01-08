using System;
using System.Collections.Generic;
using System.Diagnostics;
using Uno.Disposables;
using System.Text;
using Microsoft.UI.Xaml.Input;
using Windows.UI.Core;
using System.Threading.Tasks;
using Uno.UI;
using Uno.UI.Xaml.Core;
using Windows.Devices.Input;
#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class SelectorItem : ContentControl
	{
		private static class CommonStates
		{
			public const string Selected = "Selected";
			public const string Normal = "Normal";
			public const string Over = "PointerOver";
			public const string Pressed = "Pressed";
			public const string OverSelected = "PointerOverSelected"; // "SelectedPointerOver" for ListBoxItem, ComboBoxItem and PivotHeaderItem
			public const string PressedSelected = "PressedSelected"; // "SelectedPressed" for ListBoxItem, ComboBoxItem and PivotHeaderItem
																	 // On ListViewItem and GridViewItem we also have this state declared in default style,
																	 // however it seems to never been activated
																	 // public const string OverPressed = "PointerOverPressed";
		}

		private static class DisabledStates
		{
			public const string Enabled = "Enabled";
			public const string Disabled = "Disabled";
		}

		private static class MultiSelectStates
		{
			public const string MultiSelectEnabled = "MultiSelectEnabled";
			public const string MultiSelectDisabled = "MultiSelectDisabled";
		}

		private enum ManipulationUpdateKind
		{
			None = 0,
			Begin,
			End,
			Clicked
		}

		private static readonly Stopwatch _chronometer = Stopwatch.StartNew();

		/// <summary>
		/// Delay time before setting the pressed state of an item to false, to allow time for the Pressed visual state to be drawn and perceived.
		/// </summary>
		private static readonly TimeSpan MinTimeBetweenPressStates = TimeSpan.FromMilliseconds(100);

		/// <summary>
		/// Whether the SelectorItem will handle touches. This can be set to false for compatibility with controls where the parent
		/// handles touches (ComboBox-Android, legacy ListView/GridView).
		/// </summary>
		internal bool ShouldHandlePressed { get; set; } = true;

		// Workaround for the fact that ListViewBaseItem exists internally on WinUI, but isn't included in the public API
		private bool IsListViewBaseItem => this is ListViewItem || this is GridViewItem;

		/// <remarks>
		/// Ensure that the ContentControl will create its children even
		/// if it has no parent view. This is critical for the recycling panels,
		/// where the content is databound before being assigned to its
		/// parent and displayed.
		/// </remarks>
		protected override bool CanCreateTemplateWithoutParent { get; } = true;

		private string _currentState;
		private uint _goToStateRequest;
		private TimeSpan _pauseStateUpdateUntil;
		private bool _canRaiseClickOnPointerRelease;

		public SelectorItem()
		{
			AddHandler(ManipulationStartedEvent, _onManipulationStarted, handledEventsToo: true);

			// We subscribe with handledEventsToo in order to clean up state even if the "originalSource"
			// of a PointerPressed is a child that captures the pointer and may handle Pointer<Released/Exited|Canceled>
			// without handling PointerPressed. In that case we wouldn't receive a PointerPressed without
			// a matching Pointer<Released|Exited|Canceled> without handledEventsToo.
			// Note: we also subscribe to PointerCaptureLostEvent to cleanup the state when the pointer is canceled by the scroller
			//		 (i.e. with CanceledByDirectManipulation, in that case, we only raise a CaptureLost event, not canceled).
			AddHandler(PointerReleasedEvent, _onPointerReleased, handledEventsToo: true);
			AddHandler(PointerExitedEvent, _onPointerExitedOrCanceled, handledEventsToo: true);
			AddHandler(PointerCanceledEvent, _onPointerExitedOrCanceled, handledEventsToo: true);
			AddHandler(PointerCaptureLostEvent, _onPointerExitedOrCanceled, handledEventsToo: true);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			UpdateVisualStates(useTransitions: IsLoaded);
		}

		private protected Selector Selector => ItemsControl.ItemsControlFromItemContainer(this) as Selector;

		internal override UIElement VisualParent => Selector ?? base.VisualParent;

		private bool IsItemClickEnabled
		{
			get
			{
				if (!(Selector is ListViewBase listViewBase))
				{
					return true;
				}

				return listViewBase.IsItemClickEnabled || listViewBase.SelectionMode != ListViewSelectionMode.None;
			}
		}

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			UpdateDisabledStates(useTransitions: IsLoaded);

			base.OnIsEnabledChanged(e);
		}

		partial void OnIsSelectedChangedPartial(bool oldIsSelected, bool newIsSelected)
		{
			UpdateCommonStates(useTransitions: IsLoaded);
			OnIsSelectedChanged();

			Selector?.NotifyListItemSelected(this, oldIsSelected, newIsSelected);
		}

		internal protected virtual void OnIsSelectedChanged() { }

		/// <summary>
		/// Override context requested handling for ListViewItem and GridViewItem.
		/// If the item doesn't have a ContextFlyout, try to use the parent ListView/GridView's ContextFlyout.
		/// </summary>
		/// <param name="args">The event args from the ContextRequested event.</param>
		/// <remarks>
		/// Ported from WinUI ListViewBaseItem_Partial.cpp:1252-1271
		/// </remarks>
		private protected override void OnContextRequestedImpl(ContextRequestedEventArgs args)
		{
			// Only apply this behavior to ListViewItem and GridViewItem
			if (!IsListViewBaseItem)
			{
				base.OnContextRequestedImpl(args);
				return;
			}

			// First, try to show flyout on this item
			ShowContextFlyout(args, this);

			// If not handled and we have a parent ListView/GridView with ContextFlyout, try that
			if (!args.Handled && Selector is ListViewBase parentListView)
			{
				ShowContextFlyout(args, parentListView);
			}
		}

		private void UpdateCommonStatesWithoutNeedsLayout(bool useTransitions, PointerDeviceType deviceType, ManipulationUpdateKind manipulationUpdate)
		{
			using (InterceptSetNeedsLayout())
			{
				UpdateCommonStates(useTransitions, deviceType == PointerDeviceType.Mouse, manipulationUpdate);
			}
		}

		private void UpdateVisualStates(bool useTransitions)
		{
			if (GetTemplateRoot() is { })
			{
				UpdateCommonStates(useTransitions);
				UpdateDisabledStates(useTransitions);
				UpdateMultiSelectStates(useTransitions);
			}
		}

		private void UpdateCommonStates(bool useTransitions, bool isMouse = false, ManipulationUpdateKind manipulationUpdate = ManipulationUpdateKind.None)
		{
			// On Windows, the pressed state appears only after a few, and won't appear at all if you quickly start to scroll with the finger.
			// So here we make sure to delay the beginning of a manipulation to match this behavior (and avoid flickering when scrolling)
			// We also make sure that when user taps (Enter->Pressed->Move*->Release->Exit) on the item, he is able to see the pressed (selected) state.
			var state = GetCommonState(IsEnabled, IsSelected, IsPointerOver, _canRaiseClickOnPointerRelease);
			var requestId = ++_goToStateRequest; // Request ID is use to ensure to apply only the last requested state.

			TimeSpan delay; // delay to apply the 'state'
			bool pause; // should we force a pause after applying the 'state'
			if (manipulationUpdate == ManipulationUpdateKind.Clicked
				&& _currentState != CommonStates.PressedSelected
				&& _currentState != CommonStates.Pressed)
			{
				// When clicked (i.e. pointer released), but not yet in pressed state, we force to go immediately in pressed state
				// Then we let the standard go to state process (i.e. with delay handling) reach the final expected state.
				var pressedState = GetCommonState(IsEnabled, IsSelected, IsPointerOver, isPressed: true);
				_currentState = pressedState;
				VisualStateManager.GoToState(this, pressedState, useTransitions);

				_pauseStateUpdateUntil = _chronometer.Elapsed + MinTimeBetweenPressStates;

				delay = MinTimeBetweenPressStates;
				pause = false;
			}
			else if (manipulationUpdate == ManipulationUpdateKind.Begin)
			{
				// We delay the beginning of a manipulation to avoid flickers, but not for "exact" devices which has hover states
				// (i.e. mouse when not on iOS)
				if (isMouse)
				{
					_currentState = state;
					VisualStateManager.GoToState(this, state, useTransitions);
					return;
				}

				delay = MinTimeBetweenPressStates;
				pause = true;
			}
			else
			{
				delay = _pauseStateUpdateUntil - _chronometer.Elapsed;
				pause = false;
			}

			if (delay <= TimeSpan.Zero)
			{
				_currentState = state;
				VisualStateManager.GoToState(this, state, useTransitions);
			}
			else
			{
				_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, async () =>
				{
					await Task.Delay(delay);

					if (_goToStateRequest != requestId
						// If element has been unloaded (e.g. click on a ComboBoxItem) native animations of the target state won't run.
						// Then even if on next load we request UpdateCommonStates,
						// the VSM will determine that state didn't changed and will just ignore the request.
						// Note: The issue is probably in the VSM to allow a GoToState while not in visual tree,
						//		 but this is the most common case and teh safest place to patch.
						|| !IsLoaded)
					{
						return;
					}

					_currentState = state;
					VisualStateManager.GoToState(this, state, true);

					if (pause)
					{
						_pauseStateUpdateUntil = _chronometer.Elapsed + MinTimeBetweenPressStates;
					}
				});
			}
		}

		private void UpdateDisabledStates(bool useTransitions)
		{
			// TODO: This may need to be adjusted later when we remove the Visual State mixins.
			var state = IsEnabled ? DisabledStates.Enabled : DisabledStates.Disabled;
			VisualStateManager.GoToState(this, state, useTransitions);
		}

		internal void UpdateMultiSelectStates(bool useTransitions)
		{
			if (Selector is ListViewBase { SelectionMode: ListViewSelectionMode.Multiple, IsMultiSelectCheckBoxEnabled: true })
			{
				// We can safely always go to multiselect state
				VisualStateManager.GoToState(this, MultiSelectStates.MultiSelectEnabled, useTransitions);
			}
			else
			{
				// Retrieve the current state (which 'lives' on the SelectorItem's template root, and may change if it is re-templated)
				var currentState = VisualStateManager.GetCurrentState(this, nameof(MultiSelectStates))?.Name;
				if (currentState == MultiSelectStates.MultiSelectEnabled)
				{
					// The MultiSelectDisabled state goes through VisibleRect then collapsed, which means Disabled state can't be
					// invoked if the state is already disabled without having the selected check box appearing briefly. (Issue #403)
					VisualStateManager.GoToState(this, MultiSelectStates.MultiSelectDisabled, useTransitions);
				}
			}
		}

		private string GetCommonState(bool isEnabled, bool isSelected, bool isOver, bool isPressed)
		{
			var state = CommonStates.Normal;

			if (isEnabled)
			{
				if (isSelected && isPressed)
				{
					state = CommonStates.PressedSelected;
				}
				else if (FeatureConfiguration.SelectorItem.UseOverStates
					&& isSelected && isOver)
				{
					state = CommonStates.OverSelected;
				}
				else if (isSelected)
				{
					state = CommonStates.Selected;
				}
				else if (isPressed)
				{
					state = CommonStates.Pressed;
				}
				else if (FeatureConfiguration.SelectorItem.UseOverStates
					&& isOver)
				{
					state = CommonStates.Over;
				}
			}
			else
			{
				// If item is disabled, we only care about selection state
				if (isSelected)
				{
					state = CommonStates.Selected;
				}
			}

			return state;
		}

		internal override void PrepareForRecycle()
		{
			// It's important to clear the index on the way out (recycle) and not wait to set it on the way in (reuse)
			// because this property is used e.g. in ItemsControl.ContainerFromIndexInner which is used by e.g.
			// ListView to get a clicked container.
			ClearValue(ItemsControl.IndexForItemContainerProperty);

			base.PrepareForRecycle();
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();
#if __ANDROID__
			Focusable = true;
			FocusableInTouchMode = true;
#endif

			UpdateVisualStates(true);
		}

#if __APPLE_UIKIT__
		private bool _pressedOverride;
		private new bool IsPointerPressed => _pressedOverride || base.IsPointerPressed;

		/// <summary>
		/// Used by the legacy list view to set the item pressed
		/// </summary>
		internal void LegacySetPressed(bool isPressed)
			=> _pressedOverride = isPressed;
#endif

		private static readonly ManipulationStartedEventHandler _onManipulationStarted = (snd, _) =>
		{
			if (snd is SelectorItem that)
			{
				// If children are dealing with manipulation (like a SwipeControl),
				// we don't want to trigger the ItemClick nor the selection,
				// even if the pointer remained on this SelectorItem during the whole manipulation.
				that._canRaiseClickOnPointerRelease = false;
			}
		};

		private static readonly PointerEventHandler _onPointerReleased = (snd, e) => ((SelectorItem)snd).OnPointerReleasedPrivate(e);
		private static readonly PointerEventHandler _onPointerExitedOrCanceled = (snd, e) => ((SelectorItem)snd).CleanUpPointerState(e);

		/// <inheritdoc />
		protected override void OnPointerEntered(PointerRoutedEventArgs args)
		{
			base.OnPointerEntered(args);
			UpdateCommonStatesWithoutNeedsLayout(true, (PointerDeviceType)args.Pointer.PointerDeviceType, ManipulationUpdateKind.Begin);
		}

		/// <inheritdoc />
		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			if (ShouldHandlePressed
				&& IsItemClickEnabled
				&& args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
			{
				// Note: We do allow PointerCaptureResult.AlreadyCaptured as if element has been flagged as CanDrag
				//		 (common in ListView that do allow re-ordering ;) ),
				//		 the pointer might have already been captured for this SelectorItem.

				_canRaiseClickOnPointerRelease = true;
			}

			args.Handled = ShouldHandlePressed;

			base.OnPointerPressed(args);
			UpdateCommonStatesWithoutNeedsLayout(true, (PointerDeviceType)args.Pointer.PointerDeviceType, ManipulationUpdateKind.Begin);
		}

		/// <inheritdoc />
		private void OnPointerReleasedPrivate(PointerRoutedEventArgs args)
		{
			if (args.Handled)
			{
				CleanUpPointerState(args);
			}
			else
			{
				// Selector item focus should be triggered only when pointer is released.
				Focus(FocusState.Pointer);

				var update = ManipulationUpdateKind.End;
				if (_canRaiseClickOnPointerRelease)
				{
					_canRaiseClickOnPointerRelease = false;

					update = ManipulationUpdateKind.Clicked;
					Selector?.OnItemClicked(this, args.KeyModifiers);
				}

				args.Handled = ShouldHandlePressed;
				UpdateCommonStatesWithoutNeedsLayout(true, (PointerDeviceType)args.Pointer.PointerDeviceType, update);
			}
		}

		/// <inheritdoc />
		private void CleanUpPointerState(PointerRoutedEventArgs args)
		{
			// Not like a Button, if the pointer goes out of this item, we abort the ItemClick
			_canRaiseClickOnPointerRelease = false;
			UpdateCommonStatesWithoutNeedsLayout(true, (PointerDeviceType)args.Pointer.PointerDeviceType, ManipulationUpdateKind.End);
		}

		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);
			ChangeVisualState(true);

			if (Selector is ListViewBase lvb)
			{
				var index = lvb.IndexFromContainer(this);
				lvb.FocusedIndexContainerItem = (index, this, lvb.ItemFromIndex(index));
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			ChangeVisualState(true);
		}

		private IDisposable InterceptSetNeedsLayout()
		{
#if __APPLE_UIKIT__
			bool match(UIView view) => view is ListViewBaseInternalContainer || view is Selector;
			var cell = this.FindFirstParent<UIView>(predicate: match);
			return (cell as ListViewBaseInternalContainer)?.InterceptSetNeedsLayout();
#else
			return null;
#endif
		}

		private protected override void ChangeVisualState(bool useTransitions)
		{
			// !!!!!! WARNING: This method is actually not used (at least on skia and wasm) !!!!!!
			// cf. UpdateCommonStates instead ...

			base.ChangeVisualState(useTransitions);

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
		}
	}
}

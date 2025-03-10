// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Private.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using MUXControlsTestApp.Samples.Model;
using Uno.UI.Samples.Controls;
using Windows.Foundation;

namespace UITests.Microsoft_UI_Xaml_Controls.ItemsViewTests;

#if DEBUG || !__IOS__ // Samples times out on iOS.
[Sample("ItemsView", IgnoreInSnapshotTests = true)]
#endif
public sealed partial class ItemsViewSummaryPage : Page
{
	private enum QueuedOperationType
	{
		SetWidth,
		SetHeight,
		StartBringItemIntoView,
		DataSourceAdd,
		DataSourceInsert,
		DataSourceRemove,
		DataSourceRemoveAll,
		DataSourceReplace
	}

	private class QueuedOperation
	{
		public QueuedOperation(QueuedOperationType type)
		{
			this.Type = type;
		}

		public QueuedOperation(QueuedOperationType type, double value)
		{
			this.Type = type;
			this.DoubleValue = value;
		}

		public QueuedOperation(QueuedOperationType type, int value)
		{
			this.Type = type;
			this.IntValue = value;
		}

		public QueuedOperation(QueuedOperationType type, string value)
		{
			this.Type = type;
			this.StringValue = value;
		}

		public QueuedOperationType Type { get; set; }
		public double DoubleValue { get; set; }
		public int IntValue { get; set; }
		public string StringValue { get; set; }
	}

	private string _lorem = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam laoreet erat vel massa rutrum, eget mollis massa vulputate. Vivamus semper augue leo, eget faucibus nulla mattis nec. Donec scelerisque lacus at dui ultricies, eget auctor ipsum placerat. Integer aliquet libero sed nisi eleifend, nec rutrum arcu lacinia. Sed a sem et ante gravida congue sit amet ut augue. Donec quis pellentesque urna, non finibus metus. Proin sed ornare tellus. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam laoreet erat vel massa rutrum, eget mollis massa vulputate. Vivamus semper augue leo, eget faucibus nulla mattis nec. Donec scelerisque lacus at dui ultricies, eget auctor ipsum placerat. Integer aliquet libero sed nisi eleifend, nec rutrum arcu lacinia. Sed a sem et ante gravida congue sit amet ut augue. Donec quis pellentesque urna, non finibus metus. Proin sed ornare tellus.";
	private object _asyncEventReportingLock = new object();
	private List<string> _lstAsyncEventMessage = new List<string>();
	private List<string> _fullLogs = new List<string>();
	private List<QueuedOperation> _lstQueuedOperations = new List<QueuedOperation>();
	private List<int> _lstLinedFlowLayoutLockedItemIndexes = new List<int>();
	private SolidColorBrush _redBrush = new SolidColorBrush(Colors.Red);
	private SolidColorBrush _whiteBrush = new SolidColorBrush(Colors.White);
	private ItemsView _dynamicItemsView = null;
	private ItemsView _itemsView = null;
	private ObservableCollection<Recipe> _colRecipes = null;
	private ObservableCollection<Recipe> _colSmallRecipes = null;
	private ObservableCollection<Recipe> _colSmallUniformRecipes = null;
	private ObservableCollection<ItemContainer> _colSmallItemContainers = null;
	private ObservableCollection<Image> _colSmallImages = null;
	private List<Recipe> _lstRecipes = null;
	private DataTemplate[] _recipeTemplates = new DataTemplate[17];
	private StackLayout _stackLayout = null;
	private UniformGridLayout _uniformGridLayout = null;
	private FlowLayout _flowLayout = null;
	private LinedFlowLayout _linedFlowLayout = null;
	private DispatcherTimer _queuedOperationsTimer = new DispatcherTimer();
	private PointerEventHandler _itemsViewPointerWheelChangedEventHandler = null;
	private PointerEventHandler _scrollViewPointerWheelChangedEventHandler = null;
	private long _currentItemIndexChangedToken;

	public ItemsViewSummaryPage()
	{
		this.InitializeComponent();

		UseItemsView(markupItemsView);

		if (chkLogItemsViewMessages.IsChecked == false &&
			(chkLogScrollViewMessages.IsChecked == true || chkLogItemsRepeaterMessages.IsChecked == true || chkLogLinedFlowLayoutMessages.IsChecked == true))
		{
			//MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;

			if (chkLogLinedFlowLayoutMessages.IsChecked == true)
			{
				//LayoutsTestHooks.LinedFlowLayoutSnappedAverageItemsPerLineChanged += LayoutsTestHooks_LinedFlowLayoutSnappedAverageItemsPerLineChanged;
				//LayoutsTestHooks.LinedFlowLayoutInvalidated += LayoutsTestHooks_LinedFlowLayoutInvalidated;
				//LayoutsTestHooks.LinedFlowLayoutItemLocked += LayoutsTestHooks_LinedFlowLayoutItemLocked;

				//MUXControlsTestHooks.SetLoggingLevelForType("LinedFlowLayout", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			}
			if (chkLogItemsRepeaterMessages.IsChecked == true)
			{
				//MUXControlsTestHooks.SetLoggingLevelForType("ItemsRepeater", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			}
			if (chkLogScrollViewMessages.IsChecked == true)
			{
				//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
			}
		}

		if (chkItemsViewMethods != null && svItemsViewMethods != null)
		{
			svItemsViewMethods.Visibility = chkItemsViewMethods.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
		}

		_queuedOperationsTimer.Interval = new TimeSpan(0, 0, 4 /*sec*/);
		_queuedOperationsTimer.Tick += QueuedOperationsTimer_Tick;

		//LayoutsTestHooks.LinedFlowLayoutItemLocked += LayoutsTestHooks_LinedFlowLayoutItemLocked2;

		this.KeyDown += ItemsViewSummaryPage_KeyDown;
	}

	~ItemsViewSummaryPage()
	{
	}

	private void ItemsViewSummaryPage_KeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (e.Key == Windows.System.VirtualKey.G)
		{
			GetFullLog();
		}
		else if (e.Key == Windows.System.VirtualKey.C)
		{
			ClearFullLog();
		}
	}

	private protected override void OnUnloaded()
	{
		//LayoutsTestHooks.LinedFlowLayoutSnappedAverageItemsPerLineChanged -= LayoutsTestHooks_LinedFlowLayoutSnappedAverageItemsPerLineChanged;
		//LayoutsTestHooks.LinedFlowLayoutInvalidated -= LayoutsTestHooks_LinedFlowLayoutInvalidated;
		//LayoutsTestHooks.LinedFlowLayoutItemLocked -= LayoutsTestHooks_LinedFlowLayoutItemLocked;
		//LayoutsTestHooks.LinedFlowLayoutItemLocked -= LayoutsTestHooks_LinedFlowLayoutItemLocked2;

		//MUXControlsTestHooks.SetLoggingLevelForType("ItemsRepeater", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//MUXControlsTestHooks.SetLoggingLevelForType("ItemsView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//MUXControlsTestHooks.SetLoggingLevelForType("LinedFlowLayout", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);

		//MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;

		if (_itemsView != null)
		{
			_itemsView.ItemsSource = null;

			UnhookItemsRepeaterEvents(ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView));
			UnhookScrollViewEvents(ItemsViewTestHooks.GetScrollViewPart(_itemsView));
			UnhookItemsViewEvents();
		}
		UnhookLinedFlowLayoutEvents(_linedFlowLayout);

		ChkLinedFlowLayoutShowLockedItems_Unchecked(null, null);
		ChkShowCurrentElement_Unchecked(null, null);

		base.OnUnloaded();
	}

	private void UseItemsView(ItemsView iv)
	{
		if (_itemsView == iv || iv == null)
			return;

		try
		{
			if (_itemsView != null)
			{
				if (chkLogItemsViewMessages.IsChecked == true)
				{
					//MUXControlsTestHooks.SetLoggingLevelForType("ItemsView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
					//if (chkLogScrollViewMessages.IsChecked == false &&
					//	chkLogItemsRepeaterMessages.IsChecked == false &&
					//	chkLogLinedFlowLayoutMessages.IsChecked == false)
					//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
				}

				if (chkLogItemsViewEvents.IsChecked == true)
				{
					UnhookItemsViewEvents();
				}

				UnhookItemsRepeaterEvents(ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView));
			}

			_itemsView = iv;

			if (chkLogItemsViewMessages.IsChecked == true)
			{
				//MUXControlsTestHooks.SetLoggingLevelForType("ItemsView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
				//if (chkLogScrollViewMessages.IsChecked == false &&
				//	chkLogItemsRepeaterMessages.IsChecked == false &&
				//	chkLogLinedFlowLayoutMessages.IsChecked == false)
				//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
			}

			if (chkLogItemsViewEvents.IsChecked == true)
			{
				HookItemsViewEvents();
			}

			// Update ItemsView fields
			UpdateItemsViewLayout();
			UpdateItemsViewSelectionMode();
			UpdateItemsViewIsItemInvokedEnabled();
			UpdateBackground();
			UpdateBorderBrush();
			UpdateBorderThickness();
			UpdateMargin();
			UpdatePadding();
			UpdateWidth();
			UpdateHeight();
			UpdateMaxWidth();
			UpdateMaxHeight();
			UpdateCornerRadius();
			UpdateCurrentItemIndex();
			UpdateTabNavigation();
			UpdateXYFocusKeyboardNavigation();
			UpdateIsEnabled();
			UpdateIsTabStop();

			UpdateSelectedItem();
			UpdateSelectedItems();

			UpdateLinedFlowLayoutActualLineHeight();
			UpdateLinedFlowLayoutSnappedAverageItemsPerLineDbg();
			UpdateLinedFlowLayoutAverageItemAspectRatioDbg();
			UpdateLinedFlowLayoutForcedAverageItemAspectRatioDbg();
			UpdateLinedFlowLayoutForcedAverageItemsPerLineDividerDbg();
			UpdateLinedFlowLayoutForcedWrapMultiplierDbg();
			UpdateLinedFlowLayoutFrozenItemIndexes();
			UpdateLinedFlowLayoutIsFastPathSupportedDbg();
			UpdateLinedFlowLayoutLineHeight();
			UpdateLinedFlowLayoutLineSpacing();
			UpdateLinedFlowLayoutItemsJustification();
			UpdateLinedFlowLayoutItemsStretch();
			UpdateLinedFlowLayoutMinItemSpacing();
			UpdateLinedFlowLayoutRealizedItemIndexes();
			UpdateLinedFlowLayoutRequestedRangeLength();
			UpdateLinedFlowLayoutRequestedRangeStartIndex();
			UpdateStackLayoutOrientation();
			UpdateStackLayoutSpacing();
			UpdateUniformGridLayoutMaximumRowsOrColumns();
			UpdateUniformGridLayoutMinColumnSpacing();
			UpdateUniformGridLayoutMinRowSpacing();
			UpdateUniformGridLayoutOrientation();

			UpdateScrollViewFields();

			UpdateItemsRepeaterFields();
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateScrollViewFields()
	{
		UpdateContentOrientation();
		UpdateHorizontalScrollMode();
		UpdateVerticalScrollMode();
		UpdateZoomFactor();
		UpdateZoomMode();
		UpdateHorizontalScrollBarVisibility();
		UpdateVerticalScrollBarVisibility();
		UpdateHorizontalAnchorRatio();
		UpdateVerticalAnchorRatio();
		UpdateHorizontalOffset();
		UpdateVerticalOffset();
		UpdateExtentWidth();
		UpdateExtentHeight();
	}

	private void UpdateItemsRepeaterFields()
	{
		UpdateItemsRepeaterHorizontalAlignment();
		UpdateItemsRepeaterHorizontalCacheLength();
		UpdateItemsRepeaterVerticalAlignment();
		UpdateItemsRepeaterVerticalCacheLength();
		UpdateItemsRepeaterChildrenCount();
	}

	private void ItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
	{
		AppendAsyncEventMessage($"ItemsView.ItemInvoked InvokedItem={args.InvokedItem.ToString().Substring(0, 50)}");
	}

	private void ItemsView_Loaded(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage($"ItemsView.Loaded");
		if (chkLogItemsRepeaterEvents.IsChecked == true)
		{
			LogItemsRepeaterInfo();
		}
		if (chkLogScrollViewEvents.IsChecked == true)
		{
			LogScrollViewInfo();
		}
		LogItemsViewInfo();

		UpdateScrollViewFields();
		UpdateItemsRepeaterFields();

		HookItemsRepeaterEvents(ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView));
	}

	private void ItemsView_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
	{
		string selectedItems = String.Empty;

		foreach (var selectedItem in sender.SelectedItems)
		{
			string selectedItemAsString = selectedItem.ToString();

			selectedItems += (selectedItem == null) ? "<null>, " : selectedItemAsString.Substring(0, Math.Min(selectedItemAsString.Length, 9)) + ", ";
		}

		AppendAsyncEventMessage($"ItemsView.SelectionChanged SelectedItems={selectedItems}");
	}

	private void ItemsView_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		AppendAsyncEventMessage($"ItemsView.SizeChanged Size={_itemsView.ActualWidth}, {_itemsView.ActualHeight}");
		if (chkLogItemsRepeaterEvents.IsChecked == true)
		{
			LogItemsRepeaterInfo();
		}
		if (chkLogScrollViewEvents.IsChecked == true)
		{
			LogScrollViewInfo();
		}
		LogItemsViewInfo();
	}

	private void ItemsView_GettingFocus(UIElement sender, Windows.UI.Xaml.Input.GettingFocusEventArgs args)
	{
		FrameworkElement oldFE = args.OldFocusedElement as FrameworkElement;
		string oldFEName = (oldFE == null) ? "null" : oldFE.Name;
		FrameworkElement newFE = args.NewFocusedElement as FrameworkElement;
		string newFEName = (newFE == null) ? "null" : newFE.Name;

		AppendAsyncEventMessage($"ItemsView.GettingFocus FocusState={args.FocusState}, Direction={args.Direction}, InputDevice={args.InputDevice}, oldFE={oldFEName}, newFE={newFEName}");
	}

	private void ItemsView_LostFocus(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage("ItemsView.LostFocus");
	}

	private void ItemsView_LosingFocus(UIElement sender, Windows.UI.Xaml.Input.LosingFocusEventArgs args)
	{
		FrameworkElement oldFE = args.OldFocusedElement as FrameworkElement;
		string oldFEName = (oldFE == null) ? "null" : oldFE.Name;
		FrameworkElement newFE = args.NewFocusedElement as FrameworkElement;
		string newFEName = (newFE == null) ? "null" : newFE.Name;

		AppendAsyncEventMessage($"ItemsView.LosingFocus FocusState={args.FocusState}, Direction={args.Direction}, InputDevice={args.InputDevice}, oldFE={oldFEName}, newFE={newFEName}");
	}

	private void ItemsView_GotFocus(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage("ItemsView.GotFocus");
	}

	private void ItemsView_PointerWheelChanged(object sender, PointerRoutedEventArgs args)
	{
		AppendAsyncEventMessage($"ItemsView.PointerWheelChanged args.Handled={args.Handled}");
	}

	private void ItemsRepeater_LayoutUpdated(object sender, object e)
	{
		System.Diagnostics.Debug.WriteLine("ItemsRepeater.LayoutUpdated");
	}

	private void ItemsRepeater_Loaded(object sender, RoutedEventArgs e)
	{
		if (chkLogItemsRepeaterEvents.IsChecked == true)
		{
			AppendAsyncEventMessage($"ItemsRepeater.Loaded");
			LogItemsRepeaterInfo();
			if (chkLogItemsViewEvents.IsChecked == true)
			{
				LogItemsViewInfo();
			}
		}
	}

	private void ItemsRepeater_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		if (_itemsView != null)
		{
			if (canvasLinedFlowLayoutLockedItems != null)
			{
				canvasLinedFlowLayoutLockedItems.Width = _itemsView.ActualWidth;
				canvasLinedFlowLayoutLockedItems.Height = _itemsView.ActualHeight;
			}

			if (rectVerticalKeyboardNavigationReferenceOffset != null)
			{
				rectVerticalKeyboardNavigationReferenceOffset.Height = _itemsView.ActualHeight;
			}

			if (chkLogItemsRepeaterEvents.IsChecked == true)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					AppendAsyncEventMessage($"ItemsRepeater.SizeChanged Size={itemsRepeater.ActualWidth}, {itemsRepeater.ActualHeight}");
					/*
					LogItemsRepeaterInfo();
					if (chkLogItemsViewEvents.IsChecked == true)
					{
						LogItemsViewInfo();
					}
					*/
				}
			}
		}
	}

	private void ItemsRepeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
	{
		if (chkLogItemsRepeaterEvents.IsChecked == true)
		{
			AppendAsyncEventMessage($"ItemsRepeater.ElementPrepared Index={args.Index}, Element={args.Element}");
		}

		try
		{
			FrameworkElement elementAsFE = args.Element as FrameworkElement;

			if (elementAsFE != null)
			{
				Image image = elementAsFE.FindName("image") as Image;

				if (image != null)
				{
					image.ImageOpened += Image_ImageOpened;
					image.ImageFailed += Image_ImageFailed;
				}

				if ((bool)chkSetItemsRepeaterElementMinWidth.IsChecked && elementAsFE != null)
				{
					elementAsFE.MinWidth = double.Parse(txtItemsRepeaterElementMinWidthAll.Text);
				}

				if ((bool)chkSetItemsRepeaterElementMaxWidth.IsChecked && elementAsFE != null)
				{
					elementAsFE.MaxWidth = double.Parse(txtItemsRepeaterElementMaxWidthAll.Text);
				}

				if ((bool)chkSetItemsRepeaterElementMargin.IsChecked && elementAsFE != null)
				{
					elementAsFE.Margin = GetThicknessFromString(txtItemsRepeaterElementMarginAll.Text);
				}
			}

			if ((bool)chkSetItemsRepeaterElementCanDrag.IsChecked)
			{
				args.Element.CanDrag = (bool)chkItemsRepeaterElementCanDrag.IsChecked;

				args.Element.DragStarting += UIElement_DragStarting;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ItemsRepeater_ElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
	{
		if (chkLogItemsRepeaterEvents.IsChecked == true)
		{
			AppendAsyncEventMessage($"ItemsRepeater.ElementClearing Element={args.Element}");
		}

		FrameworkElement elementAsFE = args.Element as FrameworkElement;

		if (elementAsFE != null)
		{
			Image image = elementAsFE.FindName("image") as Image;

			if (image != null)
			{
				// Not using a _yellowBrush class member to avoid memory leaks through ItemCoontainer->ItemsViewSummaryPage references.
				SetCaptionColor(image, new SolidColorBrush(Colors.Yellow));

				image.ImageOpened -= Image_ImageOpened;
				image.ImageFailed -= Image_ImageFailed;
			}
		}

		args.Element.DragStarting -= UIElement_DragStarting;
	}

	private void ScrollView_BringingIntoView(ScrollView sender, ScrollingBringingIntoViewEventArgs args)
	{
		var requestEventArgs = args.RequestEventArgs;

		AppendAsyncEventMessage($"ScrollView.BringingIntoView RequestEventArgs AnimationDesired={requestEventArgs.AnimationDesired}, VerticalAlignmentRatio={requestEventArgs.VerticalAlignmentRatio}, VerticalOffset={requestEventArgs.VerticalOffset}, TargetRect={requestEventArgs.TargetRect}");
		AppendAsyncEventMessage($"ScrollView.BringingIntoView CorrelationId={args.CorrelationId}, TargetVerticalOffset={args.TargetVerticalOffset}");

		bool animationDesired = cmbBringIntoViewOptionsAnimationDesired.SelectedIndex == 0;

		args.RequestEventArgs.AnimationDesired = animationDesired;
	}

	private void ScrollView_Loaded(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage($"ScrollView.Loaded");
		LogScrollViewInfo();
		if (chkLogItemsViewEvents.IsChecked == true)
		{
			LogItemsViewInfo();
		}
	}

	private void ScrollView_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		AppendAsyncEventMessage($"ScrollView.SizeChanged Size={_itemsView.ActualWidth}, {_itemsView.ActualHeight}");
		LogScrollViewInfo();
		if (chkLogItemsViewEvents.IsChecked == true)
		{
			LogItemsViewInfo();
		}
	}

	private void ScrollView_ExtentChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage($"ScrollView.ExtentChanged ExtentWidth={sender.ExtentWidth}, ExtentHeight={sender.ExtentHeight}, ScrollableWidth={sender.ScrollableWidth}, ScrollableHeight={sender.ScrollableHeight}");
	}

	private void ScrollView_PointerWheelChanged(object sender, PointerRoutedEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.PointerWheelChanged args.Handled={args.Handled}");
	}

	private void ScrollView_StateChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage($"ScrollView.StateChanged {sender.State.ToString()}");
	}

	private void ScrollView_ViewChanged(ScrollView sender, object args)
	{
		AppendAsyncEventMessage($"ScrollView.ViewChanged HorizontalOffset={sender.HorizontalOffset.ToString()}, VerticalOffset={sender.VerticalOffset}, ZoomFactor={sender.ZoomFactor}");
	}

	private void ScrollView_ViewChangedForLockedItemsVisuals(ScrollView sender, object args)
	{
		UpdateLinedFlowLayoutLockedItemsVisuals();
	}

	private void ScrollView_ViewChangedForCurrentElementVisual(ScrollView sender, object args)
	{
		UpdateItemsViewCurrentElementVisual(log: false);
		UpdateItemsViewKeyboardNavigationReferenceOffsetVisual(log: false);
	}

	private void ScrollView_ScrollAnimationStarting(ScrollView sender, ScrollingScrollAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.ScrollAnimationStarting OffsetsChangeCorrelationId={args.CorrelationId}, SP=({args.StartPosition.X}, {args.StartPosition.Y}), EP=({args.EndPosition.X}, {args.EndPosition.Y})");
	}

	private void ScrollView_ZoomAnimationStarting(ScrollView sender, ScrollingZoomAnimationStartingEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.ZoomAnimationStarting ZoomFactorChangeCorrelationId={args.CorrelationId}, CenterPoint={args.CenterPoint}, SZF={args.StartZoomFactor}, EZF={args.EndZoomFactor}");
	}

	private void ScrollView_ScrollCompleted(ScrollView sender, ScrollingScrollCompletedEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.ScrollCompleted OffsetsChangeCorrelationId={args.CorrelationId}");
	}

	private void ScrollView_ZoomCompleted(ScrollView sender, ScrollingZoomCompletedEventArgs args)
	{
		AppendAsyncEventMessage($"ScrollView.ZoomCompleted ZoomFactorChangeCorrelationId={args.CorrelationId}");
	}

	private void Image_ImageOpened(object sender, RoutedEventArgs e)
	{
		try
		{
			Image image = sender as Image;

			// When the Image has a TextBlock sibling, set its Foreground to White to indicate the image loading success.
			SetCaptionColor(image, _whiteBrush);

			int elementIndex = GetItemsRepeaterElementIndex(image);

			if (elementIndex != -1)
			{
				int linedFlowLayoutLogItemIndexDbg = _linedFlowLayout == null ? -1 : Convert.ToInt32(txtLinedFlowLayoutLogItemIndexDbg.Text);

				if (chkLogImageEvents.IsChecked == true || elementIndex == linedFlowLayoutLogItemIndexDbg)
				{
					string captionText = GetCaptionText(image);

					if (captionText == null)
					{
						captionText = "<None>";
					}

					AppendAsyncEventMessage($"Image_ImageOpened elementIndex={elementIndex}, Item Id={captionText}");
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e)
	{
		// When the Image has a TextBlock sibling, set its Foreground to Red to indicate the image loading failure.
		string captionText = SetCaptionColor(sender as Image, _redBrush);

		if (!string.IsNullOrEmpty(captionText))
		{
			AppendAsyncEventMessage($"Image_ImageFailed ErrorMessage={e.ErrorMessage}, Item Id={captionText}");
		}
	}

	private void LinedFlowLayout_ItemsInfoRequested(LinedFlowLayout sender, LinedFlowLayoutItemsInfoRequestedEventArgs args)
	{
		if (chkLogLinedFlowLayoutEvents.IsChecked == true)
		{
			AppendAsyncEventMessage($"LinedFlowLayout.ItemsInfoRequested ItemsRangeStartIndex={args.ItemsRangeStartIndex}, ItemsRangeRequestedLength={args.ItemsRangeRequestedLength}");
		}

		if (!(bool)chkProvideDesiredAspectRatioItemsInfo.IsChecked)
		{
			return;
		}

		try
		{
			bool provideMinSizeItemsInfo = (bool)chkProvideMinSizeItemsInfo.IsChecked;
			bool provideMaxSizeItemsInfo = (bool)chkProvideMaxSizeItemsInfo.IsChecked;
			bool provideExtraDesiredAspectRatioItemsInfo = (bool)chkProvideExtraDesiredAspectRatioItemsInfo.IsChecked;
			bool provideTemporaryDesiredAspectRatioItemsInfo = (bool)chkProvideTemporaryDesiredAspectRatioItemsInfo.IsChecked;
			bool provideMinSizeArrayItemsInfo = (bool)chkProvideMinSizeArrayItemsInfo.IsChecked;
			bool provideMaxSizeArrayItemsInfo = (bool)chkProvideMaxSizeArrayItemsInfo.IsChecked;

			int arrayOffset = 0;
			int arrayLength = 0;
			double[] desiredAspectRatios = null;
			double[] minWidths = null;
			double[] maxWiths = null;

			if (provideExtraDesiredAspectRatioItemsInfo)
			{
				arrayOffset = args.ItemsRangeStartIndex = 0;

				if (_itemsView.ItemsSource == _lstRecipes)
				{
					arrayLength = _lstRecipes.Count;
				}
				else if (_itemsView.ItemsSource == _colSmallRecipes)
				{
					arrayLength = _colSmallRecipes.Count;
				}
				else if (_itemsView.ItemsSource == _colSmallUniformRecipes)
				{
					arrayLength = _colSmallUniformRecipes.Count;
				}
				else if (_itemsView.ItemsSource == _colRecipes)
				{
					arrayLength = _colRecipes.Count;
				}
			}
			else
			{
				arrayOffset = args.ItemsRangeStartIndex;
				arrayLength = args.ItemsRangeRequestedLength;
			}

			desiredAspectRatios = new double[arrayLength];

			if (provideMinSizeItemsInfo && provideMinSizeArrayItemsInfo)
			{
				minWidths = new double[arrayLength];
			}

			if (provideMaxSizeItemsInfo && provideMaxSizeArrayItemsInfo)
			{
				maxWiths = new double[arrayLength];
			}

			double minWidth = 0.0;
			int templateId = cmbItemTemplate.SelectedIndex;

			if (provideMinSizeItemsInfo)
			{
				if ((bool)chkSetItemsRepeaterElementMinWidth.IsChecked)
				{
					minWidth = double.Parse(txtItemsRepeaterElementMinWidthAll.Text);
				}
				else
				{
					// These values come from the the 'recipeTemplate1', 'recipeTemplate2', ..., 'recipeTemplate16' DataTemplates defined
					// in ItemsViewSummaryPage.xaml.   Their MinWidth values are reused here.
					switch (templateId)
					{
						case 16:
						case 17:
							minWidth = 32.0;
							break;
						case 2:
						case 4:
						case 8:
						case 9:
							minWidth = 40.0;
							break;
						case 1:
							minWidth = 72.0;
							break;
						case 6:
						case 11:
							minWidth = 96.0;
							break;
						case 5:
						case 10:
							minWidth = 100.0;
							break;
						case 7:
						case 12:
							minWidth = 126.0;
							break;
					}
				}
			}

			double maxWidth = -1.0;

			if (provideMaxSizeItemsInfo)
			{
				if ((bool)chkSetItemsRepeaterElementMaxWidth.IsChecked)
				{
					maxWidth = double.Parse(txtItemsRepeaterElementMaxWidthAll.Text);
				}
			}

			double temporaryAspectRatio = provideTemporaryDesiredAspectRatioItemsInfo ? double.Parse(txtTemporaryAspectRatio.Text) : 1.0;

			for (int index = 0; index < arrayLength; index++)
			{
				if (provideTemporaryDesiredAspectRatioItemsInfo && index % 10 == 0)
				{
					// For 10% of the items, pretend the aspect ratio is still unknown.
					desiredAspectRatios[index] = temporaryAspectRatio;
					// A subsequent call to LinedFlowLayout.InvalidateItemsInfo after chkProvideTemporaryDesiredAspectRatioItemsInfo.IsChecked was reset
					// can cause the full information to be provided.
				}
				else
				{
					// These values come from the the 'recipeTemplate1', 'recipeTemplate2', ..., 'recipeTemplate16' DataTemplates defined
					// in ItemsViewSummaryPage.xaml.   Some do not use the Recipe object, but a hard-coded item size that is reused here.
					// Others use the Recipe object which exposes its own AspectRatio property.
					switch (templateId)
					{
						case 5:
						case 6:
						case 11:
						case 13:
						case 14:
						case 15:
							{
								desiredAspectRatios[index] = 1.0;
								break;
							}
						case 7:
						case 12:
							{
								desiredAspectRatios[index] = 1.3125; // == 126/96
								break;
							}
						default:
							{
								Recipe recipe = null;

								if (_itemsView.ItemsSource == _lstRecipes)
								{
									recipe = _lstRecipes[arrayOffset + index];
								}
								else if (_itemsView.ItemsSource == _colSmallRecipes)
								{
									recipe = _colSmallRecipes[arrayOffset + index];
								}
								else if (_itemsView.ItemsSource == _colSmallUniformRecipes)
								{
									recipe = _colSmallUniformRecipes[arrayOffset + index];
								}
								else if (_itemsView.ItemsSource == _colRecipes)
								{
									recipe = _colRecipes[arrayOffset + index];
								}

								desiredAspectRatios[index] = recipe.AspectRatio;
								break;
							}
					}
				}

				if (provideMinSizeItemsInfo && provideMinSizeArrayItemsInfo)
				{
					minWidths[index] = minWidth;
				}

				if (provideMaxSizeItemsInfo && provideMaxSizeArrayItemsInfo)
				{
					maxWiths[index] = maxWidth;
				}
			}

			args.SetDesiredAspectRatios(desiredAspectRatios);

			if (provideMinSizeItemsInfo)
			{
				if (provideMinSizeArrayItemsInfo)
				{
					args.SetMinWidths(minWidths);
				}
				else
				{
					args.MinWidth = minWidth;
				}
			}

			if (provideMaxSizeItemsInfo)
			{
				if (provideMaxSizeArrayItemsInfo)
				{
					args.SetMaxWidths(maxWiths);
				}
				else
				{
					args.MaxWidth = maxWidth;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void LinedFlowLayout_ItemsUnlocked(LinedFlowLayout sender, object args)
	{
		if (chkLogLinedFlowLayoutEvents.IsChecked == true)
		{
			AppendAsyncEventMessage("LinedFlowLayout.ItemsUnlocked");
		}

		_lstLinedFlowLayoutLockedItemIndexes.Clear();
		UpdateLinedFlowLayoutLockedItemsVisuals();
	}

	private void UIElement_DragStarting(UIElement sender, DragStartingEventArgs args)
	{
		AppendAsyncEventMessage($"UIElement_DragStarting AllowedOperations={args.AllowedOperations}");
	}

	private void LogItemsRepeaterInfo()
	{
		if (_itemsView != null)
		{
			ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

			if (itemsRepeater != null)
			{
				AppendAsyncEventMessage($"ItemsRepeater Info: ItemsSource={itemsRepeater.ItemsSource}, ItemTemplate={itemsRepeater.ItemTemplate}, Layout={itemsRepeater.Layout}, ChildrenCount={VisualTreeHelper.GetChildrenCount(itemsRepeater)}");
			}
		}
	}

	private void LogScrollViewInfo()
	{
		if (_itemsView != null)
		{
			ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

			AppendAsyncEventMessage($"ScrollView Info: HorizontalOffset={scrollView.HorizontalOffset}, VerticalOffset={scrollView.VerticalOffset}, ZoomFactor={scrollView.ZoomFactor}");
			AppendAsyncEventMessage($"ScrollView Info: ViewportWidth={scrollView.ViewportWidth}, ExtentHeight={scrollView.ViewportHeight}");
			AppendAsyncEventMessage($"ScrollView Info: ExtentWidth={scrollView.ExtentWidth}, ExtentHeight={scrollView.ExtentHeight}");
			AppendAsyncEventMessage($"ScrollView Info: ScrollableWidth={scrollView.ScrollableWidth}, ScrollableHeight={scrollView.ScrollableHeight}");
		}
	}

	private void LogItemsViewInfo()
	{
		//AppendAsyncEventMessage($"ItemsView Info: ItemsSource={itemsView.ItemsSource}, ItemTemplate={itemsView.ItemTemplate}, Layout={itemsView.Layout}");
	}

	#region UI Controls Updates

	private string GetCaptionText(Image image)
	{
		if (image != null)
		{
			Panel itemPanel = image.Parent as Panel;

			if (itemPanel != null && itemPanel.Name == "itemPanel" && itemPanel.Children.Count >= 2)
			{
				TextBlock textBlock = itemPanel.Children[1] as TextBlock;

				if (textBlock != null)
				{
					return textBlock.Text;
				}
			}
		}

		return null;
	}

	private string SetCaptionColor(Image image, Brush brush)
	{
		if (image != null)
		{
			Panel itemPanel = image.Parent as Panel;

			if (itemPanel != null && itemPanel.Name == "itemPanel" && itemPanel.Children.Count >= 2)
			{
				TextBlock textBlock = itemPanel.Children[1] as TextBlock;

				if (textBlock != null)
				{
					textBlock.Foreground = brush;

					return textBlock.Text;
				}
			}
		}

		return null;
	}

	private void UpdateBackground()
	{
		if (_itemsView != null && cmbBackground != null)
		{
			SolidColorBrush bg = _itemsView.Background as SolidColorBrush;

			if (bg == null)
			{
				cmbBackground.SelectedIndex = 0;
			}
			else if (bg.Color == Colors.Transparent)
			{
				cmbBackground.SelectedIndex = 1;
			}
			else if (bg.Color == Colors.AliceBlue)
			{
				cmbBackground.SelectedIndex = 2;
			}
			else if (bg.Color == Colors.Aqua)
			{
				cmbBackground.SelectedIndex = 3;
			}
			else
			{
				cmbBackground.SelectedIndex = -1;
			}
		}
	}

	private void UpdateBorderBrush()
	{
		if (_itemsView != null && cmbBorderBrush != null)
		{
			SolidColorBrush bb = _itemsView.BorderBrush as SolidColorBrush;

			if (bb == null)
			{
				cmbBorderBrush.SelectedIndex = 0;
			}
			else if (bb.Color == Colors.Transparent)
			{
				cmbBorderBrush.SelectedIndex = 1;
			}
			else if (bb.Color == Colors.Blue)
			{
				cmbBorderBrush.SelectedIndex = 2;
			}
			else if (bb.Color == Colors.Green)
			{
				cmbBorderBrush.SelectedIndex = 3;
			}
			else
			{
				cmbBorderBrush.SelectedIndex = -1;
			}
		}
	}

	private void UpdateBorderThickness()
	{
		if (_itemsView != null && txtBorderThickness != null)
		{
			txtBorderThickness.Text = _itemsView.BorderThickness.ToString();
		}
	}

	private void UpdateContentOrientation()
	{
		try
		{
			if (_itemsView != null && cmbContentOrientation != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					cmbContentOrientation.SelectedIndex = (int)scrollView.ContentOrientation;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateCornerRadius()
	{
		if (_itemsView != null && txtCornerRadius != null)
		{
			txtCornerRadius.Text = _itemsView.CornerRadius.ToString();
		}
	}

	private void UpdateCurrentItemIndex()
	{
		if (_itemsView != null && txtCurrentItemIndex != null)
		{
			txtCurrentItemIndex.Text = _itemsView.CurrentItemIndex.ToString();
		}
	}

	private void UpdateDataSourceItemCount()
	{
		if (_itemsView != null && txtDataSourceItemCount != null)
		{
			if (_itemsView.ItemsSource == null)
			{
				txtDataSourceItemCount.Text = "0";
			}
			else if (_itemsView.ItemsSource == _lstRecipes)
			{
				txtDataSourceItemCount.Text = _lstRecipes.Count.ToString();
			}
			else if (_itemsView.ItemsSource == _colSmallRecipes)
			{
				txtDataSourceItemCount.Text = _colSmallRecipes.Count.ToString();
			}
			else if (_itemsView.ItemsSource == _colSmallUniformRecipes)
			{
				txtDataSourceItemCount.Text = _colSmallUniformRecipes.Count.ToString();
			}
			else if (_itemsView.ItemsSource == _colRecipes)
			{
				txtDataSourceItemCount.Text = _colRecipes.Count.ToString();
			}
			else if (_itemsView.ItemsSource == _colSmallItemContainers)
			{
				txtDataSourceItemCount.Text = _colSmallItemContainers.Count.ToString();
			}
			else if (_itemsView.ItemsSource == _colSmallImages)
			{
				txtDataSourceItemCount.Text = _colSmallImages.Count.ToString();
			}
		}
	}

	private void UpdateExtentHeight()
	{
		if (_itemsView != null && txtExtentHeight != null)
		{
			ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

			if (scrollView != null)
			{
				txtExtentHeight.Text = scrollView.ExtentHeight.ToString();
			}
		}
	}

	private void UpdateExtentWidth()
	{
		if (_itemsView != null && txtExtentWidth != null)
		{
			ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

			if (scrollView != null)
			{
				txtExtentWidth.Text = scrollView.ExtentWidth.ToString();
			}
		}
	}

	private void UpdateHeight()
	{
		if (_itemsView != null && txtHeight != null)
		{
			txtHeight.Text = _itemsView.Height.ToString();
		}
	}

	private void UpdateHorizontalAnchorRatio()
	{
		if (_itemsView != null && txtHorizontalAnchorRatio != null)
		{
			ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

			if (scrollView != null)
			{
				txtHorizontalAnchorRatio.Text = scrollView.HorizontalAnchorRatio.ToString();
			}
		}
	}

	private void UpdateHorizontalOffset()
	{
		if (_itemsView != null && txtHorizontalOffset != null)
		{
			ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

			if (scrollView != null)
			{
				txtHorizontalOffset.Text = scrollView.HorizontalOffset.ToString();
			}
		}
	}

	private void UpdateHorizontalScrollBarVisibility()
	{
		try
		{
			if (_itemsView != null && cmbHorizontalScrollBarVisibility != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					cmbHorizontalScrollBarVisibility.SelectedIndex = (int)scrollView.HorizontalScrollBarVisibility;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateHorizontalScrollMode()
	{
		try
		{
			if (_itemsView != null && cmbHorizontalScrollMode != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					cmbHorizontalScrollMode.SelectedIndex = (int)scrollView.HorizontalScrollMode;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateIsEnabled()
	{
		try
		{
			if (_itemsView != null && chkIsEnabled != null)
			{
				chkIsEnabled.IsChecked = _itemsView.IsEnabled;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateIsTabStop()
	{
		try
		{
			if (_itemsView != null && chkIsTabStop != null)
			{
				chkIsTabStop.IsChecked = _itemsView.IsTabStop;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterDefaultCacheLengths()
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					if (_itemsView.Layout == _flowLayout)
					{
						itemsRepeater.HorizontalCacheLength = 2.0;
						itemsRepeater.VerticalCacheLength = 2.0;
					}
					else if (_itemsView.Layout == _linedFlowLayout)
					{
						itemsRepeater.HorizontalCacheLength = 0.0;
						itemsRepeater.VerticalCacheLength = 2.0;
					}
					else if (_itemsView.Layout == _stackLayout)
					{
						if (_stackLayout.Orientation == Orientation.Horizontal)
						{
							itemsRepeater.HorizontalCacheLength = 0.0;
							itemsRepeater.VerticalCacheLength = 2.0;
						}
						else
						{
							itemsRepeater.HorizontalCacheLength = 2.0;
							itemsRepeater.VerticalCacheLength = 0.0;
						}
					}
					else if (_itemsView.Layout == _uniformGridLayout)
					{
						if (_uniformGridLayout.Orientation == Orientation.Horizontal)
						{
							itemsRepeater.HorizontalCacheLength = 0.0;
							itemsRepeater.VerticalCacheLength = 2.0;
						}
						else
						{
							itemsRepeater.HorizontalCacheLength = 2.0;
							itemsRepeater.VerticalCacheLength = 0.0;
						}
					}

					UpdateItemsRepeaterHorizontalCacheLength();
					UpdateItemsRepeaterVerticalCacheLength();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterChildrenCount()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterChildrenCount != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					txtItemsRepeaterChildrenCount.Text = VisualTreeHelper.GetChildrenCount(itemsRepeater).ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterHorizontalAlignment()
	{
		try
		{
			if (_itemsView != null && cmbItemsRepeaterHorizontalAlignment != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					cmbItemsRepeaterHorizontalAlignment.SelectedIndex = (int)itemsRepeater.HorizontalAlignment;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterHorizontalCacheLength()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterHorizontalCacheLength != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					txtItemsRepeaterHorizontalCacheLength.Text = itemsRepeater.HorizontalCacheLength.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterVerticalAlignment()
	{
		try
		{
			if (_itemsView != null && cmbItemsRepeaterVerticalAlignment != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					cmbItemsRepeaterVerticalAlignment.SelectedIndex = (int)itemsRepeater.VerticalAlignment;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterVerticalCacheLength()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterVerticalCacheLength != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					txtItemsRepeaterVerticalCacheLength.Text = itemsRepeater.VerticalCacheLength.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsViewIsItemInvokedEnabled()
	{
		if (_itemsView != null)
		{
			chkIsItemInvokedEnabled.IsChecked = _itemsView.IsItemInvokedEnabled;
		}
	}

	private void UpdateItemsViewLayout()
	{
		if (_itemsView != null)
		{
			if (_itemsView.Layout is StackLayout stackLayout)
			{
				_stackLayout = stackLayout;
				if (cmbLayout != null)
				{
					cmbLayout.SelectedIndex = 3;
				}
			}
			else
			{
				_stackLayout = Resources["stackLayout"] as StackLayout;
				if (_itemsView.Layout == null && cmbLayout != null)
				{
					cmbLayout.SelectedIndex = 0;
				}
			}
		}
	}

	private void UpdateItemsViewSelectionMode()
	{
		if (_itemsView != null && cmbSelectionMode != null)
		{
			cmbSelectionMode.SelectedIndex = (int)_itemsView.SelectionMode;
		}
	}

	private void UpdateMargin()
	{
		if (_itemsView != null && txtMargin != null)
		{
			txtMargin.Text = _itemsView.Margin.ToString();
		}
	}

	private void UpdateMaxHeight()
	{
		if (_itemsView != null && txtMaxHeight != null)
		{
			txtMaxHeight.Text = _itemsView.MaxHeight.ToString();
		}
	}

	private void UpdateMaxWidth()
	{
		if (_itemsView != null && txtMaxWidth != null)
		{
			txtMaxWidth.Text = _itemsView.MaxWidth.ToString();
		}
	}

	private void UpdatePadding()
	{
		if (_itemsView != null && txtPadding != null)
		{
			txtPadding.Text = _itemsView.Padding.ToString();
		}
	}

	private void UpdateLinedFlowLayoutActualLineHeight()
	{
		if (_linedFlowLayout != null)
		{
			txtLinedFlowLayoutActualLineHeight.Text = _linedFlowLayout.ActualLineHeight.ToString();
		}
	}

	private void UpdateLinedFlowLayoutSnappedAverageItemsPerLineDbg()
	{
		if (_linedFlowLayout != null)
		{
			//string snappedAverageItemsPerLineDbg = LayoutsTestHooks.GetLinedFlowLayoutSnappedAverageItemsPerLine(_linedFlowLayout).ToString();
			//txtLinedFlowLayoutSnappedAverageItemsPerLineDbg.Text = snappedAverageItemsPerLineDbg.Substring(0, Math.Min(snappedAverageItemsPerLineDbg.Length, 14));
		}
	}

	private void UpdateLinedFlowLayoutAverageItemAspectRatioDbg()
	{
		if (_linedFlowLayout != null)
		{
			//string averageItemAspectRatioDbg = LayoutsTestHooks.GetLinedFlowLayoutAverageItemAspectRatio(_linedFlowLayout).ToString();
			//txtLinedFlowLayoutAverageItemAspectRatioDbg.Text = averageItemAspectRatioDbg.Substring(0, Math.Min(averageItemAspectRatioDbg.Length, 14));
		}
	}

	private void UpdateLinedFlowLayoutForcedAverageItemAspectRatioDbg()
	{
		if (_linedFlowLayout != null)
		{
			//string forcedAverageItemAspectRatioDbg = LayoutsTestHooks.GetLinedFlowLayoutForcedAverageItemAspectRatio(_linedFlowLayout).ToString();
			//txtLinedFlowLayoutForcedAverageItemAspectRatioDbg.Text = forcedAverageItemAspectRatioDbg.Substring(0, Math.Min(forcedAverageItemAspectRatioDbg.Length, 14));
		}
	}

	private void UpdateLinedFlowLayoutForcedAverageItemsPerLineDividerDbg()
	{
		if (_linedFlowLayout != null)
		{
			//string forcedAverageItemsPerLineDividerDbg = LayoutsTestHooks.GetLinedFlowLayoutForcedAverageItemsPerLineDivider(_linedFlowLayout).ToString();
			//txtLinedFlowLayoutForcedAverageItemsPerLineDividerDbg.Text = forcedAverageItemsPerLineDividerDbg.Substring(0, Math.Min(forcedAverageItemsPerLineDividerDbg.Length, 14));
		}
	}

	private void UpdateLinedFlowLayoutForcedWrapMultiplierDbg()
	{
		if (_linedFlowLayout != null)
		{
			//string forcedWrapMultiplierDbg = LayoutsTestHooks.GetLinedFlowLayoutForcedWrapMultiplier(_linedFlowLayout).ToString();
			//txtLinedFlowLayoutForcedWrapMultiplierDbg.Text = forcedWrapMultiplierDbg.Substring(0, Math.Min(forcedWrapMultiplierDbg.Length, 14));
		}
	}

	private void UpdateLinedFlowLayoutFrozenItemIndexes()
	{
		if (_linedFlowLayout != null)
		{
			//txtLinedFlowLayoutFirstFrozenItemIndexDbg.Text = LayoutsTestHooks.GetLinedFlowLayoutFirstFrozenItemIndex(_linedFlowLayout).ToString();
			//txtLinedFlowLayoutLastFrozenItemIndexDbg.Text = LayoutsTestHooks.GetLinedFlowLayoutLastFrozenItemIndex(_linedFlowLayout).ToString();
		}
	}

	private void UpdateLinedFlowLayoutIsFastPathSupportedDbg()
	{
		if (_linedFlowLayout != null)
		{
			//chkLinedFlowLayoutIsFastPathSupportedDbg.IsChecked = LayoutsTestHooks.GetLinedFlowLayoutIsFastPathSupported(_linedFlowLayout);
		}
	}

	private void UpdateLinedFlowLayoutLineHeight()
	{
		if (_linedFlowLayout != null)
		{
			txtLinedFlowLayoutLineHeight.Text = _linedFlowLayout.LineHeight.ToString();
		}
	}

	private void UpdateLinedFlowLayoutLineSpacing()
	{
		if (_linedFlowLayout != null)
		{
			txtLinedFlowLayoutLineSpacing.Text = _linedFlowLayout.LineSpacing.ToString();
		}
	}

	private void UpdateLinedFlowLayoutRequestedRangeLength()
	{
		if (_linedFlowLayout != null)
		{
			txtLinedFlowLayoutRequestedRangeLength.Text = _linedFlowLayout.RequestedRangeLength.ToString();
		}
	}

	private void UpdateLinedFlowLayoutRequestedRangeStartIndex()
	{
		if (_linedFlowLayout != null)
		{
			txtLinedFlowLayoutRequestedRangeStartIndex.Text = _linedFlowLayout.RequestedRangeStartIndex.ToString();
		}
	}

	private void UpdateItemsViewCurrentElementVisual(bool log)
	{
		int currentItemIndex = _itemsView.CurrentItemIndex;

		if (log)
		{
			AppendAsyncEventMessage($"ItemsView.CurrentItemIndex={currentItemIndex}");
		}

		if (bdrCurrentElement != null)
		{
			UIElement currentElement = null;
			ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

			if (itemsRepeater != null)
			{
				currentElement = itemsRepeater.TryGetElement(currentItemIndex);
			}

			ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

			if (currentElement == null || scrollView == null || (bool)chkShowCurrentElement.IsChecked == false)
			{
				bdrCurrentElement.Visibility = Visibility.Collapsed;
			}
			else
			{
				bdrCurrentElement.Visibility = Visibility.Visible;
				bdrCurrentElement.Width = currentElement.ActualSize.X * scrollView.ZoomFactor - bdrCurrentElement.BorderThickness.Left - bdrCurrentElement.BorderThickness.Right - 2;
				bdrCurrentElement.Height = currentElement.ActualSize.Y * scrollView.ZoomFactor - bdrCurrentElement.BorderThickness.Top - bdrCurrentElement.BorderThickness.Bottom - 2;

				double currentElementOffsetX = currentElement.ActualOffset.X * scrollView.ZoomFactor + bdrCurrentElement.BorderThickness.Left + 2;
				double currentElementOffsetY = currentElement.ActualOffset.Y * scrollView.ZoomFactor + bdrCurrentElement.BorderThickness.Top + 2;

				currentElementOffsetX -= scrollView.HorizontalOffset;
				currentElementOffsetY -= scrollView.VerticalOffset;

				double horizontalExtent = scrollView.ExtentWidth * scrollView.ZoomFactor;

				if (horizontalExtent < scrollView.ViewportWidth)
				{
					if (itemsRepeater.HorizontalAlignment == HorizontalAlignment.Center || itemsRepeater.HorizontalAlignment == HorizontalAlignment.Stretch)
					{
						currentElementOffsetX += (scrollView.ViewportWidth - horizontalExtent) / 2.0;
					}
					else if (itemsRepeater.HorizontalAlignment == HorizontalAlignment.Right)
					{
						currentElementOffsetX += scrollView.ViewportWidth - horizontalExtent;
					}
				}

				double verticalExtent = scrollView.ExtentHeight * scrollView.ZoomFactor;

				if (verticalExtent < scrollView.ViewportHeight)
				{
					if (itemsRepeater.VerticalAlignment == VerticalAlignment.Center || itemsRepeater.VerticalAlignment == VerticalAlignment.Stretch)
					{
						currentElementOffsetX += (scrollView.ViewportHeight - verticalExtent) / 2.0;
					}
					else if (itemsRepeater.VerticalAlignment == VerticalAlignment.Bottom)
					{
						currentElementOffsetX += scrollView.ViewportHeight - verticalExtent;
					}
				}

				bdrCurrentElement.Margin = new Thickness(currentElementOffsetX, currentElementOffsetY, 0, 0);
			}
		}
	}

	private void UpdateItemsViewKeyboardNavigationReferenceOffsetVisual(bool log)
	{
		Point keyboardNavigationReferenceOffset = ItemsViewTestHooks.GetKeyboardNavigationReferenceOffset(_itemsView);

		if (log)
		{
			AppendAsyncEventMessage($"ItemsView.KeyboardNavigationReferenceOffset={keyboardNavigationReferenceOffset}");
		}

		IndexBasedLayoutOrientation indexBasedLayoutOrientation = _itemsView.Layout.IndexBasedLayoutOrientation;

		if (rectVerticalKeyboardNavigationReferenceOffset != null)
		{
			if (indexBasedLayoutOrientation != IndexBasedLayoutOrientation.TopToBottom)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if ((bool)chkShowKeyboardNavigationReferenceOffset.IsChecked && scrollView != null)
				{
					double offset = keyboardNavigationReferenceOffset.X * scrollView.ZoomFactor - scrollView.HorizontalOffset - 2.0f;

					ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

					if (itemsRepeater != null)
					{
						double horizontalExtent = scrollView.ExtentWidth * scrollView.ZoomFactor;

						if (horizontalExtent < scrollView.ViewportWidth)
						{
							if (itemsRepeater.HorizontalAlignment == HorizontalAlignment.Center || itemsRepeater.HorizontalAlignment == HorizontalAlignment.Stretch)
							{
								offset += (scrollView.ViewportWidth - horizontalExtent) / 2.0;
							}
							else if (itemsRepeater.HorizontalAlignment == HorizontalAlignment.Right)
							{
								offset += scrollView.ViewportWidth - horizontalExtent;
							}
						}
					}

					rectVerticalKeyboardNavigationReferenceOffset.Margin = new Thickness(offset, 0, 0, 0);
					rectVerticalKeyboardNavigationReferenceOffset.Visibility = Visibility.Visible;
				}
				else
				{
					rectVerticalKeyboardNavigationReferenceOffset.Visibility = Visibility.Collapsed;
				}
			}
			else
			{
				rectVerticalKeyboardNavigationReferenceOffset.Visibility = Visibility.Collapsed;
			}
		}

		if (rectHorizontalKeyboardNavigationReferenceOffset != null)
		{
			if (indexBasedLayoutOrientation != IndexBasedLayoutOrientation.LeftToRight)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if ((bool)chkShowKeyboardNavigationReferenceOffset.IsChecked && scrollView != null)
				{
					double offset = keyboardNavigationReferenceOffset.Y * scrollView.ZoomFactor - scrollView.VerticalOffset - 2.0f;

					ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

					if (itemsRepeater != null)
					{
						double verticalExtent = scrollView.ExtentHeight * scrollView.ZoomFactor;

						if (verticalExtent < scrollView.ViewportHeight)
						{
							if (itemsRepeater.VerticalAlignment == VerticalAlignment.Center || itemsRepeater.VerticalAlignment == VerticalAlignment.Stretch)
							{
								offset += (scrollView.ViewportHeight - verticalExtent) / 2.0;
							}
							else if (itemsRepeater.VerticalAlignment == VerticalAlignment.Bottom)
							{
								offset += scrollView.ViewportHeight - verticalExtent;
							}
						}
					}

					rectHorizontalKeyboardNavigationReferenceOffset.Margin = new Thickness(0, offset, 0, 0);
					rectHorizontalKeyboardNavigationReferenceOffset.Visibility = Visibility.Visible;
				}
				else
				{
					rectHorizontalKeyboardNavigationReferenceOffset.Visibility = Visibility.Collapsed;
				}
			}
			else
			{
				rectHorizontalKeyboardNavigationReferenceOffset.Visibility = Visibility.Collapsed;
			}
		}
	}

	private void UpdateLinedFlowLayoutLockedItemsVisuals()
	{
		if (canvasLinedFlowLayoutLockedItems != null)
		{
			bool showLockedItems = (bool)chkLinedFlowLayoutShowLockedItems.IsChecked;

			canvasLinedFlowLayoutLockedItems.Visibility = showLockedItems ? Visibility.Visible : Visibility.Collapsed;
			canvasLinedFlowLayoutLockedItems.Children.Clear();

			if (showLockedItems && _lstLinedFlowLayoutLockedItemIndexes.Count > 0)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				foreach (int lockedItemIndex in _lstLinedFlowLayoutLockedItemIndexes)
				{
					UIElement lockedItem = GetItemsRepeaterElement(itemsRepeater, lockedItemIndex);
					if (lockedItem != null)
					{
						GeneralTransform gt = lockedItem.TransformToVisual(itemsRepeater);
						Point lockedItemOriginPoint = new Point();
						lockedItemOriginPoint = gt.TransformPoint(lockedItemOriginPoint);

						if (lockedItemOriginPoint.X - scrollView.HorizontalOffset >= 0 && lockedItemOriginPoint.Y - scrollView.VerticalOffset >= 0 &&
							lockedItemOriginPoint.X - scrollView.HorizontalOffset + 8 <= canvasLinedFlowLayoutLockedItems.Width &&
							lockedItemOriginPoint.Y - scrollView.VerticalOffset + 8 <= canvasLinedFlowLayoutLockedItems.Height)
						{
							Windows.UI.Xaml.Shapes.Rectangle lockedItemRectangle = new Windows.UI.Xaml.Shapes.Rectangle()
							{
								Fill = new SolidColorBrush(Colors.Red),
								Width = 8,
								Height = 8
							};

							canvasLinedFlowLayoutLockedItems.Children.Add(lockedItemRectangle);
							Canvas.SetLeft(lockedItemRectangle, lockedItemOriginPoint.X - scrollView.HorizontalOffset);
							Canvas.SetTop(lockedItemRectangle, lockedItemOriginPoint.Y - scrollView.VerticalOffset);
						}
					}
				}
			}
		}
	}

	private void UpdateLinedFlowLayoutLogItemIndexDbg()
	{
		if (_linedFlowLayout != null)
		{
			//txtLinedFlowLayoutLogItemIndexDbg.Text = LayoutsTestHooks.GetLinedFlowLayoutLogItemIndex(_linedFlowLayout).ToString();
		}
	}

	private void UpdateLinedFlowLayoutItemsJustification()
	{
		if (_linedFlowLayout != null)
		{
			cmbLinedFlowLayoutItemsJustification.SelectedIndex = (int)_linedFlowLayout.ItemsJustification;
		}
	}

	private void UpdateLinedFlowLayoutItemsStretch()
	{
		if (_linedFlowLayout != null)
		{
			cmbLinedFlowLayoutItemsStretch.SelectedIndex = (int)_linedFlowLayout.ItemsStretch;
		}
	}

	private void UpdateLinedFlowLayoutMinItemSpacing()
	{
		if (_linedFlowLayout != null)
		{
			txtLinedFlowLayoutMinItemSpacing.Text = _linedFlowLayout.MinItemSpacing.ToString();
		}
	}

	private void UpdateLinedFlowLayoutRealizedItemIndexes()
	{
		if (_linedFlowLayout != null)
		{
			//txtLinedFlowLayoutFirstRealizedItemIndexDbg.Text = LayoutsTestHooks.GetLayoutFirstRealizedItemIndex(_linedFlowLayout).ToString();
			//txtLinedFlowLayoutLastRealizedItemIndexDbg.Text = LayoutsTestHooks.GetLayoutLastRealizedItemIndex(_linedFlowLayout).ToString();
		}
	}

	private void UpdateScrollViewDefaultAnchorRatios()
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = _itemsView.GetValue(ItemsView.ScrollViewProperty) as ScrollView;

				if (scrollView != null)
				{
					if (_itemsView.Layout == _flowLayout)
					{
						scrollView.HorizontalAnchorRatio = 0.0;
						scrollView.VerticalAnchorRatio = 0.0;
					}
					else if (_itemsView.Layout == _linedFlowLayout)
					{
						scrollView.HorizontalAnchorRatio = double.NaN;
						scrollView.VerticalAnchorRatio = double.NaN;
					}
					else if (_itemsView.Layout == _stackLayout)
					{
						if (_stackLayout.Orientation == Orientation.Horizontal)
						{
							scrollView.HorizontalAnchorRatio = double.NaN;
							scrollView.VerticalAnchorRatio = 0.0;
						}
						else
						{
							scrollView.HorizontalAnchorRatio = 0.0;
							scrollView.VerticalAnchorRatio = double.NaN;
						}
					}
					else if (_itemsView.Layout == _uniformGridLayout)
					{
						if (_uniformGridLayout.Orientation == Orientation.Horizontal)
						{
							scrollView.HorizontalAnchorRatio = double.NaN;
							scrollView.VerticalAnchorRatio = 0.0;
						}
						else
						{
							scrollView.HorizontalAnchorRatio = 0.0;
							scrollView.VerticalAnchorRatio = double.NaN;
						}
					}

					UpdateHorizontalAnchorRatio();
					UpdateVerticalAnchorRatio();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateSelectedItem()
	{
		if (txtSelectedItem != null)
		{
			if (_itemsView == null || _itemsView.SelectedItem == null)
			{
				txtSelectedItem.Text = "<null>";
			}
			else
			{
				string selectedItemAsString = _itemsView.SelectedItem.ToString();

				txtSelectedItem.Text = selectedItemAsString.Substring(0, Math.Min(selectedItemAsString.Length, 12));
			}
		}
	}

	private void UpdateSelectedItems()
	{
		if (txtSelectedItems != null)
		{
			if (_itemsView == null)
			{
				txtSelectedItems.Text = "<null>";
			}
			else
			{
				txtSelectedItems.Text = String.Empty;

				foreach (var selectedItem in _itemsView.SelectedItems)
				{
					string selectedItemAsString = selectedItem.ToString();

					txtSelectedItems.Text += (selectedItem == null) ? "<null>, " : selectedItemAsString.Substring(0, Math.Min(selectedItemAsString.Length, 9)) + ", ";
				}
			}
		}
	}

	private void UpdateStackLayoutOrientation()
	{
		if (_stackLayout != null && cmbStackLayoutOrientation != null)
		{
			cmbStackLayoutOrientation.SelectedIndex = _stackLayout.Orientation == Orientation.Horizontal ? 0 : 1;
		}
	}

	private void UpdateStackLayoutSpacing()
	{
		if (_stackLayout != null && txtStackLayoutSpacing != null)
		{
			txtStackLayoutSpacing.Text = _stackLayout.Spacing.ToString();
		}
	}

	private void UpdateTabNavigation()
	{
		try
		{
			if (_itemsView != null && cmbTabNavigation != null)
			{
				cmbTabNavigation.SelectedIndex = (int)_itemsView.TabNavigation;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateUniformGridLayoutMaximumRowsOrColumns()
	{
		if (_uniformGridLayout != null)
		{
			txtUniformGridLayoutMaximumRowsOrColumns.Text = _uniformGridLayout.MaximumRowsOrColumns.ToString();
		}
	}

	private void UpdateUniformGridLayoutMinColumnSpacing()
	{
		if (_uniformGridLayout != null)
		{
			txtUniformGridLayoutMinColumnSpacing.Text = _uniformGridLayout.MinColumnSpacing.ToString();
		}
	}

	private void UpdateUniformGridLayoutMinRowSpacing()
	{
		if (_uniformGridLayout != null)
		{
			txtUniformGridLayoutMinRowSpacing.Text = _uniformGridLayout.MinRowSpacing.ToString();
		}
	}

	private void UpdateUniformGridLayoutOrientation()
	{
		if (_uniformGridLayout != null)
		{
			cmbUniformGridLayoutOrientation.SelectedIndex = (int)_uniformGridLayout.Orientation;
		}
	}

	private void UpdateVerticalAnchorRatio()
	{
		if (_itemsView != null && txtVerticalAnchorRatio != null)
		{
			ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

			if (scrollView != null)
			{
				txtVerticalAnchorRatio.Text = scrollView.VerticalAnchorRatio.ToString();
			}
		}
	}

	private void UpdateVerticalOffset()
	{
		if (_itemsView != null && txtVerticalOffset != null)
		{
			ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

			if (scrollView != null)
			{
				txtVerticalOffset.Text = scrollView.VerticalOffset.ToString();
			}
		}
	}

	private void UpdateVerticalScrollBarVisibility()
	{
		try
		{
			if (_itemsView != null && cmbVerticalScrollBarVisibility != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					cmbVerticalScrollBarVisibility.SelectedIndex = (int)scrollView.VerticalScrollBarVisibility;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateVerticalScrollMode()
	{
		try
		{
			if (_itemsView != null && cmbVerticalScrollMode != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					cmbVerticalScrollMode.SelectedIndex = (int)scrollView.VerticalScrollMode;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateWidth()
	{
		if (_itemsView != null && txtWidth != null)
		{
			txtWidth.Text = _itemsView.Width.ToString();
		}
	}

	private void UpdateXYFocusKeyboardNavigation()
	{
		try
		{
			if (_itemsView != null && cmbXYFocusKeyboardNavigation != null)
			{
				cmbXYFocusKeyboardNavigation.SelectedIndex = (int)_itemsView.XYFocusKeyboardNavigation;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateZoomFactor()
	{
		try
		{
			if (_itemsView != null && txtZoomFactor != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					txtZoomFactor.Text = scrollView.ZoomFactor.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateZoomMode()
	{
		try
		{
			if (_itemsView != null && cmbZoomMode != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					cmbZoomMode.SelectedIndex = (int)scrollView.ZoomMode;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementActualHeight()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementActualHeight != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					txtItemsRepeaterElementActualHeight.Text = frameworkElement.ActualHeight.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementActualWidth()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementActualWidth != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					txtItemsRepeaterElementActualWidth.Text = frameworkElement.ActualWidth.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementIsEnabled()
	{
		try
		{
			if (_itemsView != null && chkItemsRepeaterElementIsEnabled != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is Control control)
				{
					chkItemsRepeaterElementIsEnabled.IsChecked = control != null && control.IsEnabled;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementMinWidth()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementMinWidth != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					txtItemsRepeaterElementMinWidth.Text = frameworkElement.MinWidth.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementMaxWidth()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementMaxWidth != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					txtItemsRepeaterElementMaxWidth.Text = frameworkElement.MaxWidth.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementWidth()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementWidth != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					txtItemsRepeaterElementWidth.Text = frameworkElement.Width.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementMinHeight()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementMinHeight != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					txtItemsRepeaterElementMinHeight.Text = frameworkElement.MinHeight.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementMaxHeight()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementMaxHeight != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					txtItemsRepeaterElementMaxHeight.Text = frameworkElement.MaxHeight.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementHeight()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementHeight != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					txtItemsRepeaterElementHeight.Text = frameworkElement.Height.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementDesiredSize()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementDesiredSize != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element != null)
				{
					txtItemsRepeaterElementDesiredSize.Text = element.DesiredSize.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterElementRenderSize()
	{
		try
		{
			if (_itemsView != null && txtItemsRepeaterElementRenderSize != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element != null)
				{
					txtItemsRepeaterElementRenderSize.Text = element.RenderSize.ToString();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterItemContainerCanUserSelect()
	{
		try
		{
			if (_itemsView != null && chkItemsRepeaterItemContainerCanUserSelect != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is ItemContainer itemContainer)
				{
#if MUX_PRERELEASE
						if ((int)(itemContainer.CanUserSelect & ItemContainerUserSelectMode.UserCannotSelect) == (int)ItemContainerUserSelectMode.UserCannotSelect)
						{
							chkItemsRepeaterItemContainerCanUserSelect.IsChecked = false;
						}
						else if ((int)(itemContainer.CanUserSelect & ItemContainerUserSelectMode.UserCanSelect) == (int)ItemContainerUserSelectMode.UserCanSelect)
						{
							chkItemsRepeaterItemContainerCanUserSelect.IsChecked = true;
						}
						else
						{
							chkItemsRepeaterItemContainerCanUserSelect.IsChecked = null;
						}
#endif
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterItemContainerIsSelected()
	{
		try
		{
			if (_itemsView != null && chkItemsRepeaterItemContainerIsSelected != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is ItemContainer itemContainer)
				{
					chkItemsRepeaterItemContainerIsSelected.IsChecked = itemContainer != null && itemContainer.IsSelected;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterItemContainerCanUserInvoke()
	{
		try
		{
			if (_itemsView != null && chkItemsRepeaterItemContainerCanUserInvoke != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is ItemContainer itemContainer)
				{
#if MUX_PRERELEASE
						if ((int)(itemContainer.CanUserInvoke & ItemContainerUserInvokeMode.UserCannotInvoke) == (int)ItemContainerUserInvokeMode.UserCannotInvoke)
						{
							chkItemsRepeaterItemContainerCanUserInvoke.IsChecked = false;
						}
						else if ((int)(itemContainer.CanUserInvoke & ItemContainerUserInvokeMode.UserCanInvoke) == (int)ItemContainerUserInvokeMode.UserCanInvoke)
						{
							chkItemsRepeaterItemContainerCanUserInvoke.IsChecked = true;
						}
						else
						{
							chkItemsRepeaterItemContainerCanUserInvoke.IsChecked = null;
						}
#endif
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void UpdateItemsRepeaterLogItemIndexDbg()
	{
		if (txtItemsRepeaterLogItemIndexDbg != null)
		{
			//txtItemsRepeaterLogItemIndexDbg.Text = RepeaterTestHooks.GetLogItemIndex().ToString();
		}
	}

	#endregion

	#region Property Getters

	private void BtnGetBorderThickness_Click(object sender, RoutedEventArgs e)
	{
		UpdateBorderThickness();
	}

	private void BtnGetContentOrientation_Click(object sender, RoutedEventArgs e)
	{
		UpdateContentOrientation();
	}

	private void BtnGetCornerRadius_Click(object sender, RoutedEventArgs e)
	{
		UpdateCornerRadius();
	}

	private void BtnGetCurrentItemIndex_Click(object sender, RoutedEventArgs e)
	{
		UpdateCurrentItemIndex();
	}

	private void BtnGetExtentHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateExtentHeight();
	}

	private void BtnGetExtentWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateExtentWidth();
	}

	private void BtnGetHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateHeight();
	}

	private void BtnGetHorizontalAnchorRatio_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalAnchorRatio();
	}

	private void BtnGetHorizontalOffset_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalOffset();
	}

	private void BtnGetHorizontalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalScrollMode();
	}

	private void BtnGetHorizontalScrollBarVisibility_Click(object sender, RoutedEventArgs e)
	{
		UpdateHorizontalScrollBarVisibility();
	}

	private void BtnGetItemsRepeaterChildrenCount_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterChildrenCount();
	}

	private void BtnGetItemsRepeaterHorizontalAlignment_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterHorizontalAlignment();
	}

	private void BtnGetItemsRepeaterHorizontalCacheLength_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterHorizontalCacheLength();
	}

	private void BtnGetItemsRepeaterLogItemIndexDbg_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterLogItemIndexDbg();
	}

	private void BtnGetItemsRepeaterVerticalAlignment_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterVerticalAlignment();
	}

	private void BtnGetItemsRepeaterVerticalCacheLength_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterVerticalCacheLength();
	}

	private void BtnGetLayoutForcedIndexBasedLayoutOrientation_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				Layout layout = _itemsView.Layout;

				if (layout != null)
				{
					//cmbLayoutForcedIndexBasedLayoutOrientation.SelectedIndex = (int)LayoutsTestHooks.GetLayoutForcedIndexBasedLayoutOrientation(layout);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnGetMargin_Click(object sender, RoutedEventArgs e)
	{
		UpdateMargin();
	}

	private void BtnGetMaxHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateMaxHeight();
	}

	private void BtnGetMaxWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateMaxWidth();
	}

	private void BtnGetPadding_Click(object sender, RoutedEventArgs e)
	{
		UpdatePadding();
	}

	private void BtnGetSelectedItem_Click(object sender, RoutedEventArgs e)
	{
		UpdateSelectedItem();
	}

	private void BtnGetSelectedItems_Click(object sender, RoutedEventArgs e)
	{
		UpdateSelectedItems();
	}

	private void BtnGetStackLayoutOrientation_Click(object sender, RoutedEventArgs e)
	{
		UpdateStackLayoutOrientation();
	}

	private void BtnGetStackLayoutSpacing_Click(object sender, RoutedEventArgs e)
	{
		UpdateStackLayoutSpacing();
	}

	private void BtnGetLinedFlowLayoutActualLineHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutActualLineHeight();
	}

	private void BtnGetLinedFlowLayoutSnappedAverageItemsPerLineDbg_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutSnappedAverageItemsPerLineDbg();
	}

	private void BtnGetLinedFlowLayoutAverageItemAspectRatioDbg_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutAverageItemAspectRatioDbg();
	}

	private void BtnGetLinedFlowLayoutForcedAverageItemAspectRatioDbg_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutForcedAverageItemAspectRatioDbg();
	}

	private void BtnGetLinedFlowLayoutForcedAverageItemsPerLineDividerDbg_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutForcedAverageItemsPerLineDividerDbg();
	}

	private void BtnGetLinedFlowLayoutForcedWrapMultiplierDbg_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutForcedWrapMultiplierDbg();
	}

	private void BtnGetLinedFlowLayoutFrozenItemIndexesDbg_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutFrozenItemIndexes();
	}

	private void BtnGetLinedFlowLayoutLineHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutLineHeight();
	}

	private void BtnGetLinedFlowLayoutLineSpacing_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutLineSpacing();
	}

	private void BtnGetLinedFlowLayoutLogItemIndexDbg_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutLogItemIndexDbg();
	}

	private void BtnGetLinedFlowLayoutMinItemSpacing_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutMinItemSpacing();
	}

	private void BtnGetLinedFlowLayoutRealizedItemIndexesDbg_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutRealizedItemIndexes();
	}

	private void BtnGetLinedFlowLayoutRequestedRangeLength_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutRequestedRangeLength();
	}

	private void BtnGetLinedFlowLayoutRequestedRangeStartIndex_Click(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutRequestedRangeStartIndex();
	}

	private void BtnGetUniformGridLayoutMaximumRowsOrColumns_Click(object sender, RoutedEventArgs e)
	{
		UpdateUniformGridLayoutMaximumRowsOrColumns();
	}

	private void BtnGetUniformGridLayoutMinColumnSpacing_Click(object sender, RoutedEventArgs e)
	{
		UpdateUniformGridLayoutMinColumnSpacing();
	}

	private void BtnGetUniformGridLayoutMinRowSpacing_Click(object sender, RoutedEventArgs e)
	{
		UpdateUniformGridLayoutMinRowSpacing();
	}

	private void BtnGetUniformGridLayoutOrientation_Click(object sender, RoutedEventArgs e)
	{
		UpdateUniformGridLayoutOrientation();
	}

	private void BtnGetVerticalAnchorRatio_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalAnchorRatio();
	}

	private void BtnGetVerticalOffset_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalOffset();
	}

	private void BtnGetVerticalScrollBarVisibility_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalScrollBarVisibility();
	}

	private void BtnGetVerticalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateVerticalScrollMode();
	}

	private void BtnGetWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateWidth();
	}

	private void BtnGetZoomFactor_Click(object sender, RoutedEventArgs e)
	{
		UpdateZoomFactor();
	}

	private void BtnGetZoomMode_Click(object sender, RoutedEventArgs e)
	{
		UpdateZoomMode();
	}

	private void BtnGetItemsRepeaterElementActualHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementActualHeight();
	}

	private void BtnGetItemsRepeaterElementActualWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementActualWidth();
	}

	private void BtnGetItemsRepeaterElementIsEnabled_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementIsEnabled();
	}

	private void BtnGetItemsRepeaterElementMinWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementMinWidth();
	}

	private void BtnGetItemsRepeaterElementMaxWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementMaxWidth();
	}

	private void BtnGetItemsRepeaterElementDesiredSize_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementDesiredSize();
	}

	private void BtnGetItemsRepeaterElementRenderSize_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementRenderSize();
	}

	private void BtnGetItemsRepeaterElementWidth_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementWidth();
	}

	private void BtnGetItemsRepeaterElementMinHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementMinHeight();
	}

	private void BtnGetItemsRepeaterElementMaxHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementMaxHeight();
	}

	private void BtnGetItemsRepeaterElementHeight_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterElementHeight();
	}

	private void BtnItemsRepeaterElementInvalidateMeasure_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterMethodElement(itemsRepeater);
				if (element != null)
				{
					element.InvalidateMeasure();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnItemsRepeaterElementInvalidateArrange_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterMethodElement(itemsRepeater);
				if (element != null)
				{
					element.InvalidateArrange();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnGetItemsRepeaterItemContainerCanUserInvoke_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterItemContainerCanUserInvoke();
	}

	private void BtnGetItemsRepeaterItemContainerCanUserSelect_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterItemContainerCanUserSelect();
	}

	private void BtnGetItemsRepeaterItemContainerIsSelected_Click(object sender, RoutedEventArgs e)
	{
		UpdateItemsRepeaterItemContainerIsSelected();
	}

	private void BtnItemsViewTryGetItemIndex_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				double horizontalViewportRatio = double.Parse(txtTryGetItemIndexHorizontalViewportRatio.Text);
				double verticalViewportRatio = double.Parse(txtTryGetItemIndexVerticalViewportRatio.Text);
				int index;
				_itemsView.TryGetItemIndex(horizontalViewportRatio, verticalViewportRatio, out index);

				txtItemsViewTryGetItemIndex.Text = index.ToString();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnItemsViewStartBringItemIntoView_Click(object sender, RoutedEventArgs e)
	{
		ItemsViewStartBringItemIntoView();
	}

	private void ItemsViewStartBringItemIntoView()
	{
		try
		{
			if (_itemsView != null)
			{
				int index = -1;

				int.TryParse(txtItemsViewMethodIndex.Text, out index);

				BringIntoViewOptions bringIntoViewOptions = new BringIntoViewOptions();

				bool startBringItemIntoViewSuccess = false;
				bool animationDesired = cmbBringIntoViewOptionsAnimationDesired.SelectedIndex == 0;
				double verticalAlignmentRatio = Convert.ToDouble(txtBringIntoViewOptionsVerticalAlignmentRatio.Text);
				double verticalOffset = Convert.ToDouble(txtBringIntoViewOptionsVerticalOffset.Text);

				if (!animationDesired ||
					double.IsNaN(verticalAlignmentRatio) != double.IsNaN(bringIntoViewOptions.VerticalAlignmentRatio) ||
					(!double.IsNaN(verticalAlignmentRatio) && verticalAlignmentRatio != bringIntoViewOptions.VerticalAlignmentRatio) ||
					verticalOffset != bringIntoViewOptions.VerticalOffset ||
					!string.IsNullOrWhiteSpace(txtBringIntoViewOptionsTargetRect.Text))
				{
					if (!string.IsNullOrWhiteSpace(txtBringIntoViewOptionsTargetRect.Text))
					{
						string[] rectComponents = txtBringIntoViewOptionsTargetRect.Text.Split(',');

						if (rectComponents.Length != 4)
						{
							AppendEventMessage("Invalid TargetRect format.");
							return;
						}
						bringIntoViewOptions.TargetRect = new Rect(double.Parse(rectComponents[0]), double.Parse(rectComponents[1]), double.Parse(rectComponents[2]), double.Parse(rectComponents[3]));
					}

					bringIntoViewOptions.AnimationDesired = animationDesired;
					bringIntoViewOptions.VerticalAlignmentRatio = verticalAlignmentRatio;
					bringIntoViewOptions.VerticalOffset = verticalOffset;

					try
					{
						_itemsView.StartBringItemIntoView(index, bringIntoViewOptions);
						startBringItemIntoViewSuccess = true;
					}
					catch (Exception)
					{
						startBringItemIntoViewSuccess = false;
					}
				}
				else
				{
					try
					{
						_itemsView.StartBringItemIntoView(index, null);
						startBringItemIntoViewSuccess = true;
					}
					catch (Exception)
					{
						startBringItemIntoViewSuccess = false;
					}
				}

				AppendEventMessage($"StartBringItemIntoView({index})={startBringItemIntoViewSuccess}");
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnItemsViewStartBringItemIntoViewAsync_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (txtItemsViewStartBringItemIntoViewAsyncCountdown != null)
			{
				int startBringItemIntoViewAsyncCountdown = int.Parse(txtItemsViewStartBringItemIntoViewAsyncCountdown.Text);

				_queuedOperationsTimer.Interval = new TimeSpan(0, 0, startBringItemIntoViewAsyncCountdown /*sec*/);
			}

			_lstQueuedOperations.Add(new QueuedOperation(QueuedOperationType.StartBringItemIntoView));
			_queuedOperationsTimer.Start();
			AppendAsyncEventMessage("Queued StartBringItemIntoView");
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnLayoutInvalidateMeasure_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			Layout layout = _itemsView == null ? null : _itemsView.Layout;

			if (layout != null)
			{
				//LayoutsTestHooks.LayoutInvalidateMeasure(layout, relayout: true);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnLinedFlowLayoutInvalidateItemsInfo_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				_linedFlowLayout.InvalidateItemsInfo();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnLinedFlowLayoutGetLineIndex_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				//int itemIndex = -1;

				//int.TryParse(txtLinedFlowLayoutMethodIndex.Text, out itemIndex);

				//int lineIndex = LayoutsTestHooks.GetLinedFlowLayoutLineIndex(_linedFlowLayout, itemIndex);

				//txtLinedFlowLayoutLineIndex.Text = lineIndex.ToString();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnLinedFlowLayoutLockItemToLine_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				int itemIndex = -1;

				int.TryParse(txtLinedFlowLayoutMethodIndex.Text, out itemIndex);

				int lineIndex = _linedFlowLayout.LockItemToLine(itemIndex);

				txtLinedFlowLayoutLockLine.Text = lineIndex.ToString();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnLinedFlowLayoutClearItemAspectRatios_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				//LayoutsTestHooks.ClearLinedFlowLayoutItemAspectRatios(_linedFlowLayout);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnLinedFlowLayoutUnlockItems_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				//LayoutsTestHooks.UnlockLinedFlowLayoutItems(_linedFlowLayout);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.SelectAll();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnDeselectAll_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.DeselectAll();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnInvertSelection_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.InvertSelection();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnIsSelected_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null && txtIsSelected != null && txtSelectionIndex != null)
			{
				int index = -1;

				int.TryParse(txtSelectionIndex.Text, out index);

				txtIsSelected.Text = _itemsView.IsSelected(index).ToString(); ;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSelect_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null && txtSelectionIndex != null)
			{
				int index = -1;

				int.TryParse(txtSelectionIndex.Text, out index);

				_itemsView.Select(index);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnDeselect_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null && txtSelectionIndex != null)
			{
				int index = -1;

				int.TryParse(txtSelectionIndex.Text, out index);

				_itemsView.Deselect(index);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	#endregion

	#region Property Setters

	private void ChkIsEnabled_Checked(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.IsEnabled = true;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ChkIsEnabled_Unchecked(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.IsEnabled = false;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ChkIsTabStop_Checked(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.IsTabStop = true;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ChkIsTabStop_Unchecked(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.IsTabStop = false;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ChkLinedFlowLayoutIsFastPathSupportedDbg_IsCheckedChanged(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				//LayoutsTestHooks.SetLinedFlowLayoutIsFastPathSupported(_linedFlowLayout, (bool)chkLinedFlowLayoutIsFastPathSupportedDbg.IsChecked);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ChkLinedFlowLayoutShowLockedItems_Checked(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutLockedItemsVisuals();

		ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

		if (scrollView != null)
		{
			scrollView.ViewChanged += ScrollView_ViewChangedForLockedItemsVisuals;
		}
	}

	private void ChkLinedFlowLayoutShowLockedItems_Unchecked(object sender, RoutedEventArgs e)
	{
		UpdateLinedFlowLayoutLockedItemsVisuals();

		ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

		if (scrollView != null)
		{
			scrollView.ViewChanged -= ScrollView_ViewChangedForLockedItemsVisuals;
		}
	}

	private void ChkShowCurrentElement_Checked(object sender, RoutedEventArgs e)
	{
		_currentItemIndexChangedToken = _itemsView.RegisterPropertyChangedCallback(
														ItemsView.CurrentItemIndexProperty,
														new DependencyPropertyChangedCallback(ItemsView_CurrentItemIndexChanged));

		UpdateItemsViewCurrentElementVisual(log: true);

		ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

		if (scrollView != null)
		{
			scrollView.ViewChanged += ScrollView_ViewChangedForCurrentElementVisual;
		}
	}

	private void ChkShowCurrentElement_Unchecked(object sender, RoutedEventArgs e)
	{
		_itemsView.UnregisterPropertyChangedCallback(ItemsView.CurrentItemIndexProperty, _currentItemIndexChangedToken);

		UpdateItemsViewCurrentElementVisual(log: false);

		ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

		if (scrollView != null)
		{
			scrollView.ViewChanged -= ScrollView_ViewChangedForCurrentElementVisual;
		}
	}

	private void ChkShowKeyboardNavigationReferenceOffset_Checked(object sender, RoutedEventArgs e)
	{
		ItemsViewTestHooks.KeyboardNavigationReferenceOffsetChanged += ItemsViewTestHooks_KeyboardNavigationReferenceOffsetChanged;

		UpdateItemsViewKeyboardNavigationReferenceOffsetVisual(log: true);
	}

	private void ChkShowKeyboardNavigationReferenceOffset_Unchecked(object sender, RoutedEventArgs e)
	{
		ItemsViewTestHooks.KeyboardNavigationReferenceOffsetChanged -= ItemsViewTestHooks_KeyboardNavigationReferenceOffsetChanged;

		UpdateItemsViewKeyboardNavigationReferenceOffsetVisual(log: true);
	}

	private void BtnSetBorderThickness_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.BorderThickness = GetThicknessFromString(txtBorderThickness.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetContentOrientation_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					ScrollingContentOrientation co = (ScrollingContentOrientation)cmbContentOrientation.SelectedIndex;

					scrollView.ContentOrientation = co;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetCornerRadius_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			_itemsView.CornerRadius = GetCornerRadiusFromString(txtCornerRadius.Text);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ItemsViewSetHeight(Convert.ToDouble(txtHeight.Text));
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ItemsViewSetHeight(double value)
	{
		_itemsView.Height = value;
	}

	private void BtnSetHorizontalAnchorRatio_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					scrollView.HorizontalAnchorRatio = Convert.ToDouble(txtHorizontalAnchorRatio.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetHorizontalOffset_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					scrollView.ScrollTo(Convert.ToDouble(txtHorizontalOffset.Text), scrollView.VerticalOffset);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetHorizontalScrollBarVisibility_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					scrollView.HorizontalScrollBarVisibility = (ScrollingScrollBarVisibility)cmbHorizontalScrollBarVisibility.SelectedIndex;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetHorizontalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					ScrollingScrollMode ssm = (ScrollingScrollMode)cmbHorizontalScrollMode.SelectedIndex;

					scrollView.HorizontalScrollMode = ssm;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterHorizontalAlignment_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					itemsRepeater.HorizontalAlignment = (HorizontalAlignment)cmbItemsRepeaterHorizontalAlignment.SelectedIndex;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterHorizontalCacheLength_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					itemsRepeater.HorizontalCacheLength = Convert.ToDouble(txtItemsRepeaterHorizontalCacheLength.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterLogItemIndexDbg_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (txtItemsRepeaterLogItemIndexDbg != null)
			{
				//RepeaterTestHooks.SetLogItemIndex(Convert.ToInt32(txtItemsRepeaterLogItemIndexDbg.Text));
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterVerticalAlignment_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					itemsRepeater.VerticalAlignment = (VerticalAlignment)cmbItemsRepeaterVerticalAlignment.SelectedIndex;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterVerticalCacheLength_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					itemsRepeater.VerticalCacheLength = Convert.ToDouble(txtItemsRepeaterVerticalCacheLength.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnResetLayoutForcedIndexBasedLayoutOrientation_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				Layout layout = _itemsView.Layout;

				if (layout != null)
				{
					//LayoutsTestHooks.ResetLayoutForcedIndexBasedLayoutOrientation(layout);
					if ((bool)chkShowKeyboardNavigationReferenceOffset.IsChecked)
					{
						UpdateItemsViewKeyboardNavigationReferenceOffsetVisual(log: true);
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetLayoutForcedIndexBasedLayoutOrientation_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				Layout layout = _itemsView.Layout;

				if (layout != null)
				{
					//LayoutsTestHooks.SetLayoutForcedIndexBasedLayoutOrientation(layout, (IndexBasedLayoutOrientation)cmbLayoutForcedIndexBasedLayoutOrientation.SelectedIndex);
					if ((bool)chkShowKeyboardNavigationReferenceOffset.IsChecked)
					{
						UpdateItemsViewKeyboardNavigationReferenceOffsetVisual(log: true);
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetMargin_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.Margin = GetThicknessFromString(txtMargin.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetMaxHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			_itemsView.MaxHeight = Convert.ToDouble(txtMaxHeight.Text);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetMaxWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			_itemsView.MaxWidth = Convert.ToDouble(txtMaxWidth.Text);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetPadding_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				_itemsView.Padding = GetThicknessFromString(txtPadding.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetLinedFlowLayoutForcedAverageItemAspectRatioDbg_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				//LayoutsTestHooks.SetLinedFlowLayoutForcedAverageItemAspectRatio(_linedFlowLayout, Convert.ToDouble(txtLinedFlowLayoutForcedAverageItemAspectRatioDbg.Text));
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetLinedFlowLayoutForcedAverageItemsPerLineDividerDbg_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				//LayoutsTestHooks.SetLinedFlowLayoutForcedAverageItemsPerLineDivider(_linedFlowLayout, Convert.ToDouble(txtLinedFlowLayoutForcedAverageItemsPerLineDividerDbg.Text));
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetLinedFlowLayoutForcedWrapMultiplierDbg_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				//LayoutsTestHooks.SetLinedFlowLayoutForcedWrapMultiplier(_linedFlowLayout, Convert.ToDouble(txtLinedFlowLayoutForcedWrapMultiplierDbg.Text));
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetLinedFlowLayoutLineHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				_linedFlowLayout.LineHeight = Convert.ToDouble(txtLinedFlowLayoutLineHeight.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetLinedFlowLayoutLineSpacing_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				_linedFlowLayout.LineSpacing = Convert.ToDouble(txtLinedFlowLayoutLineSpacing.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetLinedFlowLayoutLogItemIndexDbg_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				//LayoutsTestHooks.SetLinedFlowLayoutLogItemIndex(_linedFlowLayout, Convert.ToInt32(txtLinedFlowLayoutLogItemIndexDbg.Text));
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetLinedFlowLayoutMinItemSpacing_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null)
			{
				_linedFlowLayout.MinItemSpacing = Convert.ToDouble(txtLinedFlowLayoutMinItemSpacing.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetStackLayoutOrientation_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_stackLayout != null)
			{
				_stackLayout.Orientation = cmbStackLayoutOrientation.SelectedIndex == 0 ? Orientation.Horizontal : Orientation.Vertical;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetStackLayoutSpacing_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_stackLayout != null)
			{
				_stackLayout.Spacing = Convert.ToDouble(txtStackLayoutSpacing.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetUniformGridLayoutMaximumRowsOrColumns_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_uniformGridLayout != null)
			{
				_uniformGridLayout.MaximumRowsOrColumns = Convert.ToInt32(txtUniformGridLayoutMaximumRowsOrColumns.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetUniformGridLayoutMinColumnSpacing_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_uniformGridLayout != null)
			{
				_uniformGridLayout.MinColumnSpacing = Convert.ToDouble(txtUniformGridLayoutMinColumnSpacing.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetUniformGridLayoutMinRowSpacing_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_uniformGridLayout != null)
			{
				_uniformGridLayout.MinRowSpacing = Convert.ToDouble(txtUniformGridLayoutMinRowSpacing.Text);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetUniformGridLayoutOrientation_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_uniformGridLayout != null)
			{
				_uniformGridLayout.Orientation = (Orientation)cmbUniformGridLayoutOrientation.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetVerticalAnchorRatio_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					scrollView.VerticalAnchorRatio = Convert.ToDouble(txtVerticalAnchorRatio.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetVerticalConstantVelocity_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					float verticalConstantVelocity = float.Parse(this.txtVerticalConstantVelocity.Text);

					if (verticalConstantVelocity == 0.0f)
					{
						scrollView.ScrollBy(0, 0, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled, ScrollingSnapPointsMode.Ignore));
					}
					else
					{
						scrollView.AddScrollVelocity(new System.Numerics.Vector2(0.0f, verticalConstantVelocity), new System.Numerics.Vector2(0.0f, 0.0f));
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetVerticalOffset_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					scrollView.ScrollTo(scrollView.HorizontalOffset, Convert.ToDouble(txtVerticalOffset.Text));
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetVerticalScrollBarVisibility_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					scrollView.VerticalScrollBarVisibility = (ScrollingScrollBarVisibility)cmbVerticalScrollBarVisibility.SelectedIndex;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetVerticalScrollMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					ScrollingScrollMode ssm = (ScrollingScrollMode)cmbVerticalScrollMode.SelectedIndex;

					scrollView.VerticalScrollMode = ssm;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			ItemsViewSetWidth(Convert.ToDouble(txtWidth.Text));
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ItemsViewSetWidth(double value)
	{
		_itemsView.Width = value;

		if (rectHorizontalKeyboardNavigationReferenceOffset != null)
		{
			rectHorizontalKeyboardNavigationReferenceOffset.Width = _itemsView.ActualWidth;
		}
	}

	private void BtnSetZoomFactor_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					scrollView.ZoomTo(float.Parse(txtZoomFactor.Text), null);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetZoomMode_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ScrollView scrollView = ItemsViewTestHooks.GetScrollViewPart(_itemsView);

				if (scrollView != null)
				{
					ScrollingZoomMode szm = (ScrollingZoomMode)cmbZoomMode.SelectedIndex;

					scrollView.ZoomMode = szm;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterElementIsEnabled_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is Control control)
				{
					control.IsEnabled = (bool)chkItemsRepeaterElementIsEnabled.IsChecked;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterElementMinWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					frameworkElement.MinWidth = double.Parse(txtItemsRepeaterElementMinWidth.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterElementMaxWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					frameworkElement.MaxWidth = double.Parse(txtItemsRepeaterElementMaxWidth.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterElementWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					frameworkElement.Width = double.Parse(txtItemsRepeaterElementWidth.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterElementMinHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					frameworkElement.MinHeight = double.Parse(txtItemsRepeaterElementMinHeight.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterElementMaxHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					frameworkElement.MaxHeight = double.Parse(txtItemsRepeaterElementMaxHeight.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterElementHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is FrameworkElement frameworkElement)
				{
					frameworkElement.Height = double.Parse(txtItemsRepeaterElementHeight.Text);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterItemContainerCanUserInvoke_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is ItemContainer itemContainer)
				{
#if MUX_PRERELEASE
						if (chkItemsRepeaterItemContainerCanUserInvoke.IsChecked == null)
						{
							itemContainer.CanUserInvoke = ItemContainerUserInvokeMode.Auto;
						}
						else if (chkItemsRepeaterItemContainerCanUserInvoke.IsChecked == true)
						{
							itemContainer.CanUserInvoke = ItemContainerUserInvokeMode.Auto | ItemContainerUserInvokeMode.UserCanInvoke;
						}
						else
						{
							itemContainer.CanUserInvoke = ItemContainerUserInvokeMode.Auto | ItemContainerUserInvokeMode.UserCannotInvoke;
						}
#endif
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterItemContainerCanUserSelect_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is ItemContainer itemContainer)
				{
#if MUX_PRERELEASE
						if (chkItemsRepeaterItemContainerCanUserSelect.IsChecked == null)
						{
							itemContainer.CanUserSelect = ItemContainerUserSelectMode.Auto;
						}
						else if (chkItemsRepeaterItemContainerCanUserSelect.IsChecked == true)
						{
							itemContainer.CanUserSelect = ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCanSelect;
						}
						else
						{
							itemContainer.CanUserSelect = ItemContainerUserSelectMode.Auto | ItemContainerUserSelectMode.UserCannotSelect;
						}
#endif
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnSetItemsRepeaterItemContainerIsSelected_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);
				UIElement element = GetItemsRepeaterPropertyElement(itemsRepeater);

				if (element is ItemContainer itemContainer)
				{
					itemContainer.IsSelected = (bool)chkItemsRepeaterItemContainerIsSelected.IsChecked;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	#endregion

	#region Operation Queueing

	private void BtnQueueWidth_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			double value = Convert.ToDouble(txtWidth.Text);
			_lstQueuedOperations.Add(new QueuedOperation(QueuedOperationType.SetWidth, value));
			AppendAsyncEventMessage("Queued SetWidth " + value);
			_queuedOperationsTimer.Start();
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnQueueHeight_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			double value = Convert.ToDouble(txtHeight.Text);
			_lstQueuedOperations.Add(new QueuedOperation(QueuedOperationType.SetHeight, value));
			AppendAsyncEventMessage("Queued SetHeight " + value);
			_queuedOperationsTimer.Start();
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	#endregion

	private void ChkIsItemInvokedEnabled_Checked(object sender, RoutedEventArgs e)
	{
		if (_itemsView != null)
		{
			_itemsView.IsItemInvokedEnabled = true;
		}
	}

	private void ChkIsItemInvokedEnabled_Unchecked(object sender, RoutedEventArgs e)
	{
		if (_itemsView != null)
		{
			_itemsView.IsItemInvokedEnabled = false;
		}
	}

	private void CmbItemTemplate_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				if (cmbItemTemplate.SelectedIndex == 0)
				{
					_itemsView.ItemTemplate = null;
				}
				else
				{
					int templateIndex = cmbItemTemplate.SelectedIndex - 1;

					if (_recipeTemplates[templateIndex] == null)
					{
						_recipeTemplates[templateIndex] = Resources["recipeTemplate" + cmbItemTemplate.SelectedIndex.ToString()] as DataTemplate;
					}

					_itemsView.ItemTemplate = _recipeTemplates[templateIndex];
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbItemsSource_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				switch (cmbItemsSource.SelectedIndex)
				{
					case 0:
						_itemsView.ItemsSource = null;
						break;
					case 1:
						if (_lstRecipes == null)
						{
							var rnd = new Random();
							_lstRecipes = new List<Recipe>();

							for (int itemIndex = 0; itemIndex < 400; itemIndex++)
							{
								_lstRecipes.Add(new Recipe()
								{
									ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", itemIndex % 126 + 1)),
									Id = itemIndex,
									Description = itemIndex + " - " + _lorem.Substring(0, rnd.Next(25, 125))
								});
							}
						}
						_itemsView.ItemsSource = _lstRecipes;
						break;
					case 2:
						if (_colSmallRecipes == null)
						{
							_colSmallRecipes = new ObservableCollection<Recipe>();

							for (int itemIndex = 0; itemIndex < 7; itemIndex++)
							{
								_colSmallRecipes.Add(new Recipe()
								{
									ImageUri = new Uri(string.Format("ms-appx:///Images/Rect{0}.png", itemIndex % 6)),
									Id = itemIndex,
									Description = itemIndex.ToString()
								});
							}
						}
						_itemsView.ItemsSource = _colSmallRecipes;
						break;
					case 3:
						if (_colSmallUniformRecipes == null)
						{
							var rnd = new Random();
							_colSmallUniformRecipes = new ObservableCollection<Recipe>();

							for (int itemIndex = 0; itemIndex < 7; itemIndex++)
							{
								_colSmallUniformRecipes.Add(new Recipe()
								{
									ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", itemIndex % 126 + 100)),
									Id = itemIndex,
									Description = itemIndex + " - " + _lorem.Substring(0, rnd.Next(50, 350))
								});
							}
						}
						_itemsView.ItemsSource = _colSmallUniformRecipes;
						break;
					case 4:
						if (_colRecipes == null)
						{
							var rnd = new Random();
							_colRecipes = new ObservableCollection<Recipe>();

							for (int itemIndex = 0; itemIndex < 300; itemIndex++)
							{
								_colRecipes.Add(new Recipe()
								{
									ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", itemIndex % 126 + 1)),
									Id = itemIndex,
									Description = itemIndex + " - " + _lorem.Substring(0, rnd.Next(50, 350))
								});
							}
						}
						_itemsView.ItemsSource = _colRecipes;
						break;
					case 5:
						if (_colSmallItemContainers == null)
						{
							_colSmallItemContainers = new ObservableCollection<ItemContainer>();

							for (int itemIndex = 0; itemIndex < 7; itemIndex++)
							{
								ItemContainer itemContainer = GetItemContainerAsItemsSourceItem(itemIndex);

								_colSmallItemContainers.Add(itemContainer);
							}
						}
						_itemsView.ItemsSource = _colSmallItemContainers;
						break;
					case 6:
						if (_colSmallImages == null)
						{
							_colSmallImages = new ObservableCollection<Image>();

							for (int itemIndex = 0; itemIndex < 7; itemIndex++)
							{
								Image image = GetImageAsItemsSourceItem(itemIndex);

								_colSmallImages.Add(image);
							}
						}
						_itemsView.ItemsSource = _colSmallImages;
						break;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbLayout_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				switch (cmbLayout.SelectedIndex)
				{
					case 0:
						_itemsView.Layout = null;
						break;
					case 1:
						if (_flowLayout == null)
						{
							_flowLayout = Resources["flowLayout"] as FlowLayout;
						}
						_itemsView.Layout = _flowLayout;
						break;
					case 2:
						if (_linedFlowLayout == null)
						{
							_linedFlowLayout = Resources["linedFlowLayout"] as LinedFlowLayout;
							HookLinedFlowLayoutEvents(_linedFlowLayout);
							BtnSetLinedFlowLayoutLineHeight_Click(null, null);
							BtnSetLinedFlowLayoutLineSpacing_Click(null, null);
							BtnSetLinedFlowLayoutMinItemSpacing_Click(null, null);
							ChkLinedFlowLayoutIsFastPathSupportedDbg_IsCheckedChanged(null, null);
							CmbLinedFlowLayoutItemsJustification_SelectionChanged(null, null);
							CmbLinedFlowLayoutItemsStretch_SelectionChanged(null, null);
						}
						_itemsView.Layout = _linedFlowLayout;
						break;
					case 3:
						_itemsView.Layout = _stackLayout;
						break;
					case 4:
						if (_uniformGridLayout == null)
						{
							_uniformGridLayout = Resources["uniformGridLayout"] as UniformGridLayout;
							BtnSetUniformGridLayoutMinColumnSpacing_Click(null, null);
							BtnSetUniformGridLayoutMinRowSpacing_Click(null, null);
							CmbUniformGridLayoutItemsJustification_SelectionChanged(null, null);
							CmbUniformGridLayoutItemsStretch_SelectionChanged(null, null);
						}
						_itemsView.Layout = _uniformGridLayout;
						break;
				}

				UpdateItemsRepeaterDefaultCacheLengths();
				UpdateScrollViewDefaultAnchorRatios();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbSelectionMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_itemsView != null && cmbSelectionMode != null && _itemsView.SelectionMode != (ItemsViewSelectionMode)cmbSelectionMode.SelectedIndex)
			{
				_itemsView.SelectionMode = (ItemsViewSelectionMode)cmbSelectionMode.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbBackground_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				switch (cmbBackground.SelectedIndex)
				{
					case 0:
						_itemsView.Background = null;
						break;
					case 1:
						_itemsView.Background = new SolidColorBrush(Colors.Transparent);
						break;
					case 2:
						_itemsView.Background = new SolidColorBrush(Colors.AliceBlue);
						break;
					case 3:
						_itemsView.Background = new SolidColorBrush(Colors.Aqua);
						break;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbBorderBrush_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_itemsView != null)
			{
				switch (cmbBorderBrush.SelectedIndex)
				{
					case 0:
						_itemsView.BorderBrush = null;
						break;
					case 1:
						_itemsView.BorderBrush = new SolidColorBrush(Colors.Transparent);
						break;
					case 2:
						_itemsView.BorderBrush = new SolidColorBrush(Colors.Blue);
						break;
					case 3:
						_itemsView.BorderBrush = new SolidColorBrush(Colors.Green);
						break;
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbTabNavigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_itemsView != null && cmbTabNavigation != null && _itemsView.TabNavigation != (KeyboardNavigationMode)cmbTabNavigation.SelectedIndex)
			{
				_itemsView.TabNavigation = (KeyboardNavigationMode)cmbTabNavigation.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbXYFocusKeyboardNavigation_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_itemsView != null && cmbXYFocusKeyboardNavigation != null && _itemsView.XYFocusKeyboardNavigation != (XYFocusKeyboardNavigationMode)cmbXYFocusKeyboardNavigation.SelectedIndex)
			{
				_itemsView.XYFocusKeyboardNavigation = (XYFocusKeyboardNavigationMode)cmbXYFocusKeyboardNavigation.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbLinedFlowLayoutItemsJustification_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null && cmbLinedFlowLayoutItemsJustification != null && _linedFlowLayout.ItemsJustification != (LinedFlowLayoutItemsJustification)cmbLinedFlowLayoutItemsJustification.SelectedIndex)
			{
				_linedFlowLayout.ItemsJustification = (LinedFlowLayoutItemsJustification)cmbLinedFlowLayoutItemsJustification.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbLinedFlowLayoutItemsStretch_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null && cmbLinedFlowLayoutItemsStretch != null && _linedFlowLayout.ItemsStretch != (LinedFlowLayoutItemsStretch)cmbLinedFlowLayoutItemsStretch.SelectedIndex)
			{
				_linedFlowLayout.ItemsStretch = (LinedFlowLayoutItemsStretch)cmbLinedFlowLayoutItemsStretch.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbUniformGridLayoutItemsJustification_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_uniformGridLayout != null && cmbUniformGridLayoutItemsJustification != null && _uniformGridLayout.ItemsJustification != (UniformGridLayoutItemsJustification)cmbUniformGridLayoutItemsJustification.SelectedIndex)
			{
				_uniformGridLayout.ItemsJustification = (UniformGridLayoutItemsJustification)cmbUniformGridLayoutItemsJustification.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void CmbUniformGridLayoutItemsStretch_SelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		try
		{
			if (_uniformGridLayout != null && cmbLinedFlowLayoutItemsStretch != null && _uniformGridLayout.ItemsStretch != (UniformGridLayoutItemsStretch)cmbUniformGridLayoutItemsStretch.SelectedIndex)
			{
				_uniformGridLayout.ItemsStretch = (UniformGridLayoutItemsStretch)cmbUniformGridLayoutItemsStretch.SelectedIndex;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ChkItemsViewProperties_Checked(object sender, RoutedEventArgs e)
	{
		if (svItemsViewProperties != null)
			svItemsViewProperties.Visibility = Visibility.Visible;
	}

	private void ChkItemsViewProperties_Unchecked(object sender, RoutedEventArgs e)
	{
		if (svItemsViewProperties != null)
			svItemsViewProperties.Visibility = Visibility.Collapsed;
	}

	private void ChkChildrenProperties_Checked(object sender, RoutedEventArgs e)
	{
		if (svChildrenProperties != null)
			svChildrenProperties.Visibility = Visibility.Visible;
	}

	private void ChkChildrenProperties_Unchecked(object sender, RoutedEventArgs e)
	{
		if (svChildrenProperties != null)
			svChildrenProperties.Visibility = Visibility.Collapsed;
	}

	private void ChkItemsViewMethods_Checked(object sender, RoutedEventArgs e)
	{
		if (svItemsViewMethods != null)
			svItemsViewMethods.Visibility = Visibility.Visible;
	}

	private void ChkItemsViewMethods_Unchecked(object sender, RoutedEventArgs e)
	{
		if (svItemsViewMethods != null)
			svItemsViewMethods.Visibility = Visibility.Collapsed;
	}

	private void ChkLogs_Checked(object sender, RoutedEventArgs e)
	{
		if (grdLogs != null)
			grdLogs.Visibility = Visibility.Visible;
	}

	private void ChkLogs_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdLogs != null)
			grdLogs.Visibility = Visibility.Collapsed;
	}

	private void ChkPageMethods_Checked(object sender, RoutedEventArgs e)
	{
		if (grdPageMethods != null)
			grdPageMethods.Visibility = Visibility.Visible;
	}

	private void ChkPageMethods_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdPageMethods != null)
			grdPageMethods.Visibility = Visibility.Collapsed;
	}

	private void ChkItemsViewDataSource_Checked(object sender, RoutedEventArgs e)
	{
		if (grdItemsViewDataSource != null)
			grdItemsViewDataSource.Visibility = Visibility.Visible;
	}

	private void ChkItemsViewDataSource_Unchecked(object sender, RoutedEventArgs e)
	{
		if (grdItemsViewDataSource != null)
			grdItemsViewDataSource.Visibility = Visibility.Collapsed;
	}

	private void BtnDataSourceGetItemCount_Click(object sender, RoutedEventArgs e)
	{
		UpdateDataSourceItemCount();
	}

	private void BtnDataSourceSetItemCount_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_itemsView != null && txtDataSourceItemCount != null)
			{
				int newItemCount = int.Parse(txtDataSourceItemCount.Text);
				var rnd = new Random();

				if (_itemsView.ItemsSource == null || _itemsView.ItemsSource == _lstRecipes)
				{
					bool setItemsSource = _itemsView.ItemsSource != null && _itemsView.ItemsSource == _lstRecipes;
					int currentItemCount = _lstRecipes == null ? 0 : _lstRecipes.Count;

					if (currentItemCount < newItemCount)
					{
						var lstRecipesEnd = new List<Recipe>();

						for (int itemIndex = currentItemCount; itemIndex < newItemCount; itemIndex++)
						{
							lstRecipesEnd.Add(new Recipe()
							{
								ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", itemIndex % 126 + 1)),
								Id = itemIndex,
								Description = itemIndex + " - " + _lorem.Substring(0, rnd.Next(25, 125))
							});
						}

						if (currentItemCount == 0)
						{
							_lstRecipes = lstRecipesEnd;
						}
						else
						{
							_lstRecipes = new List<Recipe>(_lstRecipes.Concat(lstRecipesEnd));
						}
					}
					else if (currentItemCount > newItemCount)
					{
						_lstRecipes = new List<Recipe>(_lstRecipes.Take(newItemCount));
					}

					if (setItemsSource)
					{
						_itemsView.ItemsSource = _lstRecipes;
					}
				}

				if (_itemsView.ItemsSource == null || _itemsView.ItemsSource == _colSmallRecipes)
				{
					bool setItemsSource = _itemsView.ItemsSource != null && _itemsView.ItemsSource == _colSmallRecipes;
					int currentItemCount = _colSmallRecipes == null ? 0 : _colSmallRecipes.Count;

					if (currentItemCount < newItemCount)
					{
						var colRecipesEnd = new ObservableCollection<Recipe>();

						for (int itemIndex = currentItemCount; itemIndex < newItemCount; itemIndex++)
						{
							colRecipesEnd.Add(new Recipe()
							{
								ImageUri = new Uri(string.Format("ms-appx:///Images/Rect{0}.png", itemIndex % 6)),
								Id = itemIndex,
								Description = itemIndex.ToString()
							});
						}

						if (currentItemCount == 0)
						{
							_colSmallRecipes = colRecipesEnd;
						}
						else
						{
							_colSmallRecipes = new ObservableCollection<Recipe>(_colSmallRecipes.Concat(colRecipesEnd));
						}
					}
					else if (currentItemCount > newItemCount)
					{
						_colSmallRecipes = new ObservableCollection<Recipe>(_colSmallRecipes.Take(newItemCount));
					}

					if (setItemsSource)
					{
						_itemsView.ItemsSource = _colSmallRecipes;
					}
				}

				if (_itemsView.ItemsSource == null || _itemsView.ItemsSource == _colSmallUniformRecipes)
				{
					bool setItemsSource = _itemsView.ItemsSource != null && _itemsView.ItemsSource == _colSmallUniformRecipes;
					int currentItemCount = _colSmallUniformRecipes == null ? 0 : _colSmallUniformRecipes.Count;

					if (currentItemCount < newItemCount)
					{
						var colRecipesEnd = new ObservableCollection<Recipe>();

						for (int itemIndex = currentItemCount; itemIndex < newItemCount; itemIndex++)
						{
							colRecipesEnd.Add(new Recipe()
							{
								ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", itemIndex % 126 + 1)),
								Id = itemIndex,
								Description = itemIndex + " - " + _lorem.Substring(0, rnd.Next(50, 350))
							});
						}

						if (currentItemCount == 0)
						{
							_colSmallUniformRecipes = colRecipesEnd;
						}
						else
						{
							_colSmallUniformRecipes = new ObservableCollection<Recipe>(_colSmallUniformRecipes.Concat(colRecipesEnd));
						}
					}
					else if (currentItemCount > newItemCount)
					{
						_colSmallUniformRecipes = new ObservableCollection<Recipe>(_colSmallUniformRecipes.Take(newItemCount));
					}

					if (setItemsSource)
					{
						_itemsView.ItemsSource = _colSmallUniformRecipes;
					}
				}

				if (_itemsView.ItemsSource == null || _itemsView.ItemsSource == _colRecipes)
				{
					bool setItemsSource = _itemsView.ItemsSource != null && _itemsView.ItemsSource == _colRecipes;
					int currentItemCount = _colRecipes == null ? 0 : _colRecipes.Count;

					if (currentItemCount < newItemCount)
					{
						var colRecipesEnd = new ObservableCollection<Recipe>();

						for (int itemIndex = currentItemCount; itemIndex < newItemCount; itemIndex++)
						{
							colRecipesEnd.Add(new Recipe()
							{
								ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", itemIndex % 126 + 1)),
								Id = itemIndex,
								Description = itemIndex + " - " + _lorem.Substring(0, rnd.Next(50, 350))
							});
						}

						if (currentItemCount == 0)
						{
							_colRecipes = colRecipesEnd;
						}
						else
						{
							_colRecipes = new ObservableCollection<Recipe>(_colRecipes.Concat(colRecipesEnd));
						}
					}
					else if (currentItemCount > newItemCount)
					{
						_colRecipes = new ObservableCollection<Recipe>(_colRecipes.Take(newItemCount));
					}

					if (setItemsSource)
					{
						_itemsView.ItemsSource = _colRecipes;
					}
				}

				if (_itemsView.ItemsSource == null || _itemsView.ItemsSource == _colSmallItemContainers)
				{
					bool setItemsSource = _itemsView.ItemsSource != null && _itemsView.ItemsSource == _colSmallItemContainers;
					int currentItemCount = _colSmallItemContainers == null ? 0 : _colSmallItemContainers.Count;

					if (currentItemCount < newItemCount)
					{
						var colItemContainersEnd = new ObservableCollection<ItemContainer>();

						for (int itemIndex = currentItemCount; itemIndex < newItemCount; itemIndex++)
						{
							ItemContainer itemContainer = GetItemContainerAsItemsSourceItem(itemIndex);

							colItemContainersEnd.Add(itemContainer);
						}

						if (currentItemCount == 0)
						{
							_colSmallItemContainers = colItemContainersEnd;
						}
						else
						{
							_colSmallItemContainers = new ObservableCollection<ItemContainer>(_colSmallItemContainers.Concat(colItemContainersEnd));
						}
					}
					else if (currentItemCount > newItemCount)
					{
						_colSmallItemContainers = new ObservableCollection<ItemContainer>(_colSmallItemContainers.Take(newItemCount));
					}

					if (setItemsSource)
					{
						_itemsView.ItemsSource = _colSmallItemContainers;
					}
				}

				if (_itemsView.ItemsSource == null || _itemsView.ItemsSource == _colSmallImages)
				{
					bool setItemsSource = _itemsView.ItemsSource != null && _itemsView.ItemsSource == _colSmallImages;
					int currentItemCount = _colSmallImages == null ? 0 : _colSmallImages.Count;

					if (currentItemCount < newItemCount)
					{
						var colImagesEnd = new ObservableCollection<Image>();

						for (int itemIndex = currentItemCount; itemIndex < newItemCount; itemIndex++)
						{
							Image image = GetImageAsItemsSourceItem(itemIndex);

							colImagesEnd.Add(image);
						}

						if (currentItemCount == 0)
						{
							_colSmallImages = colImagesEnd;
						}
						else
						{
							_colSmallImages = new ObservableCollection<Image>(_colSmallImages.Concat(colImagesEnd));
						}
					}
					else if (currentItemCount > newItemCount)
					{
						_colSmallImages = new ObservableCollection<Image>(_colSmallImages.Take(newItemCount));
					}

					if (setItemsSource)
					{
						_itemsView.ItemsSource = _colSmallImages;
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void DataSourceAddItem()
	{
		try
		{
			if (_itemsView != null && _itemsView.ItemsSource != null)
			{
				var rnd = new Random();

				if (_itemsView.ItemsSource == _lstRecipes)
				{
					var recipe = new Recipe()
					{
						ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", _lstRecipes.Count % 126 + 1)),
						Id = _lstRecipes.Count,
						Description = _lstRecipes.Count + " - " + _lorem.Substring(0, rnd.Next(25, 125))
					};

					_lstRecipes.Add(recipe);
				}
				else
				{
					ObservableCollection<Recipe> colRecipes = _itemsView.ItemsSource as ObservableCollection<Recipe>;

					if (colRecipes != null)
					{
						string uriString;
						string description;

						if (colRecipes == _colRecipes || colRecipes == _colSmallUniformRecipes)
						{
							uriString = string.Format("ms-appx:///Images/vette{0}.jpg", colRecipes.Count % 126 + 1);
							description = colRecipes.Count.ToString() + " - " + _lorem.Substring(0, rnd.Next(50, 350));
						}
						else
						{
							uriString = string.Format("ms-appx:///Images/Rect{0}.png", colRecipes.Count % 6);
							description = colRecipes.Count.ToString();
						}

						var recipe = new Recipe()
						{
							ImageUri = new Uri(uriString),
							Id = colRecipes.Count,
							Description = description
						};

						colRecipes.Add(recipe);
					}
					else
					{
						ObservableCollection<ItemContainer> colItemContainers = _itemsView.ItemsSource as ObservableCollection<ItemContainer>;

						if (colItemContainers != null)
						{
							ItemContainer itemContainer = GetItemContainerAsItemsSourceItem(colItemContainers.Count);

							colItemContainers.Add(itemContainer);
						}
						else
						{
							ObservableCollection<Image> colImages = _itemsView.ItemsSource as ObservableCollection<Image>;

							if (colImages != null)
							{
								Image image = GetImageAsItemsSourceItem(colImages.Count);

								colImages.Add(image);
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void DataSourceInsertItem(int newItemIndex)
	{
		try
		{
			if (_itemsView != null && _itemsView.ItemsSource != null)
			{
				var rnd = new Random();

				if (_itemsView.ItemsSource == _lstRecipes)
				{
					var recipe = new Recipe()
					{
						ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", _lstRecipes.Count % 126 + 1)),
						Id = _lstRecipes.Count,
						Description = _lstRecipes.Count + " - " + _lorem.Substring(0, rnd.Next(25, 125))
					};

					_lstRecipes.Insert(newItemIndex, recipe);
				}
				else
				{
					ObservableCollection<Recipe> colRecipes = _itemsView.ItemsSource as ObservableCollection<Recipe>;

					if (colRecipes != null)
					{
						string uriString;
						string description;

						if (colRecipes == _colRecipes || colRecipes == _colSmallUniformRecipes)
						{
							uriString = string.Format("ms-appx:///Images/vette{0}.jpg", colRecipes.Count % 126 + 1);
							description = colRecipes.Count.ToString() + " - " + _lorem.Substring(0, rnd.Next(50, 350));
						}
						else
						{
							uriString = string.Format("ms-appx:///Images/Rect{0}.png", colRecipes.Count % 6);
							description = colRecipes.Count.ToString();
						}

						var recipe = new Recipe()
						{
							ImageUri = new Uri(uriString),
							Id = colRecipes.Count,
							Description = description
						};

						colRecipes.Insert(newItemIndex, recipe);
					}
					else
					{
						ObservableCollection<ItemContainer> colItemContainers = _itemsView.ItemsSource as ObservableCollection<ItemContainer>;

						if (colItemContainers != null)
						{
							ItemContainer itemContainer = GetItemContainerAsItemsSourceItem(colItemContainers.Count);

							colItemContainers.Insert(newItemIndex, itemContainer);
						}
						else
						{
							ObservableCollection<Image> colImages = _itemsView.ItemsSource as ObservableCollection<Image>;

							if (colImages != null)
							{
								Image image = GetImageAsItemsSourceItem(colImages.Count);

								colImages.Insert(newItemIndex, image);
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void DataSourceRemoveAllItems()
	{
		try
		{
			if (_itemsView != null && _itemsView.ItemsSource != null)
			{
				if (_itemsView.ItemsSource == _lstRecipes)
				{
					_lstRecipes.Clear();
				}
				else if (_itemsView.ItemsSource == _colSmallRecipes)
				{
					_colSmallRecipes.Clear();
				}
				else if (_itemsView.ItemsSource == _colSmallUniformRecipes)
				{
					_colSmallUniformRecipes.Clear();
				}
				else if (_itemsView.ItemsSource == _colRecipes)
				{
					_colRecipes.Clear();
				}
				else if (_itemsView.ItemsSource == _colSmallItemContainers)
				{
					_colSmallItemContainers.Clear();
				}
				else if (_itemsView.ItemsSource == _colSmallImages)
				{
					_colSmallImages.Clear();
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void DataSourceRemoveItem(int oldItemIndex)
	{
		try
		{
			if (_itemsView != null && _itemsView.ItemsSource != null)
			{
				if (_itemsView.ItemsSource == _lstRecipes)
				{
					_lstRecipes.RemoveAt(oldItemIndex);
				}
				else if (_itemsView.ItemsSource == _colSmallRecipes)
				{
					_colSmallRecipes.RemoveAt(oldItemIndex);
				}
				else if (_itemsView.ItemsSource == _colSmallUniformRecipes)
				{
					_colSmallUniformRecipes.RemoveAt(oldItemIndex);
				}
				else if (_itemsView.ItemsSource == _colRecipes)
				{
					_colRecipes.RemoveAt(oldItemIndex);
				}
				else if (_itemsView.ItemsSource == _colSmallItemContainers)
				{
					_colSmallItemContainers.RemoveAt(oldItemIndex);
				}
				else if (_itemsView.ItemsSource == _colSmallImages)
				{
					_colSmallImages.RemoveAt(oldItemIndex);
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void DataSourceReplaceItem(int itemIndex)
	{
		try
		{
			if (_itemsView != null && _itemsView.ItemsSource != null)
			{
				var rnd = new Random();

				if (_itemsView.ItemsSource == _lstRecipes)
				{
					var recipe = new Recipe()
					{
						ImageUri = new Uri(string.Format("ms-appx:///Images/vette{0}.jpg", _lstRecipes.Count % 126 + 1)),
						Id = _lstRecipes.Count,
						Description = _lstRecipes.Count + " - " + _lorem.Substring(0, rnd.Next(25, 125))
					};

					_lstRecipes[itemIndex] = recipe;
				}
				else
				{
					ObservableCollection<Recipe> colRecipes = _itemsView.ItemsSource as ObservableCollection<Recipe>;

					if (colRecipes != null)
					{
						string uriString;
						string description;

						if (colRecipes == _colRecipes || colRecipes == _colSmallUniformRecipes)
						{
							uriString = string.Format("ms-appx:///Images/vette{0}.jpg", colRecipes.Count % 126 + 1);
							description = colRecipes.Count.ToString() + " - " + _lorem.Substring(0, rnd.Next(50, 350));
						}
						else
						{
							uriString = string.Format("ms-appx:///Images/Rect{0}.png", colRecipes.Count % 6);
							description = colRecipes.Count.ToString();
						}

						var recipe = new Recipe()
						{
							ImageUri = new Uri(uriString),
							Id = colRecipes.Count,
							Description = description
						};

						colRecipes[itemIndex] = recipe;
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void DataSourceReplaceItemContent(int itemIndex)
	{
		try
		{
			if (_itemsView != null)
			{
				ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

				if (itemsRepeater != null)
				{
					UIElement element = GetItemsRepeaterElement(itemsRepeater, itemIndex);

					if (element is ItemContainer || element is ContentControl)
					{
						Border border = new Border()
						{
							BorderThickness = new Thickness(4),
							Background = _whiteBrush,
							BorderBrush = _redBrush
						};

						ItemContainer itemContainer = element as ItemContainer;

						if (itemContainer != null)
						{
							border.Width = 2 * itemContainer.ActualWidth;
							border.Height = itemContainer.ActualHeight;
							itemContainer.Child = border;
						}
						else
						{
							ContentControl contentControl = element as ContentControl;

							border.Width = 2 * contentControl.ActualWidth;
							border.Height = contentControl.ActualHeight;
							contentControl.Content = border;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnDataSourceAddItem_Click(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage("DataSourceAdd");
		DataSourceAddItem();
	}

	private void BtnQueueDataSourceAddItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			AppendAsyncEventMessage("Queued DataSourceAdd");
			_lstQueuedOperations.Add(new QueuedOperation(QueuedOperationType.DataSourceAdd));
			_queuedOperationsTimer.Start();
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnDataSourceInsertItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (txtDataSourceItemIndex != null)
			{
				int newItemIndex = int.Parse(txtDataSourceItemIndex.Text);

				AppendAsyncEventMessage("DataSourceInsert " + newItemIndex);
				DataSourceInsertItem(newItemIndex);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnQueueDataSourceInsertItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (txtDataSourceItemIndex != null)
			{
				int newItemIndex = int.Parse(txtDataSourceItemIndex.Text);

				AppendAsyncEventMessage("Queued DataSourceInsert " + newItemIndex);
				_lstQueuedOperations.Add(new QueuedOperation(QueuedOperationType.DataSourceInsert, newItemIndex));
				_queuedOperationsTimer.Start();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnDataSourceRemoveAllItems_Click(object sender, RoutedEventArgs e)
	{
		AppendAsyncEventMessage("DataSourceRemoveAll");
		DataSourceRemoveAllItems();
	}

	private void BtnQueueDataSourceRemoveAllItems_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			AppendAsyncEventMessage("Queued DataSourceRemoveAll");
			_lstQueuedOperations.Add(new QueuedOperation(QueuedOperationType.DataSourceRemoveAll));
			_queuedOperationsTimer.Start();
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnDataSourceRemoveItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (txtDataSourceItemIndex != null)
			{
				int oldItemIndex = int.Parse(txtDataSourceItemIndex.Text);

				AppendAsyncEventMessage("DataSourceRemove " + oldItemIndex);
				DataSourceRemoveItem(oldItemIndex);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnQueueDataSourceRemoveItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (txtDataSourceItemIndex != null)
			{
				int oldItemIndex = int.Parse(txtDataSourceItemIndex.Text);

				AppendAsyncEventMessage("Queued DataSourceRemove " + oldItemIndex);
				_lstQueuedOperations.Add(new QueuedOperation(QueuedOperationType.DataSourceRemove, oldItemIndex));
				_queuedOperationsTimer.Start();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnDataSourceReplaceItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (txtDataSourceItemIndex != null)
			{
				int itemIndex = int.Parse(txtDataSourceItemIndex.Text);

				AppendAsyncEventMessage("DataSourceReplace " + itemIndex);
				DataSourceReplaceItem(itemIndex);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnDataSourceReplaceItemContent_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (txtDataSourceItemIndex != null)
			{
				int itemIndex = int.Parse(txtDataSourceItemIndex.Text);

				AppendAsyncEventMessage("DataSourceReplace Content " + itemIndex);
				DataSourceReplaceItemContent(itemIndex);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnQueueDataSourceReplaceItem_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (txtDataSourceItemIndex != null)
			{
				int itemIndex = int.Parse(txtDataSourceItemIndex.Text);

				AppendAsyncEventMessage("Queued DataSourceReplace " + itemIndex);
				_lstQueuedOperations.Add(new QueuedOperation(QueuedOperationType.DataSourceReplace, itemIndex));
				_queuedOperationsTimer.Start();
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnParentMarkupItemsView_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			root.Children.Add(markupItemsView);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnUnparentMarkupItemsView_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			root.Children.Remove(markupItemsView);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnUnparentReparentMarkupItemsView_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			root.Children.Remove(markupItemsView);
			root.Children.Add(markupItemsView);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnCreateDynamicItemsView_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			_dynamicItemsView = new ItemsView();
			_dynamicItemsView.Name = "dynamicItemsView";
			_dynamicItemsView.Width = 300.0;
			_dynamicItemsView.Height = 400.0;
			_dynamicItemsView.Margin = new Thickness(1);
			_dynamicItemsView.Background = new SolidColorBrush(Colors.HotPink);
			_dynamicItemsView.VerticalAlignment = VerticalAlignment.Top;
			Grid.SetRow(_dynamicItemsView, 1);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnParentDynamicItemsView_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			root.Children.Add(_dynamicItemsView);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnUnparentDynamicItemsView_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			root.Children.Remove(_dynamicItemsView);
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnUseMarkupItemsView_Click(object sender, RoutedEventArgs e)
	{
		UseItemsView(markupItemsView);
	}

	private void BtnUseDynamicItemsView_Click(object sender, RoutedEventArgs e)
	{
		UseItemsView(_dynamicItemsView);
	}

	private void BtnReleaseDynamicItemsView_Click(object sender, RoutedEventArgs e)
	{
		_dynamicItemsView = null;

		GC.Collect();
		GC.WaitForPendingFinalizers();
	}

	private void BtnShareLinedFlowLayout_Click(object sender, RoutedEventArgs e)
	{
		try
		{
			if (_linedFlowLayout != null && _dynamicItemsView != null)
			{
				_dynamicItemsView.Layout = _linedFlowLayout;
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void BtnExecuteQueuedOperations_Click(object sender, RoutedEventArgs e)
	{
		ExecuteQueuedOperations();
	}

	private void BtnDiscardQueuedOperations_Click(object sender, RoutedEventArgs e)
	{
		_lstQueuedOperations.Clear();
		AppendAsyncEventMessage("Queued operations cleared.");
	}

	private void BtnGetFullLog_Click(object sender, RoutedEventArgs e)
	{
		GetFullLog();
	}

	private void BtnClearFullLog_Click(object sender, RoutedEventArgs e)
	{
		ClearFullLog();
	}

	private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
	{
		lstLogs.Items.Clear();
	}

	private void BtnCopyItemsViewEvents_Click(object sender, RoutedEventArgs e)
	{
		string logs = string.Empty;

		foreach (object log in lstLogs.Items)
		{
			logs += log.ToString() + "\n";
		}

		var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
		dataPackage.SetText(logs);
		Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);
	}

	private void QueuedOperationsTimer_Tick(object sender, object e)
	{
		_queuedOperationsTimer.Stop();
		ExecuteQueuedOperations();
	}

	private void ExecuteQueuedOperations()
	{
		try
		{
			while (_lstQueuedOperations.Count > 0)
			{
				QueuedOperation qo = _lstQueuedOperations[0];

				switch (qo.Type)
				{
					case QueuedOperationType.SetWidth:
						ItemsViewSetWidth(qo.DoubleValue);
						break;
					case QueuedOperationType.SetHeight:
						ItemsViewSetHeight(qo.DoubleValue);
						break;
					case QueuedOperationType.StartBringItemIntoView:
						ItemsViewStartBringItemIntoView();
						break;
					case QueuedOperationType.DataSourceAdd:
						DataSourceAddItem();
						break;
					case QueuedOperationType.DataSourceInsert:
						DataSourceInsertItem(qo.IntValue);
						break;
					case QueuedOperationType.DataSourceRemove:
						DataSourceRemoveItem(qo.IntValue);
						break;
					case QueuedOperationType.DataSourceRemoveAll:
						DataSourceRemoveAllItems();
						break;
					case QueuedOperationType.DataSourceReplace:
						DataSourceReplaceItem(qo.IntValue);
						break;
				}

				_lstQueuedOperations.RemoveAt(0);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}
	}

	private void ChkLogScrollViewEvents_Checked(object sender, RoutedEventArgs e)
	{
		if (_itemsView != null)
		{
			HookScrollViewEvents(ItemsViewTestHooks.GetScrollViewPart(_itemsView));
		}
	}

	private void ChkLogScrollViewEvents_Unchecked(object sender, RoutedEventArgs e)
	{
		if (_itemsView != null)
		{
			UnhookScrollViewEvents(ItemsViewTestHooks.GetScrollViewPart(_itemsView));
		}
	}

	private void ChkLogItemsViewEvents_Checked(object sender, RoutedEventArgs e)
	{
		HookItemsViewEvents();
	}

	private void ChkLogItemsViewEvents_Unchecked(object sender, RoutedEventArgs e)
	{
		UnhookItemsViewEvents();
	}

	private void ChkLogLinedFlowLayoutMessages_Checked(object sender, RoutedEventArgs e)
	{
		//LayoutsTestHooks.LinedFlowLayoutSnappedAverageItemsPerLineChanged += LayoutsTestHooks_LinedFlowLayoutSnappedAverageItemsPerLineChanged;
		//LayoutsTestHooks.LinedFlowLayoutInvalidated += LayoutsTestHooks_LinedFlowLayoutInvalidated;
		//LayoutsTestHooks.LinedFlowLayoutItemLocked += LayoutsTestHooks_LinedFlowLayoutItemLocked;

		//MUXControlsTestHooks.SetLoggingLevelForType("LinedFlowLayout", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogItemsViewMessages.IsChecked == false &&
		//	chkLogScrollViewMessages.IsChecked == false &&
		//	chkLogItemsRepeaterMessages.IsChecked == false &&
		//	chkLogItemContainerMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogLinedFlowLayoutMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//LayoutsTestHooks.LinedFlowLayoutSnappedAverageItemsPerLineChanged -= LayoutsTestHooks_LinedFlowLayoutSnappedAverageItemsPerLineChanged;
		//LayoutsTestHooks.LinedFlowLayoutInvalidated -= LayoutsTestHooks_LinedFlowLayoutInvalidated;
		//LayoutsTestHooks.LinedFlowLayoutItemLocked -= LayoutsTestHooks_LinedFlowLayoutItemLocked;

		//MUXControlsTestHooks.SetLoggingLevelForType("LinedFlowLayout", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogItemsViewMessages.IsChecked == false &&
		//	chkLogScrollViewMessages.IsChecked == false &&
		//	chkLogItemsRepeaterMessages.IsChecked == false &&
		//	chkLogItemContainerMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogItemsRepeaterMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ItemsRepeater", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogItemsViewMessages.IsChecked == false &&
		//	chkLogScrollViewMessages.IsChecked == false &&
		//	chkLogLinedFlowLayoutMessages.IsChecked == false &&
		//	chkLogItemContainerMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogItemsRepeaterMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ItemsRepeater", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogItemsViewMessages.IsChecked == false &&
		//	chkLogScrollViewMessages.IsChecked == false &&
		//	chkLogLinedFlowLayoutMessages.IsChecked == false &&
		//	chkLogItemContainerMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogItemsViewMessages.IsChecked == false &&
		//	chkLogItemsRepeaterMessages.IsChecked == false &&
		//	chkLogLinedFlowLayoutMessages.IsChecked == false &&
		//	chkLogItemContainerMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogScrollViewMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ScrollView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogItemsViewMessages.IsChecked == false &&
		//	chkLogItemsRepeaterMessages.IsChecked == false &&
		//	chkLogLinedFlowLayoutMessages.IsChecked == false &&
		//	chkLogItemContainerMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogItemContainerMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ItemContainer", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogScrollViewMessages.IsChecked == false &&
		//	chkLogItemsRepeaterMessages.IsChecked == false &&
		//	chkLogLinedFlowLayoutMessages.IsChecked == false &&
		//	chkLogItemsViewMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogItemContainerMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ItemContainer", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogScrollViewMessages.IsChecked == false &&
		//	chkLogItemsRepeaterMessages.IsChecked == false &&
		//	chkLogLinedFlowLayoutMessages.IsChecked == false &&
		//	chkLogItemsViewMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogItemsViewMessages_Checked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ItemsView", isLoggingInfoLevel: true, isLoggingVerboseLevel: true);
		//if (chkLogScrollViewMessages.IsChecked == false &&
		//	chkLogItemsRepeaterMessages.IsChecked == false &&
		//	chkLogLinedFlowLayoutMessages.IsChecked == false &&
		//	chkLogItemContainerMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage += MUXControlsTestHooks_LoggingMessage;
	}

	private void ChkLogItemsViewMessages_Unchecked(object sender, RoutedEventArgs e)
	{
		//MUXControlsTestHooks.SetLoggingLevelForType("ItemsView", isLoggingInfoLevel: false, isLoggingVerboseLevel: false);
		//if (chkLogScrollViewMessages.IsChecked == false &&
		//	chkLogItemsRepeaterMessages.IsChecked == false &&
		//	chkLogLinedFlowLayoutMessages.IsChecked == false &&
		//	chkLogItemContainerMessages.IsChecked == false)
		//	MUXControlsTestHooks.LoggingMessage -= MUXControlsTestHooks_LoggingMessage;
	}

	private void HookLinedFlowLayoutEvents(LinedFlowLayout linedFlowLayout)
	{
		if (linedFlowLayout != null)
		{
			linedFlowLayout.ItemsInfoRequested += LinedFlowLayout_ItemsInfoRequested;
			linedFlowLayout.ItemsUnlocked += LinedFlowLayout_ItemsUnlocked;
		}
	}

	private void UnhookLinedFlowLayoutEvents(LinedFlowLayout linedFlowLayout)
	{
		if (linedFlowLayout != null)
		{
			linedFlowLayout.ItemsInfoRequested -= LinedFlowLayout_ItemsInfoRequested;
			linedFlowLayout.ItemsUnlocked -= LinedFlowLayout_ItemsUnlocked;
		}
	}

	private void HookItemsRepeaterEvents(ItemsRepeater itemsRepeater)
	{
		if (itemsRepeater != null)
		{
			//Uncomment if needed for debugging purposes
			//itemsRepeater.LayoutUpdated += ItemsRepeater_LayoutUpdated;

			itemsRepeater.Loaded += ItemsRepeater_Loaded;
			itemsRepeater.SizeChanged += ItemsRepeater_SizeChanged;
			itemsRepeater.ElementPrepared += ItemsRepeater_ElementPrepared;
			itemsRepeater.ElementClearing += ItemsRepeater_ElementClearing;
		}
	}

	private void UnhookItemsRepeaterEvents(ItemsRepeater itemsRepeater)
	{
		if (itemsRepeater != null)
		{
			itemsRepeater.LayoutUpdated -= ItemsRepeater_LayoutUpdated;
			itemsRepeater.Loaded -= ItemsRepeater_Loaded;
			itemsRepeater.SizeChanged -= ItemsRepeater_SizeChanged;
			itemsRepeater.ElementPrepared -= ItemsRepeater_ElementPrepared;
			itemsRepeater.ElementClearing -= ItemsRepeater_ElementClearing;
		}
	}

	private void HookScrollViewEvents(ScrollView scrollView)
	{
		if (scrollView != null)
		{
			scrollView.Loaded += ScrollView_Loaded;
			scrollView.SizeChanged += ScrollView_SizeChanged;
			scrollView.ExtentChanged += ScrollView_ExtentChanged;
			scrollView.StateChanged += ScrollView_StateChanged;
			scrollView.ViewChanged += ScrollView_ViewChanged;
			scrollView.ScrollAnimationStarting += ScrollView_ScrollAnimationStarting;
			scrollView.ZoomAnimationStarting += ScrollView_ZoomAnimationStarting;
			scrollView.ScrollCompleted += ScrollView_ScrollCompleted;
			scrollView.ZoomCompleted += ScrollView_ZoomCompleted;

			if (_scrollViewPointerWheelChangedEventHandler == null)
			{
				_scrollViewPointerWheelChangedEventHandler = new PointerEventHandler(ScrollView_PointerWheelChanged);
			}
			scrollView.AddHandler(UIElement.PointerWheelChangedEvent, _scrollViewPointerWheelChangedEventHandler, true);
		}
	}

	private void UnhookScrollViewEvents(ScrollView scrollView)
	{
		if (scrollView != null)
		{
			scrollView.Loaded -= ScrollView_Loaded;
			scrollView.SizeChanged -= ScrollView_SizeChanged;
			scrollView.ExtentChanged -= ScrollView_ExtentChanged;
			scrollView.StateChanged -= ScrollView_StateChanged;
			scrollView.ViewChanged -= ScrollView_ViewChanged;
			scrollView.ScrollAnimationStarting -= ScrollView_ScrollAnimationStarting;
			scrollView.ZoomAnimationStarting -= ScrollView_ZoomAnimationStarting;
			scrollView.ScrollCompleted -= ScrollView_ScrollCompleted;
			scrollView.ZoomCompleted -= ScrollView_ZoomCompleted;

			if (_scrollViewPointerWheelChangedEventHandler != null)
			{
				scrollView.RemoveHandler(UIElement.PointerWheelChangedEvent, _scrollViewPointerWheelChangedEventHandler);
			}
		}
	}

	private void HookItemsViewEvents()
	{
		if (_itemsView != null)
		{
			_itemsView.ItemInvoked += ItemsView_ItemInvoked;
			_itemsView.GettingFocus += ItemsView_GettingFocus;
			_itemsView.GotFocus += ItemsView_GotFocus;
			_itemsView.LosingFocus += ItemsView_LosingFocus;
			_itemsView.LostFocus += ItemsView_LostFocus;
			_itemsView.Loaded += ItemsView_Loaded;
			_itemsView.SelectionChanged += ItemsView_SelectionChanged;
			_itemsView.SizeChanged += ItemsView_SizeChanged;

			if (_itemsViewPointerWheelChangedEventHandler == null)
			{
				_itemsViewPointerWheelChangedEventHandler = new PointerEventHandler(ItemsView_PointerWheelChanged);
			}
			_itemsView.AddHandler(UIElement.PointerWheelChangedEvent, _itemsViewPointerWheelChangedEventHandler, true);
		}
	}

	private void UnhookItemsViewEvents()
	{
		if (_itemsView != null)
		{
			_itemsView.ItemInvoked -= ItemsView_ItemInvoked;
			_itemsView.GettingFocus -= ItemsView_GettingFocus;
			_itemsView.GotFocus -= ItemsView_GotFocus;
			_itemsView.LosingFocus -= ItemsView_LosingFocus;
			_itemsView.LostFocus -= ItemsView_LostFocus;
			_itemsView.Loaded -= ItemsView_Loaded;
			_itemsView.SelectionChanged -= ItemsView_SelectionChanged;
			_itemsView.SizeChanged -= ItemsView_SizeChanged;

			if (_itemsViewPointerWheelChangedEventHandler != null)
			{
				_itemsView.RemoveHandler(UIElement.PointerWheelChangedEvent, _itemsViewPointerWheelChangedEventHandler);
			}
		}
	}

	//private void LayoutsTestHooks_LinedFlowLayoutSnappedAverageItemsPerLineChanged(object sender, object args)
	//{
	//	AppendAsyncEventMessage("LinedFlowLayout snapped average items per line changed. SnappedAverageItemsPerLine: " + LayoutsTestHooks.GetLinedFlowLayoutSnappedAverageItemsPerLine(sender as LinedFlowLayout).ToString() + ", AverageItemAspectRatio: " + LayoutsTestHooks.GetLinedFlowLayoutAverageItemAspectRatio(sender as LinedFlowLayout).ToString());
	//}

	//private void LayoutsTestHooks_LinedFlowLayoutInvalidated(object sender, LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs args)
	//{
	//	AppendAsyncEventMessage("LinedFlowLayout invalidated. InvalidationTrigger: " + args.InvalidationTrigger.ToString());
	//}

	//private void LayoutsTestHooks_LinedFlowLayoutItemLocked(object sender, LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs args)
	//{
	//	AppendAsyncEventMessage("LinedFlowLayout item locked. ItemIndex: " + args.ItemIndex.ToString() + ", LineIndex: " + args.LineIndex.ToString());
	//}

	//private void LayoutsTestHooks_LinedFlowLayoutItemLocked2(object sender, LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs args)
	//{
	//	if (!_lstLinedFlowLayoutLockedItemIndexes.Contains(args.ItemIndex))
	//	{
	//		_lstLinedFlowLayoutLockedItemIndexes.Add(args.ItemIndex);
	//		UpdateLinedFlowLayoutLockedItemsVisuals();
	//	}
	//}

	private void ItemsView_CurrentItemIndexChanged(DependencyObject o, DependencyProperty p)
	{
		UpdateItemsViewCurrentElementVisual(log: true);
	}

	private void ItemsViewTestHooks_KeyboardNavigationReferenceOffsetChanged(ItemsView itemsView, object args)
	{
		UpdateItemsViewKeyboardNavigationReferenceOffsetVisual(log: true);

		if ((bool)chkShowCurrentElement.IsChecked)
		{
			UpdateItemsViewCurrentElementVisual(log: false);
		}
	}

	//private void MUXControlsTestHooks_LoggingMessage(object sender, MUXControlsTestHooksLoggingMessageEventArgs args)
	//{
	//	// Cut off the terminating new line.
	//	string msg = args.Message.Substring(0, args.Message.Length - 1);
	//	string asyncEventMessage;
	//	string senderName = string.Empty;

	//	try
	//	{
	//		FrameworkElement fe = sender as FrameworkElement;

	//		if (fe != null)
	//		{
	//			senderName = "s:" + fe.Name + ", ";
	//		}
	//	}
	//	catch
	//	{
	//	}

	//	if (args.IsVerboseLevel)
	//	{
	//		asyncEventMessage = "Verbose: " + senderName + "m:" + msg;
	//	}
	//	else
	//	{
	//		asyncEventMessage = "Info: " + senderName + "m:" + msg;
	//	}

	//	AppendAsyncEventMessage(asyncEventMessage);
	//}

	private void AppendAsyncEventMessage(string asyncEventMessage)
	{
		lock (_asyncEventReportingLock)
		{
			while (asyncEventMessage.Length > 0)
			{
				string msgHead = asyncEventMessage;

				if (asyncEventMessage.Length > 110)
				{
					int commaIndex = asyncEventMessage.IndexOf(',', 110);
					if (commaIndex != -1)
					{
						msgHead = asyncEventMessage.Substring(0, commaIndex);
						asyncEventMessage = asyncEventMessage.Substring(commaIndex + 1);
					}
					else
					{
						asyncEventMessage = string.Empty;
					}
				}
				else
				{
					asyncEventMessage = string.Empty;
				}

				_lstAsyncEventMessage.Add(msgHead);
			}
			var ignored = this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, AppendAsyncEventMessage);

		}
	}

	private void AppendAsyncEventMessage()
	{
		lock (_asyncEventReportingLock)
		{
			foreach (string asyncEventMessage in _lstAsyncEventMessage)
			{
				AppendEventMessage(asyncEventMessage);
			}
			_lstAsyncEventMessage.Clear();
		}
	}

	private void AppendEventMessage(string eventMessage)
	{
		lstLogs.Items.Add(eventMessage);
		_fullLogs.Add(eventMessage);

		if ((bool)chkOutputDebugString.IsChecked)
		{
			System.Diagnostics.Debug.WriteLine(eventMessage);
		}
	}

	private void GetFullLog()
	{
		this.txtStatus.Text = "GetFullLog. Populating cmbFullLog...";
		chkLogCleared.IsChecked = false;
		foreach (string log in _fullLogs)
		{
			this.cmbFullLog.Items.Add(log);
		}
		chkLogUpdated.IsChecked = true;
		this.txtStatus.Text = "GetFullLog. Done.";
	}

	private void ClearFullLog()
	{
		this.txtStatus.Text = "ClearFullLog. Clearing cmbFullLog & fullLogs...";
		chkLogUpdated.IsChecked = false;
		_fullLogs.Clear();
		this.cmbFullLog.Items.Clear();
		chkLogCleared.IsChecked = true;
		this.txtStatus.Text = "ClearFullLog. Done.";
	}

	private void BtnClearExceptionReport_Click(object sender, RoutedEventArgs e)
	{
		txtExceptionReport.Text = string.Empty;
	}

	private UIElement GetItemsRepeaterMethodElement(ItemsRepeater itemsRepeater)
	{
		try
		{
			if (itemsRepeater != null)
			{
				int index = -1;

				int.TryParse(txtItemsRepeaterElementMethodIndex.Text, out index);

				return GetItemsRepeaterElement(itemsRepeater, index);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}

		return null;
	}

	private UIElement GetItemsRepeaterPropertyElement(ItemsRepeater itemsRepeater)
	{
		try
		{
			if (itemsRepeater != null)
			{
				int index = -1;

				int.TryParse(txtItemsRepeaterElementPropertyIndex.Text, out index);

				return GetItemsRepeaterElement(itemsRepeater, index);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}

		return null;
	}

	private UIElement GetLinedFlowLayoutMethodElement(ItemsRepeater itemsRepeater)
	{
		try
		{
			if (itemsRepeater != null)
			{
				int index = -1;

				int.TryParse(txtLinedFlowLayoutMethodIndex.Text, out index);

				return GetItemsRepeaterElement(itemsRepeater, index);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}

		return null;
	}

	private UIElement GetItemsRepeaterElement(ItemsRepeater itemsRepeater, int index)
	{
		try
		{
			if (itemsRepeater != null)
			{
				return itemsRepeater.TryGetElement(index);
			}
		}
		catch (Exception ex)
		{
			txtExceptionReport.Text = ex.ToString();
			AppendEventMessage(ex.ToString());
		}

		return null;
	}

	private int GetItemsRepeaterElementIndex(FrameworkElement element)
	{
		if (_itemsView != null)
		{
			ItemsRepeater itemsRepeater = ItemsViewTestHooks.GetItemsRepeaterPart(_itemsView);

			if (itemsRepeater != null)
			{
				FrameworkElement parent1 = element;
				FrameworkElement parent2 = VisualTreeHelper.GetParent(parent1) as FrameworkElement;

				while (parent1 != null && parent2 != itemsRepeater)
				{
					parent1 = parent2;
					parent2 = VisualTreeHelper.GetParent(parent1) as FrameworkElement;
				}

				if (parent1 != null)
				{
					return itemsRepeater.GetElementIndex(parent1);
				}
			}
		}

		return -1;
	}

	private ItemContainer GetItemContainerAsItemsSourceItem(int itemIndex)
	{
		Grid grid = new Grid()
		{
			Name = "itemPanel"
		};

		BitmapImage bitmapImage = new BitmapImage()
		{
			DecodePixelHeight = 96,
			UriSource = new Uri(string.Format("ms-appx:///Images/Rect{0}.png", itemIndex % 6))
		};

		Image image = new Image()
		{
			Name = "image",
			Stretch = Stretch.UniformToFill,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Source = bitmapImage
		};

		grid.Children.Add(image);

		TextBlock textBlock = new TextBlock()
		{
			Text = itemIndex.ToString(),
			TextWrapping = TextWrapping.Wrap,
			Margin = new Thickness(4),
			Foreground = new SolidColorBrush(Colors.Yellow),
			FontSize = 14.0,
			MaxWidth = 68.0,
			MaxHeight = 48.0,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Bottom
		};

		grid.Children.Add(textBlock);

		ItemContainer itemContainer = new ItemContainer()
		{
			MinWidth = 72.0,
			Height = 96.0,
			Background = new SolidColorBrush(Colors.Gray),
			Child = grid
		};

		return itemContainer;
	}

	private Image GetImageAsItemsSourceItem(int itemIndex)
	{
		BitmapImage bitmapImage = new BitmapImage()
		{
			DecodePixelHeight = 96,
			UriSource = new Uri(string.Format("ms-appx:///Images/Rect{0}.png", itemIndex % 6))
		};

		Image image = new Image()
		{
			Name = "image",
			MinWidth = 72.0,
			Height = 96.0,
			Stretch = Stretch.UniformToFill,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			Source = bitmapImage
		};

		return image;
	}

	private Thickness GetThicknessFromString(string thickness)
	{
		string[] lengths = thickness.Split(',');
		if (lengths.Length < 4)
			return new Thickness(
				Convert.ToDouble(lengths[0]));
		else
			return new Thickness(
				Convert.ToDouble(lengths[0]), Convert.ToDouble(lengths[1]), Convert.ToDouble(lengths[2]), Convert.ToDouble(lengths[3]));
	}

	private CornerRadius GetCornerRadiusFromString(string cornerRadius)
	{
		string[] lengths = cornerRadius.Split(',');
		if (lengths.Length < 4)
			return new CornerRadius(
				Convert.ToDouble(lengths[0]));
		else
			return new CornerRadius(
				Convert.ToDouble(lengths[0]), Convert.ToDouble(lengths[1]), Convert.ToDouble(lengths[2]), Convert.ToDouble(lengths[3]));
	}
}

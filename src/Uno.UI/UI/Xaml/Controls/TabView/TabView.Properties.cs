// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\Generated\TabView.properties.cpp, commit 65718e2813

using System.Collections.Generic;
using System.Windows.Input;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

public partial class TabView
{
	/// <summary>
	/// Gets or sets the command to invoke when the add (+) button is tapped.
	/// </summary>
	public ICommand AddTabButtonCommand
	{
		get => (ICommand)GetValue(AddTabButtonCommandProperty);
		set => SetValue(AddTabButtonCommandProperty, value);
	}

	/// <summary>
	/// Identifies the AddButtonCommand dependency property.
	/// </summary>
	public static DependencyProperty AddTabButtonCommandProperty { get; } =
		DependencyProperty.Register(nameof(AddTabButtonCommand), typeof(ICommand), typeof(TabView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the parameter to pass to the AddTabButtonCommand property.
	/// </summary>
	public object AddTabButtonCommandParameter
	{
		get => (object)GetValue(AddTabButtonCommandParameterProperty);
		set => SetValue(AddTabButtonCommandParameterProperty, value);
	}

	/// <summary>
	/// Identifies the AddTabButtonCommandParameter dependency property.
	/// </summary>
	public static DependencyProperty AddTabButtonCommandParameterProperty { get; } =
		DependencyProperty.Register(nameof(AddTabButtonCommandParameter), typeof(object), typeof(TabView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value that determines whether the TabView can be a drop target for the purposes of drag-and-drop operations.
	/// </summary>
	public bool AllowDropTabs
	{
		get => (bool)GetValue(AllowDropTabsProperty);
		set => SetValue(AllowDropTabsProperty, value);
	}

	/// <summary>
	/// Identifies the AllowDropTabs dependency property.
	/// </summary>
	public static DependencyProperty AllowDropTabsProperty { get; } =
		DependencyProperty.Register(nameof(AllowDropTabs), typeof(bool), typeof(TabView), new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets a value that indicates whether tabs can be dragged as a data payload.
	/// </summary>
	public bool CanDragTabs
	{
		get => (bool)GetValue(CanDragTabsProperty);
		set => SetValue(CanDragTabsProperty, value);
	}

	/// <summary>
	/// Gets or sets a value that indicates whether tabs can be dragged as a data payload.
	/// </summary>
	public static DependencyProperty CanDragTabsProperty { get; } =
		DependencyProperty.Register(nameof(CanDragTabs), typeof(bool), typeof(TabView), new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value that indicates whether the tabs in the TabStrip can be reordered through user interaction.
	/// </summary>
	public bool CanReorderTabs
	{
		get => (bool)GetValue(CanReorderTabsProperty);
		set => SetValue(CanReorderTabsProperty, value);
	}

	/// <summary>
	/// Identifies the CanReorderTabs dependency property.
	/// </summary>
	public static DependencyProperty CanReorderTabsProperty { get; } =
		DependencyProperty.Register(nameof(CanReorderTabs), typeof(bool), typeof(TabView), new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets a value indicating whether tabs can be torn off to create new windows.
	/// </summary>
	public bool CanTearOutTabs
	{
		get => (bool)GetValue(CanTearOutTabsProperty);
		set => SetValue(CanTearOutTabsProperty, value);
	}

	/// <summary>
	/// Identifies the CanTearOutTabs dependency property.
	/// </summary>
	public static DependencyProperty CanTearOutTabsProperty { get; } =
		DependencyProperty.Register(nameof(CanTearOutTabs), typeof(bool), typeof(TabView), new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets a value that indicates the behavior of the close button within tabs.
	/// </summary>
	public TabViewCloseButtonOverlayMode CloseButtonOverlayMode
	{
		get => (TabViewCloseButtonOverlayMode)GetValue(CloseButtonOverlayModeProperty);
		set => SetValue(CloseButtonOverlayModeProperty, value);
	}

	/// <summary>
	/// Identifies the CloseButtonOverlayMode dependency property.
	/// </summary>
	public static DependencyProperty CloseButtonOverlayModeProperty { get; } =
		DependencyProperty.Register(nameof(CloseButtonOverlayMode), typeof(TabViewCloseButtonOverlayMode), typeof(TabView), new FrameworkPropertyMetadata(TabViewCloseButtonOverlayMode.Auto, OnCloseButtonOverlayModePropertyChanged));

	/// <summary>
	/// Gets or sets whether the add (+) tab button is visible.
	/// </summary>
	public bool IsAddTabButtonVisible
	{
		get => (bool)GetValue(IsAddTabButtonVisibleProperty);
		set => SetValue(IsAddTabButtonVisibleProperty, value);
	}

	/// <summary>
	/// Identifies the IsAddTabButtonVisible dependency property.
	/// </summary>
	public static DependencyProperty IsAddTabButtonVisibleProperty { get; } =
		DependencyProperty.Register(nameof(IsAddTabButtonVisible), typeof(bool), typeof(TabView), new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets the index of the selected item.
	/// </summary>
	public int SelectedIndex
	{
		get => (int)GetValue(SelectedIndexProperty);
		set => SetValue(SelectedIndexProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedIndex dependency property.
	/// </summary>
	public static DependencyProperty SelectedIndexProperty { get; } =
		DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(TabView), new FrameworkPropertyMetadata(0, OnSelectedIndexPropertyChanged));

	/// <summary>
	/// Gets or sets the selected item.
	/// </summary>
	public object SelectedItem
	{
		get => (object)GetValue(SelectedItemProperty);
		set => SetValue(SelectedItemProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedItem dependency property.
	/// </summary>
	public static DependencyProperty SelectedItemProperty { get; } =
		DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(TabView), new FrameworkPropertyMetadata(null, OnSelectedItemPropertyChanged));

	/// <summary>
	/// Gets the collection used to generate the tabs within the control.
	/// </summary>
	public IList<object> TabItems
	{
		get => (IList<object>)GetValue(TabItemsProperty);
		private set => SetValue(TabItemsProperty, value);
	}

	/// <summary>
	/// Identifies the TabItems dependency property.
	/// </summary>
	public static DependencyProperty TabItemsProperty { get; } =
		DependencyProperty.Register(nameof(TabItems), typeof(IList<object>), typeof(TabView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets an object source used to generate the tabs within the TabView.
	/// </summary>
	public object TabItemsSource
	{
		get => (object)GetValue(TabItemsSourceProperty);
		set => SetValue(TabItemsSourceProperty, value);
	}

	/// <summary>
	/// Identifies the TabItemsSource dependency property.
	/// </summary>
	public static DependencyProperty TabItemsSourceProperty { get; } =
		DependencyProperty.Register(nameof(TabItemsSource), typeof(object), typeof(TabView), new FrameworkPropertyMetadata(null, OnTabItemsSourcePropertyChanged));

	/// <summary>
	/// Gets or sets the DataTemplate used to display each item.
	/// </summary>
	public DataTemplate TabItemTemplate
	{
		get => (DataTemplate)GetValue(TabItemTemplateProperty);
		set => SetValue(TabItemTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the TabItemTemplate dependency property.
	/// </summary>
	public static DependencyProperty TabItemTemplateProperty { get; } =
		DependencyProperty.Register(nameof(TabItemTemplate), typeof(DataTemplate), typeof(TabView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets a selection object that changes the DataTemplate to apply for content,
	/// based on processing information about the content item or its container at run time.
	/// </summary>
	public Microsoft.UI.Xaml.Controls.DataTemplateSelector TabItemTemplateSelector
	{
		get => (Microsoft.UI.Xaml.Controls.DataTemplateSelector)GetValue(TabItemTemplateSelectorProperty);
		set => SetValue(TabItemTemplateSelectorProperty, value);
	}

	/// <summary>
	/// Identifies the TabItemTemplateSelector dependency property.
	/// </summary>
	public static DependencyProperty TabItemTemplateSelectorProperty { get; } =
		DependencyProperty.Register(nameof(TabItemTemplateSelector), typeof(Microsoft.UI.Xaml.Controls.DataTemplateSelector), typeof(TabView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the content that is shown to the right of the tab strip.
	/// </summary>
	public object TabStripFooter
	{
		get => (object)GetValue(TabStripFooterProperty);
		set => SetValue(TabStripFooterProperty, value);
	}

	/// <summary>
	/// Identifies the TabStripFooter dependency property.
	/// </summary>
	public static DependencyProperty TabStripFooterProperty { get; } =
		DependencyProperty.Register(nameof(TabStripFooter), typeof(object), typeof(TabView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the DataTemplate used to display the content of the TabStripFooter.
	/// </summary>
	public DataTemplate TabStripFooterTemplate
	{
		get => (DataTemplate)GetValue(TabStripFooterTemplateProperty);
		set => SetValue(TabStripFooterTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the TabStripFooterTemplate dependency property.
	/// </summary>
	public static DependencyProperty TabStripFooterTemplateProperty { get; } =
		DependencyProperty.Register(nameof(TabStripFooterTemplate), typeof(DataTemplate), typeof(TabView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets the content that is shown to the left of the tab strip.
	/// </summary>
	public object TabStripHeader
	{
		get => (object)GetValue(TabStripHeaderProperty);
		set => SetValue(TabStripHeaderProperty, value);
	}

	/// <summary>
	/// Identifies the TabStripHeader dependency property.
	/// </summary>
	public static DependencyProperty TabStripHeaderProperty { get; } =
		DependencyProperty.Register(nameof(TabStripHeader), typeof(object), typeof(TabView), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the DataTemplate used to display the content of the TabStripHeader.
	/// </summary>
	public DataTemplate TabStripHeaderTemplate
	{
		get => (DataTemplate)GetValue(TabStripHeaderTemplateProperty);
		set => SetValue(TabStripHeaderTemplateProperty, value);
	}

	/// <summary>
	/// Identifies the TabStripHeaderTemplate dependency property.
	/// </summary>
	public static DependencyProperty TabStripHeaderTemplateProperty { get; } =
		DependencyProperty.Register(nameof(TabStripHeaderTemplate), typeof(DataTemplate), typeof(TabView), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Gets or sets how the tabs should be sized.
	/// </summary>
	public TabViewWidthMode TabWidthMode
	{
		get => (TabViewWidthMode)GetValue(TabWidthModeProperty);
		set => SetValue(TabWidthModeProperty, value);
	}

	/// <summary>
	/// Identifies the TabWidthMode dependency property.
	/// </summary>
	public static DependencyProperty TabWidthModeProperty { get; } =
		DependencyProperty.Register(nameof(TabWidthMode), typeof(TabViewWidthMode), typeof(TabView), new FrameworkPropertyMetadata(TabViewWidthMode.Equal, OnTabWidthModePropertyChanged));

	private static void OnCloseButtonOverlayModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TabView)sender;
		owner.OnCloseButtonOverlayModePropertyChanged(args);
	}

	private static void OnSelectedIndexPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TabView)sender;
		owner.OnSelectedIndexPropertyChanged(args);
	}

	private static void OnSelectedItemPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TabView)sender;
		owner.OnSelectedItemPropertyChanged(args);
	}

	private static void OnTabItemsSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TabView)sender;
		owner.OnTabItemsSourcePropertyChanged(args);
	}

	private static void OnTabWidthModePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		var owner = (TabView)sender;
		owner.OnTabWidthModePropertyChanged(args);
	}

	/// <summary>
	/// Occurs when the add (+) tab button has been clicked.
	/// </summary>
	public event TypedEventHandler<TabView, object> AddTabButtonClick;

	/// <summary>
	/// Occurs when an external torn out tab is dropped into the TabView.
	/// </summary>
	public event TypedEventHandler<TabView, TabViewExternalTornOutTabsDroppedEventArgs> ExternalTornOutTabsDropped;

	/// <summary>
	/// Occurs when an external torn out tab is being dropped into the TabView.
	/// </summary>
	public event TypedEventHandler<TabView, TabViewExternalTornOutTabsDroppingEventArgs> ExternalTornOutTabsDropping;

	/// <summary>
	/// Occurs when the currently selected tab changes.
	/// </summary>
	public event Microsoft.UI.Xaml.Controls.SelectionChangedEventHandler SelectionChanged;

	/// <summary>
	/// Raised when the user attempts to close a Tab via clicking the x-to-close button, CTRL+F4, or mousewheel.
	/// </summary>
	public event TypedEventHandler<TabView, TabViewTabCloseRequestedEventArgs> TabCloseRequested;

	/// <summary>
	/// Raised when the user completes the drag action.
	/// </summary>
	public event TypedEventHandler<TabView, TabViewTabDragCompletedEventArgs> TabDragCompleted;

	/// <summary>
	/// Occurs when a drag operation is initiated.
	/// </summary>
	public event TypedEventHandler<TabView, TabViewTabDragStartingEventArgs> TabDragStarting;

	/// <summary>
	/// Occurs when the user completes a drag and drop operation by dropping a tab outside of the TabStrip area.
	/// </summary>
	public event TypedEventHandler<TabView, TabViewTabDroppedOutsideEventArgs> TabDroppedOutside;

	/// <summary>
	/// Raised when the items collection has changed.
	/// </summary>
	public event TypedEventHandler<TabView, IVectorChangedEventArgs> TabItemsChanged;

	/// <summary>
	/// Occurs when the input system reports an underlying drag event with the TabStrip as the potential drop target.
	/// </summary>
	public event DragEventHandler TabStripDragOver;

	/// <summary>
	/// Occurs when the input system reports an underlying drop event with the TabStrip as the drop target.
	/// </summary>
	public event DragEventHandler TabStripDrop;

	/// <summary>
	/// Occurs when the user attempts to tear out a tab.
	/// </summary>
	public event TypedEventHandler<TabView, TabViewTabTearOutRequestedEventArgs> TabTearOutRequested;

	/// <summary>
	/// Occurs when a torn out requests a window to be created.
	/// </summary>
	public event TypedEventHandler<TabView, TabViewTabTearOutWindowRequestedEventArgs> TabTearOutWindowRequested;
}

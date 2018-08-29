# Implemented members by control - Windows.UI.Xaml.Controls

This document lists which individual properties, methods, and events are implemented and not implemented per control in the namespace Windows.UI.Xaml.Controls.

## AppBar

*Implemented for:* all platforms

**Implemented properties:** bool IsSticky; bool IsOpen; AppBarClosedDisplayMode ClosedDisplayMode; AppBarTemplateSettings TemplateSettings; DependencyProperty IsOpenProperty; DependencyProperty IsStickyProperty; DependencyProperty ClosedDisplayModeProperty; DependencyProperty LightDismissOverlayModeProperty; 

**Implemented methods:** void OnClosed(object); void OnOpened(object); void OnClosing(object); void OnOpening(object); 

**Implemented events:** EventHandler<object> Closed; EventHandler<object> Opened; EventHandler<object> Closing; EventHandler<object> Opening; 

---

**Not implemented properties:** LightDismissOverlayMode LightDismissOverlayMode; 

---


## AppBarButton

*Implemented for:* Android, iOS

**Implemented properties:** string Label *(Android, iOS)* ; IconElement Icon *(Android, iOS)* ; bool IsCompact *(Android, iOS)* ; int DynamicOverflowOrder *(Android, iOS)* ; bool IsInOverflow *(Android, iOS)* ; DependencyProperty IconProperty *(Android, iOS)* ; DependencyProperty IsCompactProperty *(Android, iOS)* ; DependencyProperty LabelProperty *(Android, iOS)* ; DependencyProperty DynamicOverflowOrderProperty *(Android, iOS)* ; DependencyProperty IsInOverflowProperty *(Android, iOS)* ; DependencyProperty LabelPositionProperty *(Android, iOS)* ; 

---

**Not implemented properties:** string Label *(WASM)* ; IconElement Icon *(WASM)* ; CommandBarLabelPosition LabelPosition; string KeyboardAcceleratorTextOverride; AppBarButtonTemplateSettings TemplateSettings; bool IsCompact *(WASM)* ; int DynamicOverflowOrder *(WASM)* ; bool IsInOverflow *(WASM)* ; DependencyProperty IconProperty *(WASM)* ; DependencyProperty IsCompactProperty *(WASM)* ; DependencyProperty LabelProperty *(WASM)* ; DependencyProperty DynamicOverflowOrderProperty *(WASM)* ; DependencyProperty IsInOverflowProperty *(WASM)* ; DependencyProperty LabelPositionProperty *(WASM)* ; DependencyProperty KeyboardAcceleratorTextOverrideProperty; 

---


## AppBarSeparator

*Implemented for:* all platforms

**Implemented properties:** bool IsCompact; int DynamicOverflowOrder; bool IsInOverflow; DependencyProperty IsCompactProperty; DependencyProperty DynamicOverflowOrderProperty; DependencyProperty IsInOverflowProperty; 

---

---


## AppBarToggleButton

*Implemented for:* all platforms

**Implemented properties:** string Label; IconElement Icon; bool IsCompact; int DynamicOverflowOrder; bool IsInOverflow; DependencyProperty IconProperty; DependencyProperty IsCompactProperty; DependencyProperty LabelProperty; DependencyProperty DynamicOverflowOrderProperty; DependencyProperty IsInOverflowProperty; DependencyProperty LabelPositionProperty; 

---

**Not implemented properties:** CommandBarLabelPosition LabelPosition; string KeyboardAcceleratorTextOverride; AppBarToggleButtonTemplateSettings TemplateSettings; DependencyProperty KeyboardAcceleratorTextOverrideProperty; 

---


## AutoSuggestBox

*Implemented for:* all platforms

**Implemented properties:** string PlaceholderText; double MaxSuggestionListHeight; bool IsSuggestionListOpen; object Header; bool AutoMaximizeSuggestionArea; bool UpdateTextOnSelect; string TextMemberPath; Style TextBoxStyle; string Text; IconElement QueryIcon; DependencyProperty MaxSuggestionListHeightProperty; DependencyProperty PlaceholderTextProperty; DependencyProperty TextBoxStyleProperty; DependencyProperty TextMemberPathProperty; DependencyProperty TextProperty; DependencyProperty UpdateTextOnSelectProperty; DependencyProperty AutoMaximizeSuggestionAreaProperty; DependencyProperty HeaderProperty; DependencyProperty IsSuggestionListOpenProperty; DependencyProperty QueryIconProperty; 

**Implemented events:** TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SuggestionChosen; TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> TextChanged; TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> QuerySubmitted; 

---

**Not implemented properties:** LightDismissOverlayMode LightDismissOverlayMode; DependencyProperty LightDismissOverlayModeProperty; 

---


## BitmapIcon

*Implemented for:* Android, iOS

**Implemented properties:** Uri UriSource *(Android, iOS)* ; DependencyProperty UriSourceProperty *(Android, iOS)* ; 

---

**Not implemented properties:** Uri UriSource *(WASM)* ; bool ShowAsMonochrome; DependencyProperty UriSourceProperty *(WASM)* ; DependencyProperty ShowAsMonochromeProperty; 

---


## Border

*Implemented for:* all platforms

**Implemented properties:** Thickness Padding; CornerRadius CornerRadius; TransitionCollection ChildTransitions; UIElement Child *(WASM)* ; Thickness BorderThickness; Brush BorderBrush; Brush Background; 

---

**Not implemented properties:** UIElement Child *(Android, iOS)* ; DependencyProperty BackgroundProperty; DependencyProperty BorderBrushProperty; DependencyProperty BorderThicknessProperty; DependencyProperty ChildTransitionsProperty; DependencyProperty CornerRadiusProperty; DependencyProperty PaddingProperty; 

---


## Button

*Implemented for:* all platforms

**Implemented properties:** FlyoutBase Flyout; 

---

**Not implemented properties:** DependencyProperty FlyoutProperty; 

---


## Canvas

*Implemented for:* all platforms

**Implemented methods:** double GetLeft(UIElement); void SetLeft(UIElement, double); double GetTop(UIElement); void SetTop(UIElement, double); int GetZIndex(UIElement); void SetZIndex(UIElement, int); 

---

**Not implemented properties:** DependencyProperty LeftProperty; DependencyProperty TopProperty; DependencyProperty ZIndexProperty; 

---


## CheckBox

*Implemented for:* all platforms

---

---


## ComboBox

*Implemented for:* all platforms

**Implemented properties:** double MaxDropDownHeight; bool IsDropDownOpen; ComboBoxTemplateSettings TemplateSettings; DataTemplate HeaderTemplate; string PlaceholderText; object Header; 

**Implemented events:** EventHandler<object> DropDownClosed; EventHandler<object> DropDownOpened; 

---

**Not implemented properties:** bool IsSelectionBoxHighlighted; bool IsEditable; object SelectionBoxItem; DataTemplate SelectionBoxItemTemplate; LightDismissOverlayMode LightDismissOverlayMode; bool IsTextSearchEnabled; ComboBoxSelectionChangedTrigger SelectionChangedTrigger; Brush PlaceholderForeground; DependencyProperty IsDropDownOpenProperty; DependencyProperty MaxDropDownHeightProperty; DependencyProperty HeaderProperty; DependencyProperty HeaderTemplateProperty; DependencyProperty PlaceholderTextProperty; DependencyProperty IsTextSearchEnabledProperty; DependencyProperty LightDismissOverlayModeProperty; DependencyProperty SelectionChangedTriggerProperty; DependencyProperty PlaceholderForegroundProperty; 

**Not implemented methods:** void OnDropDownClosed(object); void OnDropDownOpened(object); 

---


## ComboBoxItem

*Implemented for:* all platforms

---

---


## CommandBar

*Implemented for:* all platforms

**Implemented properties:** IObservableVector<ICommandBarElement> PrimaryCommands; IObservableVector<ICommandBarElement> SecondaryCommands; Style CommandBarOverflowPresenterStyle; CommandBarTemplateSettings CommandBarTemplateSettings; bool IsDynamicOverflowEnabled; DependencyProperty PrimaryCommandsProperty; DependencyProperty SecondaryCommandsProperty; DependencyProperty DefaultLabelPositionProperty; DependencyProperty IsDynamicOverflowEnabledProperty; DependencyProperty OverflowButtonVisibilityProperty; 

**Implemented events:** TypedEventHandler<CommandBar, DynamicOverflowItemsChangingEventArgs> DynamicOverflowItemsChanging; 

---

**Not implemented properties:** CommandBarOverflowButtonVisibility OverflowButtonVisibility; CommandBarDefaultLabelPosition DefaultLabelPosition; DependencyProperty CommandBarOverflowPresenterStyleProperty; 

---


## CommandBarOverflowPresenter

*Implemented for:* Android, iOS

---

---


## ContentControl

*Implemented for:* all platforms

**Implemented properties:** TransitionCollection ContentTransitions; DataTemplateSelector ContentTemplateSelector; DataTemplate ContentTemplate; object Content; UIElement ContentTemplateRoot *(WASM)* ; 

**Implemented methods:** void OnContentChanged(object, object); void OnContentTemplateChanged(DataTemplate, DataTemplate); void OnContentTemplateSelectorChanged(DataTemplateSelector, DataTemplateSelector); 

---

**Not implemented properties:** UIElement ContentTemplateRoot *(Android, iOS)* ; DependencyProperty ContentProperty; DependencyProperty ContentTemplateProperty; DependencyProperty ContentTemplateSelectorProperty; DependencyProperty ContentTransitionsProperty; 

---


## ContentPresenter

*Implemented for:* all platforms

**Implemented properties:** FontFamily FontFamily; TransitionCollection ContentTransitions; DataTemplateSelector ContentTemplateSelector; DataTemplate ContentTemplate; object Content; Brush Foreground; FontWeight FontWeight; FontStyle FontStyle; double FontSize; CornerRadius CornerRadius; Thickness BorderThickness; Brush BorderBrush; Brush Background; HorizontalAlignment HorizontalContentAlignment; VerticalAlignment VerticalContentAlignment; TextWrapping TextWrapping; Thickness Padding; int MaxLines; 

**Implemented methods:** void OnContentTemplateChanged(DataTemplate, DataTemplate); void OnContentTemplateSelectorChanged(DataTemplateSelector, DataTemplateSelector); 

---

**Not implemented properties:** int CharacterSpacing; FontStretch FontStretch; OpticalMarginAlignment OpticalMarginAlignment; TextLineBounds TextLineBounds; bool IsTextScaleFactorEnabled; LineStackingStrategy LineStackingStrategy; double LineHeight; DependencyProperty ForegroundProperty; DependencyProperty FontWeightProperty; DependencyProperty FontStyleProperty; DependencyProperty FontStretchProperty; DependencyProperty FontSizeProperty; DependencyProperty FontFamilyProperty; DependencyProperty ContentProperty; DependencyProperty ContentTransitionsProperty; DependencyProperty ContentTemplateSelectorProperty; DependencyProperty ContentTemplateProperty; DependencyProperty CharacterSpacingProperty; DependencyProperty OpticalMarginAlignmentProperty; DependencyProperty TextLineBoundsProperty; DependencyProperty IsTextScaleFactorEnabledProperty; DependencyProperty VerticalContentAlignmentProperty; DependencyProperty LineStackingStrategyProperty; DependencyProperty TextWrappingProperty; DependencyProperty PaddingProperty; DependencyProperty MaxLinesProperty; DependencyProperty LineHeightProperty; DependencyProperty HorizontalContentAlignmentProperty; DependencyProperty CornerRadiusProperty; DependencyProperty BorderThicknessProperty; DependencyProperty BorderBrushProperty; DependencyProperty BackgroundProperty; 

---


## Control

*Implemented for:* all platforms

**Implemented properties:** double FontSize; FontFamily FontFamily; FontStyle FontStyle; Thickness Padding; HorizontalAlignment HorizontalContentAlignment; Thickness BorderThickness; Brush Background; Brush Foreground; bool IsTabStop; bool IsEnabled; Brush BorderBrush; FontWeight FontWeight; ControlTemplate Template; VerticalAlignment VerticalContentAlignment; FocusState FocusState; object DefaultStyleKey; DependencyProperty IsEnabledProperty *(WASM)* ; 

**Implemented methods:** bool ApplyTemplate(); bool Focus(FocusState); void OnPointerEntered(PointerRoutedEventArgs); void OnPointerPressed(PointerRoutedEventArgs); void OnPointerMoved(PointerRoutedEventArgs); void OnPointerReleased(PointerRoutedEventArgs); void OnPointerExited(PointerRoutedEventArgs); void OnPointerCanceled(PointerRoutedEventArgs); void OnGotFocus(RoutedEventArgs); void OnLostFocus(RoutedEventArgs); DependencyObject GetTemplateChild(string); 

**Implemented events:** DependencyPropertyChangedEventHandler IsEnabledChanged; 

---

**Not implemented properties:** FontStretch FontStretch; int CharacterSpacing; KeyboardNavigationMode TabNavigation; int TabIndex; bool IsTextScaleFactorEnabled; bool UseSystemFocusVisuals; DependencyObject XYFocusDown; DependencyObject XYFocusUp; bool IsFocusEngagementEnabled; DependencyObject XYFocusLeft; DependencyObject XYFocusRight; RequiresPointer RequiresPointer; ElementSoundMode ElementSoundMode; bool IsFocusEngaged; Uri DefaultStyleResourceUri; DependencyProperty BackgroundProperty; DependencyProperty BorderBrushProperty; DependencyProperty BorderThicknessProperty; DependencyProperty CharacterSpacingProperty; DependencyProperty DefaultStyleKeyProperty; DependencyProperty FocusStateProperty; DependencyProperty FontFamilyProperty; DependencyProperty FontStretchProperty; DependencyProperty FontStyleProperty; DependencyProperty FontWeightProperty; DependencyProperty ForegroundProperty; DependencyProperty HorizontalContentAlignmentProperty; DependencyProperty IsEnabledProperty *(Android, iOS)* ; DependencyProperty IsTabStopProperty; DependencyProperty PaddingProperty; DependencyProperty TabIndexProperty; DependencyProperty TabNavigationProperty; DependencyProperty TemplateProperty; DependencyProperty VerticalContentAlignmentProperty; DependencyProperty FontSizeProperty; DependencyProperty IsTextScaleFactorEnabledProperty; DependencyProperty IsTemplateFocusTargetProperty; DependencyProperty UseSystemFocusVisualsProperty; DependencyProperty ElementSoundModeProperty; DependencyProperty IsFocusEngagedProperty; DependencyProperty RequiresPointerProperty; DependencyProperty XYFocusDownProperty; DependencyProperty XYFocusLeftProperty; DependencyProperty XYFocusRightProperty; DependencyProperty XYFocusUpProperty; DependencyProperty IsFocusEngagementEnabledProperty; DependencyProperty IsTemplateKeyTipTargetProperty; DependencyProperty DefaultStyleResourceUriProperty; 

**Not implemented methods:** void OnPointerCaptureLost(PointerRoutedEventArgs); void OnPointerWheelChanged(PointerRoutedEventArgs); void OnTapped(TappedRoutedEventArgs); void OnDoubleTapped(DoubleTappedRoutedEventArgs); void OnHolding(HoldingRoutedEventArgs); void OnRightTapped(RightTappedRoutedEventArgs); void OnManipulationStarting(ManipulationStartingRoutedEventArgs); void OnManipulationInertiaStarting(ManipulationInertiaStartingRoutedEventArgs); void OnManipulationStarted(ManipulationStartedRoutedEventArgs); void OnManipulationDelta(ManipulationDeltaRoutedEventArgs); void OnManipulationCompleted(ManipulationCompletedRoutedEventArgs); void OnKeyUp(KeyRoutedEventArgs); void OnKeyDown(KeyRoutedEventArgs); void OnDragEnter(DragEventArgs); void OnDragLeave(DragEventArgs); void OnDragOver(DragEventArgs); void OnDrop(DragEventArgs); void RemoveFocusEngagement(); void OnPreviewKeyDown(KeyRoutedEventArgs); void OnPreviewKeyUp(KeyRoutedEventArgs); void OnCharacterReceived(CharacterReceivedRoutedEventArgs); bool GetIsTemplateKeyTipTarget(DependencyObject); void SetIsTemplateKeyTipTarget(DependencyObject, bool); bool GetIsTemplateFocusTarget(FrameworkElement); void SetIsTemplateFocusTarget(FrameworkElement, bool); 

**Not implemented events:** TypedEventHandler<Control, FocusDisengagedEventArgs> FocusDisengaged; TypedEventHandler<Control, FocusEngagedEventArgs> FocusEngaged; 

---


## DatePicker

*Implemented for:* all platforms

**Implemented properties:** DateTimeOffset MaxYear; bool DayVisible; bool YearVisible; DateTimeOffset Date; bool MonthVisible; DateTimeOffset MinYear; 

**Implemented events:** EventHandler<DatePickerValueChangedEventArgs> DateChanged; 

---

**Not implemented properties:** DataTemplate HeaderTemplate; string MonthFormat; object Header; string DayFormat; string CalendarIdentifier; string YearFormat; Orientation Orientation; LightDismissOverlayMode LightDismissOverlayMode; DependencyProperty OrientationProperty; DependencyProperty YearFormatProperty; DependencyProperty YearVisibleProperty; DependencyProperty CalendarIdentifierProperty; DependencyProperty DateProperty; DependencyProperty DayFormatProperty; DependencyProperty DayVisibleProperty; DependencyProperty HeaderProperty; DependencyProperty HeaderTemplateProperty; DependencyProperty MaxYearProperty; DependencyProperty MinYearProperty; DependencyProperty MonthFormatProperty; DependencyProperty MonthVisibleProperty; DependencyProperty LightDismissOverlayModeProperty; 

---


## DatePickerFlyoutPresenter

*Implemented for:* Android, iOS

---

---


## FlipView

*Implemented for:* all platforms

**Implemented properties:** bool UseTouchAnimationsForAllNavigation; 

---

**Not implemented properties:** DependencyProperty UseTouchAnimationsForAllNavigationProperty; 

---


## FlipViewItem

*Implemented for:* all platforms

---

---


## FlyoutPresenter

*Implemented for:* Android, iOS

---

---


## FontIcon

*Implemented for:* all platforms

**Implemented properties:** string Glyph; FontStyle FontStyle; double FontSize; FontFamily FontFamily; 

---

**Not implemented properties:** FontWeight FontWeight; bool IsTextScaleFactorEnabled; bool MirroredWhenRightToLeft; DependencyProperty FontFamilyProperty; DependencyProperty FontSizeProperty; DependencyProperty FontStyleProperty; DependencyProperty FontWeightProperty; DependencyProperty GlyphProperty; DependencyProperty IsTextScaleFactorEnabledProperty; DependencyProperty MirroredWhenRightToLeftProperty; 

---


## Frame

*Implemented for:* all platforms

**Implemented properties:** Type SourcePageType; int CacheSize; int BackStackDepth; bool CanGoBack; bool CanGoForward; Type CurrentSourcePageType; IList<PageStackEntry> BackStack; IList<PageStackEntry> ForwardStack; 

**Implemented methods:** void GoBack(); void GoForward(); bool Navigate(Type, object); string GetNavigationState(); void SetNavigationState(string); bool Navigate(Type); bool Navigate(Type, object, NavigationTransitionInfo); void GoBack(NavigationTransitionInfo); 

**Implemented events:** NavigatedEventHandler Navigated; NavigatingCancelEventHandler Navigating; NavigationFailedEventHandler NavigationFailed; NavigationStoppedEventHandler NavigationStopped; 

---

**Not implemented properties:** DependencyProperty BackStackDepthProperty; DependencyProperty CacheSizeProperty; DependencyProperty CanGoBackProperty; DependencyProperty CanGoForwardProperty; DependencyProperty CurrentSourcePageTypeProperty; DependencyProperty SourcePageTypeProperty; DependencyProperty BackStackProperty; DependencyProperty ForwardStackProperty; 

**Not implemented methods:** void SetNavigationState(string, bool); 

---


## Grid

*Implemented for:* all platforms

**Implemented properties:** ColumnDefinitionCollection ColumnDefinitions; RowDefinitionCollection RowDefinitions; Thickness Padding; CornerRadius CornerRadius; Thickness BorderThickness; Brush BorderBrush; 

---

**Not implemented properties:** double RowSpacing; double ColumnSpacing; DependencyProperty ColumnProperty; DependencyProperty ColumnSpanProperty; DependencyProperty RowProperty; DependencyProperty RowSpanProperty; DependencyProperty BorderBrushProperty; DependencyProperty BorderThicknessProperty; DependencyProperty CornerRadiusProperty; DependencyProperty PaddingProperty; DependencyProperty ColumnSpacingProperty; DependencyProperty RowSpacingProperty; 

**Not implemented methods:** int GetRow(FrameworkElement); void SetRow(FrameworkElement, int); int GetColumn(FrameworkElement); void SetColumn(FrameworkElement, int); int GetRowSpan(FrameworkElement); void SetRowSpan(FrameworkElement, int); int GetColumnSpan(FrameworkElement); void SetColumnSpan(FrameworkElement, int); 

---


## GridView

*Implemented for:* all platforms

---

---


## GridViewHeaderItem

*Implemented for:* Android, iOS

---

---


## GridViewItem

*Implemented for:* all platforms

**Implemented properties:** GridViewItemTemplateSettings TemplateSettings; 

---

---


## HyperlinkButton

*Implemented for:* all platforms

**Implemented properties:** Uri NavigateUri; DependencyProperty NavigateUriProperty; 

---

---


## IconElement

*Implemented for:* all platforms

**Implemented properties:** Brush Foreground; 

---

**Not implemented properties:** DependencyProperty ForegroundProperty; 

---


## Image

*Implemented for:* all platforms

**Implemented properties:** Stretch Stretch; ImageSource Source; 

**Implemented events:** ExceptionRoutedEventHandler ImageFailed; RoutedEventHandler ImageOpened; 

---

**Not implemented properties:** Thickness NineGrid; PlayToSource PlayToSource; DependencyProperty NineGridProperty; DependencyProperty PlayToSourceProperty; DependencyProperty SourceProperty; DependencyProperty StretchProperty; 

**Not implemented methods:** CastingSource GetAsCastingSource(); CompositionBrush GetAlphaMask(); 

---


## ItemsControl

*Implemented for:* all platforms

**Implemented properties:** object ItemsSource; ItemsPanelTemplate ItemsPanel; DataTemplateSelector ItemTemplateSelector; Style ItemContainerStyle; DataTemplate ItemTemplate; StyleSelector ItemContainerStyleSelector; string DisplayMemberPath; IObservableVector<GroupStyle> GroupStyle; ItemCollection Items; bool IsGrouping; Panel ItemsPanelRoot; 

**Implemented methods:** bool IsItemItsOwnContainerOverride(object); DependencyObject GetContainerForItemOverride(); void ClearContainerForItemOverride(DependencyObject, object); void PrepareContainerForItemOverride(DependencyObject, object); void OnItemContainerStyleChanged(Style, Style); void OnItemContainerStyleSelectorChanged(StyleSelector, StyleSelector); void OnItemTemplateChanged(DataTemplate, DataTemplate); void OnItemTemplateSelectorChanged(DataTemplateSelector, DataTemplateSelector); object ItemFromContainer(DependencyObject); DependencyObject ContainerFromItem(object); int IndexFromContainer(DependencyObject); DependencyObject ContainerFromIndex(int); ItemsControl GetItemsOwner(DependencyObject); ItemsControl ItemsControlFromItemContainer(DependencyObject); 

---

**Not implemented properties:** TransitionCollection ItemContainerTransitions; GroupStyleSelector GroupStyleSelector; ItemContainerGenerator ItemContainerGenerator; DependencyProperty DisplayMemberPathProperty; DependencyProperty GroupStyleSelectorProperty; DependencyProperty IsGroupingProperty; DependencyProperty ItemContainerStyleProperty; DependencyProperty ItemContainerStyleSelectorProperty; DependencyProperty ItemContainerTransitionsProperty; DependencyProperty ItemTemplateProperty; DependencyProperty ItemTemplateSelectorProperty; DependencyProperty ItemsPanelProperty; DependencyProperty ItemsSourceProperty; 

**Not implemented methods:** void OnItemsChanged(object); void OnGroupStyleSelectorChanged(GroupStyleSelector, GroupStyleSelector); DependencyObject GroupHeaderContainerFromItemContainer(DependencyObject); 

---


## ItemsPresenter

*Implemented for:* all platforms

**Implemented properties:** Thickness Padding; bool AreHorizontalSnapPointsRegular; bool AreVerticalSnapPointsRegular; 

**Implemented methods:** IReadOnlyList<float> GetIrregularSnapPoints(Orientation, SnapPointsAlignment); float GetRegularSnapPoints(Orientation, SnapPointsAlignment, float); 

---

**Not implemented properties:** TransitionCollection HeaderTransitions; DataTemplate HeaderTemplate; object Header; TransitionCollection FooterTransitions; DataTemplate FooterTemplate; object Footer; DependencyProperty HeaderProperty; DependencyProperty HeaderTemplateProperty; DependencyProperty HeaderTransitionsProperty; DependencyProperty PaddingProperty; DependencyProperty FooterProperty; DependencyProperty FooterTemplateProperty; DependencyProperty FooterTransitionsProperty; 

**Not implemented events:** EventHandler<object> HorizontalSnapPointsChanged; EventHandler<object> VerticalSnapPointsChanged; 

---


## ItemsStackPanel

*Implemented for:* all platforms

**Implemented properties:** Thickness GroupPadding; GroupHeaderPlacement GroupHeaderPlacement; double CacheLength *(Android)* ; Orientation Orientation; int FirstCacheIndex *(Android)* ; int FirstVisibleIndex *(Android, iOS)* ; int LastCacheIndex *(Android)* ; int LastVisibleIndex *(Android, iOS)* ; bool AreStickyGroupHeadersEnabled; 

---

**Not implemented properties:** double CacheLength *(iOS, WASM)* ; ItemsUpdatingScrollMode ItemsUpdatingScrollMode; int FirstCacheIndex *(iOS, WASM)* ; int FirstVisibleIndex *(WASM)* ; int LastCacheIndex *(iOS, WASM)* ; int LastVisibleIndex *(WASM)* ; PanelScrollingDirection ScrollingDirection; DependencyProperty CacheLengthProperty; DependencyProperty GroupHeaderPlacementProperty; DependencyProperty GroupPaddingProperty; DependencyProperty OrientationProperty; DependencyProperty AreStickyGroupHeadersEnabledProperty; 

---


## ItemsWrapGrid

*Implemented for:* Android, iOS

**Implemented properties:** GroupHeaderPlacement GroupHeaderPlacement; double ItemHeight; Thickness GroupPadding; double CacheLength *(Android)* ; Orientation Orientation; int MaximumRowsOrColumns; double ItemWidth; int FirstCacheIndex *(Android)* ; int FirstVisibleIndex *(Android, iOS)* ; int LastCacheIndex *(Android)* ; int LastVisibleIndex *(Android, iOS)* ; bool AreStickyGroupHeadersEnabled; 

---

**Not implemented properties:** double CacheLength *(iOS, WASM)* ; int FirstCacheIndex *(iOS, WASM)* ; int FirstVisibleIndex *(WASM)* ; int LastCacheIndex *(iOS, WASM)* ; int LastVisibleIndex *(WASM)* ; PanelScrollingDirection ScrollingDirection; DependencyProperty CacheLengthProperty; DependencyProperty GroupHeaderPlacementProperty; DependencyProperty GroupPaddingProperty; DependencyProperty ItemHeightProperty; DependencyProperty ItemWidthProperty; DependencyProperty MaximumRowsOrColumnsProperty; DependencyProperty OrientationProperty; DependencyProperty AreStickyGroupHeadersEnabledProperty; 

---


## ListView

*Implemented for:* all platforms

---

---


## ListViewBase

*Implemented for:* all platforms

**Implemented properties:** IncrementalLoadingTrigger IncrementalLoadingTrigger; double IncrementalLoadingThreshold; DataTemplate HeaderTemplate; object Header; bool IsItemClickEnabled; double DataFetchSize; ListViewSelectionMode SelectionMode; IList<object> SelectedItems; object Footer; DataTemplate FooterTemplate; 

**Implemented methods:** void ScrollIntoView(object) *(Android, iOS)* ; void ScrollIntoView(object, ScrollIntoViewAlignment) *(Android, iOS)* ; 

**Implemented events:** ItemClickEventHandler ItemClick; 

---

**Not implemented properties:** bool IsSwipeEnabled; TransitionCollection HeaderTransitions; bool CanReorderItems; bool CanDragItems; bool ShowsScrollingPlaceholders; TransitionCollection FooterTransitions; ListViewReorderMode ReorderMode; bool IsMultiSelectCheckBoxEnabled; IReadOnlyList<ItemIndexRange> SelectedRanges; bool SingleSelectionFollowsFocus; SemanticZoom SemanticZoomOwner; bool IsZoomedInView; bool IsActiveView; DependencyProperty CanDragItemsProperty; DependencyProperty CanReorderItemsProperty; DependencyProperty DataFetchSizeProperty; DependencyProperty HeaderProperty; DependencyProperty HeaderTemplateProperty; DependencyProperty HeaderTransitionsProperty; DependencyProperty IncrementalLoadingTriggerProperty; DependencyProperty IsActiveViewProperty; DependencyProperty IsItemClickEnabledProperty; DependencyProperty IsSwipeEnabledProperty; DependencyProperty IsZoomedInViewProperty; DependencyProperty SelectionModeProperty; DependencyProperty SemanticZoomOwnerProperty; DependencyProperty IncrementalLoadingThresholdProperty; DependencyProperty FooterProperty; DependencyProperty FooterTemplateProperty; DependencyProperty FooterTransitionsProperty; DependencyProperty ShowsScrollingPlaceholdersProperty; DependencyProperty ReorderModeProperty; DependencyProperty IsMultiSelectCheckBoxEnabledProperty; DependencyProperty SingleSelectionFollowsFocusProperty; 

**Not implemented methods:** void ScrollIntoView(object) *(WASM)* ; void SelectAll(); IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(); void ScrollIntoView(object, ScrollIntoViewAlignment) *(WASM)* ; void InitializeViewChange(); void CompleteViewChange(); void MakeVisible(SemanticZoomLocation); void StartViewChangeFrom(SemanticZoomLocation, SemanticZoomLocation); void StartViewChangeTo(SemanticZoomLocation, SemanticZoomLocation); void CompleteViewChangeFrom(SemanticZoomLocation, SemanticZoomLocation); void CompleteViewChangeTo(SemanticZoomLocation, SemanticZoomLocation); void SetDesiredContainerUpdateDuration(TimeSpan); void SelectRange(ItemIndexRange); void DeselectRange(ItemIndexRange); bool IsDragSource(); IAsyncOperation<bool> TryStartConnectedAnimationAsync(ConnectedAnimation, object, string); ConnectedAnimation PrepareConnectedAnimation(string, object, string); 

**Not implemented events:** DragItemsStartingEventHandler DragItemsStarting; TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> ContainerContentChanging; TypedEventHandler<ListViewBase, ChoosingGroupHeaderContainerEventArgs> ChoosingGroupHeaderContainer; TypedEventHandler<ListViewBase, ChoosingItemContainerEventArgs> ChoosingItemContainer; TypedEventHandler<ListViewBase, DragItemsCompletedEventArgs> DragItemsCompleted; 

---


## ListViewBaseHeaderItem

*Implemented for:* all platforms

---

---


## ListViewHeaderItem

*Implemented for:* all platforms

---

---


## ListViewItem

*Implemented for:* all platforms

---

**Not implemented properties:** ListViewItemTemplateSettings TemplateSettings; 

---


## MenuFlyoutItem

*Implemented for:* all platforms

**Implemented properties:** string Text; object CommandParameter; ICommand Command; DependencyProperty CommandParameterProperty; DependencyProperty CommandProperty; DependencyProperty TextProperty; 

**Implemented events:** RoutedEventHandler Click *(Android)* ; 

---

**Not implemented properties:** IconElement Icon; string KeyboardAcceleratorTextOverride; MenuFlyoutItemTemplateSettings TemplateSettings; DependencyProperty IconProperty; DependencyProperty KeyboardAcceleratorTextOverrideProperty; 

**Not implemented events:** RoutedEventHandler Click *(iOS, WASM)* ; 

---


## MenuFlyoutItemBase

*Implemented for:* all platforms

---

---


## Page

*Implemented for:* all platforms

**Implemented properties:** AppBar TopAppBar; AppBar BottomAppBar; Frame Frame; DependencyProperty BottomAppBarProperty; DependencyProperty FrameProperty; DependencyProperty TopAppBarProperty; 

**Implemented methods:** void OnNavigatedFrom(NavigationEventArgs); void OnNavigatedTo(NavigationEventArgs); void OnNavigatingFrom(NavigatingCancelEventArgs); 

---

**Not implemented properties:** NavigationCacheMode NavigationCacheMode; 

---


## Panel

*Implemented for:* all platforms

**Implemented properties:** TransitionCollection ChildrenTransitions; Brush Background; UIElementCollection Children; bool IsItemsHost; 

---

**Not implemented properties:** DependencyProperty BackgroundProperty; DependencyProperty ChildrenTransitionsProperty; DependencyProperty IsItemsHostProperty; 

---


## PasswordBox

*Implemented for:* all platforms

**Implemented properties:** string Password; int MaxLength; DataTemplate HeaderTemplate; object Header; bool PreventKeyboardDisplayOnProgrammaticFocus *(Android)* ; string PlaceholderText; InputScope InputScope; DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty *(Android)* ; 

**Implemented events:** RoutedEventHandler PasswordChanged; 

---

**Not implemented properties:** string PasswordChar; bool IsPasswordRevealButtonEnabled; SolidColorBrush SelectionHighlightColor; bool PreventKeyboardDisplayOnProgrammaticFocus *(iOS, WASM)* ; TextReadingOrder TextReadingOrder; PasswordRevealMode PasswordRevealMode; DependencyProperty PasswordProperty; DependencyProperty IsPasswordRevealButtonEnabledProperty; DependencyProperty MaxLengthProperty; DependencyProperty PasswordCharProperty; DependencyProperty HeaderProperty; DependencyProperty HeaderTemplateProperty; DependencyProperty PlaceholderTextProperty; DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty *(iOS, WASM)* ; DependencyProperty SelectionHighlightColorProperty; DependencyProperty TextReadingOrderProperty; DependencyProperty PasswordRevealModeProperty; DependencyProperty InputScopeProperty; 

**Not implemented methods:** void SelectAll(); 

**Not implemented events:** ContextMenuOpeningEventHandler ContextMenuOpening; TextControlPasteEventHandler Paste; TypedEventHandler<PasswordBox, PasswordBoxPasswordChangingEventArgs> PasswordChanging; 

---


## PathIcon

*Implemented for:* all platforms

**Implemented properties:** Geometry Data; 

---

**Not implemented properties:** DependencyProperty DataProperty; 

---


## Pivot

*Implemented for:* all platforms

**Implemented properties:** DataTemplate TitleTemplate; object Title; object SelectedItem; int SelectedIndex; DataTemplate HeaderTemplate; DataTemplate RightHeaderTemplate; object RightHeader; DataTemplate LeftHeaderTemplate; object LeftHeader; DependencyProperty HeaderTemplateProperty; DependencyProperty IsLockedProperty; DependencyProperty SelectedIndexProperty; DependencyProperty SelectedItemProperty; DependencyProperty TitleProperty; DependencyProperty TitleTemplateProperty; DependencyProperty LeftHeaderProperty; DependencyProperty LeftHeaderTemplateProperty; DependencyProperty RightHeaderProperty; DependencyProperty RightHeaderTemplateProperty; 

**Implemented events:** TypedEventHandler<Pivot, PivotItemEventArgs> PivotItemLoaded; TypedEventHandler<Pivot, PivotItemEventArgs> PivotItemLoading; TypedEventHandler<Pivot, PivotItemEventArgs> PivotItemUnloaded; TypedEventHandler<Pivot, PivotItemEventArgs> PivotItemUnloading; SelectionChangedEventHandler SelectionChanged; 

---

**Not implemented properties:** bool IsLocked; bool IsHeaderItemsCarouselEnabled; PivotHeaderFocusVisualPlacement HeaderFocusVisualPlacement; DependencyProperty SlideInAnimationGroupProperty; DependencyProperty HeaderFocusVisualPlacementProperty; DependencyProperty IsHeaderItemsCarouselEnabledProperty; 

**Not implemented methods:** PivotSlideInAnimationGroup GetSlideInAnimationGroup(FrameworkElement); void SetSlideInAnimationGroup(FrameworkElement, PivotSlideInAnimationGroup); 

---


## PivotItem

*Implemented for:* all platforms

**Implemented properties:** object Header; 

---

**Not implemented properties:** DependencyProperty HeaderProperty; 

---


## ProgressBar

*Implemented for:* all platforms

**Implemented properties:** bool ShowPaused; bool ShowError; bool IsIndeterminate; ProgressBarTemplateSettings TemplateSettings; 

---

**Not implemented properties:** DependencyProperty IsIndeterminateProperty; DependencyProperty ShowErrorProperty; DependencyProperty ShowPausedProperty; 

---


## ProgressRing

*Implemented for:* all platforms

**Implemented properties:** bool IsActive; 

---

**Not implemented properties:** ProgressRingTemplateSettings TemplateSettings; DependencyProperty IsActiveProperty; 

---


## RadioButton

*Implemented for:* all platforms

**Implemented properties:** string GroupName; 

---

**Not implemented properties:** DependencyProperty GroupNameProperty; 

---


## RelativePanel

*Implemented for:* all platforms

**Implemented properties:** Thickness Padding; CornerRadius CornerRadius; Thickness BorderThickness; Brush BorderBrush; 

**Implemented methods:** object GetLeftOf(UIElement); void SetLeftOf(UIElement, object); object GetAbove(UIElement); void SetAbove(UIElement, object); object GetRightOf(UIElement); void SetRightOf(UIElement, object); object GetBelow(UIElement); void SetBelow(UIElement, object); object GetAlignHorizontalCenterWith(UIElement); void SetAlignHorizontalCenterWith(UIElement, object); object GetAlignVerticalCenterWith(UIElement); void SetAlignVerticalCenterWith(UIElement, object); object GetAlignLeftWith(UIElement); void SetAlignLeftWith(UIElement, object); object GetAlignTopWith(UIElement); void SetAlignTopWith(UIElement, object); object GetAlignRightWith(UIElement); void SetAlignRightWith(UIElement, object); object GetAlignBottomWith(UIElement); void SetAlignBottomWith(UIElement, object); bool GetAlignLeftWithPanel(UIElement); void SetAlignLeftWithPanel(UIElement, bool); bool GetAlignTopWithPanel(UIElement); void SetAlignTopWithPanel(UIElement, bool); bool GetAlignRightWithPanel(UIElement); void SetAlignRightWithPanel(UIElement, bool); bool GetAlignBottomWithPanel(UIElement); void SetAlignBottomWithPanel(UIElement, bool); bool GetAlignHorizontalCenterWithPanel(UIElement); void SetAlignHorizontalCenterWithPanel(UIElement, bool); bool GetAlignVerticalCenterWithPanel(UIElement); void SetAlignVerticalCenterWithPanel(UIElement, bool); 

---

**Not implemented properties:** DependencyProperty AboveProperty; DependencyProperty AlignBottomWithPanelProperty; DependencyProperty AlignBottomWithProperty; DependencyProperty AlignHorizontalCenterWithPanelProperty; DependencyProperty AlignHorizontalCenterWithProperty; DependencyProperty AlignLeftWithPanelProperty; DependencyProperty AlignLeftWithProperty; DependencyProperty AlignRightWithPanelProperty; DependencyProperty AlignRightWithProperty; DependencyProperty AlignTopWithPanelProperty; DependencyProperty AlignTopWithProperty; DependencyProperty AlignVerticalCenterWithPanelProperty; DependencyProperty AlignVerticalCenterWithProperty; DependencyProperty BelowProperty; DependencyProperty BorderBrushProperty; DependencyProperty BorderThicknessProperty; DependencyProperty CornerRadiusProperty; DependencyProperty LeftOfProperty; DependencyProperty PaddingProperty; DependencyProperty RightOfProperty; 

---


## ScrollContentPresenter

*Implemented for:* all platforms

**Implemented methods:** Rect MakeVisible(UIElement, Rect) *(Android, iOS)* ; 

---

**Not implemented properties:** object ScrollOwner; bool CanVerticallyScroll; bool CanHorizontallyScroll; double ExtentHeight; double ExtentWidth; double HorizontalOffset; double VerticalOffset; double ViewportHeight; double ViewportWidth; 

**Not implemented methods:** void LineUp(); void LineDown(); void LineLeft(); void LineRight(); void PageUp(); void PageDown(); void PageLeft(); void PageRight(); void MouseWheelUp(); void MouseWheelDown(); void MouseWheelLeft(); void MouseWheelRight(); void SetHorizontalOffset(double); void SetVerticalOffset(double); Rect MakeVisible(UIElement, Rect) *(WASM)* ; 

---


## ScrollViewer

*Implemented for:* all platforms

**Implemented properties:** bool BringIntoViewOnFocusChange *(Android, WASM)* ; ScrollBarVisibility HorizontalScrollBarVisibility; SnapPointsAlignment HorizontalSnapPointsAlignment; SnapPointsType HorizontalSnapPointsType; ScrollMode HorizontalScrollMode; ZoomMode ZoomMode; SnapPointsType VerticalSnapPointsType; SnapPointsAlignment VerticalSnapPointsAlignment; ScrollMode VerticalScrollMode; float MinZoomFactor; float MaxZoomFactor; ScrollBarVisibility VerticalScrollBarVisibility; double HorizontalOffset; double ExtentHeight; double ScrollableHeight; double ScrollableWidth; double VerticalOffset; double ViewportWidth; float ZoomFactor; double ViewportHeight; double ExtentWidth; DependencyProperty VerticalSnapPointsAlignmentProperty; DependencyProperty VerticalSnapPointsTypeProperty; DependencyProperty ViewportHeightProperty; DependencyProperty ViewportWidthProperty; DependencyProperty BringIntoViewOnFocusChangeProperty; DependencyProperty ExtentHeightProperty; DependencyProperty ExtentWidthProperty; DependencyProperty HorizontalSnapPointsAlignmentProperty; DependencyProperty HorizontalSnapPointsTypeProperty; DependencyProperty ScrollableHeightProperty; DependencyProperty ScrollableWidthProperty; 

**Implemented methods:** bool ChangeView(double?, double?, float?); bool ChangeView(double?, double?, float?, bool); ScrollBarVisibility GetHorizontalScrollBarVisibility(DependencyObject); void SetHorizontalScrollBarVisibility(DependencyObject, ScrollBarVisibility); ScrollBarVisibility GetVerticalScrollBarVisibility(DependencyObject); void SetVerticalScrollBarVisibility(DependencyObject, ScrollBarVisibility); ScrollMode GetHorizontalScrollMode(DependencyObject); void SetHorizontalScrollMode(DependencyObject, ScrollMode); ScrollMode GetVerticalScrollMode(DependencyObject); void SetVerticalScrollMode(DependencyObject, ScrollMode); 

**Implemented events:** EventHandler<ScrollViewerViewChangedEventArgs> ViewChanged; 

---

**Not implemented properties:** bool IsZoomInertiaEnabled; bool IsZoomChainingEnabled; bool IsVerticalScrollChainingEnabled; bool IsVerticalRailEnabled; bool IsScrollInertiaEnabled; bool BringIntoViewOnFocusChange *(iOS)* ; bool IsHorizontalScrollChainingEnabled; bool IsHorizontalRailEnabled; bool IsDeferredScrollingEnabled; SnapPointsType ZoomSnapPointsType; Visibility ComputedHorizontalScrollBarVisibility; Visibility ComputedVerticalScrollBarVisibility; IList<float> ZoomSnapPoints; UIElement TopLeftHeader; UIElement TopHeader; UIElement LeftHeader; DependencyProperty VerticalOffsetProperty; DependencyProperty VerticalScrollBarVisibilityProperty; DependencyProperty VerticalScrollModeProperty; DependencyProperty ZoomFactorProperty; DependencyProperty ZoomModeProperty; DependencyProperty ZoomSnapPointsProperty; DependencyProperty ZoomSnapPointsTypeProperty; DependencyProperty ComputedHorizontalScrollBarVisibilityProperty; DependencyProperty ComputedVerticalScrollBarVisibilityProperty; DependencyProperty HorizontalOffsetProperty; DependencyProperty HorizontalScrollBarVisibilityProperty; DependencyProperty HorizontalScrollModeProperty; DependencyProperty IsDeferredScrollingEnabledProperty; DependencyProperty IsHorizontalRailEnabledProperty; DependencyProperty IsHorizontalScrollChainingEnabledProperty; DependencyProperty IsScrollInertiaEnabledProperty; DependencyProperty IsVerticalRailEnabledProperty; DependencyProperty IsVerticalScrollChainingEnabledProperty; DependencyProperty IsZoomChainingEnabledProperty; DependencyProperty IsZoomInertiaEnabledProperty; DependencyProperty MaxZoomFactorProperty; DependencyProperty MinZoomFactorProperty; DependencyProperty LeftHeaderProperty; DependencyProperty TopHeaderProperty; DependencyProperty TopLeftHeaderProperty; 

**Not implemented methods:** void ScrollToHorizontalOffset(double); void ScrollToVerticalOffset(double); void ZoomToFactor(float); void InvalidateScrollInfo(); bool GetIsHorizontalRailEnabled(DependencyObject); void SetIsHorizontalRailEnabled(DependencyObject, bool); bool GetIsVerticalRailEnabled(DependencyObject); void SetIsVerticalRailEnabled(DependencyObject, bool); bool GetIsHorizontalScrollChainingEnabled(DependencyObject); void SetIsHorizontalScrollChainingEnabled(DependencyObject, bool); bool GetIsVerticalScrollChainingEnabled(DependencyObject); void SetIsVerticalScrollChainingEnabled(DependencyObject, bool); bool GetIsZoomChainingEnabled(DependencyObject); void SetIsZoomChainingEnabled(DependencyObject, bool); bool GetIsScrollInertiaEnabled(DependencyObject); void SetIsScrollInertiaEnabled(DependencyObject, bool); bool GetIsZoomInertiaEnabled(DependencyObject); void SetIsZoomInertiaEnabled(DependencyObject, bool); ZoomMode GetZoomMode(DependencyObject); void SetZoomMode(DependencyObject, ZoomMode); bool GetIsDeferredScrollingEnabled(DependencyObject); void SetIsDeferredScrollingEnabled(DependencyObject, bool); bool GetBringIntoViewOnFocusChange(DependencyObject); void SetBringIntoViewOnFocusChange(DependencyObject, bool); 

**Not implemented events:** EventHandler<ScrollViewerViewChangingEventArgs> ViewChanging; EventHandler<object> DirectManipulationCompleted; EventHandler<object> DirectManipulationStarted; 

---


## Slider

*Implemented for:* all platforms

**Implemented properties:** SliderSnapsTo SnapsTo; Orientation Orientation; bool IsThumbToolTipEnabled; bool IsDirectionReversed; double IntermediateValue; TickPlacement TickPlacement; double TickFrequency; IValueConverter ThumbToolTipValueConverter; double StepFrequency; DataTemplate HeaderTemplate; object Header; 

---

**Not implemented properties:** DependencyProperty OrientationProperty; DependencyProperty SnapsToProperty; DependencyProperty StepFrequencyProperty; DependencyProperty ThumbToolTipValueConverterProperty; DependencyProperty TickFrequencyProperty; DependencyProperty TickPlacementProperty; DependencyProperty IntermediateValueProperty; DependencyProperty IsDirectionReversedProperty; DependencyProperty IsThumbToolTipEnabledProperty; DependencyProperty HeaderProperty; DependencyProperty HeaderTemplateProperty; 

---


## SplitView

*Implemented for:* all platforms

**Implemented properties:** SplitViewPanePlacement PanePlacement; Brush PaneBackground; UIElement Pane *(WASM)* ; double OpenPaneLength; bool IsPaneOpen; SplitViewDisplayMode DisplayMode; UIElement Content *(WASM)* ; double CompactPaneLength; SplitViewTemplateSettings TemplateSettings; 

**Implemented events:** TypedEventHandler<SplitView, object> PaneClosed; TypedEventHandler<SplitView, SplitViewPaneClosingEventArgs> PaneClosing; 

---

**Not implemented properties:** UIElement Pane *(Android, iOS)* ; UIElement Content *(Android, iOS)* ; LightDismissOverlayMode LightDismissOverlayMode; DependencyProperty CompactPaneLengthProperty; DependencyProperty ContentProperty; DependencyProperty DisplayModeProperty; DependencyProperty IsPaneOpenProperty; DependencyProperty OpenPaneLengthProperty; DependencyProperty PaneBackgroundProperty; DependencyProperty PanePlacementProperty; DependencyProperty PaneProperty; DependencyProperty TemplateSettingsProperty; DependencyProperty LightDismissOverlayModeProperty; 

**Not implemented events:** TypedEventHandler<SplitView, object> PaneOpened; TypedEventHandler<SplitView, object> PaneOpening; 

---


## StackPanel

*Implemented for:* all platforms

**Implemented properties:** Orientation Orientation; Thickness Padding; CornerRadius CornerRadius; Thickness BorderThickness; Brush BorderBrush; double Spacing; 

---

**Not implemented properties:** bool AreScrollSnapPointsRegular; bool AreHorizontalSnapPointsRegular; bool AreVerticalSnapPointsRegular; DependencyProperty AreScrollSnapPointsRegularProperty; DependencyProperty OrientationProperty; DependencyProperty BorderBrushProperty; DependencyProperty BorderThicknessProperty; DependencyProperty CornerRadiusProperty; DependencyProperty PaddingProperty; DependencyProperty SpacingProperty; 

**Not implemented methods:** IReadOnlyList<float> GetIrregularSnapPoints(Orientation, SnapPointsAlignment); float GetRegularSnapPoints(Orientation, SnapPointsAlignment, float); void GetInsertionIndexes(Point, int, int); 

**Not implemented events:** EventHandler<object> HorizontalSnapPointsChanged; EventHandler<object> VerticalSnapPointsChanged; 

---


## SymbolIcon

*Implemented for:* Android, iOS

**Implemented properties:** Symbol Symbol *(Android, iOS)* ; 

---

**Not implemented properties:** Symbol Symbol *(WASM)* ; DependencyProperty SymbolProperty; 

---


## TextBlock

*Implemented for:* all platforms

**Implemented properties:** FontFamily FontFamily; double LineHeight; int CharacterSpacing; Thickness Padding; Brush Foreground; FontWeight FontWeight; LineStackingStrategy LineStackingStrategy; FontStyle FontStyle; double FontSize; TextWrapping TextWrapping; TextTrimming TextTrimming; TextAlignment TextAlignment; string Text; InlineCollection Inlines; int MaxLines; TextDecorations TextDecorations; 

---

**Not implemented properties:** bool IsTextSelectionEnabled; FontStretch FontStretch; double BaselineOffset; TextPointer ContentEnd; TextPointer ContentStart; string SelectedText; TextPointer SelectionEnd; TextPointer SelectionStart; OpticalMarginAlignment OpticalMarginAlignment; TextReadingOrder TextReadingOrder; TextLineBounds TextLineBounds; SolidColorBrush SelectionHighlightColor; bool IsColorFontEnabled; bool IsTextScaleFactorEnabled; TextAlignment HorizontalTextAlignment; bool IsTextTrimmed; IList<TextHighlighter> TextHighlighters; DependencyProperty CharacterSpacingProperty; DependencyProperty FontFamilyProperty; DependencyProperty FontSizeProperty; DependencyProperty FontStretchProperty; DependencyProperty FontStyleProperty; DependencyProperty FontWeightProperty; DependencyProperty IsTextSelectionEnabledProperty; DependencyProperty LineHeightProperty; DependencyProperty LineStackingStrategyProperty; DependencyProperty PaddingProperty; DependencyProperty SelectedTextProperty; DependencyProperty TextAlignmentProperty; DependencyProperty TextProperty; DependencyProperty TextTrimmingProperty; DependencyProperty TextWrappingProperty; DependencyProperty ForegroundProperty; DependencyProperty IsColorFontEnabledProperty; DependencyProperty MaxLinesProperty; DependencyProperty OpticalMarginAlignmentProperty; DependencyProperty SelectionHighlightColorProperty; DependencyProperty TextLineBoundsProperty; DependencyProperty TextReadingOrderProperty; DependencyProperty IsTextScaleFactorEnabledProperty; DependencyProperty TextDecorationsProperty; DependencyProperty HorizontalTextAlignmentProperty; DependencyProperty IsTextTrimmedProperty; 

**Not implemented methods:** void SelectAll(); void Select(TextPointer, TextPointer); bool Focus(FocusState); CompositionBrush GetAlphaMask(); 

**Not implemented events:** ContextMenuOpeningEventHandler ContextMenuOpening; RoutedEventHandler SelectionChanged; TypedEventHandler<TextBlock, IsTextTrimmedChangedEventArgs> IsTextTrimmedChanged; 

---


## TextBox

*Implemented for:* all platforms

**Implemented properties:** int MaxLength; bool IsTextPredictionEnabled; bool IsSpellCheckEnabled; bool IsReadOnly; InputScope InputScope; bool AcceptsReturn; TextWrapping TextWrapping; TextAlignment TextAlignment; string Text; int SelectionStart; int SelectionLength; string PlaceholderText; bool PreventKeyboardDisplayOnProgrammaticFocus *(Android)* ; object Header; DataTemplate HeaderTemplate; DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty *(Android)* ; 

**Implemented events:** RoutedEventHandler SelectionChanged; TextChangedEventHandler TextChanged; 

---

**Not implemented properties:** string SelectedText; SolidColorBrush SelectionHighlightColor; bool PreventKeyboardDisplayOnProgrammaticFocus *(iOS, WASM)* ; bool IsColorFontEnabled; CandidateWindowAlignment DesiredCandidateWindowAlignment; TextReadingOrder TextReadingOrder; SolidColorBrush SelectionHighlightColorWhenNotFocused; Brush PlaceholderForeground; TextAlignment HorizontalTextAlignment; CharacterCasing CharacterCasing; bool IsHandwritingViewEnabled; HandwritingView HandwritingView; DependencyProperty IsTextPredictionEnabledProperty; DependencyProperty IsSpellCheckEnabledProperty; DependencyProperty IsReadOnlyProperty; DependencyProperty InputScopeProperty; DependencyProperty AcceptsReturnProperty; DependencyProperty MaxLengthProperty; DependencyProperty TextAlignmentProperty; DependencyProperty TextProperty; DependencyProperty TextWrappingProperty; DependencyProperty HeaderProperty; DependencyProperty HeaderTemplateProperty; DependencyProperty IsColorFontEnabledProperty; DependencyProperty PlaceholderTextProperty; DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty *(iOS, WASM)* ; DependencyProperty SelectionHighlightColorProperty; DependencyProperty TextReadingOrderProperty; DependencyProperty DesiredCandidateWindowAlignmentProperty; DependencyProperty SelectionHighlightColorWhenNotFocusedProperty; DependencyProperty PlaceholderForegroundProperty; DependencyProperty HorizontalTextAlignmentProperty; DependencyProperty CharacterCasingProperty; DependencyProperty IsHandwritingViewEnabledProperty; DependencyProperty HandwritingViewProperty; 

**Not implemented methods:** void Select(int, int); void SelectAll(); Rect GetRectFromCharacterIndex(int, bool); IAsyncOperation<IReadOnlyList<string>> GetLinguisticAlternativesAsync(); 

**Not implemented events:** ContextMenuOpeningEventHandler ContextMenuOpening; TextControlPasteEventHandler Paste; TypedEventHandler<TextBox, CandidateWindowBoundsChangedEventArgs> CandidateWindowBoundsChanged; TypedEventHandler<TextBox, TextBoxTextChangingEventArgs> TextChanging; TypedEventHandler<TextBox, TextCompositionChangedEventArgs> TextCompositionChanged; TypedEventHandler<TextBox, TextCompositionEndedEventArgs> TextCompositionEnded; TypedEventHandler<TextBox, TextCompositionStartedEventArgs> TextCompositionStarted; TypedEventHandler<TextBox, TextBoxBeforeTextChangingEventArgs> BeforeTextChanging; TypedEventHandler<TextBox, TextControlCopyingToClipboardEventArgs> CopyingToClipboard; TypedEventHandler<TextBox, TextControlCuttingToClipboardEventArgs> CuttingToClipboard; 

---


## TimePicker

*Implemented for:* Android, iOS

**Implemented properties:** TimeSpan Time; string ClockIdentifier; 

---

**Not implemented properties:** int MinuteIncrement; DataTemplate HeaderTemplate; object Header; LightDismissOverlayMode LightDismissOverlayMode; DependencyProperty ClockIdentifierProperty; DependencyProperty HeaderProperty; DependencyProperty HeaderTemplateProperty; DependencyProperty MinuteIncrementProperty; DependencyProperty TimeProperty; DependencyProperty LightDismissOverlayModeProperty; 

**Not implemented events:** EventHandler<TimePickerValueChangedEventArgs> TimeChanged; 

---


## TimePickerFlyoutPresenter

*Implemented for:* all platforms

---

---


## ToggleMenuFlyoutItem

*Implemented for:* all platforms

**Implemented properties:** bool IsChecked; DependencyProperty IsCheckedProperty; 

---

---


## ToggleSwitch

*Implemented for:* all platforms

**Implemented properties:** DataTemplate OnContentTemplate; object OnContent; DataTemplate OffContentTemplate; object OffContent; bool IsOn; DataTemplate HeaderTemplate; object Header; ToggleSwitchTemplateSettings TemplateSettings; 

**Implemented events:** RoutedEventHandler Toggled; 

---

**Not implemented properties:** DependencyProperty HeaderProperty; DependencyProperty HeaderTemplateProperty; DependencyProperty IsOnProperty; DependencyProperty OffContentProperty; DependencyProperty OffContentTemplateProperty; DependencyProperty OnContentProperty; DependencyProperty OnContentTemplateProperty; 

**Not implemented methods:** void OnToggled(); void OnOnContentChanged(object, object); void OnOffContentChanged(object, object); void OnHeaderChanged(object, object); 

---


## UserControl

*Implemented for:* all platforms

---

**Not implemented properties:** UIElement Content; DependencyProperty ContentProperty; 

---


## VariableSizedWrapGrid

*Implemented for:* all platforms

---

**Not implemented properties:** VerticalAlignment VerticalChildrenAlignment; Orientation Orientation; int MaximumRowsOrColumns; double ItemWidth; double ItemHeight; HorizontalAlignment HorizontalChildrenAlignment; DependencyProperty ColumnSpanProperty; DependencyProperty HorizontalChildrenAlignmentProperty; DependencyProperty ItemHeightProperty; DependencyProperty ItemWidthProperty; DependencyProperty MaximumRowsOrColumnsProperty; DependencyProperty OrientationProperty; DependencyProperty RowSpanProperty; DependencyProperty VerticalChildrenAlignmentProperty; 

**Not implemented methods:** int GetRowSpan(UIElement); void SetRowSpan(UIElement, int); int GetColumnSpan(UIElement); void SetColumnSpan(UIElement, int); 

---


## VirtualizingPanel

*Implemented for:* iOS, WASM

---

**Not implemented properties:** ItemContainerGenerator ItemContainerGenerator; 

**Not implemented methods:** void OnItemsChanged(object, ItemsChangedEventArgs); void OnClearChildren(); void BringIndexIntoView(int); void AddInternalChild(UIElement); void InsertInternalChild(int, UIElement); void RemoveInternalChildRange(int, int); 

---


## WebView

*Implemented for:* all platforms

**Implemented properties:** Uri Source; bool CanGoBack; bool CanGoForward; 

**Implemented methods:** void Navigate(Uri); void NavigateToString(string); void GoForward(); void GoBack(); void Refresh() *(Android, iOS)* ; void Stop(); bool Focus(FocusState); 

**Implemented events:** WebViewNavigationFailedEventHandler NavigationFailed; TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs> NavigationCompleted; TypedEventHandler<WebView, WebViewNavigationStartingEventArgs> NavigationStarting; TypedEventHandler<WebView, WebViewNewWindowRequestedEventArgs> NewWindowRequested; TypedEventHandler<WebView, WebViewUnsupportedUriSchemeIdentifiedEventArgs> UnsupportedUriSchemeIdentified; 

---

**Not implemented properties:** IList<Uri> AllowedScriptNotifyUris; DataPackage DataTransferPackage; Color DefaultBackgroundColor; string DocumentTitle; bool ContainsFullScreenElement; IList<WebViewDeferredPermissionRequest> DeferredPermissionRequests; WebViewExecutionMode ExecutionMode; WebViewSettings Settings; DependencyObject XYFocusRight; DependencyObject XYFocusLeft; DependencyObject XYFocusDown; DependencyObject XYFocusUp; DependencyProperty SourceProperty; DependencyProperty DataTransferPackageProperty; IList<Uri> AnyScriptNotifyUri; DependencyProperty AllowedScriptNotifyUrisProperty; DependencyProperty CanGoForwardProperty; DependencyProperty DefaultBackgroundColorProperty; DependencyProperty DocumentTitleProperty; DependencyProperty CanGoBackProperty; DependencyProperty ContainsFullScreenElementProperty; WebViewExecutionMode DefaultExecutionMode; DependencyProperty XYFocusUpProperty; DependencyProperty XYFocusRightProperty; DependencyProperty XYFocusLeftProperty; DependencyProperty XYFocusDownProperty; 

**Not implemented methods:** string InvokeScript(string, string[]); void Refresh() *(WASM)* ; IAsyncAction CapturePreviewToStreamAsync(IRandomAccessStream); IAsyncOperation<string> InvokeScriptAsync(string, IEnumerable<string>); IAsyncOperation<DataPackage> CaptureSelectedContentToDataPackageAsync(); void NavigateToLocalStreamUri(Uri, IUriToStreamResolver); Uri BuildLocalStreamUri(string, string); void NavigateWithHttpRequestMessage(HttpRequestMessage); void AddWebAllowedObject(string, object); WebViewDeferredPermissionRequest DeferredPermissionRequestById(uint); IAsyncAction ClearTemporaryWebDataAsync(); 

**Not implemented events:** LoadCompletedEventHandler LoadCompleted; NotifyEventHandler ScriptNotify; TypedEventHandler<WebView, WebViewContentLoadingEventArgs> ContentLoading; TypedEventHandler<WebView, WebViewDOMContentLoadedEventArgs> DOMContentLoaded; TypedEventHandler<WebView, WebViewContentLoadingEventArgs> FrameContentLoading; TypedEventHandler<WebView, WebViewDOMContentLoadedEventArgs> FrameDOMContentLoaded; TypedEventHandler<WebView, WebViewNavigationCompletedEventArgs> FrameNavigationCompleted; TypedEventHandler<WebView, WebViewNavigationStartingEventArgs> FrameNavigationStarting; TypedEventHandler<WebView, WebViewLongRunningScriptDetectedEventArgs> LongRunningScriptDetected; TypedEventHandler<WebView, object> UnsafeContentWarningDisplaying; TypedEventHandler<WebView, WebViewUnviewableContentIdentifiedEventArgs> UnviewableContentIdentified; TypedEventHandler<WebView, object> ContainsFullScreenElementChanged; TypedEventHandler<WebView, WebViewPermissionRequestedEventArgs> PermissionRequested; TypedEventHandler<WebView, WebViewSeparateProcessLostEventArgs> SeparateProcessLost; 

---




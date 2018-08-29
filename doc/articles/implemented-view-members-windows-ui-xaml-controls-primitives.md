# Implemented members by control - Windows.UI.Xaml.Controls.Primitives

This document lists which individual properties, methods, and events are implemented and not implemented per control in the namespace Windows.UI.Xaml.Controls.Primitives.

## ButtonBase

*Implemented for:* all platforms

**Implemented properties:** object CommandParameter; ICommand Command; bool IsPointerOver; DependencyProperty CommandParameterProperty; 

**Implemented events:** RoutedEventHandler Click; 

---

**Not implemented properties:** ClickMode ClickMode; bool IsPressed; DependencyProperty ClickModeProperty; DependencyProperty CommandProperty; DependencyProperty IsPointerOverProperty; DependencyProperty IsPressedProperty; 

---


## CarouselPanel

*Implemented for:* all platforms

---

**Not implemented properties:** bool CanVerticallyScroll; bool CanHorizontallyScroll; object ScrollOwner; double ExtentHeight; double ExtentWidth; double HorizontalOffset; double VerticalOffset; double ViewportHeight; double ViewportWidth; bool AreHorizontalSnapPointsRegular; bool AreVerticalSnapPointsRegular; 

**Not implemented methods:** void LineUp(); void LineDown(); void LineLeft(); void LineRight(); void PageUp(); void PageDown(); void PageLeft(); void PageRight(); void MouseWheelUp(); void MouseWheelDown(); void MouseWheelLeft(); void MouseWheelRight(); void SetHorizontalOffset(double); void SetVerticalOffset(double); Rect MakeVisible(UIElement, Rect); IReadOnlyList<float> GetIrregularSnapPoints(Orientation, SnapPointsAlignment); float GetRegularSnapPoints(Orientation, SnapPointsAlignment, float); 

**Not implemented events:** EventHandler<object> HorizontalSnapPointsChanged; EventHandler<object> VerticalSnapPointsChanged; 

---


## GridViewItemPresenter

*Implemented for:* Android, iOS

---

**Not implemented properties:** double DisabledOpacity; Thickness ContentMargin; Brush CheckHintBrush; Brush CheckBrush; Thickness GridViewItemPresenterPadding; HorizontalAlignment GridViewItemPresenterHorizontalContentAlignment; Brush FocusBorderBrush; Brush PointerOverBackground; double DragOpacity; Brush DragForeground; Brush DragBackground; Brush SelectedPointerOverBorderBrush; Brush SelectedPointerOverBackground; Brush CheckSelectingBrush; Brush SelectedForeground; Thickness SelectedBorderThickness; Brush SelectedBackground; bool SelectionCheckMarkVisualEnabled; double ReorderHintOffset; Thickness PointerOverBackgroundMargin; Brush PlaceholderBackground; VerticalAlignment GridViewItemPresenterVerticalContentAlignment; DependencyProperty CheckBrushProperty; DependencyProperty CheckHintBrushProperty; DependencyProperty CheckSelectingBrushProperty; DependencyProperty ContentMarginProperty; DependencyProperty DisabledOpacityProperty; DependencyProperty DragBackgroundProperty; DependencyProperty DragForegroundProperty; DependencyProperty DragOpacityProperty; DependencyProperty FocusBorderBrushProperty; DependencyProperty GridViewItemPresenterHorizontalContentAlignmentProperty; DependencyProperty GridViewItemPresenterPaddingProperty; DependencyProperty GridViewItemPresenterVerticalContentAlignmentProperty; DependencyProperty PlaceholderBackgroundProperty; DependencyProperty PointerOverBackgroundMarginProperty; DependencyProperty PointerOverBackgroundProperty; DependencyProperty ReorderHintOffsetProperty; DependencyProperty SelectedBackgroundProperty; DependencyProperty SelectedBorderThicknessProperty; DependencyProperty SelectedForegroundProperty; DependencyProperty SelectedPointerOverBackgroundProperty; DependencyProperty SelectedPointerOverBorderBrushProperty; DependencyProperty SelectionCheckMarkVisualEnabledProperty; 

---


## ListViewItemPresenter

*Implemented for:* Android, iOS

---

**Not implemented properties:** double DragOpacity; Brush DragForeground; Brush DragBackground; double DisabledOpacity; Thickness ContentMargin; Brush CheckSelectingBrush; Brush PointerOverBackground; Brush CheckHintBrush; Brush CheckBrush; VerticalAlignment ListViewItemPresenterVerticalContentAlignment; Thickness PointerOverBackgroundMargin; Brush PlaceholderBackground; double ReorderHintOffset; Thickness ListViewItemPresenterPadding; HorizontalAlignment ListViewItemPresenterHorizontalContentAlignment; Brush FocusBorderBrush; bool SelectionCheckMarkVisualEnabled; Brush SelectedPointerOverBorderBrush; Brush SelectedForeground; Thickness SelectedBorderThickness; Brush SelectedBackground; Brush SelectedPointerOverBackground; Brush SelectedPressedBackground; Brush CheckBoxBrush; Brush PointerOverForeground; ListViewItemPresenterCheckMode CheckMode; Brush PressedBackground; Brush FocusSecondaryBorderBrush; Brush RevealBorderBrush; bool RevealBackgroundShowsAboveContent; Brush RevealBackground; Thickness RevealBorderThickness; DependencyProperty CheckBrushProperty; DependencyProperty CheckHintBrushProperty; DependencyProperty CheckSelectingBrushProperty; DependencyProperty ContentMarginProperty; DependencyProperty DisabledOpacityProperty; DependencyProperty DragBackgroundProperty; DependencyProperty DragForegroundProperty; DependencyProperty DragOpacityProperty; DependencyProperty FocusBorderBrushProperty; DependencyProperty ListViewItemPresenterPaddingProperty; DependencyProperty ListViewItemPresenterVerticalContentAlignmentProperty; DependencyProperty PlaceholderBackgroundProperty; DependencyProperty PointerOverBackgroundMarginProperty; DependencyProperty PointerOverBackgroundProperty; DependencyProperty ReorderHintOffsetProperty; DependencyProperty SelectedBackgroundProperty; DependencyProperty SelectedBorderThicknessProperty; DependencyProperty SelectedForegroundProperty; DependencyProperty SelectedPointerOverBackgroundProperty; DependencyProperty SelectedPointerOverBorderBrushProperty; DependencyProperty SelectionCheckMarkVisualEnabledProperty; DependencyProperty ListViewItemPresenterHorizontalContentAlignmentProperty; DependencyProperty CheckModeProperty; DependencyProperty FocusSecondaryBorderBrushProperty; DependencyProperty PointerOverForegroundProperty; DependencyProperty PressedBackgroundProperty; DependencyProperty SelectedPressedBackgroundProperty; DependencyProperty CheckBoxBrushProperty; DependencyProperty RevealBackgroundProperty; DependencyProperty RevealBackgroundShowsAboveContentProperty; DependencyProperty RevealBorderBrushProperty; DependencyProperty RevealBorderThicknessProperty; 

---


## OrientedVirtualizingPanel

*Implemented for:* iOS, WASM

---

**Not implemented properties:** bool CanVerticallyScroll; bool CanHorizontallyScroll; object ScrollOwner; double ExtentHeight; double ExtentWidth; double HorizontalOffset; double VerticalOffset; double ViewportHeight; double ViewportWidth; bool AreHorizontalSnapPointsRegular; bool AreVerticalSnapPointsRegular; 

**Not implemented methods:** void LineUp(); void LineDown(); void LineLeft(); void LineRight(); void PageUp(); void PageDown(); void PageLeft(); void PageRight(); void MouseWheelUp(); void MouseWheelDown(); void MouseWheelLeft(); void MouseWheelRight(); void SetHorizontalOffset(double); void SetVerticalOffset(double); Rect MakeVisible(UIElement, Rect); IReadOnlyList<float> GetIrregularSnapPoints(Orientation, SnapPointsAlignment); float GetRegularSnapPoints(Orientation, SnapPointsAlignment, float); void GetInsertionIndexes(Point, int, int); 

**Not implemented events:** EventHandler<object> HorizontalSnapPointsChanged; EventHandler<object> VerticalSnapPointsChanged; 

---


## PivotHeaderItem

*Implemented for:* all platforms

---

---


## PivotHeaderPanel

*Implemented for:* all platforms

---

---


## Popup

*Implemented for:* WASM

---

**Not implemented properties:** double VerticalOffset; bool IsOpen; bool IsLightDismissEnabled; double HorizontalOffset; TransitionCollection ChildTransitions; UIElement Child; LightDismissOverlayMode LightDismissOverlayMode; DependencyProperty ChildProperty; DependencyProperty ChildTransitionsProperty; DependencyProperty HorizontalOffsetProperty; DependencyProperty IsLightDismissEnabledProperty; DependencyProperty IsOpenProperty; DependencyProperty VerticalOffsetProperty; DependencyProperty LightDismissOverlayModeProperty; 

**Not implemented events:** EventHandler<object> Closed; EventHandler<object> Opened; 

---


## RangeBase

*Implemented for:* all platforms

**Implemented properties:** double Value; double SmallChange; double Minimum; double Maximum; double LargeChange; 

**Implemented methods:** void OnMinimumChanged(double, double); void OnMaximumChanged(double, double); void OnValueChanged(double, double); 

**Implemented events:** RangeBaseValueChangedEventHandler ValueChanged; 

---

**Not implemented properties:** DependencyProperty LargeChangeProperty; DependencyProperty MaximumProperty; DependencyProperty MinimumProperty; DependencyProperty SmallChangeProperty; DependencyProperty ValueProperty; 

---


## Selector

*Implemented for:* all platforms

**Implemented properties:** object SelectedItem; int SelectedIndex; 

**Implemented events:** SelectionChangedEventHandler SelectionChanged; 

---

**Not implemented properties:** string SelectedValuePath; object SelectedValue; bool? IsSynchronizedWithCurrentItem; DependencyProperty IsSynchronizedWithCurrentItemProperty; DependencyProperty SelectedIndexProperty; DependencyProperty SelectedItemProperty; DependencyProperty SelectedValuePathProperty; DependencyProperty SelectedValueProperty; 

**Not implemented methods:** bool GetIsSelectionActive(DependencyObject); 

---


## SelectorItem

*Implemented for:* all platforms

**Implemented properties:** bool IsSelected; 

---

**Not implemented properties:** DependencyProperty IsSelectedProperty; 

---


## Thumb

*Implemented for:* all platforms

**Implemented properties:** bool IsDragging; 

**Implemented methods:** void CancelDrag(); 

**Implemented events:** DragCompletedEventHandler DragCompleted; DragDeltaEventHandler DragDelta; DragStartedEventHandler DragStarted; 

---

**Not implemented properties:** DependencyProperty IsDraggingProperty; 

---


## TickBar

*Implemented for:* all platforms

**Implemented properties:** Brush Fill; 

---

**Not implemented properties:** DependencyProperty FillProperty; 

---


## ToggleButton

*Implemented for:* all platforms

**Implemented properties:** bool? IsChecked; 

**Implemented events:** RoutedEventHandler Checked; RoutedEventHandler Unchecked; 

---

**Not implemented properties:** bool IsThreeState; DependencyProperty IsCheckedProperty; DependencyProperty IsThreeStateProperty; 

**Not implemented methods:** void OnToggle(); 

**Not implemented events:** RoutedEventHandler Indeterminate; 

---




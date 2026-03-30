// MUX Reference ListViewItemPresenter.g.cpp, tag winui3/release/1.8.4

using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewItemPresenter
{
	public ListViewItemPresenter()
	{
	}

	#region CheckBox Brushes

	/// <summary>
	/// Gets or sets the brush used for the check box border.
	/// </summary>
	public Brush CheckBoxBorderBrush
	{
		get => (Brush)GetValue(CheckBoxBorderBrushProperty);
		set => SetValue(CheckBoxBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box background.
	/// </summary>
	public Brush CheckBoxBrush
	{
		get => (Brush)GetValue(CheckBoxBrushProperty);
		set => SetValue(CheckBoxBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box border when disabled.
	/// </summary>
	public Brush CheckBoxDisabledBorderBrush
	{
		get => (Brush)GetValue(CheckBoxDisabledBorderBrushProperty);
		set => SetValue(CheckBoxDisabledBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxDisabledBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxDisabledBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxDisabledBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box background when disabled.
	/// </summary>
	public Brush CheckBoxDisabledBrush
	{
		get => (Brush)GetValue(CheckBoxDisabledBrushProperty);
		set => SetValue(CheckBoxDisabledBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxDisabledBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxDisabledBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxDisabledBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box border on pointer over.
	/// </summary>
	public Brush CheckBoxPointerOverBorderBrush
	{
		get => (Brush)GetValue(CheckBoxPointerOverBorderBrushProperty);
		set => SetValue(CheckBoxPointerOverBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxPointerOverBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxPointerOverBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxPointerOverBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box background on pointer over.
	/// </summary>
	public Brush CheckBoxPointerOverBrush
	{
		get => (Brush)GetValue(CheckBoxPointerOverBrushProperty);
		set => SetValue(CheckBoxPointerOverBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxPointerOverBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxPointerOverBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxPointerOverBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box border when pressed.
	/// </summary>
	public Brush CheckBoxPressedBorderBrush
	{
		get => (Brush)GetValue(CheckBoxPressedBorderBrushProperty);
		set => SetValue(CheckBoxPressedBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxPressedBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxPressedBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxPressedBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box background when pressed.
	/// </summary>
	public Brush CheckBoxPressedBrush
	{
		get => (Brush)GetValue(CheckBoxPressedBrushProperty);
		set => SetValue(CheckBoxPressedBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxPressedBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxPressedBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxPressedBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box background when selected.
	/// </summary>
	public Brush CheckBoxSelectedBrush
	{
		get => (Brush)GetValue(CheckBoxSelectedBrushProperty);
		set => SetValue(CheckBoxSelectedBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxSelectedBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxSelectedBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxSelectedBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box background when selected and disabled.
	/// </summary>
	public Brush CheckBoxSelectedDisabledBrush
	{
		get => (Brush)GetValue(CheckBoxSelectedDisabledBrushProperty);
		set => SetValue(CheckBoxSelectedDisabledBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxSelectedDisabledBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxSelectedDisabledBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxSelectedDisabledBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box background when selected and pointer over.
	/// </summary>
	public Brush CheckBoxSelectedPointerOverBrush
	{
		get => (Brush)GetValue(CheckBoxSelectedPointerOverBrushProperty);
		set => SetValue(CheckBoxSelectedPointerOverBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxSelectedPointerOverBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxSelectedPointerOverBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxSelectedPointerOverBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check box background when selected and pressed.
	/// </summary>
	public Brush CheckBoxSelectedPressedBrush
	{
		get => (Brush)GetValue(CheckBoxSelectedPressedBrushProperty);
		set => SetValue(CheckBoxSelectedPressedBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxSelectedPressedBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxSelectedPressedBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxSelectedPressedBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the corner radius of the check box.
	/// </summary>
	public CornerRadius CheckBoxCornerRadius
	{
		get => (CornerRadius)GetValue(CheckBoxCornerRadiusProperty);
		set => SetValue(CheckBoxCornerRadiusProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBoxCornerRadius dependency property.
	/// </summary>
	public static DependencyProperty CheckBoxCornerRadiusProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBoxCornerRadius),
			typeof(CornerRadius),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(CornerRadius), OnChromePropertyChanged));

	#endregion

	#region Check Brushes

	/// <summary>
	/// Gets or sets the brush used for the check mark.
	/// </summary>
	public Brush CheckBrush
	{
		get => (Brush)GetValue(CheckBrushProperty);
		set => SetValue(CheckBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check mark when disabled.
	/// </summary>
	public Brush CheckDisabledBrush
	{
		get => (Brush)GetValue(CheckDisabledBrushProperty);
		set => SetValue(CheckDisabledBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckDisabledBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckDisabledBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckDisabledBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check hint.
	/// </summary>
	public Brush CheckHintBrush
	{
		get => (Brush)GetValue(CheckHintBrushProperty);
		set => SetValue(CheckHintBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckHintBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckHintBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckHintBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check mark when pressed.
	/// </summary>
	public Brush CheckPressedBrush
	{
		get => (Brush)GetValue(CheckPressedBrushProperty);
		set => SetValue(CheckPressedBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckPressedBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckPressedBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckPressedBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the check mark when selecting.
	/// </summary>
	public Brush CheckSelectingBrush
	{
		get => (Brush)GetValue(CheckSelectingBrushProperty);
		set => SetValue(CheckSelectingBrushProperty, value);
	}

	/// <summary>
	/// Identifies the CheckSelectingBrush dependency property.
	/// </summary>
	public static DependencyProperty CheckSelectingBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckSelectingBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	#endregion

	#region CheckMode

	/// <summary>
	/// Gets or sets a value that indicates how check marks are displayed.
	/// </summary>
	public ListViewItemPresenterCheckMode CheckMode
	{
		get => (ListViewItemPresenterCheckMode)GetValue(CheckModeProperty);
		set => SetValue(CheckModeProperty, value);
	}

	/// <summary>
	/// Identifies the CheckMode dependency property.
	/// </summary>
	public static DependencyProperty CheckModeProperty { get; } =
		DependencyProperty.Register(
			nameof(CheckMode),
			typeof(ListViewItemPresenterCheckMode),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(ListViewItemPresenterCheckMode.Inline, OnChromePropertyChanged));

	#endregion

	#region Selection Indicator

	/// <summary>
	/// Gets or sets the brush used for the selection indicator.
	/// </summary>
	public Brush SelectionIndicatorBrush
	{
		get => (Brush)GetValue(SelectionIndicatorBrushProperty);
		set => SetValue(SelectionIndicatorBrushProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionIndicatorBrush dependency property.
	/// </summary>
	public static DependencyProperty SelectionIndicatorBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionIndicatorBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the selection indicator on pointer over.
	/// </summary>
	public Brush SelectionIndicatorPointerOverBrush
	{
		get => (Brush)GetValue(SelectionIndicatorPointerOverBrushProperty);
		set => SetValue(SelectionIndicatorPointerOverBrushProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionIndicatorPointerOverBrush dependency property.
	/// </summary>
	public static DependencyProperty SelectionIndicatorPointerOverBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionIndicatorPointerOverBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the selection indicator when pressed.
	/// </summary>
	public Brush SelectionIndicatorPressedBrush
	{
		get => (Brush)GetValue(SelectionIndicatorPressedBrushProperty);
		set => SetValue(SelectionIndicatorPressedBrushProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionIndicatorPressedBrush dependency property.
	/// </summary>
	public static DependencyProperty SelectionIndicatorPressedBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionIndicatorPressedBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the selection indicator when disabled.
	/// </summary>
	public Brush SelectionIndicatorDisabledBrush
	{
		get => (Brush)GetValue(SelectionIndicatorDisabledBrushProperty);
		set => SetValue(SelectionIndicatorDisabledBrushProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionIndicatorDisabledBrush dependency property.
	/// </summary>
	public static DependencyProperty SelectionIndicatorDisabledBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionIndicatorDisabledBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the corner radius of the selection indicator.
	/// </summary>
	public CornerRadius SelectionIndicatorCornerRadius
	{
		get => (CornerRadius)GetValue(SelectionIndicatorCornerRadiusProperty);
		set => SetValue(SelectionIndicatorCornerRadiusProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionIndicatorCornerRadius dependency property.
	/// </summary>
	public static DependencyProperty SelectionIndicatorCornerRadiusProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionIndicatorCornerRadius),
			typeof(CornerRadius),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(CornerRadius), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates the selection indicator display mode.
	/// </summary>
	public ListViewItemPresenterSelectionIndicatorMode SelectionIndicatorMode
	{
		get => (ListViewItemPresenterSelectionIndicatorMode)GetValue(SelectionIndicatorModeProperty);
		set => SetValue(SelectionIndicatorModeProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionIndicatorMode dependency property.
	/// </summary>
	public static DependencyProperty SelectionIndicatorModeProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionIndicatorMode),
			typeof(ListViewItemPresenterSelectionIndicatorMode),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(ListViewItemPresenterSelectionIndicatorMode), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the selection indicator visual is enabled.
	/// </summary>
	public bool SelectionIndicatorVisualEnabled
	{
		get => (bool)GetValue(SelectionIndicatorVisualEnabledProperty);
		set => SetValue(SelectionIndicatorVisualEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionIndicatorVisualEnabled dependency property.
	/// </summary>
	public static DependencyProperty SelectionIndicatorVisualEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionIndicatorVisualEnabled),
			typeof(bool),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(bool), OnChromePropertyChanged));

	#endregion

	#region Selected State

	/// <summary>
	/// Gets or sets the brush used for the background when selected.
	/// </summary>
	public Brush SelectedBackground
	{
		get => (Brush)GetValue(SelectedBackgroundProperty);
		set => SetValue(SelectedBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedBackground dependency property.
	/// </summary>
	public static DependencyProperty SelectedBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedBackground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the foreground when selected.
	/// </summary>
	public Brush SelectedForeground
	{
		get => (Brush)GetValue(SelectedForegroundProperty);
		set => SetValue(SelectedForegroundProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedForeground dependency property.
	/// </summary>
	public static DependencyProperty SelectedForegroundProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedForeground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the background when selected and pointer over.
	/// </summary>
	public Brush SelectedPointerOverBackground
	{
		get => (Brush)GetValue(SelectedPointerOverBackgroundProperty);
		set => SetValue(SelectedPointerOverBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedPointerOverBackground dependency property.
	/// </summary>
	public static DependencyProperty SelectedPointerOverBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedPointerOverBackground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the border when selected and pointer over.
	/// </summary>
	public Brush SelectedPointerOverBorderBrush
	{
		get => (Brush)GetValue(SelectedPointerOverBorderBrushProperty);
		set => SetValue(SelectedPointerOverBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedPointerOverBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty SelectedPointerOverBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedPointerOverBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the background when selected and pressed.
	/// </summary>
	public Brush SelectedPressedBackground
	{
		get => (Brush)GetValue(SelectedPressedBackgroundProperty);
		set => SetValue(SelectedPressedBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedPressedBackground dependency property.
	/// </summary>
	public static DependencyProperty SelectedPressedBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedPressedBackground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the border when selected and pressed.
	/// </summary>
	public Brush SelectedPressedBorderBrush
	{
		get => (Brush)GetValue(SelectedPressedBorderBrushProperty);
		set => SetValue(SelectedPressedBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedPressedBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty SelectedPressedBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedPressedBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the border when selected.
	/// </summary>
	public Brush SelectedBorderBrush
	{
		get => (Brush)GetValue(SelectedBorderBrushProperty);
		set => SetValue(SelectedBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty SelectedBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the background when selected and disabled.
	/// </summary>
	public Brush SelectedDisabledBackground
	{
		get => (Brush)GetValue(SelectedDisabledBackgroundProperty);
		set => SetValue(SelectedDisabledBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedDisabledBackground dependency property.
	/// </summary>
	public static DependencyProperty SelectedDisabledBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedDisabledBackground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the border when selected and disabled.
	/// </summary>
	public Brush SelectedDisabledBorderBrush
	{
		get => (Brush)GetValue(SelectedDisabledBorderBrushProperty);
		set => SetValue(SelectedDisabledBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedDisabledBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty SelectedDisabledBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedDisabledBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the inner border when selected.
	/// </summary>
	public Brush SelectedInnerBorderBrush
	{
		get => (Brush)GetValue(SelectedInnerBorderBrushProperty);
		set => SetValue(SelectedInnerBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedInnerBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty SelectedInnerBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedInnerBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the border thickness when selected.
	/// </summary>
	public Thickness SelectedBorderThickness
	{
		get => (Thickness)GetValue(SelectedBorderThicknessProperty);
		set => SetValue(SelectedBorderThicknessProperty, value);
	}

	/// <summary>
	/// Identifies the SelectedBorderThickness dependency property.
	/// </summary>
	public static DependencyProperty SelectedBorderThicknessProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectedBorderThickness),
			typeof(Thickness),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Thickness), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the selection check mark visual is shown.
	/// </summary>
	public bool SelectionCheckMarkVisualEnabled
	{
		get => (bool)GetValue(SelectionCheckMarkVisualEnabledProperty);
		set => SetValue(SelectionCheckMarkVisualEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the SelectionCheckMarkVisualEnabled dependency property.
	/// </summary>
	public static DependencyProperty SelectionCheckMarkVisualEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(SelectionCheckMarkVisualEnabled),
			typeof(bool),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(true, OnChromePropertyChanged));

	#endregion

	#region PointerOver State

	/// <summary>
	/// Gets or sets the brush used for the background on pointer over.
	/// </summary>
	public Brush PointerOverBackground
	{
		get => (Brush)GetValue(PointerOverBackgroundProperty);
		set => SetValue(PointerOverBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the PointerOverBackground dependency property.
	/// </summary>
	public static DependencyProperty PointerOverBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(PointerOverBackground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the border on pointer over.
	/// </summary>
	public Brush PointerOverBorderBrush
	{
		get => (Brush)GetValue(PointerOverBorderBrushProperty);
		set => SetValue(PointerOverBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the PointerOverBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty PointerOverBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(PointerOverBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the foreground on pointer over.
	/// </summary>
	public Brush PointerOverForeground
	{
		get => (Brush)GetValue(PointerOverForegroundProperty);
		set => SetValue(PointerOverForegroundProperty, value);
	}

	/// <summary>
	/// Identifies the PointerOverForeground dependency property.
	/// </summary>
	public static DependencyProperty PointerOverForegroundProperty { get; } =
		DependencyProperty.Register(
			nameof(PointerOverForeground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the margin around the background on pointer over.
	/// </summary>
	public Thickness PointerOverBackgroundMargin
	{
		get => (Thickness)GetValue(PointerOverBackgroundMarginProperty);
		set => SetValue(PointerOverBackgroundMarginProperty, value);
	}

	/// <summary>
	/// Identifies the PointerOverBackgroundMargin dependency property.
	/// </summary>
	public static DependencyProperty PointerOverBackgroundMarginProperty { get; } =
		DependencyProperty.Register(
			nameof(PointerOverBackgroundMargin),
			typeof(Thickness),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Thickness), OnChromePropertyChanged));

	#endregion

	#region Pressed State

	/// <summary>
	/// Gets or sets the brush used for the background when pressed.
	/// </summary>
	public Brush PressedBackground
	{
		get => (Brush)GetValue(PressedBackgroundProperty);
		set => SetValue(PressedBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the PressedBackground dependency property.
	/// </summary>
	public static DependencyProperty PressedBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(PressedBackground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	#endregion

	#region Focus

	/// <summary>
	/// Gets or sets the brush used for the focus border.
	/// </summary>
	public Brush FocusBorderBrush
	{
		get => (Brush)GetValue(FocusBorderBrushProperty);
		set => SetValue(FocusBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the FocusBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty FocusBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(FocusBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the secondary focus border.
	/// </summary>
	public Brush FocusSecondaryBorderBrush
	{
		get => (Brush)GetValue(FocusSecondaryBorderBrushProperty);
		set => SetValue(FocusSecondaryBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the FocusSecondaryBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty FocusSecondaryBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(FocusSecondaryBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	#endregion

	#region Drag

	/// <summary>
	/// Gets or sets the brush used for the background during drag.
	/// </summary>
	public Brush DragBackground
	{
		get => (Brush)GetValue(DragBackgroundProperty);
		set => SetValue(DragBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the DragBackground dependency property.
	/// </summary>
	public static DependencyProperty DragBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(DragBackground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the foreground during drag.
	/// </summary>
	public Brush DragForeground
	{
		get => (Brush)GetValue(DragForegroundProperty);
		set => SetValue(DragForegroundProperty, value);
	}

	/// <summary>
	/// Identifies the DragForeground dependency property.
	/// </summary>
	public static DependencyProperty DragForegroundProperty { get; } =
		DependencyProperty.Register(
			nameof(DragForeground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the opacity during drag operations.
	/// </summary>
	public double DragOpacity
	{
		get => (double)GetValue(DragOpacityProperty);
		set => SetValue(DragOpacityProperty, value);
	}

	/// <summary>
	/// Identifies the DragOpacity dependency property.
	/// </summary>
	public static DependencyProperty DragOpacityProperty { get; } =
		DependencyProperty.Register(
			nameof(DragOpacity),
			typeof(double),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(0.8, OnChromePropertyChanged));

	#endregion

	#region Reveal

	/// <summary>
	/// Gets or sets the brush used for the reveal background effect.
	/// </summary>
	public Brush RevealBackground
	{
		get => (Brush)GetValue(RevealBackgroundProperty);
		set => SetValue(RevealBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the RevealBackground dependency property.
	/// </summary>
	public static DependencyProperty RevealBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(RevealBackground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the brush used for the reveal border effect.
	/// </summary>
	public Brush RevealBorderBrush
	{
		get => (Brush)GetValue(RevealBorderBrushProperty);
		set => SetValue(RevealBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the RevealBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty RevealBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(RevealBorderBrush),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the reveal border thickness.
	/// </summary>
	public Thickness RevealBorderThickness
	{
		get => (Thickness)GetValue(RevealBorderThicknessProperty);
		set => SetValue(RevealBorderThicknessProperty, value);
	}

	/// <summary>
	/// Identifies the RevealBorderThickness dependency property.
	/// </summary>
	public static DependencyProperty RevealBorderThicknessProperty { get; } =
		DependencyProperty.Register(
			nameof(RevealBorderThickness),
			typeof(Thickness),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Thickness), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets a value that indicates whether the reveal background is shown above content.
	/// </summary>
	public bool RevealBackgroundShowsAboveContent
	{
		get => (bool)GetValue(RevealBackgroundShowsAboveContentProperty);
		set => SetValue(RevealBackgroundShowsAboveContentProperty, value);
	}

	/// <summary>
	/// Identifies the RevealBackgroundShowsAboveContent dependency property.
	/// </summary>
	public static DependencyProperty RevealBackgroundShowsAboveContentProperty { get; } =
		DependencyProperty.Register(
			nameof(RevealBackgroundShowsAboveContent),
			typeof(bool),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(bool), OnChromePropertyChanged));

	#endregion

	#region Other

	/// <summary>
	/// Gets or sets the brush used for the placeholder background.
	/// </summary>
	public Brush PlaceholderBackground
	{
		get => (Brush)GetValue(PlaceholderBackgroundProperty);
		set => SetValue(PlaceholderBackgroundProperty, value);
	}

	/// <summary>
	/// Identifies the PlaceholderBackground dependency property.
	/// </summary>
	public static DependencyProperty PlaceholderBackgroundProperty { get; } =
		DependencyProperty.Register(
			nameof(PlaceholderBackground),
			typeof(Brush),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Brush), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the content margin.
	/// </summary>
	public Thickness ContentMargin
	{
		get => (Thickness)GetValue(ContentMarginProperty);
		set => SetValue(ContentMarginProperty, value);
	}

	/// <summary>
	/// Identifies the ContentMargin dependency property.
	/// </summary>
	public static DependencyProperty ContentMarginProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentMargin),
			typeof(Thickness),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Thickness), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the opacity when disabled.
	/// </summary>
	public double DisabledOpacity
	{
		get => (double)GetValue(DisabledOpacityProperty);
		set => SetValue(DisabledOpacityProperty, value);
	}

	/// <summary>
	/// Identifies the DisabledOpacity dependency property.
	/// </summary>
	public static DependencyProperty DisabledOpacityProperty { get; } =
		DependencyProperty.Register(
			nameof(DisabledOpacity),
			typeof(double),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(0.3, OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the offset for the reorder hint.
	/// </summary>
	public double ReorderHintOffset
	{
		get => (double)GetValue(ReorderHintOffsetProperty);
		set => SetValue(ReorderHintOffsetProperty, value);
	}

	/// <summary>
	/// Identifies the ReorderHintOffset dependency property.
	/// </summary>
	public static DependencyProperty ReorderHintOffsetProperty { get; } =
		DependencyProperty.Register(
			nameof(ReorderHintOffset),
			typeof(double),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(10.0, OnChromePropertyChanged));

	#endregion

	#region Legacy Properties

	/// <summary>
	/// Gets or sets the padding for the list view item presenter. This is a legacy property that maps to the base Padding property.
	/// </summary>
	public Thickness ListViewItemPresenterPadding
	{
		get => Padding;
		set => Padding = value;
	}

	/// <summary>
	/// Identifies the ListViewItemPresenterPadding dependency property.
	/// </summary>
	public static DependencyProperty ListViewItemPresenterPaddingProperty { get; } =
		DependencyProperty.Register(
			nameof(ListViewItemPresenterPadding),
			typeof(Thickness),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(Thickness), OnChromePropertyChanged));

	/// <summary>
	/// Gets or sets the horizontal content alignment. This is a legacy property that maps to the base HorizontalContentAlignment property.
	/// </summary>
	public HorizontalAlignment ListViewItemPresenterHorizontalContentAlignment
	{
		get => HorizontalContentAlignment;
		set => HorizontalContentAlignment = value;
	}

	/// <summary>
	/// Identifies the ListViewItemPresenterHorizontalContentAlignment dependency property.
	/// </summary>
	public static DependencyProperty ListViewItemPresenterHorizontalContentAlignmentProperty { get; } =
		DependencyProperty.Register(
			nameof(ListViewItemPresenterHorizontalContentAlignment),
			typeof(HorizontalAlignment),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(HorizontalAlignment)));

	/// <summary>
	/// Gets or sets the vertical content alignment. This is a legacy property that maps to the base VerticalContentAlignment property.
	/// </summary>
	public VerticalAlignment ListViewItemPresenterVerticalContentAlignment
	{
		get => VerticalContentAlignment;
		set => VerticalContentAlignment = value;
	}

	/// <summary>
	/// Identifies the ListViewItemPresenterVerticalContentAlignment dependency property.
	/// </summary>
	public static DependencyProperty ListViewItemPresenterVerticalContentAlignmentProperty { get; } =
		DependencyProperty.Register(
			nameof(ListViewItemPresenterVerticalContentAlignment),
			typeof(VerticalAlignment),
			typeof(ListViewItemPresenter),
			new FrameworkPropertyMetadata(default(VerticalAlignment)));

	#endregion
}

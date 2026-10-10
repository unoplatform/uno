#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Helper;
using Windows.System;

namespace Uno.UI.Samples.Tests;

/// <summary>
/// The runner's 5 px splitter cell: a 1 px divider line, a resize cursor and keyboard resizing.
/// Dragging stays with the owner's ManipulationDelta handler.
/// </summary>
public sealed partial class ShellSplitter : Grid
{
	internal const double KeyboardStep = 16;

	private readonly Rectangle _line = new() { IsHitTestVisible = false };
	private readonly ContentControl _focusTarget = new()
	{
		IsTabStop = true,
		UseSystemFocusVisuals = true,
		HorizontalAlignment = HorizontalAlignment.Stretch,
		VerticalAlignment = VerticalAlignment.Stretch,
		HorizontalContentAlignment = HorizontalAlignment.Stretch,
		VerticalContentAlignment = VerticalAlignment.Stretch,
	};
	private bool _isPointerOver;

	public ShellSplitter()
	{
		Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
		Children.Add(_line);
		Children.Add(_focusTarget);

		PointerEntered += (_, _) => SetPointerOver(true);
		PointerExited += (_, _) => SetPointerOver(false);
		PointerCanceled += (_, _) => SetPointerOver(false);
		DoubleTapped += (_, e) =>
		{
			e.Handled = true;
			ResetRequested?.Invoke(this, EventArgs.Empty);
		};
		_focusTarget.KeyDown += OnFocusTargetKeyDown;
		_focusTarget.GotFocus += (_, _) => UpdateLine();
		_focusTarget.LostFocus += (_, _) => UpdateLine();
		ActualThemeChanged += (_, _) => UpdateLine();
		Loaded += (_, _) =>
		{
			AutomationProperties.SetName(_focusTarget, AutomationProperties.GetName(this));
			UpdateLine();
		};

		ApplyOrientation();
	}

	/// <summary>Horizontal resizes a row (Up/Down), Vertical resizes a column (Left/Right).</summary>
	public Orientation Orientation
	{
		get => (Orientation)GetValue(OrientationProperty);
		set => SetValue(OrientationProperty, value);
	}

	public static DependencyProperty OrientationProperty { get; } =
		DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(ShellSplitter), new PropertyMetadata(Orientation.Vertical, OnOrientationChanged));

	private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((ShellSplitter)d).ApplyOrientation();

	/// <summary>Raised with the size change in DIPs when an arrow key is pressed.</summary>
	public event EventHandler<double>? KeyboardResize;

	/// <summary>Raised on Home or a double tap.</summary>
	public event EventHandler? ResetRequested;

	internal static double? GetKeyboardDelta(VirtualKey key, Orientation orientation, bool isRightToLeft) => (key, orientation) switch
	{
		(VirtualKey.Up, Orientation.Horizontal) => -KeyboardStep,
		(VirtualKey.Down, Orientation.Horizontal) => KeyboardStep,
		(VirtualKey.Left, Orientation.Vertical) => isRightToLeft ? KeyboardStep : -KeyboardStep,
		(VirtualKey.Right, Orientation.Vertical) => isRightToLeft ? -KeyboardStep : KeyboardStep,
		_ => null,
	};

	private void OnFocusTargetKeyDown(object sender, KeyRoutedEventArgs e) => e.Handled = HandleKey(e.Key);

	internal bool HandleKey(VirtualKey key)
	{
		if (key == VirtualKey.Home)
		{
			ResetRequested?.Invoke(this, EventArgs.Empty);
			return true;
		}

		if (GetKeyboardDelta(key, Orientation, FlowDirection == FlowDirection.RightToLeft) is { } delta)
		{
			KeyboardResize?.Invoke(this, delta);
			return true;
		}

		return false;
	}

	private void ApplyOrientation()
	{
		var isRow = Orientation == Orientation.Horizontal;
		_line.Height = isRow ? 1 : double.NaN;
		_line.Width = isRow ? double.NaN : 1;
		_line.HorizontalAlignment = isRow ? HorizontalAlignment.Stretch : HorizontalAlignment.Center;
		_line.VerticalAlignment = isRow ? VerticalAlignment.Center : VerticalAlignment.Stretch;

		try
		{
			ProtectedCursor = InputSystemCursor.Create(isRow ? InputSystemCursorShape.SizeNorthSouth : InputSystemCursorShape.SizeWestEast);
		}
		catch (Exception)
		{
			// Hosts without cursor support keep the default cursor.
		}
	}

	private void SetPointerOver(bool isOver)
	{
		_isPointerOver = isOver;
		UpdateLine();
	}

	private void UpdateLine()
		=> _line.Fill = ShellThemeBrushes.Get(
			_isPointerOver || _focusTarget.FocusState != FocusState.Unfocused ? "ControlStrongStrokeColorDefaultBrush" : "DividerStrokeColorDefaultBrush",
			ActualTheme);
}

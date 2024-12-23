#nullable enable

using Windows.Foundation;
using Windows.System;

namespace Windows.UI.Xaml.Input;

partial class KeyboardAccelerator
{
	/// <summary>
	/// Gets or sets whether a keyboard shortcut (accelerator) is available to the user.
	/// </summary>
	public bool IsEnabled
	{
		get => (bool)GetValue(IsEnabledProperty);
		set => SetValue(IsEnabledProperty, value);
	}

	/// <summary>
	/// Identifies the IsEnabled dependency property.
	/// </summary>
	public static DependencyProperty IsEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsEnabled),
			typeof(bool),
			typeof(KeyboardAccelerator),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets or sets the virtual key (used in conjunction with one or more modifier keys) for a keyboard shortcut (accelerator).
	/// A keyboard shortcut is invoked when the modifier keys associated with the shortcut are pressed and then the non-modifier
	/// key is pressed at the same time.For example, Ctrl+C for copy and Ctrl+S for save.
	/// </summary>
	public VirtualKey Key
	{
		get => (VirtualKey)GetValue(KeyProperty);
		set => SetValue(KeyProperty, value);
	}

	/// <summary>
	/// Identifies the Key dependency property.
	/// </summary>
	public static DependencyProperty KeyProperty { get; } =
			DependencyProperty.Register(
				nameof(Key),
				typeof(VirtualKey),
				typeof(KeyboardAccelerator),
				new FrameworkPropertyMetadata(VirtualKey.None));

	/// <summary>
	/// Gets or sets the virtual key used to modify another keypress for a keyboard shortcut (accelerator).
	/// A keyboard shortcut is invoked when the modifier keys associated with the shortcut are pressed and then 
	/// the non-modifier key is pressed at the same time.For example, Ctrl+C for copy and Ctrl+S for save.
	/// </summary>
	public VirtualKeyModifiers Modifiers
	{
		get => (VirtualKeyModifiers)GetValue(ModifiersProperty);
		set => SetValue(ModifiersProperty, value);
	}

	/// <summary>
	/// Identifies the Modifiers dependency property.
	/// </summary>
	public static DependencyProperty ModifiersProperty { get; } =
		DependencyProperty.Register(
			nameof(Modifiers),
			typeof(VirtualKeyModifiers),
			typeof(KeyboardAccelerator),
			new FrameworkPropertyMetadata(VirtualKeyModifiers.None));

	/// <summary>
	/// Gets or sets the scope (or target) of the keyboard accelerator.
	/// </summary>
	public DependencyObject? ScopeOwner
	{
		get => (DependencyObject?)GetValue(ScopeOwnerProperty);
		set => SetValue(ScopeOwnerProperty, value);
	}

	/// <summary>
	/// Identifies the ScopeOwner dependency property.
	/// </summary>
	public static DependencyProperty ScopeOwnerProperty { get; } =
		DependencyProperty.Register(
			nameof(ScopeOwner),
			typeof(DependencyObject),
			typeof(KeyboardAccelerator),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

	/// <summary>
	/// Occurs when the key combination for this KeyboardAccelerator is pressed.
	/// </summary>
	public event TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> Invoked;
}

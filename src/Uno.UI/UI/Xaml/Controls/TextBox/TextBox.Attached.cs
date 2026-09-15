using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

public class TextBoxExtensions
{
	public static InputReturnType GetInputReturnType(DependencyObject obj) => (InputReturnType)obj.GetValue(InputReturnTypeProperty);

	public static void SetInputReturnType(DependencyObject obj, InputReturnType value) => obj.SetValue(InputReturnTypeProperty, value);

	[DynamicDependency(nameof(GetInputReturnType))]
	[DynamicDependency(nameof(SetInputReturnType))]
	public static readonly DependencyProperty InputReturnTypeProperty =
		DependencyProperty.RegisterAttached(
			nameof(InputReturnType),
			typeof(InputReturnType),
			typeof(TextBox),
			new FrameworkPropertyMetadata(InputReturnType.Default, OnInputReturnTypeChanged));

	private static void OnInputReturnTypeChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		// Attached to any text-input control, so it resolves the engine rather than a concrete control type.
		if (dependencyObject is not ITextBoxHost { Core: { } core })
		{
			return;
		}

		core.OnInputReturnTypeChanged((InputReturnType)args.NewValue, initial: false);
	}

	/// <summary>
	/// Gets whether the text inputs at or below <paramref name="obj"/> show a bar carrying a "Done" button
	/// above the soft keyboard, which dismisses it.
	/// </summary>
	public static bool GetShowKeyboardDismissButton(DependencyObject obj) => (bool)obj.GetValue(ShowKeyboardDismissButtonProperty);

	/// <summary>
	/// Sets whether the text inputs at or below <paramref name="obj"/> show a bar carrying a "Done" button
	/// above the soft keyboard, which dismisses it.
	/// </summary>
	public static void SetShowKeyboardDismissButton(DependencyObject obj, bool value) => obj.SetValue(ShowKeyboardDismissButtonProperty, value);

	/// <summary>
	/// Shows a bar with a "Done" button above the soft keyboard, dismissing it when tapped. This is meant
	/// for inputs the Enter key cannot dismiss the keyboard from - a multi-line <see cref="TextBox"/>, where
	/// Enter inserts a newline, being the primary case.
	/// </summary>
	/// <remarks>
	/// The value is inherited: set it on the input itself, or on any element above one - a page or a container
	/// opts in every text input below it, and an input can opt back out locally.
	/// This is currently an iOS-only feature; other targets ignore the property, as their soft keyboards
	/// already offer a way out (the Android back button) or are backed by a hardware keyboard.
	/// </remarks>
	[DynamicDependency(nameof(GetShowKeyboardDismissButton))]
	[DynamicDependency(nameof(SetShowKeyboardDismissButton))]
	public static readonly DependencyProperty ShowKeyboardDismissButtonProperty =
		DependencyProperty.RegisterAttached(
			"ShowKeyboardDismissButton",
			typeof(bool),
			typeof(TextBox),
			new FrameworkPropertyMetadata(
				defaultValue: false,
				options: FrameworkPropertyMetadataOptions.Inherits,
				propertyChangedCallback: OnShowKeyboardDismissButtonChanged));

	private static void OnShowKeyboardDismissButtonChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
	{
		// Inherited, so this is raised on every text input below the element the value was set on.
		if (dependencyObject is not ITextBoxHost { Core: { } core })
		{
			return;
		}

		core.OnShowKeyboardDismissButtonChanged();
	}
}

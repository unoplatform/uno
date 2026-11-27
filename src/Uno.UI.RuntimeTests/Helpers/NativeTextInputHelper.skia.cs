#nullable enable

using System;
using System.Globalization;
using System.Reflection;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Helpers;

/// <summary>
/// Reads the native view behind a text input's entry (the iOS UITextField proxy), which lives in a platform runtime the
/// tests can't reference. Only reads members the runtime itself uses, so that they survive trimming.
/// </summary>
internal static class NativeTextInputHelper
{
	/// <summary>The native view of the input, or null when it has none.</summary>
	public static object? GetNativeView(Control input)
	{
		var textBoxView = input switch
		{
			TextBox textBox => textBox.Core.TextBoxView,
			PasswordBox passwordBox => passwordBox.Core.TextBoxView,
			_ => throw new ArgumentException($"{input.GetType().Name} is not a text input.", nameof(input)),
		};
		var extension = textBoxView.Extension ?? throw new InvalidOperationException("The input has no native view extension.");
		var field = extension.GetType().GetField("_textBoxView", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"{extension.GetType().Name} has no native view.");

		return field.GetValue(extension);
	}

	public static object? GetMember(object instance, string name)
		=> GetProperty(instance, name).GetValue(instance);

	public static void SetMember(object instance, string name, object? value)
		=> GetProperty(instance, name).SetValue(instance, value);

	public static object? Invoke(object instance, string methodName)
		=> (instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public, Type.EmptyTypes)
			?? throw new InvalidOperationException($"{instance.GetType().Name} has no {methodName}() method.")).Invoke(instance, null);

	public static bool IsInHierarchy(object? nativeView)
		=> nativeView is not null && GetMember(nativeView, "Superview") is not null;

	/// <summary>The top-left corner of the native view's frame, in points.</summary>
	public static (double X, double Y) GetFrameOrigin(object nativeView)
	{
		var frame = GetMember(nativeView, "Frame")!;
		return (ToDouble(GetMember(frame, "X")), ToDouble(GetMember(frame, "Y")));
	}

	// UIKit coordinates are NFloat, which doesn't implement IConvertible.
	private static double ToDouble(object? value)
		=> value is IConvertible convertible
			? convertible.ToDouble(CultureInfo.InvariantCulture)
			: double.Parse(value?.ToString() ?? "", CultureInfo.CurrentCulture);

	private static PropertyInfo GetProperty(object instance, string name)
		=> instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"{instance.GetType().Name} has no {name} property.");
}

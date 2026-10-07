#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Samples.Helper;

/// <summary>
/// Swaps ShellMonospaceTextStyle's Consolas for the host's own monospace font off Windows. A XAML family
/// fallback list would do it on WinUI, but Uno Skia reads the whole list as a single family name.
/// </summary>
public partial class ShellMonospace
{
	private static FontFamily? _hostFontFamily;

	public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

	public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

	public static DependencyProperty IsEnabledProperty { get; } =
		DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ShellMonospace), new PropertyMetadata(false, OnIsEnabledChanged));

	/// <summary>The family to use instead of Consolas, or null where Consolas is installed.</summary>
	internal static string? HostFamilyName =>
		OperatingSystem.IsWindows() ? null
		: OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() ? "Menlo"
		: "monospace";

	private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is true && d is TextBlock text && HostFamilyName is { } name)
		{
			text.FontFamily = _hostFontFamily ??= new FontFamily(name);
		}
	}
}

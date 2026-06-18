#nullable enable

using Uno.Foundation.Extensibility;

namespace Windows.UI.ViewManagement;

/// <summary>
/// Extension interface for Skia platforms to provide text scale factor from the OS.
/// </summary>
internal interface ITextScaleFactorExtension
{
	event global::System.EventHandler TextScaleFactorChanged;
	double GetTextScaleFactor();
}

/// <summary>
/// Extension interface for Skia platforms to provide the OS accent color.
/// </summary>
internal interface IAccentColorExtension
{
	event global::System.EventHandler AccentColorChanged;
	Color GetAccentColor();
}

public partial class UISettings
{
	private static ITextScaleFactorExtension? _textScaleFactorExtension;
	private static IAccentColorExtension? _accentColorExtension;
	private static bool _accentColorExtensionChecked;
	private static bool _accentColorChangesObserved;

	public double TextScaleFactor => GetTextScaleFactorValue();

	internal static double GetTextScaleFactorValue() => GetTextScaleFactorExtension()?.GetTextScaleFactor() ?? 1.0;

	private static ITextScaleFactorExtension? GetTextScaleFactorExtension()
	{
		if (_textScaleFactorExtension is null)
		{
			ApiExtensibility.CreateInstance(typeof(UISettings), out _textScaleFactorExtension);
		}

		return _textScaleFactorExtension;
	}

	internal static IAccentColorExtension? GetAccentColorExtension()
	{
		if (!_accentColorExtensionChecked)
		{
			ApiExtensibility.CreateInstance(typeof(UISettings), out _accentColorExtension);
			_accentColorExtensionChecked = true;
		}

		return _accentColorExtension;
	}

	static partial void ObserveTextScaleFactorChangesPlatform()
	{
		if (GetTextScaleFactorExtension() is { } extension)
		{
			extension.TextScaleFactorChanged += (_, _) =>
			{
				TextScaleFactorChangedInternal?.Invoke(null, global::System.EventArgs.Empty);
			};
		}
	}

	static partial void ObserveAccentColorChangesPlatform()
	{
		if (_accentColorChangesObserved)
		{
			return;
		}

		if (GetAccentColorExtension() is { } extension)
		{
			_accentColorChangesObserved = true;
			extension.AccentColorChanged += (_, _) => OnColorValuesChanged();
		}
	}
}

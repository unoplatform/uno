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

public partial class UISettings
{
	private static ITextScaleFactorExtension? _textScaleFactorExtension;

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
}

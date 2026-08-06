#if __SKIA__ || __NETSTD_REFERENCE__

#if __SKIA__
using System.Linq;
#endif
using Uno.Helpers.Theming;
using Uno.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Microsoft.UI.Xaml;

public partial class UIElement
{
	private static readonly UISettings _highContrastUISettings = new();

	// WinUI default: Application. The property is inherited so subtree overrides match the
	// UIElement metadata contract in the WinUI XamlOM model.
	[GeneratedDependencyProperty(
		DefaultValue = ElementHighContrastAdjustment.Application,
		Options = FrameworkPropertyMetadataOptions.Inherits,
		ChangedCallback = true)]
#if __NETSTD_REFERENCE__
	[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
#endif
	public static DependencyProperty HighContrastAdjustmentProperty { get; } = CreateHighContrastAdjustmentProperty();

	/// <summary>
	/// Gets or sets a value that indicates whether the framework automatically adjusts the element's visual properties when high contrast themes are enabled.
	/// </summary>
#if __NETSTD_REFERENCE__
	[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
#endif
	public ElementHighContrastAdjustment HighContrastAdjustment
	{
		get => GetHighContrastAdjustmentValue();
		set => SetHighContrastAdjustmentValue(value);
	}

	internal bool IsHighContrastAdjustmentEnabled() =>
		HighContrastAdjustment == ElementHighContrastAdjustment.Auto
		|| (HighContrastAdjustment == ElementHighContrastAdjustment.Application
			&& (Application.Current?.HighContrastAdjustment ?? ApplicationHighContrastAdjustment.Auto) == ApplicationHighContrastAdjustment.Auto);

	internal bool UseHighContrastAdjustment() =>
		ThemingHelper.IsHighContrastActive && IsHighContrastAdjustmentEnabled();

	internal (Color foreground, Color background, Color selectionForeground, Color selectionBackground) GetHighContrastTextColors()
	{
		var useDisabledTextColor = IsInDisabledSubtree();
		if (SystemThemeHelper.HighContrastSystemColors is { } colors)
		{
			return (
				useDisabledTextColor ? colors.DisabledTextColor : colors.WindowTextColor,
				colors.WindowColor,
				colors.HighlightTextColor,
				colors.HighlightColor);
		}

		var foreground = _highContrastUISettings.GetColorValue(UIColorType.Foreground);
		var background = _highContrastUISettings.GetColorValue(UIColorType.Background);

		return (
			ResolveHighContrastColor(
				useDisabledTextColor ? "SystemColorDisabledTextColor" : "SystemColorWindowTextColor",
				foreground),
			ResolveHighContrastColor("SystemColorWindowColor", background),
			ResolveHighContrastColor("SystemColorHighlightTextColor", foreground),
			ResolveHighContrastColor("SystemColorHighlightColor", background));
	}

	private bool IsInDisabledSubtree()
	{
		for (UIElement current = this; current is not null; current = current.GetVisualTreeParent())
		{
			if (current is Controls.Control { IsEnabled: false })
			{
				return true;
			}
		}

		return false;
	}

	private static Color ResolveHighContrastColor(string resourceKey, Color fallback) =>
		Uno.UI.ResourceResolver.ResolveTopLevelResource(resourceKey, fallback) is Color color
			? color
			: fallback;

	private void OnHighContrastAdjustmentChanged(
		ElementHighContrastAdjustment oldValue,
		ElementHighContrastAdjustment newValue)
	{
#if __SKIA__
		UpdateHighContrastOpacityOverride();
#endif
	}

	internal void NotifyApplicationHighContrastAdjustmentChangedCore()
	{
#if __SKIA__
		if (HighContrastAdjustment == ElementHighContrastAdjustment.Application)
		{
			UpdateHighContrastOpacityOverride();
		}

		foreach (var child in _children.ToArray())
		{
			child.NotifyApplicationHighContrastAdjustmentChangedCore();
		}
#endif
	}

#if __SKIA__
	internal void UpdateHighContrastOpacityOverride(bool forceInvalidate = false)
	{
		var visual = Hosting.ElementCompositionPreview.GetElementVisual(this);
		var useHighContrastAdjustment = UseHighContrastAdjustment();
		var adjustmentChanged = visual.IsHighContrastOpacityOverrideActive != useHighContrastAdjustment;
		visual.IsHighContrastOpacityOverrideActive = useHighContrastAdjustment;

		if (adjustmentChanged || forceInvalidate)
		{
			visual.InvalidateSubtreePaint();
			visual.Compositor.InvalidateRender(visual);
		}

		(this as IHighContrastAdjustmentAware)?.OnHighContrastAdjustmentChanged();
	}
#endif
}

#endif

using Uno.Helpers.Theming;
using Uno.UI.Xaml;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace Microsoft.UI.Xaml
{
	public partial class UIElement
	{
		[GeneratedDependencyProperty(
			DefaultValue = ElementHighContrastAdjustment.Application,
			Options = FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender,
			ChangedCallback = true)]
		public static DependencyProperty HighContrastAdjustmentProperty { get; } = CreateHighContrastAdjustmentProperty();

		public ElementHighContrastAdjustment HighContrastAdjustment
		{
			get => GetHighContrastAdjustmentValue();
			set => SetHighContrastAdjustmentValue(value);
		}

		private static readonly UISettings _highContrastUiSettings = new();

		internal ApplicationHighContrastAdjustment GetEffectiveHighContrastAdjustment()
			=> HighContrastAdjustment switch
			{
				ElementHighContrastAdjustment.None => ApplicationHighContrastAdjustment.None,
				ElementHighContrastAdjustment.Auto => ApplicationHighContrastAdjustment.Auto,
				_ => Application.Current?.HighContrastAdjustment ?? ApplicationHighContrastAdjustment.Auto,
			};

		internal bool UseHighContrastTextAdjustment()
			=> AccessibilitySettings.IsHighContrastActive
				&& GetEffectiveHighContrastAdjustment() == ApplicationHighContrastAdjustment.Auto;

		internal float GetEffectiveTextOpacity(float opacity)
			=> UseHighContrastTextAdjustment() && opacity > 0 ? 1f : opacity;

		// MUX Reference hwwalk.cpp ShouldOverrideRenderOpacity
		// Returns true when HC is active and this element's effective HighContrastAdjustment
		// resolves to Auto — meaning opacity should be forced to 1 during rendering.
		internal bool ShouldOverrideRenderOpacity()
			=> AccessibilitySettings.IsHighContrastActive
				&& GetEffectiveHighContrastAdjustment() == ApplicationHighContrastAdjustment.Auto;

		private void OnHighContrastAdjustmentChanged(ElementHighContrastAdjustment oldValue, ElementHighContrastAdjustment newValue)
		{
#if __SKIA__
			UpdateHighContrastOpacityOverride();
#endif
		}

#if __SKIA__
		/// <summary>
		/// Updates the Visual's HC opacity override flag based on current HC state
		/// and this element's effective HighContrastAdjustment.
		/// </summary>
		internal void UpdateHighContrastOpacityOverride()
		{
			var visual = Hosting.ElementCompositionPreview.GetElementVisual(this);
			visual.IsHighContrastOpacityOverrideActive = ShouldOverrideRenderOpacity();
		}
#endif

		internal static (Color foreground, Color background, Color highlightForeground) GetHighContrastTextColors()
		{
			var highContrastColors = SystemThemeHelper.HighContrastSystemColors;

			return (
				foreground: _highContrastUiSettings.GetColorValue(UIColorType.Foreground),
				background: _highContrastUiSettings.GetColorValue(UIColorType.Background),
				highlightForeground: highContrastColors?.HighlightTextColor ?? _highContrastUiSettings.GetColorValue(UIColorType.Foreground));
		}
	}
}
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
			Options = FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender)]
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
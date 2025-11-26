using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Text;
using Windows.UI.ViewManagement;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Represents a button control that functions as a hyperlink.
	/// </summary>
	public partial class HyperlinkButton : ButtonBase
	{
		private const string HyperlinkUnderlineVisibleKey = "HyperlinkUnderlineVisible";

		/// <summary>
		/// Initializes a new instance of the HyperlinkButton class.
		/// </summary>
		public HyperlinkButton()
		{
			DefaultStyleKey = typeof(HyperlinkButton);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// This differs from UWP, where it looks for a template child named "ContentPresenter", 
			// but ultimately sets the underline on the TextBlock of the first ContentPresenter with a {TemplateBinding Content}.
			// UWP also doesn't seem to do this in OnApplyTemplate (it's done between the first Measure and the first Arrange).
			if (GetTemplateChild("ContentPresenter") is ContentPresenter contentPresenter)
			{
				// Forces ContentPresenter to materialize its template.
				contentPresenter.Measure(new Size(0, 0));
				if (VisualTreeHelper.GetChildrenCount(contentPresenter) == 1 && VisualTreeHelper.GetChild(contentPresenter, 0) is ImplicitTextBlock textBlock)
				{
					// Only apply underline if HighContrast is enabled OR HyperlinkUnderlineVisible is true
					if (ShouldUnderlineHyperlink())
					{
						textBlock.TextDecorations = TextDecorations.Underline;
					}
				}
			}
		}

		private bool ShouldUnderlineHyperlink()
		{
			// Check if high contrast is enabled
			var accessibilitySettings = new AccessibilitySettings();
			if (accessibilitySettings.HighContrast)
			{
				return true;
			}

			// Check if HyperlinkUnderlineVisible resource is set to true
			if (Application.Current?.Resources.TryGetValue(HyperlinkUnderlineVisibleKey, out var underlineVisible) == true
				&& underlineVisible is bool boolValue
				&& boolValue)
			{
				return true;
			}

			return false;
		}

		#region NavigateUri

		public Uri NavigateUri
		{
			get => (Uri)GetValue(NavigateUriProperty);
			set => SetValue(NavigateUriProperty, value);
		}

		/// <summary>
		/// Identifies the NavigateUri dependency property.
		/// </summary>
		public static DependencyProperty NavigateUriProperty { get; } =
			DependencyProperty.Register(
				nameof(NavigateUri),
				typeof(Uri),
				typeof(HyperlinkButton),
				new FrameworkPropertyMetadata(default(Uri)));

		#endregion
	}
}

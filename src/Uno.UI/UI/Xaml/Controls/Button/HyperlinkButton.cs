using System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Text;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Represents a button control that functions as a hyperlink.
	/// </summary>
	public partial class HyperlinkButton : ButtonBase
	{
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
					textBlock.TextDecorations = TextDecorations.Underline;
				}
			}
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

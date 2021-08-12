using System;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

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

		/// <summary>
		/// Gets or sets the Uniform Resource Identifier (URI) to navigate to when the HyperlinkButton is clicked.
		/// </summary>
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
	}
}

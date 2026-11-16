using System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Controls
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

using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class HyperlinkButton : ButtonBase
	{
		public HyperlinkButton()
		{
			InitializeVisualStates();

			Click += (s, e) => TryNavigate();
		}

		#region NavigateUri

		public Uri NavigateUri
		{
			get { return (Uri)GetValue(NavigateUriProperty); }
			set { SetValue(NavigateUriProperty, value); }
		}

		public static global::Windows.UI.Xaml.DependencyProperty NavigateUriProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"NavigateUri", typeof(global::System.Uri),
				typeof(global::Windows.UI.Xaml.Controls.HyperlinkButton),
				new FrameworkPropertyMetadata(default(global::System.Uri)));

		#endregion

		private void TryNavigate()
		{
			if (NavigateUri != null)
			{
				Launcher.LaunchUriAsync(NavigateUri);
			}
		}

		partial void NavigatePartial();
	}
}

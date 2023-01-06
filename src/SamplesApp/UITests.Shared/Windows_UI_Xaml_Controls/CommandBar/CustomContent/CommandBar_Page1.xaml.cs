using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

#if __IOS__
using UIKit;
using Uno.UI.Controls;
#endif

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.CommandBar.CustomContent
{
	public sealed partial class CommandBar_Page1 : Page
	{
		public CommandBar_Page1()
		{
			this.InitializeComponent();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
#if __IOS__
			UIView parent = this;
			while (parent.HasParent())
			{
				parent = parent.Superview;
			}

			var titleViewChild = parent.FindFirstChild<TitleView>()?.Child;

			var height = titleViewChild?.Frame.Height ?? 0;
			var width = titleViewChild?.Frame.Width ?? 0;

			var expectedHeight = titleViewChild?.DesiredSize.Height ?? 0;
			var expectedWidth = titleViewChild?.DesiredSize.Width ?? 0;

			ExpectedSize.Text = $"Title Content desired size: ({expectedWidth}x{expectedHeight})";
			CurrentSize.Text = $"Title Content current size: ({width}x{height})";

			Result.Text = height > 0 && width > 0 ? "PASSED" : "FAILED";
#endif
		}
	}
}

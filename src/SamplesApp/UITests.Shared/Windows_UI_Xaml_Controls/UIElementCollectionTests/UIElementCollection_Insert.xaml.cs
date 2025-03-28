using System;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.UIElementCollectionTests
{
	[Sample(
		"UIElement",
		Description = "This test asserts that an element inserted after the first draw is really inserted at the right place",
		IgnoreInSnapshotTests = false)]
	public sealed partial class UIElementCollection_Insert : Page
	{
		public UIElementCollection_Insert()
		{
			this.InitializeComponent();

			Loaded += (snd, e) =>
			{
				When_Insert_First.Children.Insert(0, new Border
				{
					Background = new SolidColorBrush(Color.FromArgb(0xff, 0xff, 0x00, 0x00)),
					Margin = new Thickness(0),
				});

				When_Insert_Middle.Children.Insert(3, new Border
				{
					Background = new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x80, 0x00)),
					Margin = new Thickness(30, 0, 30, 0),
				});

				When_Insert_Last.Children.Add(new Border
				{
					Background = new SolidColorBrush(Color.FromArgb(0xff, 0xa0, 0x00, 0xc0)),
					Margin = new Thickness(50, 0, 50, 0),
				});
			};
		}
	}
}

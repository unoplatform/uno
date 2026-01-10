using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("ScrollViewer")]
	public sealed partial class ScrollViewer_Options : Page
	{
		private static string[] Sizes = new[] { "Small", "Large" };
		public ScrollViewer_Options()
		{
			this.InitializeComponent();

			var scrollModes = Enum
				.GetValues(typeof(ScrollMode))
				.Cast<ScrollMode>()
				.Select(x => x.ToString())
				.ToArray();

			horizontalMode.ItemsSource = scrollModes;
			horizontalMode.SelectedIndex = 0;
			verticalMode.ItemsSource = scrollModes;
			verticalMode.SelectedIndex = 0;

			var visibilities = Enum
				.GetValues(typeof(ScrollBarVisibility))
				.Cast<ScrollBarVisibility>()
				.Select(x => x.ToString())
				.ToArray();

			horizontalVisibility.ItemsSource = visibilities;
			horizontalVisibility.SelectedIndex = 0;
			verticalVisibility.ItemsSource = visibilities;
			verticalVisibility.SelectedIndex = 0;

			contentSize.ItemsSource = Sizes;
			contentSize.SelectedIndex = 0;

			horizontalMode.SelectionChanged += (snd, evt) => Update();
			verticalMode.SelectionChanged += (snd, evt) => Update();
			horizontalVisibility.SelectionChanged += (snd, evt) => Update();
			verticalVisibility.SelectionChanged += (snd, evt) => Update();
			contentSize.SelectionChanged += (snd, evt) => Update();

			Update();
		}

		private void Update()
		{
			if (contentSize.SelectedItem as string == Sizes[0])
			{
				content.Width = 300;
				content.Height = 300;
			}
			else
			{
				content.Width = 1200;
				content.Height = 1200;
			}

			scroll.HorizontalScrollMode = Enum.Parse<ScrollMode>("" + horizontalMode.SelectedItem);
			scroll.VerticalScrollMode = Enum.Parse<ScrollMode>("" + verticalMode.SelectedItem);

			scroll.HorizontalScrollBarVisibility = Enum.Parse<ScrollBarVisibility>("" + horizontalVisibility.SelectedItem);
			scroll.VerticalScrollBarVisibility = Enum.Parse<ScrollBarVisibility>("" + verticalVisibility.SelectedItem);
		}
	}
}

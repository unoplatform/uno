using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[SampleControlInfo("ScrollViewer", "ScrollViewer_Dimension_Properties", description: "ScrollViewer illustrating ViewportHeight, ExtentHeight, and ScrollableHeight.")]
	public sealed partial class ScrollViewer_Dimension_Properties : UserControl
	{
		private readonly Random _random = new Random(2049);

		public ScrollViewer_Dimension_Properties()
		{
			this.InitializeComponent();

			// This seems to only work on Windows, probably because UserControl is handled as a special case in Uno.
			TargetScrollViewer.DataContext = GetLongString();
		}

		private void ChangeText(object sender, RoutedEventArgs e)
		{
			TargetScrollViewer.DataContext = GetLongString();
		}

		private void RemoveContent(object sender, RoutedEventArgs e)
		{
			TargetScrollViewer.Content = null;
		}

		private string GetLongString()
		{
			var length = _random.Next(2000, 4000);

			var sb = new StringBuilder();

			const int wordLength = 7;
			for (int i = 0; i < length; i++)
			{
				if (i % wordLength == 0)
				{
					sb.Append(" ");
				}
				else if (i % 2 == 0)
				{
					sb.Append(GetRandom(Vowels, _random));
				}
				else
				{
					sb.Append(GetRandom(Consonants, _random));
				}
			}

			return sb.ToString();
		}

		private static T GetRandom<T>(T[] array, Random random)
		{
			return array[random.Next(0, array.Length)];
		}

		public static string[] Vowels { get; } = "AEIOU".Select(c => c.ToString()).ToArray();

		public static string[] Consonants { get; } = "BCDFGHJKLMNPQRSTVWXYZ".Select(c => c.ToString()).ToArray();
	}
}

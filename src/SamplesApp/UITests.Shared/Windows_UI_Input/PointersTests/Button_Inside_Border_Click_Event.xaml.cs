using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Shared.Windows_UI_Input.PointersTests
{
	public static class Extensions
	{
		public static TElement OnTapped<TElement>(this TElement element, Windows.UI.Xaml.Input.TappedEventHandler handler) where TElement : UIElement
		{
			element.Tapped += handler;
			return element;
		}
	}

	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Button")]
	public sealed partial class Button_Inside_Border_Click_Event : Page
	{
		public Button_Inside_Border_Click_Event()
		{
			this.InitializeComponent();
			var myButton = new Button
			{
				Name = "MyButton",
				Content = "Click me",
				Tag = "",
			};

			myButton.OnTapped((s, e) =>
			{
				e.Handled = true;
				myButton.Tag += "Hit MyButton.OnTapped\n";
			});

			Content = new Border
			{
				Background = new SolidColorBrush(Windows.UI.Colors.Blue),
				Height = 80,
				Width = 400,
				Child = myButton,
			}.OnTapped((s, e) => myButton.Tag = "Hit Border.OnTapped\n");
		}
	}
}

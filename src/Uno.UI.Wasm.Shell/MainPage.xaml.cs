using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Uno.UI.Wasm.App
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
		public MainPage()
		{
			this.InitializeComponent();

			DataContext = new MainPageViewModel();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			new MessageDialog("Button clicked!").ShowAsync();
		}

		private Storyboard _storyboard;
		private bool _storyboardIsRunning;
		private void StartAnimation(object sender, TappedRoutedEventArgs e)
		{
			if (_storyboard == null)
			{
				var animation = new DoubleAnimation
				{
					From = 0,
					To = 360,
					RepeatBehavior = RepeatBehavior.Forever,
					AutoReverse = true,
					Duration = new Duration(TimeSpan.FromSeconds(10)),
					EnableDependentAnimation = true
				};
				Storyboard.SetTarget(animation, _transform);
				Storyboard.SetTargetProperty(animation, nameof(_transform.Angle));

				_storyboard = new Storyboard
				{
					Children = { animation }
				};

				_storyboardIsRunning = true;
				_storyboard.Begin();
			}
			else if (_storyboardIsRunning)
			{
				_storyboardIsRunning = false;
				_storyboard.Pause();
			}
			else
			{
				_storyboardIsRunning = true;
				_storyboard.Resume();
			}
		}
	}

	[Bindable]
	public class MainPageViewModel
	{
		public MainPageViewModel()
		{
			Items = Enumerable.Range(0, 3).Select(i =>
				new Item
				{
					Title = $"Title {i}",
					Subtitle = $"Subtitle {i}",
					Image = "http://lorempixel.com/100/100/",
				}
			)
			.ToArray();
		}

		public Item[] Items { get; set; }
	}

	[Bindable]
	public class Item
	{
		public string Subtitle { get; set; }
		public string Title { get; set; }
		public string Image { get; set; }

		public override string ToString() => Title;
	}
}

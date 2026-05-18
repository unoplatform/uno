using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI.Samples.Controls;


namespace Uno.UI.Samples.Content.UITests.Animations
{
	[Sample("Animations", Name = "DoubleAnimation_VisualStates")]
	public sealed partial class DoubleAnimation_VisualStates : UserControl
	{
		public DoubleAnimation_VisualStates()
		{
			this.InitializeComponent();

			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
		}

		private IDisposable _pulling;
		private Border _sut;
		private TextBlock _output;

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			_sut = _myControl.FindFirstChild<Border>(b => b.Name == "StateTwoContent");
			_output = _myControl.FindFirstChild<TextBlock>(t => t.Name == "PullingOutput");

			var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
			timer.Tick += (snd, args) =>
			{
				_output.Text = _sut.Opacity.ToString("F2");
			};
			timer.Start();

			_pulling = Disposable.Create(() =>
			{
				timer.Stop();
			});
		}

		private void OnUnloaded(object sender, RoutedEventArgs e)
		{
			_pulling?.Dispose();
		}

		private void LaunchAnimation1(object sender, TappedRoutedEventArgs e)
		{
			VisualStateManager.GoToState(_myControl, "StateOne", true);
		}

		private void LaunchAnimation2(object sender, TappedRoutedEventArgs e)
		{
			VisualStateManager.GoToState(_myControl, "StateTwo", true);
		}

		private void SetOpacity(object sender, TappedRoutedEventArgs e)
		{
			_sut.Opacity = .5;
		}
	}
}

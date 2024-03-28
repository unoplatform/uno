using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI.Samples.Controls;


namespace Uno.UI.Samples.Content.UITests.Animations
{
	[SampleControlInfo("Animations", "DoubleAnimation_VisualStates")]
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

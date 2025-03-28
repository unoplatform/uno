using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Uno.Disposables;
using Private.Infrastructure;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	public partial class DelayedImagePresenter : ContentControl
	{
		private Windows.UI.Xaml.Controls.Image _image;
		private TextBlock _waitingText;

		private readonly SerialDisposable _loadingSubscription = new SerialDisposable();

		public ImageSource Source
		{
			get { return (ImageSource)this.GetValue(SourceProperty); }
			set { this.SetValue(SourceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Source.  This enables animation, styling, binding, etc...
		public static DependencyProperty SourceProperty { get; } =
			DependencyProperty.Register(
				"Source",
				typeof(ImageSource),
				typeof(DelayedImagePresenter),
				new PropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((DelayedImagePresenter)s).OnSourceChanged((ImageSource)e.OldValue, (ImageSource)e.NewValue))
				);

		private void OnSourceChanged(ImageSource oldValue, ImageSource newValue)
		{
			if (_image == null)
			{
				return;
			}

			_image.Source = newValue;

			if (newValue == null)
			{
				_loadingSubscription.Disposable = null;
				_image.Opacity = 1;
				_waitingText.Opacity = 0;
				return;
			}

			_image.Opacity = 0.5;
			_waitingText.Opacity = 1;

			_waitingText.Text = $"Waiting for source {GetTail((newValue as BitmapImage)?.UriSource.ToString() ?? "<null>")} to load.";

			var cd = new CancellationDisposable();
			_loadingSubscription.Disposable = cd;
			var ct = cd.Token;

			var t = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, SetImageVisible);

			async void SetImageVisible()
			{
				try
				{
					const int delayms = 1000;
					const int ticks = 10;
					for (int i = 0; i < ticks; i++)
					{
						await Task.Delay(delayms / ticks, ct);
						_image.Opacity = (double)(i + 1) / (double)ticks;
						if (ct.IsCancellationRequested) { return; }
					}
					_image.Opacity = 1;
					_waitingText.Opacity = 0;
				}
				catch (Exception) { }
			}
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_image = GetTemplateChild("Image") as Windows.UI.Xaml.Controls.Image;
			_waitingText = GetTemplateChild("WaitingText") as TextBlock;
		}

		private static string GetTail(string s)
		{
			var start = Math.Max(0, s.Length - 15);
			var output = "";
			if (s.Length > 15)
			{
				output += "...";
			}
			output += s.Substring(start);
			return output;
		}
	}
}

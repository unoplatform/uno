using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media.Imaging;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml_Controls.Composition
{
	/// <summary>
	/// This sample is based off of https://github.com/XamlBrewer/UWP-Composition-API-Clock
	/// </summary>
	[SampleControlInfo("Composition", "Composition_BasicClock")]
	public sealed partial class Composition_BasicClock : UserControl
	{
		private Compositor _compositor;
		private ContainerVisual _root;

		private SpriteVisual _hourhand;
		private SpriteVisual _minutehand;
		private SpriteVisual _secondhand;
		private CompositionScopedBatch _batch;

		private DispatcherTimer _timer = new DispatcherTimer();

		public Composition_BasicClock()
		{
			this.InitializeComponent();

			this.Loaded += Clock_Loaded;

			_timer.Interval = TimeSpan.FromSeconds(1);
			_timer.Tick += Timer_Tick;
		}


		public bool ShowTicks { get; set; } = true;

		public Brush FaceColor { get; set; } = new SolidColorBrush(Colors.Transparent);
		public ImageSource BackgroundImage { get; set; }

		private void Clock_Loaded(object sender, RoutedEventArgs e)
		{
			Face.Fill = FaceColor;

			var hostVisual = ElementCompositionPreview.GetElementVisual(Container);
			var root = hostVisual.Compositor.CreateContainerVisual();
			ElementCompositionPreview.SetElementChildVisual(Container, root);

			_compositor = root.Compositor;

			// Hour Ticks
			if (ShowTicks)
			{
				SpriteVisual tick;
				for (int i = 0; i < 12; i++)
				{
					tick = _compositor.CreateSpriteVisual();
					tick.Size = new Vector2(4.0f, 20.0f);
					tick.Brush = _compositor.CreateColorBrush(Colors.Silver);
					tick.Offset = new Vector3(98.0f, 0.0f, 0);
					tick.CenterPoint = new Vector3(2.0f, 100.0f, 0);
					tick.RotationAngleInDegrees = i * 30;
					root.Children.InsertAtTop(tick);
				}
			}

			// Second Hand
			_secondhand = _compositor.CreateSpriteVisual();
			_secondhand.Size = new Vector2(2.0f, 120.0f);
			_secondhand.Brush = _compositor.CreateColorBrush(Colors.Red);
			_secondhand.CenterPoint = new Vector3(1.0f, 100.0f, 0);
			_secondhand.Offset = new Vector3(99.0f, 0.0f, 0);
			root.Children.InsertAtTop(_secondhand);
			_secondhand.RotationAngleInDegrees = (float)(int)DateTime.Now.TimeOfDay.TotalSeconds * 6;

			// Hour Hand
			_hourhand = _compositor.CreateSpriteVisual();
			_hourhand.Size = new Vector2(4.0f, 100.0f);
			_hourhand.Brush = _compositor.CreateColorBrush(Colors.Black);
			_hourhand.CenterPoint = new Vector3(2.0f, 80.0f, 0);
			_hourhand.Offset = new Vector3(98.0f, 20.0f, 0);
			root.Children.InsertAtTop(_hourhand);

			// Minute Hand
			_minutehand = _compositor.CreateSpriteVisual();
			_minutehand.Size = new Vector2(4.0f, 120.0f);
			_minutehand.Brush = _compositor.CreateColorBrush(Colors.Black);
			_minutehand.CenterPoint = new Vector3(2.0f, 100.0f, 0);
			_minutehand.Offset = new Vector3(98.0f, 0.0f, 0);
			root.Children.InsertAtTop(_minutehand);

			SetHoursAndMinutes();

			// Add XAML element.
			if (BackgroundImage != null)
			{
				var xaml = new global::Windows.UI.Xaml.Controls.Image
				{
					Source = BackgroundImage,
					Height = 200,
					Width = 200
				};
				Container.Children.Add(xaml);
			}

			_timer.Start();
		}

		private void Timer_Tick(object sender, object e)
		{
			var now = DateTime.Now;

			_batch = _compositor.CreateScopedBatch(CompositionBatchTypes.Animation);
			var animation = _compositor.CreateScalarKeyFrameAnimation();
			var seconds = (float)(int)now.TimeOfDay.TotalSeconds;

			// This works:
			animation.InsertKeyFrame(0.00f, seconds * 6);
			animation.InsertKeyFrame(1.00f, (seconds + 1) * 6);

			// Just an example of using expressions:
			//animation.SetScalarParameter("start", seconds * 6);
			//animation.InsertExpressionKeyFrame(0.00f, "start");
			//animation.SetScalarParameter("delta", 6.0f);
			//animation.InsertExpressionKeyFrame(1.00f, "start + delta");

			animation.Duration = TimeSpan.FromMilliseconds(900);
			_secondhand.StartAnimation(nameof(_secondhand.RotationAngleInDegrees), animation);
			_batch.End();
			_batch.Completed += Batch_Completed;

			SetHoursAndMinutes();
		}

		/// <summary>
		/// Fired at the end of the secondhand animation. 
		/// </summary>
		private void Batch_Completed(object sender, CompositionBatchCompletedEventArgs args)
		{
			_batch.Completed -= Batch_Completed;

			SetHoursAndMinutes();
		}

		private void SetHoursAndMinutes()
		{
			var now = DateTime.Now;

			_hourhand.RotationAngleInDegrees = (float)now.TimeOfDay.TotalHours * 30;
			_minutehand.RotationAngleInDegrees = now.Minute * 6;
		}
	}
}

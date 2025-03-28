using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Composition;
using Windows.UI.Composition.Interactions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

#if HAS_UNO_WINUI || WINAPPSDK
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif


namespace UITests.Windows_UI_Composition;

[Sample("Windows.UI.Composition")]
public sealed partial class InteractionTrackerAndExpressionAnimationSample : Page
{
	private Visual _border1Visual;
	private readonly Visual _border2Visual;
	private VisualInteractionSource _interactionSource;

	public InteractionTrackerAndExpressionAnimationSample()
	{
		this.InitializeComponent();
		_border1Visual = ElementCompositionPreview.GetElementVisual(border1);
		_border2Visual = ElementCompositionPreview.GetElementVisual(border2);

		border1.PointerPressed += (_, e) =>
		{
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
			{
				_interactionSource.TryRedirectForManipulation(e.GetCurrentPoint(null));
			}
		};

		this.Loaded += (_, _) =>
		{
			DispatcherQueue.TryEnqueue(() =>
			{
				var compositor = _border1Visual.Compositor;
				var tracker = InteractionTracker.Create(compositor);

				tracker.MinPosition = new Vector3(0);
				tracker.MaxPosition = new Vector3((float)XamlRoot.Size.Width - _border2Visual.Size.X, (float)XamlRoot.Size.Height, 0);

				// On non-Skia (e.g, Android), the Visual CompositionTarget is set from XamlRoot.
				// So, we need to call GetElementVisual on Loaded. Otherwise, it won't work.
				// NOTE: The sample still doesn't work on platforms other than Skia
				_border1Visual = ElementCompositionPreview.GetElementVisual(border1);

				_interactionSource = VisualInteractionSource.Create(_border1Visual);

				tracker.InteractionSources.Add(_interactionSource);

				_interactionSource.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.CapableTouchpadAndPointerWheel;
				_interactionSource.PositionXSourceMode = InteractionSourceMode.EnabledWithInertia;
				_interactionSource.PositionYSourceMode = InteractionSourceMode.EnabledWithInertia;

				var animation = compositor.CreateExpressionAnimation("Vector3(tracker.Position.X, original.Offset.Y, 0)");
				animation.SetReferenceParameter("tracker", tracker);
				animation.SetReferenceParameter("original", _border2Visual);
				_border2Visual.StartAnimation("Offset", animation);
			});
		};

		this.Unloaded += (_, _) =>
		{
			_border2Visual.StopAnimation("Offset");
		};
	}

	private void OnManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
	{
		logTB.Text += "OnManipulationStarting\r\n";
	}

	private void OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
	{
		logTB.Text += "OnManipulationStarted\r\n";
	}

	private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
	{
		logTB.Text += "OnManipulationDelta\r\n";
	}

	private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
	{
		logTB.Text += "OnManipulationCompleted\r\n";
	}

	private void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
	{
		logTB.Text += "OnManipulationInertiaStarting\r\n";
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;


namespace UITests.Windows_UI_Composition;

[Sample("Microsoft.UI.Composition")]
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
			if (e.Pointer.PointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch)
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
}

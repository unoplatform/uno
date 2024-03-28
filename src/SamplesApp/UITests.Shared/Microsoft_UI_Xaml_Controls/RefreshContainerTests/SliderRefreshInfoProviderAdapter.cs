#if HAS_UNO
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

using RefreshInteractionRatioChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.Controls.RefreshInteractionRatioChangedEventArgs;
using IRefreshInfoProvider = Microsoft.UI.Private.Controls.IRefreshInfoProvider;
using PullToRefreshHelperTestApi = Microsoft.UI.Private.Controls.PullToRefreshHelperTestApi;

namespace RefreshVizualizer_TestUI
{
	internal class SliderRefreshInfoProviderAdapter
	{
		Slider mySlider;
		DispatcherTimer myTimer;

		public SliderRefreshInfoProviderAdapter(Slider slider, DispatcherTimer timer)
		{
			mySlider = slider;
			myTimer = timer;
		}

		public RefreshInfoProviderImplementation adapt()
		{
			return new RefreshInfoProviderImplementation(mySlider, myTimer);
		}
	}

	internal class RefreshInfoProviderImplementation : IRefreshInfoProvider
	{
		private DispatcherTimer timer;
		private Slider slider;
		private double executionRatio;
		private CompositionPropertySet compositionProperties;
		private Boolean isInteractingForRefresh;
		public RefreshInfoProviderImplementation(Slider slider, DispatcherTimer timer)
		{
			this.timer = timer;
			this.slider = slider;
			var visual = ElementCompositionPreview.GetElementVisual(slider);
			var compositor = visual.Compositor;

			this.ExecutionRatio = 0.8f;
			this.CompositionProperties = compositor.CreatePropertySet();
			this.CompositionProperties.InsertScalar(InteractionRatioCompositionProperty, 0.0f);

			this.timer.Interval = TimeSpan.FromMilliseconds(1);
			this.timer.Tick += timerTick;

			this.slider.ManipulationMode = ManipulationModes.TranslateX;
			this.slider.ValueChanged += onSliderValueChanged;
			this.slider.ManipulationStarting += onSliderPointerPressed;
			this.slider.ManipulationCompleted += onSliderPointerReleased;
		}

		public double ExecutionRatio
		{
			get
			{
				return this.executionRatio;
			}

			set
			{
				this.executionRatio = value;
			}
		}

		public CompositionPropertySet CompositionProperties
		{
			get
			{
				return this.compositionProperties;
			}
			private set
			{
				this.compositionProperties = value;
			}
		}

		public string InteractionRatioCompositionProperty
		{
			get
			{
				return "InteractionRatio";
			}
		}

		public bool IsInteractingForRefresh
		{
			get
			{
				return this.isInteractingForRefresh;
			}
			private set
			{
				this.isInteractingForRefresh = value;
			}
		}
		public event TypedEventHandler<IRefreshInfoProvider, RefreshInteractionRatioChangedEventArgs> InteractionRatioChanged;
		public event TypedEventHandler<IRefreshInfoProvider, object> IsInteractingForRefreshChanged;
		public event TypedEventHandler<IRefreshInfoProvider, object> RefreshCompleted;
		public event TypedEventHandler<IRefreshInfoProvider, object> RefreshStarted;

		public void OnRefreshCompleted()
		{
			if (this.RefreshCompleted != null)
			{
				this.RefreshCompleted.Invoke(this, new EventArgs());
			}

			this.slider.Value = 0;
		}

		public void OnRefreshStarted()
		{
			if (this.RefreshStarted != null)
			{
				this.RefreshStarted.Invoke(this, new EventArgs());
			}
			this.timer.Stop();
			this.slider.Value = this.slider.Maximum;
		}

		private void onSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			float interactionRatio = (float)(e.NewValue / this.slider.Maximum);
			this.CompositionProperties.InsertScalar(InteractionRatioCompositionProperty, (float)e.NewValue / (float)this.slider.Maximum);
			RaiseInteractionRatioChanged((float)e.NewValue);
		}

		private void onSliderPointerPressed(object sender, ManipulationStartingRoutedEventArgs e)
		{
			this.timer.Stop();
			UpdateIsInteractingForRefresh(true);
		}

		private void onSliderPointerReleased(object sender, ManipulationCompletedRoutedEventArgs e)
		{
			UpdateIsInteractingForRefresh(false);
		}

		private void RaiseInteractionRatioChanged(float interactionRatio)
		{
			if (this.InteractionRatioChanged != null)
			{
				this.InteractionRatioChanged.Invoke(this, PullToRefreshHelperTestApi.CreateRefreshInteractionRatioChangedEventArgsInstance((interactionRatio / (float)this.slider.Maximum)));
			}
		}

		private void UpdateIsInteractingForRefresh(Boolean value)
		{
			if (value != this.IsInteractingForRefresh)
			{
				this.IsInteractingForRefresh = value;
				if (this.IsInteractingForRefresh == false)
				{
					this.timer.Start();
				}
				RaiseInteractingForRefreshChanged();
			}
		}

		private void RaiseInteractingForRefreshChanged()
		{
			if (this.IsInteractingForRefreshChanged != null)
			{
				this.IsInteractingForRefreshChanged.Invoke(this, new EventArgs());
			}
		}

		private void timerTick(object sender, object e)
		{
			if (this.slider.Value == 0)
			{
				this.timer.Stop();
			}
			else
			{
				this.slider.Value -= 1;
			}
		}
	}
}
#endif

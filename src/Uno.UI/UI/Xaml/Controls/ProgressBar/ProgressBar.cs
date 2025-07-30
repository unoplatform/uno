// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ProgressBar.cpp, tag winui3/release/1.7-stable

#nullable enable

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using MUXC = Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class ProgressBar : RangeBase
{
#if __ANDROID__
	private Size? m_previousMeasuredWidths;
#endif

	public ProgressBar()
	{
		DefaultStyleKey = typeof(ProgressBar);

		SizeChanged += (snd, evt) => OnSizeChange();

		//LayoutUpdated += (snd, evt) => OnSizeChange();

		// NOTE: This is necessary only because Value isn't one of OUR properties, it's implemented in RangeBase.
		// If it was one of ProgressBar's properties, defined in the IDL, you'd do it differently (see IsIndeterminate).
		RegisterPropertyChangedCallback(ValueProperty, OnIndicatorWidthComponentChanged);
		RegisterPropertyChangedCallback(MinimumProperty, OnIndicatorWidthComponentChanged);
		RegisterPropertyChangedCallback(MaximumProperty, OnIndicatorWidthComponentChanged);
		RegisterPropertyChangedCallback(PaddingProperty, OnIndicatorWidthComponentChanged);

		TemplateSettings = new ProgressBarTemplateSettings();
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new ProgressBarAutomationPeer(this);

	protected override void OnApplyTemplate()
	{
		// NOTE: Example of how named parts are loaded from the template. Important to remember that it's possible for
		// any of them not to be found, since devs can replace the template with their own.

#if !UNO_HAS_ENHANCED_LIFECYCLE
		// Uno-specific: TODO: Investigate why we need this. It's a quite very old workaround from 2020
		// https://github.com/unoplatform/uno/commit/641bbc9483f33c64d5eddc474f069c07b79039ba
		// So, maybe it's no longer needed.
		// For now, we're sure it's no longer needed with enhanced lifecycle (actually, it's problematic if it exists there)
		// Note: LayoutUpdated event isn't really tied to a specific element. It really means that some element in the visual tree had a layout update.
		// So, this event subscription is wrong because it will cause the ProgressBar to transition to Updating visual state then back
		// to a state based on its properties (e.g, Indeterminate) every time any element in the visual tree has a layout update.
		// So it will cause some bad flickers in ProgressBar, and will also get us into a cycle in case there is a listener
		// to CurrentStateChanged event where the listener does something that updates the layout.
		LayoutUpdated += (snd, evt) => OnSizeChange();
#endif

		m_layoutRoot = GetTemplateChild<MUXC.Grid>(s_LayoutRootName);
		m_determinateProgressBarIndicator = GetTemplateChild<Rectangle>(s_DeterminateProgressBarIndicatorName);
		m_indeterminateProgressBarIndicator = GetTemplateChild<Rectangle>(s_IndeterminateProgressBarIndicatorName);
		m_indeterminateProgressBarIndicator2 = GetTemplateChild<Rectangle>(s_IndeterminateProgressBarIndicator2Name);

		UpdateStates();
	}

	private void OnSizeChange()
	{
#if __ANDROID__ // Uno workaround for #12312: SetProgressBarIndicatorWidth raises LayoutUpdated, and they many loops to stabilize
		if (m_layoutRoot is not null &&
			m_determinateProgressBarIndicator is not null &&
			m_previousMeasuredWidths != new Size(m_layoutRoot.ActualWidth, m_determinateProgressBarIndicator.ActualWidth))
#endif
		{
			SetProgressBarIndicatorWidth();
		}

		UpdateWidthBasedTemplateSettings();
	}

	private void OnIndicatorWidthComponentChanged(DependencyObject sender, DependencyProperty args)
	{
		// NOTE: This hits when the Value property changes, because we called RegisterPropertyChangedCallback.
		SetProgressBarIndicatorWidth();
	}

	private static void OnIsIndeterminateChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		// NOTE: This hits when IsIndeterminate changes because we set MUX_PROPERTY_CHANGED_CALLBACK to true in the idl.
		if (dependencyobject is ProgressBar progressBar)
		{
			progressBar.SetProgressBarIndicatorWidth();
			progressBar.UpdateStates();
		}
	}

	private static void OnShowPausedChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is ProgressBar progressBar)
		{
			progressBar.UpdateStates();
		}
	}

	private static void OnShowErrorChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
	{
		if (dependencyobject is ProgressBar progressBar)
		{
			progressBar.UpdateStates();
		}
	}
	private void UpdateStates()
	{
		if (IsIndeterminate)
		{
			if (ShowError)
			{
				VisualStateManager.GoToState(this, s_IndeterminateErrorStateName, true);
			}
			else if (ShowPaused)
			{
				VisualStateManager.GoToState(this, s_IndeterminatePausedStateName, true);
			}
			else
			{
				VisualStateManager.GoToState(this, s_IndeterminateStateName, true);
			}
			UpdateWidthBasedTemplateSettings();
		}
		else
		{
			if (ShowError)
			{
				VisualStateManager.GoToState(this, s_ErrorStateName, true);
			}
			else if (ShowPaused)
			{
				VisualStateManager.GoToState(this, s_PausedStateName, true);
			}
			else
			{
				VisualStateManager.GoToState(this, s_DeterminateStateName, true);
			}
		}
	}

	private void SetProgressBarIndicatorWidth()
	{
		var templateSettings = TemplateSettings;
#if __ANDROID__ // Uno workaround for #12312
		m_previousMeasuredWidths = new Size(double.NaN, double.NaN);
#endif
		var progressBar = m_layoutRoot;

		if (progressBar != null)
		{
			var determinateProgressBarIndicator = m_determinateProgressBarIndicator;
			if (determinateProgressBarIndicator != null)
			{
				var progressBarWidth = progressBar.ActualWidth;
				var prevIndicatorWidth = determinateProgressBarIndicator.ActualWidth;
				var maximum = Maximum;
				var minimum = Minimum;
				var padding = Padding;

				// Adds "Updating" state in between to trigger RepositionThemeAnimation Visual Transition
				// in ProgressBar.xaml when reverting back to previous state
				if (ShowError)
				{
					VisualStateManager.GoToState(this, s_UpdatingWithErrorStateName, true);
				}
				else
				{
					VisualStateManager.GoToState(this, s_UpdatingStateName, true);
				}

				if (IsIndeterminate)
				{
					determinateProgressBarIndicator.Width = 0;

					var indeterminateProgressBarIndicator = m_indeterminateProgressBarIndicator;
					if (indeterminateProgressBarIndicator != null)
					{
						indeterminateProgressBarIndicator.Width = progressBarWidth * 0.4; // 40% of ProgressBar Width
					}

					var indeterminateProgressBarIndicator2 = m_indeterminateProgressBarIndicator2;
					if (indeterminateProgressBarIndicator2 != null)
					{
						if (ShowPaused || ShowError) // If IndeterminatePaused or IndeterminateError
						{
							indeterminateProgressBarIndicator2.Width = progressBarWidth; // 100% of ProgressBar Width
						}
						else
						{
							indeterminateProgressBarIndicator2.Width = progressBarWidth * 0.6; // 60% of ProgressBar Width
						}
					}
				}
				else if (Math.Abs(maximum - minimum) > double.Epsilon)
				{
					var maxIndicatorWidth = progressBarWidth - (padding.Left + padding.Right);
					var increment = maxIndicatorWidth / (maximum - minimum);
					var indicatorWidth = increment * (Value - minimum);
					var widthDelta = indicatorWidth - prevIndicatorWidth;
					templateSettings.IndicatorLengthDelta = -widthDelta;
					determinateProgressBarIndicator.Width = indicatorWidth;
				}
				else
				{
					determinateProgressBarIndicator.Width = 0; // Error
				}

				UpdateStates(); // Reverts back to previous state
			}
		}
	}

	private void UpdateWidthBasedTemplateSettings()
	{
		var templateSettings = TemplateSettings;

		var progressBar = m_layoutRoot;

		var (width, height) = progressBar != null
			? (progressBar.ActualWidth, progressBar.ActualHeight)
			: (0.0, 0.0);

		var indeterminateProgressBarIndicatorWidth = width * 0.4; // Indicator width at 40% of ProgressBar
		var indeterminateProgressBarIndicatorWidth2 = width * 0.6; // Indicator width at 60% of ProgressBar

		templateSettings.ContainerAnimationStartPosition = indeterminateProgressBarIndicatorWidth * -1.0; // Position at -100%
		templateSettings.ContainerAnimationEndPosition = indeterminateProgressBarIndicatorWidth * 3.0; // Position at 300%

		templateSettings.Container2AnimationStartPosition = indeterminateProgressBarIndicatorWidth2 * -1.5; // Position at -150%
		templateSettings.Container2AnimationEndPosition = indeterminateProgressBarIndicatorWidth2 * 1.66; // Position at 166%

		templateSettings.ContainerAnimationMidPosition = 0;

		var padding = Padding;
		var rectangle = new RectangleGeometry
		{
			Rect = new Rect(padding.Left, padding.Top, width - (padding.Right + padding.Left), height - (padding.Bottom + padding.Top))
		};
		templateSettings.ClipRect = rectangle;

		// TemplateSetting properties from WUXC for backwards compatibility.
		templateSettings.EllipseAnimationEndPosition = (1.0 / 3.0) * width;
		templateSettings.EllipseAnimationWellPosition = (2.0 / 3.0) * width;

		if (width <= 180.0)
		{
			// Small ellipse diameter and offset.
			templateSettings.EllipseDiameter = 4.0;
			templateSettings.EllipseOffset = 4.0;
		}
		else if (width <= 280.0)
		{
			// Medium ellipse diameter and offset.
			templateSettings.EllipseDiameter = 5.0;
			templateSettings.EllipseOffset = 7.0;
		}
		else
		{
			// Large ellipse diameter and offset.
			templateSettings.EllipseDiameter = 6.0;
			templateSettings.EllipseOffset = 9.0;
		}
	}

}

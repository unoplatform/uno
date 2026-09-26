// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressBar.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ProgressBar
{
	/// <summary>
	/// Initializes a new instance of the ProgressBar class.
	/// </summary>
	public ProgressBar()
	{
		//__RP_Marker_ClassById(RuntimeProfiler::ProfId_ProgressBar);

		this.SetDefaultStyleKey();

		SizeChanged += OnSizeChanged;

		// NOTE: This is necessary only because Value isn't one of OUR properties, it's implemented in RangeBase.
		// If it was one of ProgressBar's properties, defined in the IDL, you'd do it differently (see IsIndeterminate).
		RegisterPropertyChangedCallback(ValueProperty, OnIndicatorWidthComponentChanged);
		RegisterPropertyChangedCallback(MinimumProperty, OnIndicatorWidthComponentChanged);
		RegisterPropertyChangedCallback(MaximumProperty, OnIndicatorWidthComponentChanged);
		RegisterPropertyChangedCallback(PaddingProperty, OnIndicatorWidthComponentChanged);
		RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityPropertyChanged);

		SetValue(TemplateSettingsProperty, new ProgressBarTemplateSettings());
	}

	/// <summary>
	/// Returns the ProgressBarAutomationPeer for this ProgressBar.
	/// </summary>
	/// <returns>The automation peer for this ProgressBar.</returns>
	protected override AutomationPeer OnCreateAutomationPeer() => new ProgressBarAutomationPeer(this);

	/// <summary>
	/// Invoked whenever application code or internal processes call ApplyTemplate.
	/// </summary>
	protected override void OnApplyTemplate()
	{
		// NOTE: Example of how named parts are loaded from the template. Important to remember that it's possible for
		// any of them not to be found, since devs can replace the template with their own.

		m_layoutRoot = GetTemplateChild<Grid>(s_LayoutRootName);
		m_determinateProgressBarIndicator = GetTemplateChild<Rectangle>(s_DeterminateProgressBarIndicatorName);
		m_indeterminateProgressBarIndicator = GetTemplateChild<Rectangle>(s_IndeterminateProgressBarIndicatorName);
		m_indeterminateProgressBarIndicator2 = GetTemplateChild<Rectangle>(s_IndeterminateProgressBarIndicator2Name);

		UpdateStates();
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		SetProgressBarIndicatorWidth();
		UpdateWidthBasedTemplateSettings();
	}

	private void OnIndicatorWidthComponentChanged(DependencyObject sender, DependencyProperty args)
	{
		// NOTE: This hits when the Value property changes, because we called RegisterPropertyChangedCallback.
		SetProgressBarIndicatorWidth();
	}

	private void OnIsIndeterminatePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		// NOTE: This hits when IsIndeterminate changes because we set MUX_PROPERTY_CHANGED_CALLBACK to true in the idl.
		SetProgressBarIndicatorWidth();
		UpdateStates();
	}

	private void OnShowPausedPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		SetProgressBarIndicatorWidth();
		UpdateStates();
	}

	private void OnVisibilityPropertyChanged(DependencyObject sender, DependencyProperty args) => UpdateStates();

	private void OnShowErrorPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		SetProgressBarIndicatorWidth();
		UpdateStates();
	}

	private void UpdateStates()
	{
		if (IsIndeterminate && Visibility == Visibility.Visible)
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

	// C++ round() rounds halfway cases away from zero, unlike Math.Round's default.
	private static double LayoutRound(double value, double scaleFactor) =>
		Math.Round(value * scaleFactor, MidpointRounding.AwayFromZero) / scaleFactor;

	private void SetProgressBarIndicatorWidth()
	{
		var templateSettings = TemplateSettings;

		if (m_layoutRoot is { } progressBar)
		{
			if (m_determinateProgressBarIndicator is { } determinateProgressBarIndicator)
			{
				double progressBarWidth = progressBar.ActualWidth;
				double prevIndicatorWidth = determinateProgressBarIndicator.ActualWidth;
				double maximum = Maximum;
				double minimum = Minimum;
				var padding = Padding;
				var borderThickness = BorderThickness;

				// Round the border width, if necessary. Note that the Left and Right values of BorderThickness
				// should be individually rounded (see CBorder::GetLayoutRoundedThickness).
				double scaleFactor = 1.0;  // Assuming a 1.0 scale factor when no XamlRoot is available at the moment.
				if (progressBar.XamlRoot is { } xamlRoot)
				{
					scaleFactor = xamlRoot.RasterizationScale;
				}
				double roundedBorderWidth = progressBar.UseLayoutRounding ?
					(LayoutRound(borderThickness.Left, scaleFactor) + LayoutRound(borderThickness.Right, scaleFactor)) :
					(borderThickness.Left + borderThickness.Right);

				double paddingAndBorderWidth = (padding.Left + padding.Right) + roundedBorderWidth;
				double maxIndicatorWidth = Math.Max(progressBarWidth - paddingAndBorderWidth, 0.0);

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

					if (m_indeterminateProgressBarIndicator is { } indeterminateProgressBarIndicator)
					{
						indeterminateProgressBarIndicator.Width = maxIndicatorWidth * 0.4; // 40% of ProgressBar Width
					}

					if (m_indeterminateProgressBarIndicator2 is { } indeterminateProgressBarIndicator2)
					{
						if (ShowPaused || ShowError) // If IndeterminatePaused or IndeterminateError
						{
							indeterminateProgressBarIndicator2.Width = maxIndicatorWidth; // 100% of ProgressBar Width
						}
						else
						{
							indeterminateProgressBarIndicator2.Width = maxIndicatorWidth * 0.6; // 60% of ProgressBar Width
						}
					}
				}
				else if (Math.Abs(maximum - minimum) > double.Epsilon)
				{
					double increment = maxIndicatorWidth / (maximum - minimum);
					double indicatorWidth = increment * (Value - minimum);
					double widthDelta = indicatorWidth - prevIndicatorWidth;
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

		var (width, height) = m_layoutRoot is { } progressBar
			? ((float)progressBar.ActualWidth, (float)progressBar.ActualHeight)
			: (0.0f, 0.0f);

		double indeterminateProgressBarIndicatorWidth = width * 0.4; // Indicator width at 40% of ProgressBar
		double indeterminateProgressBarIndicatorWidth2 = width * 0.6; // Indicator width at 60% of ProgressBar

		templateSettings.ContainerAnimationStartPosition = indeterminateProgressBarIndicatorWidth * -1.0; // Position at -100%
		templateSettings.ContainerAnimationEndPosition = indeterminateProgressBarIndicatorWidth * 3.0; // Position at 300%

		templateSettings.Container2AnimationStartPosition = indeterminateProgressBarIndicatorWidth2 * -1.5; // Position at -150%
		templateSettings.Container2AnimationEndPosition = indeterminateProgressBarIndicatorWidth2 * 1.66; // Position at 166%

		templateSettings.ContainerAnimationMidPosition = 0;

		var padding = Padding;
		var rectangle = new RectangleGeometry
		{
			Rect = new Rect(
				(float)padding.Left,
				(float)padding.Top,
				width - (float)(padding.Right + padding.Left),
				height - (float)(padding.Bottom + padding.Top))
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

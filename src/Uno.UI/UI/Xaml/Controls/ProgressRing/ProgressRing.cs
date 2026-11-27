// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressRing.cpp and ProgressRing.h

#nullable enable

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

public partial class ProgressRing : Control
{
	private const string s_LayoutRootName = "LayoutRoot";
	private const string s_LottiePlayerName = "LottiePlayer";
	private const string s_DefaultForegroundThemeResourceName = "SystemControlHighlightAccentBrush";
	private const string s_DefaultBackgroundThemeResourceName = "SystemControlBackgroundBaseLowBrush";
	private const string s_ForegroundName = "Foreground";
	private const string s_BackgroundName = "Background";
	private const string s_ActiveStateName = "Active";
	private const string s_DeterminateActiveStateName = "DeterminateActive";
	private const string s_InactiveStateName = "Inactive";

	private Grid? m_layoutRoot;
	private AnimatedVisualPlayer? m_player;

	private IDisposable? m_foregroundColorPropertyChangedRevoker;
	private IDisposable? m_backgroundColorPropertyChangedRevoker;
#if HAS_UNO
	private Action? m_foregroundBrushChanged;
	private Action? m_backgroundBrushChanged;
#endif

	private double m_oldValue;
	private bool m_rangeBasePropertyUpdating;

	public ProgressRing()
	{
		this.SetDefaultStyleKey();

		RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundPropertyChanged);
		RegisterPropertyChangedCallback(BackgroundProperty, OnBackgroundPropertyChanged);

		SetValue(TemplateSettingsProperty, new ProgressRingTemplateSettings());

		SizeChanged += OnSizeChanged;
		Loaded += OnLoaded;
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new ProgressRingAutomationPeer(this);

	protected override void OnApplyTemplate()
	{
		m_layoutRoot = GetTemplateChild(s_LayoutRootName) as Grid;
		m_player = GetTemplateChild(s_LottiePlayerName) as AnimatedVisualPlayer;

		SetAnimatedVisualPlayerSource();
		UpdateLottieProgress();
		UpdateStates();
	}

	private void OnDeterminateSourcePropertyChanged(DependencyPropertyChangedEventArgs args) => SetAnimatedVisualPlayerSource();

	private void OnIndeterminateSourcePropertyChanged(DependencyPropertyChangedEventArgs args) => SetAnimatedVisualPlayerSource();

	private void OnSizeChanged(object sender, SizeChangedEventArgs args) => ApplyTemplateSettings();

	private void OnLoaded(object sender, RoutedEventArgs args) => UpdateStates();

	private void OnForegroundPropertyChanged(DependencyObject sender, DependencyProperty dp)
	{
#if HAS_UNO
		// TODO Uno specific: the C++ handler holds a raw this, so a shared brush never keeps the ring alive.
		// Uno's property callbacks are strong references, so follow the brush through its weak invalidation instead.
		m_foregroundColorPropertyChangedRevoker?.Dispose();
		m_foregroundColorPropertyChangedRevoker = Brush.SetupBrushChanged(Foreground as SolidColorBrush, ref m_foregroundBrushChanged, () => OnForegroundColorPropertyChanged(null, null), initialInvoke: false);
#else
		if (Foreground is SolidColorBrush foreground)
		{
			m_foregroundColorPropertyChangedRevoker = RegisterPropertyChanged(foreground, SolidColorBrush.ColorProperty, OnForegroundColorPropertyChanged);
		}
#endif

		OnForegroundColorPropertyChanged(null, null);
	}

	private void OnForegroundColorPropertyChanged(DependencyObject? sender, DependencyProperty? dp)
	{
		if (m_player is { } player)
		{
			if (player.Source is { } progressRingAnimation)
			{
				if (IsIndeterminate)
				{
					if (IndeterminateSource is null)
					{
						SetLottieForegroundColor(progressRingAnimation);
					}
				}
				else
				{
					if (DeterminateSource is null)
					{
						SetLottieForegroundColor(progressRingAnimation);
					}
				}
			}
		}
	}

	private void OnBackgroundPropertyChanged(DependencyObject sender, DependencyProperty dp)
	{
#if HAS_UNO
		// TODO Uno specific: the C++ handler holds a raw this, so a shared brush never keeps the ring alive.
		// Uno's property callbacks are strong references, so follow the brush through its weak invalidation instead.
		m_backgroundColorPropertyChangedRevoker?.Dispose();
		m_backgroundColorPropertyChangedRevoker = Brush.SetupBrushChanged(Background as SolidColorBrush, ref m_backgroundBrushChanged, () => OnBackgroundColorPropertyChanged(null, null), initialInvoke: false);
#else
		if (Background is SolidColorBrush background)
		{
			m_backgroundColorPropertyChangedRevoker = RegisterPropertyChanged(background, SolidColorBrush.ColorProperty, OnBackgroundColorPropertyChanged);
		}
#endif

		OnBackgroundColorPropertyChanged(null, null);
	}

	private void OnBackgroundColorPropertyChanged(DependencyObject? sender, DependencyProperty? dp)
	{
		if (m_player is { } player)
		{
			if (player.Source is { } progressRingAnimation)
			{
				if (IsIndeterminate)
				{
					if (IndeterminateSource is null)
					{
						SetLottieBackgroundColor(progressRingAnimation);
					}
				}
				else
				{
					if (DeterminateSource is null)
					{
						SetLottieBackgroundColor(progressRingAnimation);
					}
				}
			}
		}
	}

	private void OnIsActivePropertyChanged(DependencyPropertyChangedEventArgs args) => UpdateStates();

	private void OnIsIndeterminatePropertyChanged(DependencyPropertyChangedEventArgs args) => UpdateStates();

	private void OnValuePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (!m_rangeBasePropertyUpdating)
		{
			m_rangeBasePropertyUpdating = true;
			try
			{
				CoerceValue();

				if (!IsIndeterminate)
				{
					UpdateLottieProgress();
#if HAS_UNO
					RaiseRangeValuePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, args.OldValue, Value);
#endif
				}
			}
			finally
			{
				m_rangeBasePropertyUpdating = false;
			}
		}
	}

	private void OnMaximumPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (!m_rangeBasePropertyUpdating)
		{
			m_rangeBasePropertyUpdating = true;
			try
			{
				CoerceMinimum();
				CoerceValue();

				if (!IsIndeterminate)
				{
					UpdateLottieProgress();
#if HAS_UNO
					RaiseRangeValuePropertyChangedEvent(RangeValuePatternIdentifiers.MaximumProperty, args.OldValue, args.NewValue);
#endif
				}
			}
			finally
			{
				m_rangeBasePropertyUpdating = false;
			}
		}
	}

	private void OnMinimumPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (!m_rangeBasePropertyUpdating)
		{
			m_rangeBasePropertyUpdating = true;
			try
			{
				CoerceMaximum();
				CoerceValue();

				if (!IsIndeterminate)
				{
					UpdateLottieProgress();
#if HAS_UNO
					RaiseRangeValuePropertyChangedEvent(RangeValuePatternIdentifiers.MinimumProperty, args.OldValue, args.NewValue);
#endif
				}
			}
			finally
			{
				m_rangeBasePropertyUpdating = false;
			}
		}
	}

	private void SetAnimatedVisualPlayerSource()
	{
		if (m_player is { } player)
		{
			if (IsIndeterminate)
			{
				// Check if custom indeterminate animation source is set.
				if (IndeterminateSource is null)
				{
					// Set default indeterminate animation source.
					player.Source = new AnimatedVisuals.ProgressRingIndeterminate();

					if (player.Source is { } progressRingAnimation)
					{
						SetLottieForegroundColor(progressRingAnimation);
						SetLottieBackgroundColor(progressRingAnimation);
					}
				}
				else
				{
					// Set custom indeterminate animation source.
					player.Source = IndeterminateSource;
				}
			}
			else
			{
				// Check if custom determinate animation source is set.
				if (DeterminateSource is null)
				{
					// Set default determinate animation source.
					player.Source = new AnimatedVisuals.ProgressRingDeterminate();

					if (player.Source is { } progressRingAnimation)
					{
						SetLottieForegroundColor(progressRingAnimation);
						SetLottieBackgroundColor(progressRingAnimation);
					}
				}
				else
				{
					// Set custom determinate animation source.
					player.Source = DeterminateSource;
				}
			}
		}
	}

	private void SetLottieForegroundColor(IAnimatedVisualSource animatedVisualSource)
	{
		var compositor = CompositionTarget.GetCompositorForCurrentThread();

		var foregroundColor = Foreground is SolidColorBrush foreground
			? foreground.Color
			// Default color fallback if Foreground() Brush does not contain SolidColorBrush with Color property.
			: ((SolidColorBrush)SharedHelpers.FindInApplicationResources(s_DefaultForegroundThemeResourceName)).Color;

		if (animatedVisualSource is AnimatedVisuals.ProgressRingIndeterminate progressRingAnimation)
		{
			progressRingAnimation.GetThemeProperties(compositor).InsertVector4(s_ForegroundName, SharedHelpers.RgbaColor(foregroundColor));
		}
#if HAS_UNO
		// TODO Uno specific: WinUI themes only the indeterminate visual, so its determinate ring ignores Foreground (#24555).
		else if (animatedVisualSource is AnimatedVisuals.ProgressRingDeterminate determinateAnimation)
		{
			determinateAnimation.GetThemeProperties(compositor).InsertVector4(s_ForegroundName, SharedHelpers.RgbaColor(foregroundColor));
		}
#endif
	}

	private void SetLottieBackgroundColor(IAnimatedVisualSource animatedVisualSource)
	{
		var compositor = CompositionTarget.GetCompositorForCurrentThread();

		var backgroundColor = Background is SolidColorBrush background
			? background.Color
			//Default color fallback if Background() Brush does not contain SolidColorBrush with Color property.
			: ((SolidColorBrush)SharedHelpers.FindInApplicationResources(s_DefaultBackgroundThemeResourceName)).Color;

		if (animatedVisualSource is AnimatedVisuals.ProgressRingIndeterminate progressRingAnimation)
		{
			progressRingAnimation.GetThemeProperties(compositor).InsertVector4(s_BackgroundName, SharedHelpers.RgbaColor(backgroundColor));
		}
#if HAS_UNO
		// TODO Uno specific: WinUI themes only the indeterminate visual, so its determinate ring ignores Background (#24555).
		else if (animatedVisualSource is AnimatedVisuals.ProgressRingDeterminate determinateAnimation)
		{
			determinateAnimation.GetThemeProperties(compositor).InsertVector4(s_BackgroundName, SharedHelpers.RgbaColor(backgroundColor));
		}
#endif
	}

	private void UpdateLottieProgress()
	{
		if (m_player is { } player)
		{
			double value = Value;
			double min = Minimum;
			double range = Maximum - min;
			double fromProgress = (m_oldValue - min) / range;
			double toProgress = (value - min) / range;
			if (fromProgress < toProgress)
			{
				_ = player.PlayAsync(fromProgress, toProgress, false);
			}
			else
			{
				player.SetProgress(toProgress);
			}

			m_oldValue = value;
		}
	}

	private void UpdateStates()
	{
		if (IsActive)
		{
			if (IsIndeterminate)
			{
				VisualStateManager.GoToState(this, s_ActiveStateName, true);

				// Swap player source to indeterminate.
				SetAnimatedVisualPlayerSource();

				if (m_player is { } player)
				{
					_ = player.PlayAsync(0, 1, true);
				}
			}
			else
			{
				VisualStateManager.GoToState(this, s_DeterminateActiveStateName, true);

				// Swap player source to determinate.
				SetAnimatedVisualPlayerSource();
				UpdateLottieProgress();
			}
			AutomationProperties.SetAccessibilityView(this, AccessibilityView.Content);
		}
		else
		{
			VisualStateManager.GoToState(this, s_InactiveStateName, true);

			if (m_player is { } player)
			{
				player.Stop();
			}

			AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
		}
	}

	private void ApplyTemplateSettings()
	{
		// TemplateSetting properties from WUXC for backwards compatibility.
		var templateSettings = TemplateSettings;

		var (width, diameterValue, anchorPoint) = GetEllipseMetrics();

		(float Width, float DiameterValue, float AnchorPoint) GetEllipseMetrics()
		{
			if (ActualWidth != 0)
			{
				float width = (float)ActualWidth;

				float diameterAdditive = width <= 40.0f ? 1.0f : 0.0f;

				float diamaterValue = (width * 0.1f) + diameterAdditive;
				float anchorPoint = (width * 0.5f) - diamaterValue;
				return (width, diamaterValue, anchorPoint);
			}

			return (0.0f, 0.0f, 0.0f);
		}

		templateSettings.EllipseDiameter = diameterValue;

		Thickness thicknessEllipseOffset = new(0, anchorPoint, 0, 0);

		templateSettings.EllipseOffset = thicknessEllipseOffset;
		templateSettings.MaxSideLength = width;
	}

	private void CoerceMinimum()
	{
		var max = Maximum;
		if (Minimum > max)
		{
			Minimum = max;
		}
	}

	private void CoerceMaximum()
	{
		var min = Minimum;
		if (Maximum < min)
		{
			Maximum = min;
		}
	}

	private void CoerceValue()
	{
		// Validate that the value is in bounds
		var value = Value;
		if (!double.IsNaN(value) && !IsInBounds(value))
		{
			// Coerce value to be within range
			var max = Maximum;
			if (value > max)
			{
				Value = max;
			}
			else
			{
				Value = Minimum;
			}
		}
	}

	private bool IsInBounds(double value) => value >= Minimum && value <= Maximum;
}

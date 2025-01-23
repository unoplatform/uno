// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\Generated\AnimatedVisualPlayer.properties.cpp, tag winui3/release/1.6.3, commit 66d24dfff

using System;
using Microsoft.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

partial class AnimatedVisualPlayer
{
	/// <summary>
	/// Gets or sets a value that specifies how animations are cached when the AnimatedVisualPlayer is idle (when PlayAsync is not active).
	/// </summary>
	public PlayerAnimationOptimization AnimationOptimization
	{
		get => (PlayerAnimationOptimization)GetValue(AnimationOptimizationProperty);
		set => SetValue(AnimationOptimizationProperty, value);
	}

	/// <summary>
	/// Identifies the AnimationOptimization dependency property.
	/// </summary>
	public static DependencyProperty AnimationOptimizationProperty { get; } =
		DependencyProperty.Register(
			nameof(AnimationOptimization),
			typeof(PlayerAnimationOptimization),
			typeof(AnimatedVisualPlayer),
			new FrameworkPropertyMetadata(PlayerAnimationOptimization.Latency));

	/// <summary>
	/// Gets or sets a value that indicates whether an animated visual plays immediately when it is loaded.
	/// </summary>
	public bool AutoPlay
	{
		get => (bool)GetValue(AutoPlayProperty);
		set => SetValue(AutoPlayProperty, value);
	}

	/// <summary>
	/// Identifies the AutoPlay dependency property.
	/// </summary>
	public static DependencyProperty AutoPlayProperty { get; } =
		DependencyProperty.Register(
			nameof(AutoPlay),
			typeof(bool),
			typeof(AnimatedVisualPlayer),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Gets optional diagnostics information about the last attempt to load an animated visual.
	/// </summary>
	public object Diagnostics => GetValue(DiagnosticsProperty);

	/// <summary>
	/// Identifies the Diagnostics dependency property.
	/// </summary>
	public static DependencyProperty DiagnosticsProperty { get; } =
		DependencyProperty.Register(
			nameof(Diagnostics),
			typeof(object),
			typeof(AnimatedVisualPlayer),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets the duration of the the currently loaded animated visual, or TimeSpan.Zero if no animated visual is loaded.
	/// </summary>
	public TimeSpan Duration => (TimeSpan)GetValue(DurationProperty);

	/// <summary>
	/// Identifies the Duration dependency property.
	/// </summary>
	public static DependencyProperty DurationProperty { get; } =
		DependencyProperty.Register(
			nameof(Duration),
			typeof(TimeSpan),
			typeof(AnimatedVisualPlayer),
			new FrameworkPropertyMetadata(default(TimeSpan)));

	/// <summary>
	/// Gets or sets content to display if an animated visual fails to load.
	/// </summary>
	public DataTemplate FallbackContent
	{
		get => (DataTemplate)GetValue(FallbackContentProperty);
		set => SetValue(FallbackContentProperty, value);
	}

	/// <summary>
	/// Identifies the FallbackContent dependency property.
	/// </summary>
	public static DependencyProperty FallbackContentProperty { get; } =
		DependencyProperty.Register(
			nameof(FallbackContent),
			typeof(DataTemplate),
			typeof(AnimatedVisualPlayer),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets a value that indicates whether an animated visual is loaded.
	/// </summary>
	public bool IsAnimatedVisualLoaded => (bool)GetValue(IsAnimatedVisualLoadedProperty);

	/// <summary>
	/// Identifies the IsAnimatedVisualLoaded dependency property.
	/// </summary>
	public static DependencyProperty IsAnimatedVisualLoadedProperty { get; } =
		DependencyProperty.Register(nameof(IsAnimatedVisualLoaded), typeof(bool), typeof(AnimatedVisualPlayer), new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets a value that indicates whether an animated visual is loaded and a play is underway.
	/// </summary>
	public bool IsPlaying => (bool)GetValue(IsPlayingProperty);

	/// <summary>
	/// Identifies the IsPlaying dependency property.
	/// </summary>
	public static DependencyProperty IsPlayingProperty { get; } =
		DependencyProperty.Register(
			nameof(IsPlaying),
			typeof(bool),
			typeof(AnimatedVisualPlayer),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Gets or sets the rate at which the animation plays.
	/// </summary>
	public double PlaybackRate
	{
		get => (double)GetValue(PlaybackRateProperty);
		set => SetValue(PlaybackRateProperty, value);
	}

	/// <summary>
	/// Identifies the PlaybackRate dependency property.
	/// </summary>
	public static DependencyProperty PlaybackRateProperty { get; } =
		DependencyProperty.Register(
			nameof(PlaybackRate),
			typeof(double),
			typeof(AnimatedVisualPlayer),
			new FrameworkPropertyMetadata(1.0));

	/// <summary>
	/// Gets or sets the provider of the animated visual for the player.
	/// </summary>
	public IAnimatedVisualSource Source
	{
		get => (IAnimatedVisualSource)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>
	/// Identifies the Source dependency property.
	/// </summary>
	public static DependencyProperty SourceProperty { get; } =
		DependencyProperty.Register(
			nameof(Source),
			typeof(IAnimatedVisualSource),
			typeof(AnimatedVisualPlayer),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets a value that describes how an animated visual should be stretched to fill the destination rectangle.
	/// </summary>
	public Stretch Stretch
	{
		get => (Stretch)GetValue(StretchProperty);
		set => SetValue(StretchProperty, value);
	}

	/// <summary>
	/// Identifies the Stretch dependency property.
	/// </summary>
	public static DependencyProperty StretchProperty { get; } =
		DependencyProperty.Register(
			nameof(Stretch),
			typeof(Stretch),
			typeof(AnimatedVisualPlayer),
			new FrameworkPropertyMetadata(Stretch.Uniform));
}

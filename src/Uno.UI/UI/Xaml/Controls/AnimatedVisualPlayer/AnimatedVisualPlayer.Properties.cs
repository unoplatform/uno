// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.idl + AnimatedVisualPlayer.properties.cpp, commit 5f9e85113

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class AnimatedVisualPlayer
{
	/// <summary>Identifies the <see cref="AutoPlay"/> dependency property.</summary>
	public static DependencyProperty AutoPlayProperty { get; } = DependencyProperty.Register(
		nameof(AutoPlay), typeof(bool), typeof(AnimatedVisualPlayer),
		new FrameworkPropertyMetadata(true, OnAutoPlayPropertyChanged));

	/// <summary>Identifies the <see cref="IsAnimatedVisualLoaded"/> dependency property.</summary>
	public static DependencyProperty IsAnimatedVisualLoadedProperty { get; } = DependencyProperty.Register(
		nameof(IsAnimatedVisualLoaded), typeof(bool), typeof(AnimatedVisualPlayer),
		new FrameworkPropertyMetadata(false));

	/// <summary>Identifies the <see cref="IsPlaying"/> dependency property.</summary>
	public static DependencyProperty IsPlayingProperty { get; } = DependencyProperty.Register(
		nameof(IsPlaying), typeof(bool), typeof(AnimatedVisualPlayer),
		new FrameworkPropertyMetadata(false));

	/// <summary>Identifies the <see cref="PlaybackRate"/> dependency property.</summary>
	public static DependencyProperty PlaybackRateProperty { get; } = DependencyProperty.Register(
		nameof(PlaybackRate), typeof(double), typeof(AnimatedVisualPlayer),
		new FrameworkPropertyMetadata(1.0, OnPlaybackRatePropertyChanged));

	/// <summary>Identifies the <see cref="FallbackContent"/> dependency property.</summary>
	public static DependencyProperty FallbackContentProperty { get; } = DependencyProperty.Register(
		nameof(FallbackContent), typeof(DataTemplate), typeof(AnimatedVisualPlayer),
		new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, OnFallbackContentPropertyChanged));

	/// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
	public static DependencyProperty SourceProperty { get; } = DependencyProperty.Register(
		nameof(Source), typeof(IAnimatedVisualSource), typeof(AnimatedVisualPlayer),
		new FrameworkPropertyMetadata(null,
			FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
			OnSourcePropertyChanged));

	/// <summary>Identifies the <see cref="Stretch"/> dependency property.</summary>
	public static DependencyProperty StretchProperty { get; } = DependencyProperty.Register(
		nameof(Stretch), typeof(Stretch), typeof(AnimatedVisualPlayer),
		new FrameworkPropertyMetadata(Stretch.Uniform,
			FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange,
			OnStretchPropertyChanged));

	/// <summary>Identifies the <see cref="Duration"/> dependency property.</summary>
	public static DependencyProperty DurationProperty { get; } = DependencyProperty.Register(
		nameof(Duration), typeof(TimeSpan), typeof(AnimatedVisualPlayer),
		new FrameworkPropertyMetadata(default(TimeSpan)));

	/// <summary>
	/// Gets or sets a value that indicates whether the animation should play immediately when loaded.
	/// </summary>
	/// <value><c>true</c> to play the animation as soon as it is loaded; otherwise, <c>false</c>. The default is <c>true</c>.</value>
	public bool AutoPlay
	{
		get => (bool)GetValue(AutoPlayProperty);
		set => SetValue(AutoPlayProperty, value);
	}

	/// <summary>
	/// Gets a value that indicates whether the animated visual provided by <see cref="Source"/>
	/// has finished loading and is ready to play.
	/// </summary>
	public bool IsAnimatedVisualLoaded
	{
		get => (bool)GetValue(IsAnimatedVisualLoadedProperty);
		internal set => SetValue(IsAnimatedVisualLoadedProperty, value);
	}

	/// <summary>
	/// Gets a value that indicates whether an animation is currently playing.
	/// </summary>
	public bool IsPlaying
	{
		get => (bool)GetValue(IsPlayingProperty);
		internal set => SetValue(IsPlayingProperty, value);
	}

	/// <summary>
	/// Gets or sets the <see cref="DataTemplate"/> used as the player's content when no animated
	/// visual could be loaded from the <see cref="Source"/>.
	/// </summary>
	public DataTemplate FallbackContent
	{
		get => (DataTemplate)GetValue(FallbackContentProperty);
		set => SetValue(FallbackContentProperty, value);
	}

	/// <summary>
	/// Gets or sets the rate at which the animation plays.
	/// </summary>
	/// <value>A value of 1.0 plays at normal speed. A negative value plays in reverse.</value>
	public double PlaybackRate
	{
		get => (double)GetValue(PlaybackRateProperty);
		set => SetValue(PlaybackRateProperty, value);
	}

	/// <summary>
	/// Gets or sets the source of the animated visual.
	/// </summary>
	/// <value>An object that implements <see cref="IAnimatedVisualSource"/>.</value>
	public IAnimatedVisualSource Source
	{
		get => (IAnimatedVisualSource)GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	/// <summary>
	/// Gets or sets a value that describes how the animated visual should be stretched to
	/// fill the player.
	/// </summary>
	/// <value>The stretch mode. The default is <see cref="Stretch.Uniform"/>.</value>
	public Stretch Stretch
	{
		get => (Stretch)GetValue(StretchProperty);
		set => SetValue(StretchProperty, value);
	}

	/// <summary>
	/// Gets the duration of the loaded animation.
	/// </summary>
	public TimeSpan Duration
	{
		get => (TimeSpan)GetValue(DurationProperty);
		internal set => SetValue(DurationProperty, value);
	}

	private static void OnSourcePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		=> (source as AnimatedVisualPlayer)?.OnSourceChanged(args);

	private static void OnAutoPlayPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		=> (source as AnimatedVisualPlayer)?.OnAutoPlayChanged(args);

	private static void OnPlaybackRatePropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		=> (source as AnimatedVisualPlayer)?.OnPlaybackRateChanged(args);

	private static void OnFallbackContentPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		=> (source as AnimatedVisualPlayer)?.OnFallbackContentChanged(args);

	private static void OnStretchPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		=> (source as AnimatedVisualPlayer)?.InvalidateMeasure();
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.cpp/.h, commit 5f9e85113

#nullable enable

using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class AnimatedVisualPlayer
{
	// ----- WinUI-aligned playback state (active on Skia when Source returns an IAnimatedVisual) -----

	// Fields below are only populated on Skia; on other targets the legacy path is used and
	// they remain null. Suppress the unassigned-field warning rather than gating each field
	// with #if __SKIA__ for readability.
#pragma warning disable CS0649

	// A Visual used for clipping and for parenting of m_animatedVisualRoot.
	private SpriteVisual? m_rootVisual;

	// The property set that contains the Progress property that will be used to set the progress
	// of the animated visual.
	private CompositionPropertySet? m_progressPropertySet;

#pragma warning restore CS0649

	private IAnimatedVisual? m_animatedVisual;

	// The native size of the current animated visual. Only valid if m_animatedVisual is not null.
	private Vector2 m_animatedVisualSize;

	private Visual? m_animatedVisualRoot;

	private int m_playAsyncVersion;

	private double m_currentPlayFromProgress;

	private AnimationPlay? m_nowPlaying;

	private bool m_isFallenBack;

	// True while the WinUI-aligned playback path owns the player. When false, the legacy Uno
	// IAnimatedVisualSource hooks are used (Lottie JSON, etc.).
	private bool m_useWinUIFlow;

	// Whether the current animated visual has its progress animations created.
	private bool m_isAnimationsCreated;

	private uint m_createAnimationsCounter;

	/// <summary>
	/// Gets the <see cref="CompositionObject"/> that exposes the player's Progress scalar property.
	/// </summary>
	/// <remarks>
	/// External code can hook the same Progress used by the player by referencing this object's
	/// <see cref="CompositionPropertySet"/> in an <see cref="ExpressionAnimation"/>.
	/// </remarks>
	public CompositionObject ProgressObject
	{
		get
		{
			EnsureWinUIRootVisual();
			return m_progressPropertySet!;
		}
	}

	private void EnsureWinUIRootVisual()
	{
#if __SKIA__
		if (m_rootVisual is not null)
		{
			return;
		}

		var compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
		m_rootVisual = compositor.CreateSpriteVisual();
		m_progressPropertySet = m_rootVisual.Properties;

		// Set an initial value for the Progress property.
		m_progressPropertySet.InsertScalar("Progress", 0);

		// Ensure the content can't render outside the bounds of the element.
		m_rootVisual.Clip = compositor.CreateInsetClip();
#endif
	}

	// Pauses the currently playing animated visual, or does nothing if no play is underway.
	private void PauseWinUI()
	{
		m_nowPlaying?.Pause();
	}

	private async Task PlayAsyncWinUI(double fromProgress, double toProgress, bool looped)
	{
		// Make sure that animations are created.
		CreateAnimations();

		// Used to detect reentrance.
		var version = ++m_playAsyncVersion;

		// Complete m_nowPlaying if it is still running.
		if (m_nowPlaying is not null)
		{
			m_progressPropertySet?.InsertScalar("Progress", (float)m_currentPlayFromProgress);
			m_nowPlaying.Complete();
		}

		if (version != m_playAsyncVersion)
		{
			// The call was overtaken by another call due to reentrance.
			return;
		}

		// Adjust for the case where there is a segment that goes from [fromProgress..0] where m_fromProgress > 0.
		// This is equivalent to [fromProgress..1].
		if (toProgress == 0 && fromProgress > 0)
		{
			toProgress = 1;
		}

		// Adjust for the case where there is a segment that goes from [1..toProgress] where toProgress > 0.
		// This is equivalent to [0..toProgress].
		if (toProgress > 0 && fromProgress == 1)
		{
			fromProgress = 0;
		}

		m_currentPlayFromProgress = Math.Clamp(fromProgress, 0.0, 1.0);

		var thisPlay = m_nowPlaying = new AnimationPlay(
			this,
			(float)Math.Clamp(fromProgress, 0.0, 1.0),
			(float)Math.Clamp(toProgress, 0.0, 1.0),
			looped);

		if (IsAnimatedVisualLoaded)
		{
			thisPlay.Start();
		}

		await thisPlay.Task;
	}

	private void ResumeWinUI()
	{
		m_nowPlaying?.Resume();
	}

	private void SetProgressWinUI(double progress)
	{
		// Make sure that animations are created.
		CreateAnimations();

		var clampedProgress = (float)Math.Clamp(progress, 0.0, 1.0);

		m_progressPropertySet?.InsertScalar("Progress", clampedProgress);

		// Ensure the current PlayAsync task is completed.
		m_nowPlaying?.Complete();
	}

	private void StopWinUI()
	{
		if (m_nowPlaying is not null)
		{
			SetProgressWinUI(m_currentPlayFromProgress);
		}
	}

	private void OnSourceChangedWinUI()
	{
		EnsureWinUIRootVisual();

		// Drop any in-flight play directly. Going through StopWinUI/SetProgressWinUI would
		// propagate a Progress write through the bound expression chain of the OUTGOING source,
		// which can hit half-disposed state (e.g. cross-typed SetAnimatableProperty calls or
		// missing visuals). We're about to replace the content anyway.
		var inflight = m_nowPlaying;
		m_nowPlaying = null;
		IsPlaying = false;
		inflight?.Detach();

		UpdateContent();
	}

	private void OnAutoPlayChangedWinUI(DependencyPropertyChangedEventArgs args)
	{
		var newValue = (bool)args.NewValue;
		if (newValue && IsAnimatedVisualLoaded && m_nowPlaying is null)
		{
			_ = PlayAsyncWinUI(0, 1, true);
		}
	}

	private void OnPlaybackRateChangedWinUI(DependencyPropertyChangedEventArgs args)
	{
		m_nowPlaying?.SetPlaybackRate((float)(double)args.NewValue);
	}

	private void OnFallbackContentChangedWinUI(DependencyPropertyChangedEventArgs args)
	{
		if (m_isFallenBack)
		{
			LoadFallbackContent();
		}
	}

	private void CreateAnimations()
	{
		m_createAnimationsCounter++;

		if (m_isAnimationsCreated || m_animatedVisual is null)
		{
			return;
		}

		// IAnimatedVisual2 is not represented in Uno today; sources that opt into the V3 surface
		// will create animations themselves on first frame.
		m_isAnimationsCreated = true;
	}

	private void DestroyAnimations()
	{
		if (!m_isAnimationsCreated || m_animatedVisual is null)
		{
			return;
		}

		// On WinUI this awaits Compositor::RequestCommitAsync to ensure ongoing GPU work has
		// drained before tearing down the animations. Uno's compositor commits synchronously
		// from the UI thread, so it is safe to flip the flag here without scheduling.
		m_isAnimationsCreated = false;
	}

	// Unload the current animated visual (if any).
	private void UnloadContent()
	{
		if (m_animatedVisualRoot is not null)
		{
			// Tear down the outgoing animated visual's bound progress expression so it stops
			// listening for player progress changes; otherwise the OLD source's keyframe chain
			// would still re-evaluate when the new source updates the player's Progress.
			m_animatedVisualRoot.Properties.StopAnimation("Progress");

			var animatedVisual = m_animatedVisual;
			if (animatedVisual is not null)
			{
				m_rootVisual?.Children.RemoveAll();
				m_animatedVisualRoot = null;
				animatedVisual.Dispose();
				m_animatedVisual = null;
			}

			InvalidateMeasure();

			Duration = TimeSpan.Zero;
			IsAnimatedVisualLoaded = false;
		}
	}

	private void UpdateContent()
	{
		// Unload the existing content, if any.
		UnloadContent();

		// Try to create a new animated visual.
		var source = Source;
		if (source is null)
		{
			return;
		}

		object? diagnostics;
		IAnimatedVisual? animatedVisual = null;
		try
		{
			animatedVisual = source.TryCreateAnimatedVisual(m_rootVisual!.Compositor, out diagnostics);
		}
		catch
		{
			// Fall through to fallback content below.
			animatedVisual = null;
		}

		m_animatedVisual = animatedVisual;
		m_isAnimationsCreated = animatedVisual is not null;

		if (animatedVisual is null)
		{
			// Source didn't return an animated visual: this player will leave the legacy path
			// in charge for sources that still rely on it (e.g. Lottie JSON via SKCanvasElement).
			m_useWinUIFlow = false;
			return;
		}

		m_useWinUIFlow = true;

		// If the content is empty, leave the player in fallback mode but mark the WinUI flow active.
		if (animatedVisual.RootVisual is null || animatedVisual.Size == Vector2.Zero)
		{
			return;
		}

		// We have non-empty content to show; clear any fallback content.
		if (m_isFallenBack)
		{
			m_isFallenBack = false;
			UnloadFallbackContent();
		}

		// Ensure the player's child visual is mounted.
		ElementCompositionPreview.SetElementChildVisual(this, m_rootVisual!);

		m_animatedVisualRoot = animatedVisual.RootVisual;
		m_animatedVisualSize = animatedVisual.Size;
		m_rootVisual!.Children.InsertAtTop(m_animatedVisualRoot);

		// Size has changed. Tell XAML to re-measure.
		InvalidateMeasure();

		// Ensure the animated visual has a Progress property. This guarantees that a composition
		// without a Progress property won't blow up when we create an expression that references it below.
		m_animatedVisualRoot.Properties.InsertScalar("Progress", 0.0F);

		// Tie the animated visual's Progress property to the player Progress with an ExpressionAnimation.
		var compositor = m_rootVisual.Compositor;
		var progressAnimation = compositor.CreateExpressionAnimation("_.Progress");
		progressAnimation.SetReferenceParameter("_", m_progressPropertySet!);
		m_animatedVisualRoot.Properties.StartAnimation("Progress", progressAnimation);

		Duration = animatedVisual.Duration;
		IsAnimatedVisualLoaded = true;

		// Check whether playing has been started already via reentrance from a DP handler.
		if (m_nowPlaying is not null)
		{
			m_nowPlaying.Start();
		}
		else if (AutoPlay)
		{
			// Start playing immediately.
			_ = PlayAsyncWinUI(0, 1, true);
		}
	}

	private void LoadFallbackContent()
	{
		// TODO Uno: Fallback content rendering is currently a no-op. The WinUI implementation
		// loads FallbackContent's DataTemplate and adds the resulting UIElement as a Panel child.
		// AnimatedVisualPlayer in Uno derives from FrameworkElement directly, so a future change
		// should switch the base type to Panel (matching WinUI) before this can be wired up.
	}

	private void UnloadFallbackContent()
	{
		// See LoadFallbackContent.
	}

	private Size MeasureOverrideWinUI(Size availableSize)
	{
		if (m_animatedVisualRoot is null || m_animatedVisualSize == Vector2.Zero)
		{
			return new Size(0, 0);
		}

		switch (Stretch)
		{
			case Stretch.None:
				return new Size(
					Math.Min(m_animatedVisualSize.X, availableSize.Width),
					Math.Min(m_animatedVisualSize.Y, availableSize.Height));

			case Stretch.Fill:
				if (!double.IsInfinity(availableSize.Width) && !double.IsInfinity(availableSize.Height))
				{
					return availableSize;
				}
				break;

			case Stretch.UniformToFill:
				if (!double.IsInfinity(availableSize.Width) && !double.IsInfinity(availableSize.Height))
				{
					var widthScaleU = availableSize.Width / m_animatedVisualSize.X;
					var heightScaleU = availableSize.Height / m_animatedVisualSize.Y;
					var measuredSize = (heightScaleU < widthScaleU)
						? new Size(availableSize.Width, m_animatedVisualSize.Y * widthScaleU)
						: new Size(m_animatedVisualSize.X * heightScaleU, availableSize.Height);

					return new Size(
						Math.Min(measuredSize.Width, availableSize.Width),
						Math.Min(measuredSize.Height, availableSize.Height));
				}
				break;
		}

		// Uniform scaling.
		var widthScale = (double.IsInfinity(availableSize.Width) ? float.MaxValue : availableSize.Width) / m_animatedVisualSize.X;
		var heightScale = (double.IsInfinity(availableSize.Height) ? float.MaxValue : availableSize.Height) / m_animatedVisualSize.Y;
		return (heightScale > widthScale)
			? new Size(availableSize.Width, m_animatedVisualSize.Y * widthScale)
			: new Size(m_animatedVisualSize.X * heightScale, availableSize.Height);
	}

	private Size ArrangeOverrideWinUI(Size finalSize)
	{
		if (m_rootVisual is null)
		{
			return finalSize;
		}

		Vector2 scale;
		Vector2 arrangedSize;

		if (m_animatedVisualRoot is null)
		{
			scale = Vector2.One;
			arrangedSize = Vector2.Zero;
		}
		else
		{
			var stretch = Stretch;
			if (stretch == Stretch.None)
			{
				scale = Vector2.One;
				arrangedSize = new Vector2(
					(float)Math.Min(finalSize.Width, m_animatedVisualSize.X),
					(float)Math.Min(finalSize.Height, m_animatedVisualSize.Y));
			}
			else
			{
				scale = new Vector2((float)finalSize.Width, (float)finalSize.Height) / m_animatedVisualSize;

				switch (stretch)
				{
					case Stretch.Uniform:
						if (scale.X < scale.Y)
						{
							scale.Y = scale.X;
						}
						else
						{
							scale.X = scale.Y;
						}
						break;
					case Stretch.UniformToFill:
						if (scale.X > scale.Y)
						{
							scale.Y = scale.X;
						}
						else
						{
							scale.X = scale.Y;
						}
						break;
				}

				arrangedSize = new Vector2(
					(float)Math.Min(finalSize.Width / scale.X, m_animatedVisualSize.X),
					(float)Math.Min(finalSize.Height / scale.Y, m_animatedVisualSize.Y));

				var offset = (new Vector2((float)finalSize.Width, (float)finalSize.Height) - (m_animatedVisualSize * scale)) / 2;
				m_rootVisual.Offset = new Vector3(offset, 0);

				if (m_rootVisual.Clip is InsetClip insetClip)
				{
					if (stretch == Stretch.UniformToFill)
					{
						var clipOffset = -(offset / scale);
						insetClip.LeftInset = clipOffset.X;
						insetClip.TopInset = clipOffset.Y;
					}
					else
					{
						insetClip.LeftInset = 0;
						insetClip.TopInset = 0;
					}
				}
			}
		}

		m_rootVisual.Size = arrangedSize;
		m_rootVisual.Scale = new Vector3(scale, 1f);

		return finalSize;
	}

	// Public API.
	// IUIElement / IUIElementOverridesHelper
	/// <summary>
	/// Creates the <see cref="Microsoft.UI.Xaml.Automation.Peers.AutomationPeer"/> implementation for this control.
	/// </summary>
	protected override Microsoft.UI.Xaml.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
		=> new Microsoft.UI.Xaml.Automation.Peers.AnimatedVisualPlayerAutomationPeer(this);

	// ---------- Public API ----------

	/// <summary>
	/// Pauses the currently playing animation. If no animation is playing, this is a no-op.
	/// </summary>
	public void Pause()
	{
		if (m_useWinUIFlow)
		{
			PauseWinUI();
		}
		else
		{
			// LEGACY: Uno's pre-7.0 path delegates back into the source for sources that still own
			// the animation timeline themselves (e.g. Lottie JSON via SKCanvasElement).
			Source?.Pause();
		}
	}

	/// <summary>
	/// Plays the animation between the specified progress points.
	/// </summary>
	/// <param name="fromProgress">The starting progress value (0..1).</param>
	/// <param name="toProgress">The ending progress value (0..1).</param>
	/// <param name="looped">Whether the play should repeat indefinitely.</param>
	/// <returns>An async action that completes when the play finishes (or never, when <paramref name="looped"/> is <c>true</c>).</returns>
	public IAsyncAction PlayAsync(double fromProgress, double toProgress, bool looped)
	{
		if (m_useWinUIFlow)
		{
			return PlayAsyncWinUI(fromProgress, toProgress, looped).AsAsyncAction();
		}

		// LEGACY: drive playback through the source itself.
		Source?.Play(fromProgress, toProgress, looped);
		return Task.CompletedTask.AsAsyncAction();
	}

	/// <summary>Resumes the animation if it has been paused.</summary>
	public void Resume()
	{
		if (m_useWinUIFlow)
		{
			ResumeWinUI();
		}
		else
		{
			// LEGACY:
			Source?.Resume();
		}
	}

	/// <summary>Sets the animation progress to a specific normalized value.</summary>
	/// <param name="progress">A normalized progress value between 0.0 and 1.0.</param>
	public void SetProgress(double progress)
	{
		if (m_useWinUIFlow)
		{
			SetProgressWinUI(progress);
		}
		else
		{
			// LEGACY:
			Source?.SetProgress(progress);
		}
	}

	/// <summary>Stops the currently playing animation, returning it to its starting position.</summary>
	public void Stop()
	{
		if (m_useWinUIFlow)
		{
			StopWinUI();
		}
		else
		{
			// LEGACY:
			Source?.Stop();
		}
	}

	private void OnSourceChanged(DependencyPropertyChangedEventArgs args)
	{
		var previousUseWinUIFlow = m_useWinUIFlow;
		// Clear cached animated visual so we re-evaluate the WinUI flow with the new source.
		m_useWinUIFlow = false;

#if __SKIA__
		if (IsLoaded)
		{
			OnSourceChangedWinUI();
		}
#endif

		if (!m_useWinUIFlow && previousUseWinUIFlow)
		{
			// We were on the WinUI flow; the legacy hooks won't have been called for the previous
			// source, so calling them here would be a no-op. Nothing to do.
		}

		if (!m_useWinUIFlow)
		{
			// LEGACY fallback: source did not provide an IAnimatedVisual, so use the legacy Uno path.
			OnSourceChangedLegacy();
		}
	}

	private void OnAutoPlayChanged(DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		if (m_useWinUIFlow)
		{
			OnAutoPlayChangedWinUI(args);
			return;
		}
#endif
		// LEGACY: ask the source to update auto-play state.
		Source?.Update(this);
	}

	private void OnPlaybackRateChanged(DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		if (m_useWinUIFlow)
		{
			OnPlaybackRateChangedWinUI(args);
			return;
		}
#endif
		// LEGACY:
		Source?.Update(this);
	}

	private void OnFallbackContentChanged(DependencyPropertyChangedEventArgs args)
	{
#if __SKIA__
		if (m_useWinUIFlow)
		{
			OnFallbackContentChangedWinUI(args);
			return;
		}
#endif
		// LEGACY: nothing for non-Skia today.
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		if (m_useWinUIFlow)
		{
			return MeasureOverrideWinUI(availableSize);
		}

		// LEGACY:
		return MeasureOverrideLegacy(availableSize);
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		if (m_useWinUIFlow)
		{
			return ArrangeOverrideWinUI(finalSize);
		}

		// LEGACY:
		return ArrangeOverrideLegacy(finalSize);
	}

	private protected override void OnLoaded()
	{
#if __SKIA__
		EnsureWinUIRootVisual();
		// First load: try to wire up the WinUI flow now that we have a XamlRoot.
		if (Source is not null && m_animatedVisual is null)
		{
			m_useWinUIFlow = false;
			UpdateContent();
		}
#endif

		if (!m_useWinUIFlow)
		{
			OnLoadedLegacy();
		}

		base.OnLoaded();
	}

	private protected override void OnUnloaded()
	{
		if (m_useWinUIFlow)
		{
#if __SKIA__
			// Tear down any in-flight play so it can't leak across loaded/unloaded transitions.
			m_nowPlaying?.Complete();
			UnloadContent();
#endif
		}
		else
		{
			OnUnloadedLegacy();
		}

		base.OnUnloaded();
	}

	// ---- Animation play awaitable ----

	// Mirrors AnimatedVisualPlayer::AnimationPlay in WinUI. Backed by a TaskCompletionSource so callers
	// of PlayAsync can await completion without polling.
	private sealed class AnimationPlay
	{
		private readonly AnimatedVisualPlayer _owner;
		private readonly float _fromProgress;
		private readonly float _toProgress;
		private readonly bool _looped;
		private readonly TimeSpan _playDuration;
		private readonly TaskCompletionSource<object?> _tcs = new();

		private AnimationController? _controller;
		private bool _isPaused;
		private bool _started;
		private ScalarKeyFrameAnimation? _animation;
		private EventHandler? _stoppedHandler;

		public AnimationPlay(AnimatedVisualPlayer owner, float fromProgress, float toProgress, bool looped)
		{
			_owner = owner;
			_fromProgress = fromProgress;
			_toProgress = toProgress;
			_looped = looped;

			// If toProgress is less than fromProgress the animation will wrap around,
			// so the time is calculated as fromProgress..end + start..toProgress.
			var durationAsProgress = fromProgress > toProgress ? ((1 - fromProgress) + toProgress) : (toProgress - fromProgress);
			_playDuration = TimeSpan.FromTicks((long)(_owner.Duration.Ticks * durationAsProgress));
		}

		public Task Task => _tcs.Task;

		private bool IsCurrentPlay => ReferenceEquals(_owner.m_nowPlaying, this);

		public void Start()
		{
			if (_started)
			{
				return;
			}

			_started = true;

			// If the duration is really short (< 20ms) don't bother trying to animate.
			if (_playDuration < TimeSpan.FromMilliseconds(20))
			{
				_owner.SetProgressWinUI(_fromProgress);
				return;
			}

			var compositor = _owner.m_progressPropertySet!.Compositor;
			var animation = compositor.CreateScalarKeyFrameAnimation();
			animation.Duration = _playDuration;
			var linearEasing = compositor.CreateLinearEasingFunction();

			animation.InsertKeyFrame(0, _fromProgress);

			if (_fromProgress > _toProgress)
			{
				var timeToEnd = (1 - _fromProgress) / ((1 - _fromProgress) + _toProgress);
				animation.InsertKeyFrame(timeToEnd, 1, linearEasing);
				animation.InsertKeyFrame(MathF.BitIncrement(timeToEnd), 0, linearEasing);
			}

			animation.InsertKeyFrame(1, _toProgress, linearEasing);

			if (_looped)
			{
				animation.IterationBehavior = AnimationIterationBehavior.Forever;
			}
			else
			{
				animation.IterationBehavior = AnimationIterationBehavior.Count;
				animation.IterationCount = 1;
			}

			_animation = animation;

			// Subscribe to the keyframe animation's Stopped event so we know when the play finishes.
			// WinUI uses CompositionScopedBatch.Completed for this; Uno's compositor does not yet
			// track per-batch animation lifetimes, so we observe the underlying animation directly.
			if (!_looped)
			{
				_stoppedHandler = (_, _) => Complete();
				animation.Stopped += _stoppedHandler;
			}

			_owner.m_progressPropertySet.StartAnimation("Progress", animation);
			_controller = _owner.m_progressPropertySet.TryGetAnimationController("Progress");

			if (_isPaused)
			{
				_controller?.Pause();
			}

			var playbackRate = (float)_owner.PlaybackRate;
			// AnimationController in Uno does not expose PlaybackRate yet; assume 1.0 for now.
			// TODO Uno: Wire AnimationController.PlaybackRate when implemented.

			_owner.IsPlaying = true;
		}

		public void Pause()
		{
			_isPaused = true;
			_controller?.Pause();
		}

		public void Resume()
		{
			_isPaused = false;
			_controller?.Resume();
		}

		public void SetPlaybackRate(float value)
		{
			// TODO Uno: AnimationController.PlaybackRate is not implemented yet.
		}

		public void Complete()
		{
			if (_animation is { } animation && _stoppedHandler is { } handler)
			{
				animation.Stopped -= handler;
				_stoppedHandler = null;
			}

			// Pin the player progress to the final value so the bound expression keeps the
			// content at the right state after the animation has stopped.
			if (IsCurrentPlay)
			{
				_owner.m_progressPropertySet?.InsertScalar("Progress", _toProgress);
				_owner.m_nowPlaying = null;
				_owner.IsPlaying = false;
			}

			_tcs.TrySetResult(null);
		}

		// Tear down without writing through the bound progress chain. Used when the source is
		// being replaced — we want to stop awaiting callers and detach handlers, but pushing a
		// final Progress value would propagate through the outgoing source's expressions.
		public void Detach()
		{
			if (_animation is { } animation && _stoppedHandler is { } handler)
			{
				animation.Stopped -= handler;
				_stoppedHandler = null;
			}

			// Stop the keyframe animation so the compositor stops ticking it.
			_owner.m_progressPropertySet?.StopAnimation("Progress");

			_tcs.TrySetResult(null);
		}
	}
}

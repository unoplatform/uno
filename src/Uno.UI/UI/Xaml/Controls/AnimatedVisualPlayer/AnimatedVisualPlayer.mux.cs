// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AnimatedVisualPlayer.cpp, commit 3cae15f0

#nullable enable

using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.Core;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class AnimatedVisualPlayer
{
	private sealed partial class AnimationPlay
	{
		public AnimationPlay(
			AnimatedVisualPlayer owner,
			float fromProgress,
			float toProgress,
			bool looped)
		{
			m_owner = owner;
			m_fromProgress = fromProgress;
			m_toProgress = toProgress;
			m_looped = looped;

			// Save the play duration as time.
			// If toProgress is less than fromProgress the animation will wrap around,
			// so the time is calculated as fromProgress..end + start..toProgress.
			var durationAsProgress = fromProgress > toProgress ? ((1 - fromProgress) + toProgress) : (toProgress - fromProgress);
			// NOTE: this relies on the Duration() being set on the owner.
			m_playDuration = TimeSpan.FromTicks((long)(owner.Duration.Ticks * durationAsProgress));
		}

		public float FromProgress()
		{
			return m_fromProgress;
		}

		// REENTRANCE SIDE EFFECT: IsPlaying DP.
		public void Start()
		{
			// m_owner should be alive since we are calling Start() from owner only
			MUX_ASSERT(m_owner is not null);
			MUX_ASSERT(IsCurrentPlay());
			MUX_ASSERT(m_controller is null);
			var owner = m_owner!;

			// TODO Uno: WinUI computes m_playDuration once in the AnimationPlay ctor (cpp:22-27), relying on
			// Duration() already being set. Uno's animated-visual sources load asynchronously, so a PlayAsync()
			// issued before the source resolves carries a zero duration and would self-complete through the
			// <20ms fast path below. Recompute only in that case; every path WinUI exercises is unchanged.
			if (m_playDuration == TimeSpan.Zero)
			{
				var durationAsProgress = m_fromProgress > m_toProgress ? ((1 - m_fromProgress) + m_toProgress) : (m_toProgress - m_fromProgress);
				m_playDuration = TimeSpan.FromTicks((long)(owner.Duration.Ticks * durationAsProgress));
			}

			// If the duration is really short (< 20ms) don't bother trying to animate.
			if (m_playDuration < TimeSpan.FromMilliseconds(20))
			{
				// Nothing to play. Jump to the from position.
				// This will have the side effect of completing this play immediately.
				owner.SetProgress(m_fromProgress);
				// Do not do anything after calling SetProgress()... the AnimationPlay is destructed already.
				return;
			}
			else
			{
				// Create an animation to drive the Progress property.
				var compositor = owner.m_progressPropertySet.Compositor;
				var animation = compositor.CreateScalarKeyFrameAnimation();
				animation.Duration = m_playDuration;
				var linearEasing = compositor.CreateLinearEasingFunction();

				// Play from fromProgress.
				animation.InsertKeyFrame(0, m_fromProgress);

				// from > to is treated as playing from fromProgress to the end, then playing from
				// the beginning to toProgress. Insert extra keyframes to do that.
				if (m_fromProgress > m_toProgress)
				{
					// Play to the end.
					var timeToEnd = (1 - m_fromProgress) / ((1 - m_fromProgress) + m_toProgress);
					animation.InsertKeyFrame(timeToEnd, 1, linearEasing);
					// Jump to the beginning.
					animation.InsertKeyFrame(MathF.BitIncrement(timeToEnd), 0, linearEasing);
				}

				// Play to toProgress
				animation.InsertKeyFrame(1, m_toProgress, linearEasing);

				if (m_looped)
				{
					animation.IterationBehavior = AnimationIterationBehavior.Forever;
				}
				else
				{
					animation.IterationBehavior = AnimationIterationBehavior.Count;
					animation.IterationCount = 1;
				}

				// Create a batch so that we can know when the animation finishes. This only
				// works for non-looping animations (the batch completes immediately
				// for looping animations).
				m_batch = m_looped
					? null
					: compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

				// Start the animation and get the controller.
				owner.m_progressPropertySet.StartAnimation("Progress", animation);

				m_controller = owner.m_progressPropertySet.TryGetAnimationController("Progress");

				if (!owner.m_isHostVisible)
				{
					m_isPausedBecauseHidden = true;
				}

				if (m_isPaused || m_isPausedBecauseHidden)
				{
					// The play was paused before it was started.
					m_controller?.Pause();
				}

				// Set the playback rate.
				var playbackRate = (float)owner.PlaybackRate;
				if (m_controller is not null)
				{
					m_controller.PlaybackRate = playbackRate;

					if (playbackRate < 0)
					{
						// Play from end to beginning if playing in reverse.
						m_controller.Progress = 1;
					}
				}

				if (m_batch is not null)
				{
					var weakThis = new WeakReference<AnimationPlay>(this);
					// Subscribe to the batch completed event.
					m_batchCompletedHandler = (sender, args) =>
					{
						if (!weakThis.TryGetTarget(out var me))
						{
							return;
						}

						if (me.m_owner is not null)
						{
							// If optimization is set to Resources - destroy animations immediately after player stops.
							if (me.m_owner.AnimationOptimization == PlayerAnimationOptimization.Resources)
							{
								me.m_owner.DestroyAnimations();
							}
						}

						// Complete the play when the batch completes.
						//
						// The "this" pointer is guaranteed to be valid because:
						// 1) The AnimationPlay (*this) is kept alive by a reference from m_owner.m_nowPlaying that
						//    is only reset by a call to the AnimationPlay::Complete() method.
						// 2) Before m_owner.m_nowPlaying is reset in AnimationPlay::Complete(),
						//    the m_batch.Completed event is unsubscribed, guaranteeing that this lambda
						//    will not run after AnimationPlay::Complete() has been called.
						// 3) To handle AnimatedVisualPlayer shutdown, AnimationPlay::Complete() is called when
						//    the AnimatedVisualPlayer is unloaded, so that the AnimationPlay cannot outlive
						//    the AnimatedVisualPlayer.
						//
						// Do not do anything after calling Complete()... the object is destructed already.
						me.Complete();
					};
					m_batch.Completed += m_batchCompletedHandler;
					// Indicate that nothing else is going into the batch.
					m_batch.End();
				}

				// WARNING - this may cause reentrance.
				owner.IsPlaying = true;
			}
		}

		public bool IsCurrentPlay()
		{
			return m_owner is not null && ReferenceEquals(m_owner.m_nowPlaying, this);
		}

		public bool HasStarted() => m_controller is not null;

		public void SetPlaybackRate(float value)
		{
			if (m_controller is not null)
			{
				m_controller.PlaybackRate = value;
			}
		}

		// Called when the animation is becoming hidden.
		public void OnHiding()
		{
			if (!m_isPausedBecauseHidden)
			{
				m_isPausedBecauseHidden = true;

				// Pause the animation if it's not already paused.
				// This is necessary to ensure that the animation doesn't
				// keep running and causing DWM to wake up when the animation
				// cannot be seen.
				if (m_controller is not null)
				{
					if (!m_isPaused)
					{
						m_controller.Pause();
					}
				}
			}
		}

		// Called when the animation was hidden but is now becoming visible.
		public void OnUnhiding()
		{
			if (m_isPausedBecauseHidden)
			{
				m_isPausedBecauseHidden = false;

				// Resume the animation that was paused due to the app being suspended.
				if (m_controller is not null)
				{
					if (!m_isPaused)
					{
						m_controller.Resume();
					}
				}
			}
		}

		public void Pause()
		{
			m_isPaused = true;

			if (m_controller is not null)
			{
				if (!m_isPausedBecauseHidden)
				{
					m_controller.Pause();
				}
			}
		}

		public void Resume()
		{
			m_isPaused = false;

			if (m_controller is not null)
			{
				if (!m_isPausedBecauseHidden)
				{
					m_controller.Resume();
				}
			}
		}

		// Completes the play, and unregisters it from the player.
		// Called on the UI thread from:
		//  * AnimatedVisualPlayer::SetProgress(...)
		//   - when any property is set that invalidates the current play, such as starting a new play or setting progress.
		//  * CompositionScopedBatch::BatchCompleted event
		//   - when a non-looping animation gets to it final keyframe.
		//  * ~AnimatedVisualPlayer - in owner's destructor
		// Do not do anything with this object after calling here... the object is destructed already.
		// REENTRANCE SIDE EFFECT: IsPlaying DP.
		public void Complete()
		{
			//
			// NOTEs about lifetime (i.e. why we can trust that m_owner is still valid)
			//  The AnimatedVisualPlayer will be alive as the time when Complete() is called. This
			//  is because:
			//  1. There is only ever one un-completed AnimationPlay. When a new play
			//     is started the current play is completed.
			//  2. An uncompleted AnimationPlay will be completed when the AnimatedVisualPlayer
			//     is unloaded or the AnimatedVisualPlayer destructor is run.
			//  3. If the call to here is from AnimatedVisualPlayer::SetProgress(...)
			//     then the AnimatedVisualPlayer is obviously still alive.
			//  4. If the batch completion event fires the AnimatedVisualPlayer must still be
			//     alive because if it had been unloaded or destroyedComplete() would have been
			//     called during the unload or from the destructor which would have unsubscribed
			//     from the batch completion event.
			//

			// Grab a copy of the pointer so the object stays alive until the method returns.
			// We need to copy pointer only in case if owner is alive,
			// because we are resetting only owner's pointer in this method
			var me = m_owner?.m_nowPlaying;

			// Unsubscribe from batch.Completed.
			if (m_batch is not null && m_batchCompletedHandler is not null)
			{
				m_batch.Completed -= m_batchCompletedHandler;
				m_batchCompletedHandler = null;
			}

			// If this play is the one that is currently associated with the player,
			// disassociate it from the player and update the player's IsPlaying property.
			if (m_owner is not null && IsCurrentPlay())
			{
				// Disconnect this AnimationPlay from the player.
				m_owner.m_nowPlaying = null;

				// Update the IsPlaying state. Note that this is done
				// after being disconnected so that this AnimationPlay won't be
				// reentered, however the AnimatedVisualPlayer may be reentered.
				// WARNING - this may cause reentrance.
				m_owner.IsPlaying = false;
			}

			// Allow anything waiting on this awaitable to complete.
			// This will not cause reentrance because this signals an event and does not call out.
			m_taskCompletionSource.TrySetResult(null);

			GC.KeepAlive(me);
		}

		// This is called in AnimatedVisualPlayer destructor to prevent
		// AnimationPlay accessing owner in case if it lives longer
		public void ResetOwner()
		{
			m_owner = null;
		}

		public Task Task => m_taskCompletionSource.Task;
	}

	/// <summary>
	/// Initializes a new instance of the AnimatedVisualPlayer class.
	/// </summary>
	public AnimatedVisualPlayer()
	{
		// __RP_Marker_ClassById(RuntimeProfiler::ProfId_AnimatedVisualPlayer);

		// EnsureProperties();

		var compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
		m_rootVisual = compositor.CreateSpriteVisual();
		m_progressPropertySet = m_rootVisual.Properties;

		// Set an initial value for the Progress property.
		m_progressPropertySet.InsertScalar("Progress", 0);

		// Ensure the content can't render outside the bounds of the element.
		m_rootVisual.Clip = compositor.CreateInsetClip();
		m_fallbackContentChildren = new UIElementCollection(this);

		// Subscribe to the Loaded/Unloaded events to ensure we unload the animated visual then reload
		// when it is next loaded.
		m_loadedRevoker.Disposable = Disposable.Create(() => Loaded -= OnLoaded);
		Loaded += OnLoaded;
		m_unloadedRevoker.Disposable = Disposable.Create(() => Unloaded -= OnUnloaded);
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		//
		// Do initialization here rather than in the constructor because when the
		// constructor is called the outer object is not fully initialized.
		//
		// Any initialization that can call back into the outer object MUST be
		// done here rather than the constructor.
		//
		// Other initialization can be done here too, so rather than having to
		// guess whether an initialization call calls back into the outer, just
		// put most of the initialization here.
		//

		// Calls back into the outer - must be done OnLoaded rather than in the constructor.
		ElementCompositionPreview.SetElementChildVisual(this, m_rootVisual);

		HookApplicationAndVisibilityEvents();

		if (m_isUnloaded || m_hasPendingContentUpdate)
		{
			// Reload the content.
			// Only do this if the element had been previously unloaded so that the
			// first Loaded event doesn't overwrite any state that was set before
			// the event was fired.
			m_isUnloaded = false;
			m_hasPendingContentUpdate = false;
			UpdateContent();
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs args)
	{
		// There is an anomaly in the Loading/Loaded/Unloaded events that can cause an Unloaded event to
		// fire when the element is in the tree. When this happens, we end up unlaoding our content
		// and not displaying it. Unfortunately, we can't fix this until at least version 2.0 so for
		// for now we will work around it (as we have suggested to customers to do), by checking to see
		// if we are actually unloaded before removing our content.
		if (!IsLoaded)
		{
			m_isUnloaded = true;
			m_hasPendingContentUpdate = false;
			UnhookApplicationAndVisibilityEvents();
			// Remove any content. If we get reloaded the content will get reloaded.
			UnloadContent();
		}
	}

	private void OnHiding()
	{
		if (m_nowPlaying is not null)
		{
			m_nowPlaying.OnHiding();
		}
	}

	private void OnUnhiding()
	{
		if (m_nowPlaying is not null)
		{
			m_nowPlaying.OnUnhiding();
		}
	}

	// Public API.
	// IUIElement / IUIElementOverridesHelper
	protected override AutomationPeer OnCreateAutomationPeer()
	{
		return new AnimatedVisualPlayerAutomationPeer(this);
	}

	internal override bool CanHaveChildren() => true;

	// Public API.
	// Overrides FrameworkElement::MeasureOverride. Returns the size that is needed to display the
	// animated visual within the available size and respecting the Stretch property.
	protected override Size MeasureOverride(Size availableSize)
	{
		if (m_isFallenBack && m_fallbackContentChildren.Count > 0)
		{
			// We are showing the fallback content due to a failure to load an animated visual.
			// Tell the content to measure itself.
			m_fallbackContentChildren[0].Measure(availableSize);
			// Our size is whatever the fallback content desires.
			return m_fallbackContentChildren[0].DesiredSize;
		}

		if (m_animatedVisualRoot is null || m_animatedVisualSize == Vector2.Zero)
		{
			return new Size(0, 0);
		}

		switch (Stretch)
		{
			case Stretch.None:
				// No scaling will be done. Measured size is the smallest of each dimension.
				return new Size(Math.Min(m_animatedVisualSize.X, availableSize.Width), Math.Min(m_animatedVisualSize.Y, availableSize.Height));
			case Stretch.Fill:
				// Both height and width will be scaled to fill the available space.
				if (!double.IsInfinity(availableSize.Width) && !double.IsInfinity(availableSize.Height))
				{
					// We will scale both dimensions to fill all available space.
					return availableSize;
				}
				// One of the dimensions is infinite and we can't fill infinite dimensions, so
				// fall back to Uniform so at least the non-infinite dimension will be filled.
				break;
			case Stretch.UniformToFill:
				// Height and width will be scaled by the same amount such that there is no space
				// around the edges.
				if (!double.IsInfinity(availableSize.Width) && !double.IsInfinity(availableSize.Height))
				{
					// Scale so there is no space around the edge.
					var widthScale = availableSize.Width / m_animatedVisualSize.X;
					var heightScale = availableSize.Height / m_animatedVisualSize.Y;
					var measuredSize = heightScale < widthScale
						? new Size(availableSize.Width, m_animatedVisualSize.Y * widthScale)
						: new Size(m_animatedVisualSize.X * heightScale, availableSize.Height);

					// Clip the size to the available size.
					measuredSize = new Size
					{
						Width = Math.Min(measuredSize.Width, availableSize.Width),
						Height = Math.Min(measuredSize.Height, availableSize.Height)
					};

					return measuredSize;
				}
				// One of the dimensions is infinite and we can't fill infinite dimensions, so
				// fall back to Uniform so at least the non-infinite dimension will be filled.
				break;
		} // end switch

		// Uniform scaling.
		// Scale so that one dimension fits exactly and no dimension exceeds the boundary.
		var uniformWidthScale = (double.IsInfinity(availableSize.Width) ? float.MaxValue : availableSize.Width) / m_animatedVisualSize.X;
		var uniformHeightScale = (double.IsInfinity(availableSize.Height) ? float.MaxValue : availableSize.Height) / m_animatedVisualSize.Y;
		return uniformHeightScale > uniformWidthScale
			? new Size(availableSize.Width, m_animatedVisualSize.Y * uniformWidthScale)
			: new Size(m_animatedVisualSize.X * uniformHeightScale, availableSize.Height);
	}

	// Public API.
	// Overrides FrameworkElement::ArrangeOverride. Scales to fit the animated visual into finalSize
	// respecting the current Stretch and returns the size actually used.
	protected override Size ArrangeOverride(Size finalSize)
	{
		if (m_isFallenBack && m_fallbackContentChildren.Count > 0)
		{
			// We are showing the fallback content due to a failure to load an animated visual.
			// Tell the content to arrange itself.
			m_fallbackContentChildren[0].Arrange(new Rect(new Point(0, 0), finalSize));
			return finalSize;
		}

		Vector2 scale;
		Vector2 arrangedSize;

		if (m_animatedVisualRoot is null)
		{
			// No content. 0 size.
			scale = Vector2.One;
			arrangedSize = Vector2.Zero;
		}
		else
		{
			var stretch = Stretch;
			if (stretch == Stretch.None)
			{
				// Do not scale, do not center.
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
						// Scale both dimensions by the same amount.
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
						// Scale both dimensions by the same amount and leave no gaps around the edges.
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

				// A size needs to be set because there's an InsetClip applied, and without a
				// size the clip will prevent anything from being visible.
				arrangedSize = new Vector2(
					(float)Math.Min(finalSize.Width / scale.X, m_animatedVisualSize.X),
					(float)Math.Min(finalSize.Height / scale.Y, m_animatedVisualSize.Y));

				// Center the animation within the available space.
				var offset = (new Vector2((float)finalSize.Width, (float)finalSize.Height) - (m_animatedVisualSize * scale)) / 2;
				var z = 0.0F;
				m_rootVisual.Offset = new Vector3(offset, z);

				// Adjust the position of the clip.
				if (m_rootVisual.Clip is not null)
				{
					m_rootVisual.Clip.Offset = stretch == Stretch.UniformToFill
						? -(offset / scale)
						: Vector2.Zero;
				}
			}
		}

		m_rootVisual.Size = arrangedSize;
		var scaleZ = 1.0F;
		m_rootVisual.Scale = new Vector3(scale, scaleZ);

		return finalSize;
	}

	// Public API.
	// Pauses the currently playing animated visual, or does nothing if no play is underway.
	/// <summary>
	/// Pauses the currently playing animated visual, or does nothing if no play is underway.
	/// </summary>
	public void Pause()
	{
		if (m_nowPlaying is not null)
		{
			m_nowPlaying.Pause();
		}
	}

	// Public API.
	/// <summary>
	/// Starts playing the loaded animated visual, or does nothing if no animated visual is loaded.
	/// </summary>
	/// <param name="fromProgress">The point from which to start the animation, as a value from 0 to 1.</param>
	/// <param name="toProgress">The point at which to finish the animation, as a value from 0 to 1.</param>
	/// <param name="looped">If <c>true</c>, the animation loops continuously between <paramref name="fromProgress"/> and <paramref name="toProgress"/>. If <c>false</c>, the animation plays once then stops.</param>
	/// <returns>An async action that is completed when the play is stopped or, if <paramref name="looped"/> is not set, when the play reaches <paramref name="toProgress"/>.</returns>
	/// <remarks>
	/// If <paramref name="toProgress"/> is less than <paramref name="fromProgress"/>, the animated visual will
	/// play from <paramref name="fromProgress"/> to the end, then play from the beginning until it reaches
	/// <paramref name="toProgress"/>. To play an animated visual in reverse, set the playback rate to a negative value.
	/// </remarks>
	public IAsyncAction PlayAsync(double fromProgress, double toProgress, bool looped)
	{
		return PlayAsyncCore().AsAsyncAction();

		async Task PlayAsyncCore()
		{
			// Make sure that animations are created.
			CreateAnimations();

			// Used to detect reentrance.
			var version = ++m_playAsyncVersion;

			// Complete m_nowPlaying if it is still running.
			// Identical to Stop() call but without destroying the animations.
			// WARNING - this call may cause reentrance via the IsPlaying DP.
			if (m_nowPlaying is not null)
			{
				m_progressPropertySet.InsertScalar("Progress", (float)m_currentPlayFromProgress);
				m_nowPlaying.Complete();
			}

			if (version != m_playAsyncVersion)
			{
				// The call was overtaken by another call due to reentrance.
				return;
			}

			MUX_ASSERT(m_nowPlaying is null);

			// Adjust for the case where there is a segment that
			// goes from [fromProgress..0] where m_fromProgress > 0.
			// This is equivalent to [fromProgress..1], and by setting
			// toProgress to 1 it saves us from generating extra key frames.
			if (toProgress == 0 && fromProgress > 0)
			{
				toProgress = 1;
			}

			// Adjust for the case where there is a segment that
			// goes from [1..toProgress] where toProgress > 0.
			// This is equivalent to [0..toProgress], and by setting
			// fromProgress to 0 it saves us from generating extra key frames.
			if (toProgress > 0 && fromProgress == 1)
			{
				fromProgress = 0;
			}

			// Create an AnimationPlay to hold the play information.
			// Keep a copy of the pointer because reentrance may cause the m_nowPlaying
			// value to change.
			var actualFromProgress = Math.Clamp((float)fromProgress, 0.0F, 1.0F);
			var actualToProgress = Math.Clamp((float)toProgress, 0.0F, 1.0F);
			m_currentPlayFromProgress = actualFromProgress;
			var thisPlay = m_nowPlaying = new AnimationPlay(
				this,
				actualFromProgress,
				actualToProgress,
				looped);

			if (IsAnimatedVisualLoaded)
			{
				// There is an animated visual loaded, so start it playing.
				// WARNING - this may cause reentrance via IsPlaying DP.
				thisPlay.Start();
			}

			// Capture the context so we can finish in the calling thread.
			//
			// Await the current play. The await will complete when the animation completes
			// or Stop() is called. It can complete on any thread.
			await thisPlay.Task;

			// Get back to the calling thread.
			// This is necessary to destruct the AnimationPlay, and because callers
			// from the dispatcher thread will expect to continue on the dispatcher thread.
		}
	}

	// Public API.
	/// <summary>
	/// Resumes the currently paused animated visual, or does nothing if there is no animated visual
	/// loaded or the animated visual is not paused.
	/// </summary>
	public void Resume()
	{
		if (m_nowPlaying is not null)
		{
			m_nowPlaying.Resume();
		}
	}

	// Public API.
	// REENTRANCE SIDE EFFECT: IsPlaying DP via m_nowPlaying->Complete() or InsertScalar iff m_nowPlaying.
	/// <summary>
	/// Moves the progress of the animated visual to the given value, or does nothing if no animated
	/// visual is loaded.
	/// </summary>
	/// <param name="progress">A value from 0 to 1 that represents the progress of the animated visual.</param>
	/// <remarks>If the animated visual was playing it will behave as if Stop was called first.</remarks>
	public void SetProgress(double progress)
	{
		// Make sure that animations are created.
		CreateAnimations();

		var clampedProgress = Math.Clamp((float)progress, 0.0F, 1.0F);

		// WARNING: Reentrance via IsPlaying DP may occur from this point down to the end of the method
		//          iff m_nowPlaying.

		// Setting the Progress value will stop the current play.
		m_progressPropertySet.InsertScalar("Progress", clampedProgress);

		// Ensure the current PlayAsync task is completed.
		// Note that this explicit call is necessary, even though InsertScalar
		// will stop the current animation, because the BatchCompleted event for
		// the animation only gets hooked up if the animation is not looped.
		// If there was a BatchCompleted event and it already fired from setting the Progress
		// value then Complete() is a no-op.
		if (m_nowPlaying is not null)
		{
			m_nowPlaying.Complete();
		}

		// If optimization is set to Resources - destroy annimations immediately.
		if (AnimationOptimization == PlayerAnimationOptimization.Resources)
		{
			DestroyAnimations();
		}
	}

	// Public API.
	// REENTRANCE SIDE EFFECT: IsPlaying DP via SetProgress(...) or InsertScalar iff m_nowPlaying.
	/// <summary>
	/// Stops the current play, or does nothing if no play is underway.
	/// </summary>
	public void Stop()
	{
		if (m_nowPlaying is not null)
		{
			// Stop the animation by setting the Progress value to the fromProgress of the
			// most recent play.
			// This may cause reentrance via the IsPlaying DP.
			SetProgress(m_currentPlayFromProgress);
		}
	}

	private void OnAutoPlayPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var newValue = (bool)args.NewValue;

		if (newValue && IsAnimatedVisualLoaded && m_nowPlaying is null)
		{
			// Start playing immediately.
			var from = 0;
			var to = 1;
			var looped = true;
			ObserveAsyncAction(PlayAsync(from, to, looped), nameof(OnAutoPlayPropertyChanged));
		}
	}

	private void OnAnimationOptimizationPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var optimization = (PlayerAnimationOptimization)args.NewValue;

		if (m_nowPlaying is not null)
		{
			// If there is something in play right now we should not create/destroy animations.
			return;
		}

		if (optimization == PlayerAnimationOptimization.Resources)
		{
			DestroyAnimations();
		}
		else if (optimization == PlayerAnimationOptimization.Latency)
		{
			CreateAnimations();
		}
	}

	private void CreateAnimations()
	{
		m_createAnimationsCounter++;

		if (m_isAnimationsCreated || m_animatedVisual is null)
		{
			return;
		}

		// Check if current animated visual supports creating animations and create them.
		if (m_animatedVisual is IAnimatedVisual2 animatedVisual2)
		{
			animatedVisual2.CreateAnimations();
			m_isAnimationsCreated = true;
		}
	}

	private void DestroyAnimations()
	{
		if (!m_isAnimationsCreated || m_animatedVisual is not IAnimatedVisual2 animatedVisual)
		{
			return;
		}

		// Call RequestCommit to make sure that previous compositor calls complete before destroying animations.
		// RequestCommitAsync is available only for RS4+
		// Previously we used get_weak() here, but we found the potential to hit a
		// refcounting problem where in some scenarios the outer object gets
		// an extra Release() in this process.
		var weakThis = new WeakReference<AnimatedVisualPlayer>(this);
		var createAnimationsCounter = m_createAnimationsCounter;
		var requestCommit = m_rootVisual.Compositor.RequestCommitAsync();
		requestCommit.Completed = (action, status) =>
		{
			if (!weakThis.TryGetTarget(out var strongThis))
			{
				return;
			}

			if (status == AsyncStatus.Error)
			{
				try
				{
					action.GetResults();
				}
				catch (Exception e)
				{
					if (strongThis.Log().IsEnabled(LogLevel.Error))
					{
						strongThis.Log().Error($"{nameof(AnimatedVisualPlayer)} failed while waiting to destroy animations.", e);
					}
				}
			}

			strongThis.CompleteDestroyAnimations(createAnimationsCounter, animatedVisual);
		};
	}

	private void CompleteDestroyAnimations(uint createAnimationsCounter, IAnimatedVisual2 animatedVisual)
	{
		// Check if there was any CreateAnimations call after DestroyAnimations.
		// We should not destroy animations in this case,
		// they will be destroyed by the following DestroyAnimations call.
		if (createAnimationsCounter != m_createAnimationsCounter)
		{
			return;
		}

		if (!ReferenceEquals(m_animatedVisual, animatedVisual))
		{
			return;
		}

		animatedVisual.DestroyAnimations();
		m_isAnimationsCreated = false;
	}

	private void OnFallbackContentPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (m_isFallenBack)
		{
			LoadFallbackContent();
		}
	}

	private void OnSourcePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var newSource = args.NewValue as IAnimatedVisualSource;

		// WARNING - this may cause reentrance via the IsPlaying DP iff m_nowPlaying.
		Stop();

		// Disconnect from the update notifications of the old source.
		m_dynamicAnimatedVisualInvalidatedRevoker.Disposable = null;

		if (newSource is IDynamicAnimatedVisualSource newDynamicSource)
		{
			// Connect to the update notifications of the new source.
			// Previously we used get_weak() here, but we found the potential to hit a
			// refcounting problem where in some scenarios the outer object gets
			// an extra Release() in this process.
			var weakThis = new WeakReference<AnimatedVisualPlayer>(this);
			TypedEventHandler<IDynamicAnimatedVisualSource, object> onAnimatedVisualInvalidated = (sender, e) =>
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.UpdateContentOrDefer();
				}
			};
			m_dynamicAnimatedVisualInvalidatedRevoker.Disposable = Disposable.Create(() => newDynamicSource.AnimatedVisualInvalidated -= onAnimatedVisualInvalidated);
			newDynamicSource.AnimatedVisualInvalidated += onAnimatedVisualInvalidated;
		}

		UpdateContentOrDefer();
	}

	// Unload the current animated visual (if any).
	private void UnloadContent(bool preserveUnstartedPlay = false)
	{
		var animatedVisual = m_animatedVisual;
		var hadAnimatedVisual = animatedVisual is not null || m_animatedVisualRoot is not null || IsAnimatedVisualLoaded;

		// This will complete any started play.
		// WARNING - this may cause reentrance via IsPlaying DP iff m_nowPlaying.
		if (m_nowPlaying is not null && (!preserveUnstartedPlay || m_nowPlaying.HasStarted()))
		{
			Stop();
		}

		if (m_animatedVisualRoot is not null)
		{
			m_rootVisual.Children.RemoveAll();
			m_animatedVisualRoot = null;
		}

		if (animatedVisual is not null)
		{
			// Notify the animated visual that it will no longer be used.
			animatedVisual.Dispose();
			m_animatedVisual = null;
		}

		m_animatedVisualSize = Vector2.Zero;
		m_isAnimationsCreated = false;

		if (!hadAnimatedVisual)
		{
			return;
		}

		// Size has changed. Tell XAML to re-measure.
		InvalidateMeasure();

		// WARNING - these may cause reentrance.
		Duration = TimeSpan.Zero;
		Diagnostics = null;
		// Set IsAnimatedVisualLoaded last as it is the property that is most likely
		// to have user code react to its state change.
		IsAnimatedVisualLoaded = false;
	}

	private void UpdateContent()
	{
		if (m_isUnloaded)
		{
			m_hasPendingContentUpdate = true;
			return;
		}

		// Unload the existing content, if any.
		UnloadContent(preserveUnstartedPlay: true);

		// Try to create a new animated visual.
		var source = Source;
		if (source is null)
		{
			// No source set. Nothing to do.
			return;
		}

		object diagnostics = null!;
		IAnimatedVisual? animatedVisual;

		var createAnimations = AnimationOptimization == PlayerAnimationOptimization.Latency;

		if (source is IAnimatedVisualSource3 source3)
		{
			animatedVisual = source3.TryCreateAnimatedVisual(m_rootVisual.Compositor, out diagnostics, createAnimations);
			m_isAnimationsCreated = createAnimations;
			m_animatedVisual = animatedVisual;
		}
		else
		{
			animatedVisual = source.TryCreateAnimatedVisual(m_rootVisual.Compositor, out diagnostics);
			m_isAnimationsCreated = true;

			// m_animatedVisual should be updated before DestroyAnimations call
			m_animatedVisual = animatedVisual;

			// Destroy animations if we don't need them.
			// Old IAnimatedVisualSource interface always creates them.
			if (!createAnimations)
			{
				DestroyAnimations();
			}
		}

		if (animatedVisual is null)
		{
			// Create failed.

			if (!m_isFallenBack)
			{
				// Show the fallback content, if any.
				m_isFallenBack = true;
				LoadFallbackContent();
			}

			// Complete any play that was started during loading.
			// WARNING - this may cause reentrance via IsPlaying DP iff m_nowPlaying.
			Stop();

			// WARNING - this may cause reentrance.
			Diagnostics = diagnostics;

			return;
		}

		// If the content is empty, do nothing. If we are in fallback from a previous
		// failure to load, stay fallen back.
		// Empty content means the source has nothing to show yet.
		if (animatedVisual.RootVisual is null || animatedVisual.Size == Vector2.Zero)
		{
			// WARNING - this may cause reentrance.
			Diagnostics = diagnostics;

			return;
		}

		// We have non-empty content to show.
		// If we were in fallback, clear that fallback content.
		if (m_isFallenBack)
		{
			// Get out of the fallback state.
			m_isFallenBack = false;
			UnloadFallbackContent();
		}

		// Hook up the new animated visual.
		m_animatedVisualRoot = animatedVisual.RootVisual;
		m_animatedVisualSize = animatedVisual.Size;
		m_rootVisual.Children.InsertAtTop(m_animatedVisualRoot);

		// Size has changed. Tell XAML to re-measure.
		InvalidateMeasure();

		// Ensure the animated visual has a Progress property. This guarantees that a composition without
		// a Progress property won't blow up when we create an expression that references it below.
		// Normally the animated visual  would have a Progress property that all its expressions reference,
		// but just in case, insert it here.
		m_animatedVisualRoot.Properties.InsertScalar("Progress", 0.0F);

		// Tie the animated visual's Progress property to the player Progress with an ExpressionAnimation.
		var compositor = m_rootVisual.Compositor;
		var progressAnimation = compositor.CreateExpressionAnimation("_.Progress");
		progressAnimation.SetReferenceParameter("_", m_progressPropertySet);
		m_animatedVisualRoot.Properties.StartAnimation("Progress", progressAnimation);

		// WARNING - these may cause reentrance.
		// Set these properties before the if (AutoPlay()) branch calls PlayAsync(...)
		// so that the properties are updated before playing starts.
		Duration = animatedVisual.Duration;
		Diagnostics = diagnostics;
		// Set IsAnimatedVisualLoaded last as it is the property that is most likely
		// to have user code react to its state change.
		IsAnimatedVisualLoaded = true;

		// Check whether playing has been started already via reentrance from a DP handler.
		if (m_nowPlaying is not null)
		{
			m_nowPlaying.Start();
		}
		else if (AutoPlay)
		{
			// Start playing immediately.
			var from = 0;
			var to = 1;
			var looped = true;
			// NOTE: If !IsAnimatedVisualLoaded() then this is a no-op.
			ObserveAsyncAction(PlayAsync(from, to, looped), nameof(UpdateContent));
		}
	}

	private void LoadFallbackContent()
	{
		MUX_ASSERT(m_isFallenBack);

		UIElement? fallbackContentElement = null;
		var fallbackContentTemplate = FallbackContent;
		if (fallbackContentTemplate is not null)
		{
			// Load the content from the DataTemplate. It should be a UIElement tree root.
			fallbackContentElement = fallbackContentTemplate.LoadContent() is UIElement uiElement
				? uiElement
				: throw new InvalidCastException();
		}

		// Set the (possibly null) content. We allow null content so as to handle the
		// case where the fallback content got removed - in which case we want to
		// clear out the existing content if any.
		SetFallbackContent(fallbackContentElement);
	}

	private void UnloadFallbackContent()
	{
		MUX_ASSERT(!m_isFallenBack);
		SetFallbackContent(null);
	}

	private void SetFallbackContent(UIElement? uiElement)
	{
		// Clear out the existing content.
		m_fallbackContentChildren.Clear();

		// Place the content in the tree.
		if (uiElement is not null)
		{
			m_fallbackContentChildren.Add(uiElement);
		}

		// Size has probably changed. Tell XAML to re-measure.
		InvalidateMeasure();
	}

	private void OnPlaybackRatePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		if (m_nowPlaying is not null)
		{
			m_nowPlaying.SetPlaybackRate((float)(double)args.NewValue);
		}
	}

	private void OnStretchPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		InvalidateMeasure();
	}

	private void HookApplicationAndVisibilityEvents()
	{
		UnhookApplicationAndVisibilityEvents();

		// Subscribe to suspending, resuming, and visibility events so we can pause the animation if it's
		// definitely not visible.
		// Previously we used get_weak() here, but we found the potential to hit a
		// refcounting problem where in some scenarios the outer object gets
		// an extra Release() in this process.
		var weakThis = new WeakReference<AnimatedVisualPlayer>(this);
		if (Application.Current is { } application)
		{
			void OnSuspending(object sender, ISuspendingEventArgs e)
			{
				if (weakThis.TryGetTarget(out var strongThis))
				{
					strongThis.OnHiding();
				}
			}

			m_suspendingRevoker.Disposable = Disposable.Create(() => application.Suspending -= OnSuspending);
			application.Suspending += OnSuspending;

			void OnResuming(object? sender, object? e)
			{
				if (weakThis.TryGetTarget(out var strongThis)
					&& CoreWindow.GetForCurrentThread()?.Visible == true)
				{
					strongThis.OnUnhiding();
				}
			}

			m_resumingRevoker.Disposable = Disposable.Create(() => application.Resuming -= OnResuming);
			application.Resuming += OnResuming;
		}

		if (XamlRoot is { } xamlRoot)
		{
			TypedEventHandler<XamlRoot, XamlRootChangedEventArgs> onXamlRootChanged = (innerSender, innerArgs) =>
			{
				if (weakThis.TryGetTarget(out var strongThis)
					&& strongThis.XamlRoot is { } strongXamlRoot)
				{
					var hostVisibility = strongXamlRoot.IsHostVisible;
					if (hostVisibility != strongThis.m_isHostVisible)
					{
						strongThis.m_isHostVisible = hostVisibility;
						if (hostVisibility)
						{
							// Transition from invisible to visible.
							strongThis.OnUnhiding();
						}
						else
						{
							// Transition from visible to invisible.
							strongThis.OnHiding();
						}
					}
				}
			};

			m_xamlRootChangedRevoker.Disposable = Disposable.Create(() => xamlRoot.Changed -= onXamlRootChanged);
			xamlRoot.Changed += onXamlRootChanged;
			m_isHostVisible = xamlRoot.IsHostVisible;
			if (m_isHostVisible)
			{
				OnUnhiding();
			}
			else
			{
				OnHiding();
			}
		}
		else
		{
			m_isHostVisible = true;
			OnUnhiding();
		}
	}

	private void UnhookApplicationAndVisibilityEvents()
	{
		m_xamlRootChangedRevoker.Disposable = null;
		m_suspendingRevoker.Disposable = null;
		m_resumingRevoker.Disposable = null;
	}

	private void UpdateContentOrDefer()
	{
		if (m_isUnloaded)
		{
			m_hasPendingContentUpdate = true;
			return;
		}

		UpdateContent();
	}

	private void ObserveAsyncAction(IAsyncAction action, string callerName)
	{
		action.Completed = (completedAction, status) =>
		{
			if (status != AsyncStatus.Error)
			{
				return;
			}

			try
			{
				completedAction.GetResults();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(AnimatedVisualPlayer)}.{callerName} failed.", e);
				}
			}
		};
	}
}

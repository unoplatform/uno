using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Windows.Foundation;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

partial class AnimatedVisualPlayer
{
	partial class AnimationPlay
	{
		private AnimationPlay(
			AnimatedVisualPlayer owner,
			float fromProgress,
			float toProgress,
			bool looped)
		{
			// Save the play duration as time.
			m_owner = owner;
			m_fromProgress = fromProgress;

			m_toProgress = toProgress;

			m_looped = looped;
			// Save the play duration as time.
			// If toProgress is less than fromProgress the animation will wrap around,
			// so the time is calculated as fromProgress..end + start..toProgress.
			var durationAsProgress = fromProgress > toProgress ? ((1 - fromProgress) + toProgress) : (toProgress - fromProgress);
			// NOTE: this relies on the Duration() being set on the owner.
			m_playDuration = std.chrono.duration_cast<TimeSpan>(m_owner.Duration() * durationAsProgress);
		}

		internal internalfloat FromProgress()
		{
			return m_fromProgress;
		}

		// REENTRANCE SIDE EFFECT: IsPlaying DP.
		internal void Start()
		{
			//// m_owner should be alive since we are calling Start() from owner only
			//MUX_ASSERT(m_owner);
			//MUX_ASSERT(IsCurrentPlay());
			//MUX_ASSERT(!m_controller);

			//// If the duration is really short (< 20ms) don't bother trying to animate.
			//if (m_playDuration < TimeSpan{ 20ms })
			//    {
			//	// Nothing to play. Jump to the from position.
			//	// This will have the side effect of completing this play immediately.
			//	m_owner.SetProgress(m_fromProgress);
			//	// Do not do anything after calling SetProgress()... the AnimationPlay is destructed already.
			//	return;
			//}

			//	else
			//{
			//	// Create an animation to drive the Progress property.
			//	var compositor = m_owner.m_progressPropertySet.Compositor();
			//	var animation = compositor.CreateScalarKeyFrameAnimation();
			//	animation.Duration(m_playDuration);
			//	var linearEasing = compositor.CreateLinearEasingFunction();

			//	// Play from fromProgress.
			//	animation.InsertKeyFrame(0, m_fromProgress);

			//	// from > to is treated as playing from fromProgress to the end, then playing from
			//	// the beginning to toProgress. Insert extra keyframes to do that.
			//	if (m_fromProgress > m_toProgress)
			//	{
			//		// Play to the end.
			//		var timeToEnd = (1 - m_fromProgress) / ((1 - m_fromProgress) + m_toProgress);
			//		animation.InsertKeyFrame(timeToEnd, 1, linearEasing);
			//		// Jump to the beginning.
			//		animation.InsertKeyFrame(.nextafterf(timeToEnd, 1), 0, linearEasing);
			//	}

			//	// Play to toProgress
			//	animation.InsertKeyFrame(1, m_toProgress, linearEasing);

			//	if (m_looped)
			//	{
			//		animation.IterationBehavior(AnimationIterationBehavior.Forever);
			//	}
			//	else
			//	{
			//		animation.IterationBehavior(AnimationIterationBehavior.Count);
			//		animation.IterationCount(1);
			//	}

			//	// Create a batch so that we can know when the animation finishes. This only
			//	// works for non-looping animations (the batch completes immediately
			//	// for looping animations).
			//	m_batch = m_looped
			//		? null
			//		: compositor.CreateScopedBatch(CompositionBatchTypes.Animation);

			//	// Start the animation and get the controller.
			//	m_owner.m_progressPropertySet.StartAnimation("Progress", animation);

			//	m_controller = m_owner.m_progressPropertySet.TryGetAnimationController("Progress");

			//	if (m_isPaused || m_isPausedBecauseHidden)
			//	{
			//		// The play was paused before it was started.
			//		m_controller.Pause();
			//	}

			//	// Set the playback rate.
			//	var playbackRate = (float)(m_owner.PlaybackRate());
			//	m_controller.PlaybackRate(playbackRate);

			//	if (playbackRate < 0)
			//	{
			//		// Play from end to beginning if playing in reverse.
			//		m_controller.Progress(1);
			//	}

			//	if (m_batch)
			//	{
			//		std.weak_ptr<AnimationPlay> me_weak = m_owner.m_nowPlaying;
			//		// Subscribe to the batch completed event.
			//		m_batchCompletedToken = m_batch.Completed([me_weak](object &, CompositionBatchCompletedEventArgs &)

			//				{
			//			var me = me_weak.lock () ;

			//			if (!me)
			//			{
			//				return;
			//			}

			//			if (me.m_owner)
			//			{
			//				// If optimization is set to Resources - destroy animations immediately after player stops.
			//				if (me.m_owner.AnimationOptimization() == PlayerAnimationOptimization.Resources)
			//				{
			//					me.m_owner.DestroyAnimations();
			//				}
			//			}

			//			// Complete the play when the batch completes.
			//			//
			//			// The "this" pointer is guaranteed to be valid because:
			//			// 1) The AnimationPlay (this) is kept alive by a reference from m_owner.m_nowPlaying that
			//			//    is only reset by a call to the AnimationPlay.Complete() method.
			//			// 2) Before m_owner.m_nowPlaying is reset in AnimationPlay.Complete(),
			//			//    the m_batch.Completed event is unsubscribed, guaranteeing that this lambda
			//			//    will not run after AnimationPlay.Complete() has been called.
			//			// 3) To handle AnimatedVisualPlayer shutdown, AnimationPlay.Complete() is called when
			//			//    the AnimatedVisualPlayer is unloaded, so that the AnimationPlay cannot outlive
			//			//    the AnimatedVisualPlayer.
			//			//
			//			// Do not do anything after calling Complete()... the object is destructed already.
			//			me.Complete();
			//		});
			//		// Indicate that nothing else is going into the batch.
			//		m_batch.End();
			//	}

			//	// WARNING - this may cause reentrance.
			//	m_owner.IsPlaying(true);
			//}
		}

		internal bool IsCurrentPlay()
		{
			return m_owner is not null && m_owner.m_nowPlaying == this;
		}

		internal void SetPlaybackRate(float value)
		{
			if (m_controller is not null)
			{
				m_controller.PlaybackRate = value;
			}
		}

		// Called when the animation is becoming hidden.
		internal void OnHiding()
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
		internal void OnUnhiding()
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

		internal void Pause()
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

		internal void Resume()
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
		//  * SetProgress
		//   - when any property is set that invalidates the current play, such as starting a new play or setting progress.
		//  * CompositionScopedBatch.BatchCompleted event
		//   - when a non-looping animation gets to it final keyframe.
		//  * ~AnimatedVisualPlayer - in owner's destructor
		// Do not do anything with this object after calling here... the object is destructed already.
		// REENTRANCE SIDE EFFECT: IsPlaying DP.
		internal void Complete()
		{
			//
			// NOTEs about lifetime (i.e. why we can trust that m_owner is still valid)
			//  The AnimatedVisualPlayer will be alive as the time when Complete() is called. This
			//  is because:
			//  1. There is only ever one un-completed AnimationPlay. When a new play
			//     is started the current play is completed.
			//  2. An uncompleted AnimationPlay will be completed when the AnimatedVisualPlayer
			//     is unloaded or the AnimatedVisualPlayer destructor is run.
			//  3. If the call to here is from SetProgress
			//     then the AnimatedVisualPlayer is obviously still alive.
			//  4. If the batch completion event fires the AnimatedVisualPlayer must still be
			//     alive because if it had been unloaded or destroyedComplete() would have been
			//     called during the unload or from the destructor which would have unsubscribed
			//     from the batch completion event.
			//    

			// Grab a copy of the pointer so the object stays alive until the method returns.
			// We need to copy pointer only in case if owner is alive,
			// because we are resetting only owner's pointer in this method
			std.AnimationPlay me;
			if (m_owner)
			{
				me = m_owner.m_nowPlaying;
			}

			// Unsubscribe from batch.Completed.
			if (m_batch)
			{
				m_batch.Completed(m_batchCompletedToken);
				m_batchCompletedToken = { 0 };
			}

			// If this play is the one that is currently associated with the player,
			// disassociate it from the player and update the player's IsPlaying property.
			if (m_owner && IsCurrentPlay())
			{
				// Disconnect this AnimationPlay from the player.
				m_owner.m_nowPlaying.reset();

				// Update the IsPlaying state. Note that this is done
				// after being disconnected so that this AnimationPlay won't be
				// reentered, however the AnimatedVisualPlayer may be reentered.
				// WARNING - this may cause reentrance.
				m_owner.IsPlaying(false);
			}

			// Allow anything waiting on this awaitable to complete.
			// This will not cause reentrance because this signals an event and does not call out.
			CompleteAwaits();
		}

		// This is called in AnimatedVisualPlayer destructor to prevent
		// AnimationPlay accessing owner in case if it lives longer
		internal void ResetOwner()
		{
			m_owner = null;
		}
	}


	public AnimatedVisualPlayer()
	{
		//__RP_Marker_ClassById(RuntimeProfiler.ProfId_AnimatedVisualPlayer);

		// EnsureProperties();

		var compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
		m_rootVisual = compositor.CreateSpriteVisual();
		m_progressPropertySet = m_rootVisual.Properties;

		// Set an initial value for the Progress property.
		m_progressPropertySet.InsertScalar("Progress", 0);

		// Ensure the content can't render outside the bounds of the element.
		m_rootVisual.Clip = compositor.CreateInsetClip();

		// Subscribe to suspending, resuming, and visibility events so we can pause the animation if it's 
		// definitely not visible.
		m_suspendingRevoker = Application.Current.Suspending(auto_revoke, [weakThis{ get_weak() }](
			var /*sender*/,
			var /*e*/)

		{
			if (var strongThis = weakThis)
	        {
				strongThis.OnHiding();
			}
		});

		m_resumingRevoker = Application.Current.Resuming(auto_revoke, [weakThis{ get_weak() }](
			var /*sender*/,
			var /*e*/)

		{
			if (var strongThis = weakThis)
	        {
				if (CoreWindow.GetForCurrentThread().Visible())
				{
					strongThis.OnUnhiding();
				}
			}
		});

		// Subscribe to the Loaded/Unloaded events to ensure we unload the animated visual then reload
		// when it is next loaded.
		Loaded += OnLoaded;
		m_loadedRevoker.Disposable = Disposable.Create(() => Loaded -= OnLoaded);
		Unloaded += OnUnloaded;
		m_unloadedRevoker.Disposable = Disposable.Create(() => Unloaded -= OnUnloaded);
	}

	~AnimatedVisualPlayer()
	{
		// TODO:MZ: Move to unloaded
		// Ensure any outstanding play is stopped.
		if (m_nowPlaying)
		{
			// To stop and destroy m_nowPlaying we need to call Complete()
			// But we need to detach us (m_owner) from m_nowPlaying first so that AnimationPlay
			// will not try to call us (m_owner), since we are already in the destructor
			m_nowPlaying.ResetOwner();
			m_nowPlaying.Complete();
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs args)
	{
		//
		// Do initialization here rather than in the ructor because when the
		// ructor is called the outer object is not fully initialized.
		//
		// Any initialization that can call back into the outer object MUST be
		// done here rather than the ructor.
		//
		// Other initialization can be done here too, so rather than having to 
		// guess whether an initialization call calls back into the outer, just 
		// put most of the initialization here.
		//

		// Calls back into the outer - must be done OnLoaded rather than in the ructor.
		ElementCompositionPreview.SetElementChildVisual(this, m_rootVisual);

		// Set the background for AnimatedVisualPlayer to ensure it will be visible to
		// hit-testing. XAML does not hit test anything that has a null background.
		// Set here rather than in the ructor so we don't have to worry about it
		// calling back into the outer.
		Background = new SolidColorBrush(Colors.Transparent);

		if (m_isUnloaded)
		{
			// Reload the content. 
			// Only do this if the element had been previously unloaded so that the
			// first Loaded event doesn't overwrite any state that was set before
			// the event was fired.
			UpdateContent();
			m_isUnloaded = false;
		}

		// TODO:MZ: Avoid leaking (XamlRoot.Changed should be a weak event!)
		var weakThis = WeakReferencePool.RentSelfWeakReference(this);
		void OnXamlRootChanged(object sender, object args)
		{
			if (weakThis.IsAlive && weakThis.Target is AnimatedVisualPlayer strongThis)
			{
				var xamlRoot = strongThis.XamlRoot;
				bool hostVisibility = xamlRoot.IsHostVisible;
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
		}

		XamlRoot.Changed += OnXamlRootChanged;
		m_xamlRootChangedRevoker.Disposable = Disposable.Create(() => XamlRoot.Changed -= OnXamlRootChanged);
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
			// Remove any content. If we get reloaded the content will get reloaded.
			UnloadContent();
		}
	}

	private void OnHiding()
	{
		if (m_nowPlaying)
		{
			m_nowPlaying.OnHiding();
		}
	}

	private void OnUnhiding()
	{
		if (m_nowPlaying)
		{
			m_nowPlaying.OnUnhiding();
		}
	}

	// Public API.
	protected override AutomationPeer OnCreateAutomationPeer() => new AnimatedVisualPlayerAutomationPeer(this);

	// Public API.
	// Overrides FrameworkElement.MeasureOverride. Returns the size that is needed to display the
	// animated visual within the available size and respecting the Stretch property.
	protected override Size MeasureOverride(Size availableSize)
	{
		if (m_isFallenBack && Children.Size > 0)
		{
			// We are showing the fallback content due to a failure to load an animated visual.
			// Tell the content to measure itself.
			Children[0].Measure(availableSize);
			// Our size is whatever the fallback content desires.
			return Children[0].DesiredSize;
		}

		if ((!m_animatedVisualRoot) || (m_animatedVisualSize == float2.zero()))
		{
			return { 0, 0 };
		}

		switch (Stretch)
		{
			case Stretch.None:
				// No scaling will be done. Measured size is the smallest of each dimension.
				return { std.min(m_animatedVisualSize.x, availableSize.Width), std.min(m_animatedVisualSize.y, availableSize.Height) };
			case Stretch.Fill:
				// Both height and width will be scaled to fill the available space.
				if (availableSize.Width != std.numeric_limits<double>.infinity() && availableSize.Height != std.numeric_limits<double>.infinity())
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
				if (availableSize.Width != std.numeric_limits<double>.infinity() && availableSize.Height != std.numeric_limits<double>.infinity())
				{
					// Scale so there is no space around the edge.
					var widthScale = availableSize.Width / m_animatedVisualSize.x;
					var heightScale = availableSize.Height / m_animatedVisualSize.y;
					var measuredSize = (heightScale < widthScale)
						? Size{ availableSize.Width, m_animatedVisualSize.y* widthScale }
		                : Size{ m_animatedVisualSize.x* heightScale, availableSize.Height };

					// Clip the size to the available size.
					measuredSize = Size{
						std.min(measuredSize.Width, availableSize.Width),
		                            std.min(measuredSize.Height, availableSize.Height)

					};

					return measuredSize;
				}
				// One of the dimensions is infinite and we can't fill infinite dimensions, so
				// fall back to Uniform so at least the non-infinite dimension will be filled.
				break;
		} // end switch

		// Uniform scaling.
		// Scale so that one dimension fits exactly and no dimension exceeds the boundary.
		var widthScale = ((availableSize.Width == std.numeric_limits<double>.infinity()) ? FLT_MAX : availableSize.Width) / m_animatedVisualSize.x;
		var heightScale = ((availableSize.Height == std.numeric_limits<double>.infinity()) ? FLT_MAX : availableSize.Height) / m_animatedVisualSize.y;
		return (heightScale > widthScale)
			? Size{ availableSize.Width, m_animatedVisualSize.y* widthScale }
		        : Size{ m_animatedVisualSize.x* heightScale, availableSize.Height };
	}

	// Public API.
	// Overrides FrameworkElement.ArrangeOverride. Scales to fit the animated visual into finalSize 
	// respecting the current Stretch and returns the size actually used.
	protected override Size ArrangeOverride(Size finalSize)
	{
		//	if (m_isFallenBack && Children.Size > 0)
		//	{
		//		// We are showing the fallback content due to a failure to load an animated visual.
		//		// Tell the content to arrange itself.
		//		Children[0].Arrange(Rect{ Point{ 0,0}, finalSize });
		//		return finalSize;
		//	}

		//	float2 scale;
		//	float2 arrangedSize;

		//	if (!m_animatedVisualRoot)
		//	{
		//		// No content. 0 size.
		//		scale = { 1, 1 };
		//		arrangedSize = { 0,0 };
		//	}
		//	else
		//	{
		//		var stretch = Stretch;
		//		if (stretch == Stretch.None)
		//		{
		//			// Do not scale, do not center.
		//			scale = { 1, 1 };
		//			arrangedSize = {
		//				std.min(finalSize.Width, m_animatedVisualSize.x),
		//                std.min(finalSize.Height, m_animatedVisualSize.y)

		//			};
		//		}
		//		else
		//		{
		//			scale = (float2)(finalSize) / m_animatedVisualSize;

		//			switch (stretch)
		//			{
		//				case Stretch.Uniform:
		//					// Scale both dimensions by the same amount.
		//					if (scale.x < scale.y)
		//					{
		//						scale.y = scale.x;
		//					}
		//					else
		//					{
		//						scale.x = scale.y;
		//					}
		//					break;
		//				case Stretch.UniformToFill:
		//					// Scale both dimensions by the same amount and leave no gaps around the edges.
		//					if (scale.x > scale.y)
		//					{
		//						scale.y = scale.x;
		//					}
		//					else
		//					{
		//						scale.x = scale.y;
		//					}
		//					break;
		//			}

		//			// A size needs to be set because there's an InsetClip applied, and without a 
		//			// size the clip will prevent anything from being visible.
		//			arrangedSize = {
		//				std.min(finalSize.Width / scale.x, m_animatedVisualSize.x),
		//                std.min(finalSize.Height / scale.y, m_animatedVisualSize.y)

		//			};

		//			// Center the animation within the available space.
		//			var offset = (finalSize - (m_animatedVisualSize * scale)) / 2;
		//			var z = 0.0F;
		//			m_rootVisual.Offset({ offset, z });

		//			// Adjust the position of the clip.
		//			m_rootVisual.Clip().Offset(
		//				(stretch == Stretch.UniformToFill)
		//				? -(offset / scale)
		//				: float2.zero()
		//			);
		//		}
		//	}

		//	m_rootVisual.Size(arrangedSize);
		//	var z = 1.0F;
		//	m_rootVisual.Scale({ scale, z });

		//	return finalSize;
	}

	// Public API.
	// Accessor for ProgressObject property.
	// NOTE: This is not a dependency property because it never changes and is not useful for binding.
	/// <summary>
	/// Gets a CompositionObject that is animated along with the progress of the AnimatedVisualPlayer.
	/// </summary>
	public CompositionObject ProgressObject => m_progressPropertySet;

	// Public API.
	/// <summary>
	/// Pauses the currently playing animated visual, or does nothing if no play is underway.
	/// </summary>
	public void Pause()
	{
		if (m_nowPlaying)
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
	/// <param name="looped">If true, the animation loops continuously between fromProgress and toProgress. If false, the animation plays once then stops.</param>
	/// <returns>An async action that is completed when the play is stopped or, if looped is not set, when the play reaches toProgress.</returns>
	public IAsyncAction PlayAsync(double fromProgress, double toProgress, bool looped)
	{
		// Make sure that animations are created.
		CreateAnimations();

		// Used to detect reentrance.
		var version = ++m_playAsyncVersion;

		// Complete m_nowPlaying if it is still running.
		// Identical to Stop() call but without destroying the animations.
		// WARNING - this call may cause reentrance via the IsPlaying DP.
		if (m_nowPlaying)
		{
			m_progressPropertySet.InsertScalar("Progress", (float)(m_currentPlayFromProgress));
			m_nowPlaying.Complete();
		}

		if (version != m_playAsyncVersion)
		{
			// The call was overtaken by another call due to reentrance.
			co_return;
		}

		MUX_ASSERT(!m_nowPlaying);

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
		var thisPlay = m_nowPlaying = std.new AnimationPlay(
			this,
			std.clamp((float)(fromProgress), 0.0F, 1.0F),
			std.clamp((float)(toProgress), 0.0F, 1.0F),
			looped);

		if (IsAnimatedVisualLoaded())
		{
			// There is an animated visual loaded, so start it playing.
			// WARNING - this may cause reentrance via IsPlaying DP.
			thisPlay.Start();
		}

		// Capture the context so we can finish in the calling thread.
		apartment_context calling_thread;

		// Await the current play. The await will complete when the animation completes
		// or Stop() is called. It can complete on any thread.
		co_await thisPlay;

		// Get back to the calling thread.
		// This is necessary to destruct the AnimationPlay, and because callers
		// from the dispatcher thread will expect to continue on the dispatcher thread.
		co_await calling_thread;
	}

	// Public API.
	/// <summary>
	/// Resumes the currently paused animated visual, or does nothing if there is no animated visual loaded or the animated visual is not paused.
	/// </summary>
	public void Resume()
	{
		if (m_nowPlaying)
		{
			m_nowPlaying.Resume();
		}
	}

	// Public API.
	// REENTRANCE SIDE EFFECT: IsPlaying DP via m_nowPlaying.Complete() or InsertScalar iff m_nowPlaying.
	/// <summary>
	/// Moves the progress of the animated visual to the given value, or does nothing if no animated visual is loaded.
	/// </summary>
	/// <param name="progress">A value from 0 to 1 that represents the progress of the animated visual.</param>
	public void SetProgress(double progress)
	{
		// Make sure that animations are created.
		CreateAnimations();

		var clampedProgress = Math.Clamp((float)(progress), 0.0F, 1.0F);

		// WARNING: Reentrance via IsPlaying DP may occur from this point down to the end of the method
		//          iff m_nowPlaying.

		// Setting the Progress value will stop the current play.
		m_progressPropertySet.InsertScalar("Progress", (float)(clampedProgress));

		// Ensure the current PlayAsync task is completed.
		// Note that this explicit call is necessary, even though InsertScalar
		// will stop the current animation, because the BatchCompleted event for
		// the animation only gets hooked up if the animation is not looped.
		// If there was a BatchCompleted event and it already fired from setting the Progress
		// value then Complete() is a no-op.
		if (m_nowPlaying)
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
	// REENTRANCE SIDE EFFECT: IsPlaying DP via SetProgress or InsertScalar iff m_nowPlaying.
	/// <summary>
	/// Stops the current play, or does nothing if no play is underway.
	/// </summary>
	public void Stop()
	{
		if (m_nowPlaying)
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

		if (newValue && IsAnimatedVisualLoaded && !m_nowPlaying)
		{
			// Start playing immediately.
			var from = 0;
			var to = 1;
			var looped = true;
			var ignore = PlayAsync(from, to, looped);
		}
	}

	private void OnAnimationOptimizationPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var optimization = (PlayerAnimationOptimization)args.NewValue;

		if (m_nowPlaying)
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

		if (m_isAnimationsCreated || m_animatedVisual == null)
		{
			return;
		}

		// Check if current animated visual supports creating animations and create them.
		if (m_animatedVisual is { } animatedVisual)
		{
			if (animatedVisual is IAnimatedVisual2 animatedVisual2)
			{
				animatedVisual2.CreateAnimations();
				m_isAnimationsCreated = true;
			}
		}
	}

	private void DestroyAnimations()
	{
		if (!m_isAnimationsCreated || m_animatedVisual == null)
		{
			return;
		}

		// Call RequestCommit to make sure that previous compositor calls complete before destroying animations.
		// RequestCommitAsync is available only for RS4+
		m_rootVisual.Compositor.RequestCommitAsync().Completed(
			[me_weak = get_weak(), createAnimationsCounter = m_createAnimationsCounter](auto, auto) {
			var me = me_weak;

			if (!me)
			{
				return;
			}

			// Check if there was any CreateAnimations call after DestroyAnimations.
			// We should not destroy animations in this case,
			// they will be destroyed by the following DestroyAnimations call.
			if (createAnimationsCounter != me.m_createAnimationsCounter)
			{
				return;
			}

			// Check if current animated visual supports destroyig animations.
			if (var animatedVisual = me.m_animatedVisual)
	            {
				if (var animatedVisual2 = animatedVisual as IAnimatedVisual2())
	                {
					animatedVisual2.DestroyAnimations();
					me.m_isAnimationsCreated = false;
				}
			}
		}
	    );
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
		m_dynamicAnimatedVisualInvalidatedRevoker.revoke();

		if (newSource is IDynamicAnimatedVisualSource newDynamicSource)
		{
			// Connect to the update notifications of the new source.
			m_dynamicAnimatedVisualInvalidatedRevoker
				= newDynamicSource.AnimatedVisualInvalidated(auto_revoke, [weakThis{ get_weak() }](
					var /*sender*/,
					var /*e*/)

			{
				if (var strongThis = weakThis)
	            {
					strongThis.UpdateContent();
				}
			});
		}

		UpdateContent();
	}

	// Unload the current animated visual (if any).
	private void UnloadContent()
	{
		if (m_animatedVisualRoot is not null)
		{
			// This will complete any current play.
			// WARNING - this may cause reentrance via IsPlaying DP iff m_nowPlaying.
			Stop();

			// Remove the old animated visual (if any).
			var animatedVisual = m_animatedVisual;
			if (animatedVisual is not null)
			{
				m_rootVisual.Children.RemoveAll();
				m_animatedVisualRoot = null;
				// Notify the animated visual that it will no longer be used.
				(animatedVisual as IDisposable)?.Dispose();
				m_animatedVisual = null;
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
	}

	private void UpdateContent()
	{
		// Unload the existing content, if any.
		UnloadContent();

		// Try to create a new animated visual.
		var source = Source;
		if (source is null)
		{
			// No source set. Nothing to do.
			return;
		}

		object diagnostics;
		IAnimatedVisual animatedVisual;

		bool createAnimations = AnimationOptimization == PlayerAnimationOptimization.Latency;

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
		if (animatedVisual?.RootVisual is null || animatedVisual.Size == default(Vector2))
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
		// Set these properties before the if (AutoPlay()) branch calls PlayAsync
		// so that the properties are updated before playing starts.
		Duration = animatedVisual.Duration;
		Diagnostics = diagnostics;
		// Set IsAnimatedVisualLoaded last as it is the property that is most likely
		// to have user code react to its state change.
		IsAnimatedVisualLoaded = true;

		// Check whether playing has been started already via reentrance from a DP handler.
		if (m_nowPlaying)
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
			var ignore = PlayAsync(from, to, looped);
		}
	}

	private void LoadFallbackContent()
	{
		MUX_ASSERT(m_isFallenBack);

		UIElement fallbackContentElement = null;
		var fallbackContentTemplate = FallbackContent;
		if (fallbackContentTemplate is not null)
		{
			// Load the content from the DataTemplate. It should be a UIElement tree root.
			DependencyObject fallbackContentObject = fallbackContentTemplate.LoadContent();
			// Get the content.
			fallbackContentElement = fallbackContentObject as UIElement;
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

	private void SetFallbackContent(UIElement uiElement)
	{
		// Clear out the existing content.
		ClearChildren();

		// Place the content in the tree.
		if (uiElement is not null)
		{
			AddChild(uiElement);
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
}

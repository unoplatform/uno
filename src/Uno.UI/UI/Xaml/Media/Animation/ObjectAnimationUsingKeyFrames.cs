using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Markup;
using Windows.UI.Core;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Media.Animation
{
	[ContentProperty(Name = "KeyFrames")]
	public sealed partial class ObjectAnimationUsingKeyFrames : Timeline, ITimeline, IKeyFramesProvider
	{
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);
		private EventActivity _traceActivity;

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{9EBBD06A-ADA3-464F-93C6-C850AB62A41D}");

			public const int Start = 1;
			public const int Stop = 2;
			public const int Pause = 3;
			public const int Resume = 4;
		}

		private KeyFrameScheduler<object> _frameScheduler;
		private IKeyFrame<object> _currentFrame;
		private (int count, TimeSpan time) _playStatus;

		public ObjectAnimationUsingKeyFrames()
		{
			KeyFrames = new ObjectKeyFrameCollection(owner: this, isAutoPropertyInheritanceEnabled: false);
		}

		#region KeyFrames DependencyProperty

		public ObjectKeyFrameCollection KeyFrames
		{
			get => (ObjectKeyFrameCollection)GetValue(KeyFramesProperty);
			internal set => SetValue(KeyFramesProperty, value);
		}

		/// <remarks>
		/// This property is not exposed as a DP in UWP, but it is required
		/// to be one for the DataContext/TemplatedParent to flow properly.
		/// </remarks>
		internal static DependencyProperty KeyFramesProperty { get; } =
			DependencyProperty.Register(
				name: "KeyFrames",
				propertyType: typeof(ObjectKeyFrameCollection),
				ownerType: typeof(ObjectAnimationUsingKeyFrames),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null
				)
			);

		#endregion

		internal override TimeSpan GetCalculatedDuration()
		{
			var duration = Duration;
			if (duration != Duration.Automatic)
			{
				return base.GetCalculatedDuration();
			}

			if (KeyFrames.Any())
			{
				var lastKeyTime = KeyFrames.Max(kf => kf.KeyTime);
				return lastKeyTime.TimeSpan;
			}

			return base.GetCalculatedDuration();
		}

		void ITimeline.Begin()
		{
			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.Start,
					EventOpcode.Start,
					payload: GetTraceProperties()
				);
			}

			Reset();

			State = TimelineState.Active;

#if __SKIA__
			PlayDeferred();
#else
			PlayImmediate();
#endif
		}

		private void PlayImmediate()
		{
			// MUX Reference: CAnimation::GetAnimationBaseValue / ReadBaseValuesFromTargetOrHandoff
			// Ensure keyframe theme resources are resolved with the target element's
			// effective theme before playback begins. This is needed because keyframe
			// values may have been resolved with the wrong theme context during template
			// materialization or resource binding updates outside a theme walk.
			EnsureKeyFrameThemeResources();

			_playStatus = default;
			_frameScheduler = new KeyFrameScheduler<object>(
				BeginTime,
				Duration.HasTimeSpan ? Duration.TimeSpan : default(TimeSpan?),
				default,
				KeyFrames,
				OnFrame,
				OnFramesEnd);
			_frameScheduler.Start();
		}

		void ITimeline.Stop()
		{
			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.Stop,
					EventOpcode.Stop,
					payload: GetTraceProperties()
				);
			}

#if __SKIA__
			CancelDeferredPlay();
#endif
			// We explicitly call the Stop of the _frameScheduler before the Reset dispose it,
			// so the EndReason will be Stopped instead of Aborted.
			_frameScheduler?.Stop();

			Reset();
			ClearValue();
		}

		void ITimeline.Resume()
		{
			if (State != TimelineState.Paused)
			{
				return;
			}

			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.Resume,
					EventOpcode.Send,
					payload: GetTraceProperties()
				);
			}

			State = TimelineState.Active;
			_frameScheduler!.Resume();
		}

		void ITimeline.Pause()
		{
			if (State != TimelineState.Active)
			{
				return;
			}

			if (_trace.IsEnabled)
			{
				_traceActivity = _trace.WriteEventActivity(
					TraceProvider.Pause,
					EventOpcode.Send,
					payload: GetTraceProperties()
				);
			}

			State = TimelineState.Paused;
			_frameScheduler!.Pause();
		}

		void ITimeline.Seek(TimeSpan offset)
		{
			_frameScheduler?.Seek(offset);
		}

		void ITimeline.SeekAlignedToLastTick(TimeSpan offset)
		{
			// Same as Seek
			((ITimeline)this).Seek(offset);
		}

		void ITimeline.SkipToFill()
		{
#if __SKIA__
			CancelDeferredPlay();
#endif
			// Set value to last keytime and set state to filling
			_frameScheduler?.Dispose();
			_frameScheduler = null;

			// Read the final value directly from the last keyframe (not cached).
			// This matches WinUI's tick-based value reading and supports the
			// Begin(); SkipToFill(); pattern used by AppBar.UpdateTemplateSettings().
			var fillFrame = KeyFrames.OrderBy(k => k.KeyTime.TimeSpan).Last();

			SetValue(fillFrame.Value);
			State = TimelineState.Stopped;
		}

		void ITimeline.Deactivate()
		{
#if __SKIA__
			CancelDeferredPlay();
#endif
			Reset();
		}

		/// <summary>
		/// Brings the Timeline to its initial state
		/// </summary>
		private void Reset()
		{
			_frameScheduler?.Dispose();
			_frameScheduler = null;
			_currentFrame = null;

			State = TimelineState.Stopped;
		}

		private IDisposable OnFrame(object currentValue, IKeyFrame<object> frame, TimeSpan duration)
		{
			// MUX Reference: CAnimation::UpdateAnimationUsingKeyFrames (animation.cpp:247)
			// WinUI reads keyframe values directly — theme resources have already been
			// resolved during NotifyThemeChangedCore tree walk propagation (which reaches
			// keyframes via: Element → VisualStateGroups → VisualState → Storyboard →
			// Animation → KeyFrameCollection → ObjectKeyFrame.UpdateAllThemeReferences).
			_currentFrame = frame;
			SetValue(frame.Value);
			return null;
		}

		// MUX Reference: CAnimation::NotifyThemeChangedCore (animation.cpp:1030)
		// WinUI requests an immediate animation tick so updated keyframe values
		// are applied to the target. In Uno, keyframe values have already been
		// refreshed by UpdateAllThemeReferences during the theme walk (via
		// UpdateChildResourceBindings propagation). We re-apply the current
		// frame's value directly.
		private protected override void OnThemeChanged()
		{
			if (_currentFrame is not null && State != TimelineState.Stopped)
			{
				SetValue(_currentFrame.Value);
			}
		}

		private void OnFramesEnd(KeyFrameScheduler<object>.EndReason endReason)
		{
			_playStatus = (_playStatus.count + 1, _playStatus.time + _frameScheduler!.Elapsed);

			if (endReason != KeyFrameScheduler<object>.EndReason.EndOfFrames)
			{
				return;
			}

			if (RepeatBehavior.ShouldRepeat(_playStatus.time, _playStatus.count))
			{
				Replay();
				return;
			}

			if (FillBehavior == FillBehavior.HoldEnd)//Two types of fill behaviors : HoldEnd - Keep displaying the last frame
			{
				Fill();
			}
			else// Stop - Put back the initial state
			{
				Reset();
				ClearValue();
			}

			OnCompleted();
		}

		/// <summary>
		/// Fills the animation: the final frame is shown and left visible
		/// </summary>
		private void Fill()
		{
			var lastKeyFrame = KeyFrames.MaxBy(k => k.KeyTime.TimeSpan);

			_frameScheduler?.Dispose();
			_frameScheduler = null;
			_currentFrame = lastKeyFrame;

			State = TimelineState.Filling;
			SetValue(lastKeyFrame.Value);
		}

		/// <summary>
		/// Replays the Timeline
		/// </summary>
		private void Replay()
		{
			_frameScheduler?.Dispose();

			ClearValue();


			_frameScheduler = new KeyFrameScheduler<object>(
				BeginTime,
				Duration.HasTimeSpan ? Duration.TimeSpan : default(TimeSpan?),
				default,
				KeyFrames,
				OnFrame,
				OnFramesEnd);
			_frameScheduler.Start();
		}

		/// <summary>
		/// Destroys the animation
		/// </summary>
		/// <param name="disposing"></param>
		private protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (_frameScheduler != null)
			{
				_frameScheduler.Dispose();
				_frameScheduler = null;
			}
		}

		/// <summary>
		/// Resolves all keyframe theme resources with the target element's effective theme.
		/// Called once at Begin time to ensure keyframe values match the element-level theme.
		/// </summary>
		private void EnsureKeyFrameThemeResources()
		{
			if (PropertyInfo?.DataContext is not FrameworkElement targetElement)
			{
				return;
			}

			var effectiveTheme = targetElement.GetTheme();
			if (effectiveTheme == Theme.None)
			{
				return;
			}

			var baseTheme = Theming.GetBaseValue(effectiveTheme);
			if (baseTheme == Theme.None)
			{
				// No base Light/Dark theme (e.g. HighContrast): resolve without overriding the active theme.
				foreach (var keyFrame in KeyFrames)
				{
					if (keyFrame is IDependencyObjectStoreProvider provider)
					{
						provider.Store.UpdateAllThemeReferences(null);
					}
				}

				return;
			}

			var themeKey = baseTheme == Theme.Light ? "Light" : "Dark";
			ResourceDictionary.PushRequestedThemeForSubTree(themeKey);
			try
			{
				foreach (var keyFrame in KeyFrames)
				{
					if (keyFrame is IDependencyObjectStoreProvider provider)
					{
						provider.Store.UpdateAllThemeReferences(null);
					}
				}
			}
			finally
			{
				ResourceDictionary.PopRequestedThemeForSubTree();
			}
		}

		IEnumerable IKeyFramesProvider.GetKeyFrames() => KeyFrames;
	}
}

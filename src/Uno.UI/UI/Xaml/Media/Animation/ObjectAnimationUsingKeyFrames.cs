using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Windows.UI.Xaml.Markup;
using Windows.UI.Core;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Media.Animation
{
    [ContentProperty(Name = "KeyFrames")]
    public sealed partial class ObjectAnimationUsingKeyFrames : Timeline, ITimeline
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

		// Initialize the field with zero capacity, as it may stay empty more often than it is being used.
		private CompositeDisposable _scheduledFrames = new CompositeDisposable(0);
        
        private Stopwatch _watch = new Stopwatch();
        private TimeSpan _elapsedTime;
        
        private int _replayCount;

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
		internal static readonly DependencyProperty KeyFramesProperty =
			DependencyProperty.Register(
				name: "KeyFrames", 
				propertyType: typeof(ObjectKeyFrameCollection), 
				ownerType: typeof(ObjectAnimationUsingKeyFrames), 
				typeMetadata: new PropertyMetadata(
					defaultValue: null
				)
			);

		#endregion


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

			_watch.Start();
            _replayCount = 1;

            Play();
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

            Play();
        }

        void ITimeline.Pause()
        {
            if (State == TimelineState.Paused)
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

			State = TimelineState.Active;
            _scheduledFrames.Clear();

            State = TimelineState.Paused;

            _elapsedTime = _watch.Elapsed;
			_watch.Stop();
			_watch.Reset();

        }
        void ITimeline.Seek(TimeSpan offset)
        {
            _elapsedTime = offset;

            if (State == TimelineState.Active)
            {
                var keyframe = KeyFrames.FirstOrDefault(f => f.KeyTime.TimeSpan >= offset);
                this.SetValue(keyframe.Value);
            }
        }

        void ITimeline.SeekAlignedToLastTick(TimeSpan offset)
        {
            // Same as Seek
            ((ITimeline)this).Seek(offset);
        }

        void ITimeline.SkipToFill()
        {
            // Set value to last keytime and set state to filling
            _scheduledFrames.Clear();
            _elapsedTime = KeyFrames.Max(k => k.KeyTime.TimeSpan);

            var fillFrame = KeyFrames.First(k => k.KeyTime.TimeSpan == _elapsedTime);

            SetValue(fillFrame.Value);
            State = TimelineState.Stopped;

		}

		void ITimeline.Deactivate()
		{
			Reset();
		}

		/// <summary>
		/// Brings the Timeline to its initial state
		/// </summary>
		private void Reset()
        {
            _scheduledFrames.Clear();
            State = TimelineState.Stopped;
            _elapsedTime = TimeSpan.Zero;
			_watch.Stop();
			_watch.Reset();
		}

		/// <summary>
		/// Runs the Timeline, By Scheduling the KeyFrames
		/// </summary>
		private void Play()
        {
            State = TimelineState.Active;

            var beginTime = BeginTime.GetValueOrDefault(TimeSpan.Zero);  //Delay before starting the animation
            
            var duration = TimeSpan.MaxValue;  //Duration, frames below this timespan ar not played    
            if (Duration.HasTimeSpan)
            {
                duration = Duration.TimeSpan;
            }
            
            var finalTime = KeyFrames
                .Max(okf => okf.KeyTime.TimeSpan)
                .Add(beginTime)
                .Subtract(_elapsedTime);
            //Final Time : the time of the last Keyframe

            foreach (var keyFrame in this.KeyFrames.Where(k => k.KeyTime.TimeSpan <= duration))
            {
                var value = keyFrame.Value;
                var dueTime = keyFrame.KeyTime.TimeSpan.Add(beginTime).Subtract(_elapsedTime);

				Action update = () =>
				{
					//When a frame is scheduled to run, the bool determines if it is the last frame
					OnFrame(value, dueTime.Equals(finalTime));
				};

				if (dueTime == TimeSpan.Zero)
				{
					update();
				}
				else
				{
					_scheduledFrames.Add(
						CoreDispatcher.Main.RunAsync(
							CoreDispatcherPriority.Normal,
							async () =>
							{
								await Task.Delay(dueTime);
								update();
							}
						)
					);
				}
            }
        }

        /// <summary>
        /// When a frame is scheduled to run
        /// </summary>
        /// <param name="value">The payload of the keyframe</param>
        /// <param name="isLast">Is this the last frame?</param>
        private void OnFrame(object value, bool isLast)
        {
            if (!isLast)
            {
                SetValue(value);
                return;
            }

            if (NeedsRepeat())
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
            var lastTime = KeyFrames.Max(k => k.KeyTime.TimeSpan);
            var lastKeyFrame = KeyFrames.First(k => k.KeyTime.TimeSpan.Equals(lastTime));

            State = TimelineState.Filling;
            _scheduledFrames.Clear();
            _elapsedTime = _watch.Elapsed;
            SetValue(lastKeyFrame.Value);
        }

        /// <summary>
        /// Replays the Timeline
        /// </summary>
        private void Replay()
        {
			ClearValue();
            _replayCount++;
            _scheduledFrames.Clear();
            _elapsedTime = TimeSpan.Zero;
			_watch.Reset();
			Play();
        }

        /// <summary>
        /// Checks if the Timeline will repeat.
        /// </summary>
        /// <returns><c>true</c>, Repeat needed, <c>false</c> otherwise.</returns>
        private bool NeedsRepeat()
        {
            var totalTime = _watch.Elapsed;

            //3 types of repeat behavors,             
            return (RepeatBehavior.Type == RepeatBehaviorType.Forever) // Forever: Will always repeat the TimeLine
                || (RepeatBehavior.HasCount && RepeatBehavior.Count > _replayCount) // Count: Will repeat the TimeLine x times
                || (RepeatBehavior.HasDuration && RepeatBehavior.Duration - totalTime > TimeSpan.Zero); // Duration: Will repeat the TimeLine for a given duration
        }

		/// <summary>
		/// Destroys the animation
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_scheduledFrames != null)
            {
                _scheduledFrames.Dispose();
                _scheduledFrames = null;
            }
        }
    }
}

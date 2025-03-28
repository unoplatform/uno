using CoreAnimation;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Animates a property using Xaml property setters.
	/// </summary>
	internal abstract class DisplayLinkValueAnimator : IValueAnimator
	{
		private const double MillisecondsPerSecond = 1000d;
		private const long MaxNumberOfFrames = 10000;// 10,000 frames is enough 40kb of floats
		private const long MinNumberOfFrames = 1;// At least 1 frame please     

		private long _duration;

		private const int FrameRate = 50;

		private long _numberOfFrames;
#if __IOS__
		private CADisplayLink _displayLink;
#else
		private NSTimer _timer;
#endif

		private double _startTime;

		private bool _isDisposed;
		private bool _isAttachedToLooper;

		protected IEasingFunction _easingFunction;

		public void Start()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}

			PrebuildFrames();//Preload as much of the animation as possible

			IsRunning = true;

			_startTime = 0;// Start time will be set if it is equal to zero

			if (!_isAttachedToLooper)
			{
#if __IOS__
				//http://www.bigspaceship.com/ios-animation-intervals/
				_displayLink = CADisplayLink.Create(OnFrame);//OnFrame is called when an animation frame is ready

				//Need to attach the _displayLink to the MainLoop (uiThread) so that the call back will be called on the UI thread
				//Default == normal UI updates
				_displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Default);
				//UITracking == updates during scrolling
				_displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.UITracking);
#else
				ScheduleTimer();
#endif
				_isAttachedToLooper = true;
			}
		}

#if __MACOS__
		private void ScheduleTimer()
		{
			if (_timer?.IsValid == true) return;
			UnscheduleTimer();
			_timer = NSTimer.CreateRepeatingScheduledTimer(0.001, OnTimerTick);
		}

		private void UnscheduleTimer()
		{
			_timer?.Invalidate();
			_timer?.Dispose();
			_timer = null;
		}

		private void OnTimerTick(NSTimer obj)
		{
			OnFrame();
		}
#endif

		/// <summary>
		/// Precalculates the frame values.
		/// </summary>
		private void PrebuildFrames()
		{
			var frames = (FrameRate * _duration) / MillisecondsPerSecond;

			_numberOfFrames = (int)Math.Max(
				Math.Min(frames, MaxNumberOfFrames),
				MinNumberOfFrames);

			PrebuildFrames(_numberOfFrames);
		}

		protected abstract void PrebuildFrames(long numberOfFrames);

		/// <summary>
		/// When a frame is free update the value
		/// </summary>
		private void OnFrame()
		{
			if (_isDisposed || !IsRunning)
			{
				return;
			}
#if __IOS__
			var currentTime = _displayLink.Timestamp; // current time in seconds
#else
			var currentTime = NSProcessInfo.ProcessInfo.SystemUptime;
#endif

			if (_startTime == 0)
			{   // if start time is not set, set it
				_startTime = currentTime - (CurrentPlayTime / MillisecondsPerSecond); // Remove current play time as an offset (lie: tell the animation it started x seconds ago)
			}

			var delta = ((currentTime - _startTime) * MillisecondsPerSecond) - StartDelay; // Remove the start Delay (the delta may be negative)

			if (delta >= _duration)
			{//animation is done

				SendUpdate(GetFinalUpdate());//Set the final value 

				if (AnimationEnd != null)
				{
					AnimationEnd(this, EventArgs.Empty); //Send an animation ended
				}

				Stop();
				return;
			}

			if (delta < 0)
			{   // Start Delay can cause this - wait till the start delay is spent before continuing
				return;
			}

			CurrentPlayTime = (long)delta; //Update play time

			var percent = delta / _duration;
			var frame = (int)(percent * _numberOfFrames); // get the appropriate frame

			SendUpdate(GetUpdate(frame)); // send an update
		}

		private void SendUpdate(object value)
		{
			AnimatedValue = value;
			Update?.Invoke(this, EventArgs.Empty);
		}

		protected abstract object GetUpdate(int frame);

		protected abstract object GetFinalUpdate();

		public void Resume()
		{
			Start();
		}

		public void Pause()
		{
			IsRunning = false;

			AnimationPause?.Invoke(this, EventArgs.Empty);
		}

		public void Cancel()
		{
			ReleaseDisplayLink();

			IsRunning = false;
			AnimationCancel?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Stop this instance.
		/// </summary>
		private void Stop()
		{
			ReleaseDisplayLink();

			IsRunning = false;
		}

		private void ReleaseDisplayLink()
		{
			if (_isAttachedToLooper)
			{
				//Detach the _displayLink to the MainLoop (uiThread).
#if __IOS__
				_displayLink?.RemoveFromRunLoop(NSRunLoop.Main, NSRunLoopMode.Default);//detaches from the UI thread
				_displayLink?.RemoveFromRunLoop(NSRunLoop.Main, NSRunLoopMode.UITracking);
				_displayLink = null;
#endif
				_isAttachedToLooper = false;
			}
		}

		public void SetDuration(long duration)
		{
			_duration = duration;
		}

		public void SetEasingFunction(IEasingFunction easingFunction)
		{
			_easingFunction = easingFunction;
		}

		public event EventHandler AnimationEnd;

		public event EventHandler AnimationPause;

		public event EventHandler AnimationCancel;

#pragma warning disable 67
		public event EventHandler AnimationFailed;
#pragma warning restore 67

		public event EventHandler Update;

		public long StartDelay { get; set; }

#if __IOS__
		public bool IsRunning
		{
			get { return !(_displayLink?.Paused ?? false); }
			private set
			{
				if (_displayLink != null)
				{
					_displayLink.Paused = !value;
				}
			}
		}
#else
		public bool IsRunning
		{
			get => _timer?.IsValid ?? false;
			private set
			{
				if (_timer != null)
				{
					if (value && !_timer.IsValid)
					{
						ScheduleTimer();
					}
					else
					{
						UnscheduleTimer();
					}
				}
			}
		}
#endif

		public long CurrentPlayTime { get; set; }

		public object AnimatedValue { get; private set; }

		public long Duration => _duration;

		public void Dispose()
		{
			_isDisposed = true;
			Update = null;
			AnimationEnd = null;
			AnimationCancel = null;

			Stop();
#if __IOS__
			_displayLink?.Dispose();
#else
			_timer?.Dispose();
#endif
		}
	}
}

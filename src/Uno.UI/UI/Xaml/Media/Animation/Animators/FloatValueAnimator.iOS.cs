using CoreAnimation;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Animates a float property using Xaml property setters.
	/// </summary>
	internal class FloatValueAnimator : IValueAnimator
	{
		private const double MillisecondsPerSecond = 1000d;
		private const long MaxNumberOfFrames = 10000;// 10,000 frames is enough 40kb of floats
		private const long MinNumberOfFrames = 1;// At least 1 frame please     

		private float _from;
		private float _to;
		private long _duration;

		private float[] _animatedValues;
		private const int FrameRate = 50;

		private long _numberOfFrames;
		private CADisplayLink _displayLink;
		private double _startTime = 0;

		private bool _isDisposed;
		private bool _isAttachedToLooper;

		private IEasingFunction _easingFunction = null;

		public FloatValueAnimator(float from, float to)
		{
			_to = to;
			_from = from;

			StartDelay = 0;
			CurrentPlayTime = 0;
		}

		public void Start()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(GetType().Name);
			}

			if (_animatedValues == null)
			{
				PrebuildFrames();//Preload as much of the animation as possible
			}

			IsRunning = true;	
			_startTime = 0;// Start time will be set if it is equal to zero

			if (!_isAttachedToLooper)
			{
				//http://www.bigspaceship.com/ios-animation-intervals/
				_displayLink = CADisplayLink.Create(OnFrame);//OnFrame is called when an animation frame is ready

				//Need to attach the _displayLink to the MainLoop (uiThread) so that the call back will be called on the UI thread
				//Default == normal UI updates
				_displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.Default);
				//UITracking == updates during scrolling
				_displayLink.AddToRunLoop(NSRunLoop.Main, NSRunLoopMode.UITracking); 
				_isAttachedToLooper = true;
			}
		}

		/// <summary>
		/// Precalculates the frame values.
		/// </summary>
		private void PrebuildFrames()
		{
			var frames = (FrameRate * _duration) / MillisecondsPerSecond;

			_numberOfFrames = (int)Math.Max(
				Math.Min(frames, MaxNumberOfFrames),
				MinNumberOfFrames);

			_animatedValues = new float[_numberOfFrames];

			var by = _to - _from; //how much to change the value
			var interpolation = by / _numberOfFrames; //step size

			for (int f = 0; f < _numberOfFrames; f++)
			{
				//Modifies the frame values of the animation depending on the easing function
				if (_easingFunction != null)
				{
					_animatedValues[f] = (float)_easingFunction.Ease(f, _from, _to, _numberOfFrames);//frame value
				}
				else
				{
					//Regular Linear Function
					_animatedValues[f] = _from + (interpolation * f);//frame value
				}
			}
		}

		/// <summary>
		/// When a frame is free update the value
		/// </summary>
		private void OnFrame()
		{
			if (_isDisposed || !IsRunning)
			{
				return;
			}

			var currentTime = _displayLink.Timestamp; // current time in seconds

			if (_startTime == 0)
			{   // if start time is not set, set it
				_startTime = currentTime - (CurrentPlayTime / MillisecondsPerSecond); // Remove current play time as an offset (lie: tell the animation it started x seconds ago)
			}

			var delta = ((currentTime - _startTime) * MillisecondsPerSecond) - StartDelay; // Remove the start Delay (the delta may be negative)

			if (delta >= _duration)
			{//animation is done

				SendUpdate(_to);//Set the final value 

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

			SendUpdate(_animatedValues[frame]); // send an update
		}

		private void SendUpdate(float value)
		{
			AnimatedValue = value;
			Update?.Invoke(this, EventArgs.Empty);
		}

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
				_displayLink?.RemoveFromRunLoop(NSRunLoop.Main, NSRunLoop.NSDefaultRunLoopMode);//detaches from the UI thread
				_displayLink?.RemoveFromRunLoop(NSRunLoop.Main, NSRunLoop.UITrackingRunLoopMode);
				_displayLink = null;
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

		public event EventHandler Update;

		public long StartDelay { get; set; }

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
			_displayLink?.Dispose();
		}
	}
}

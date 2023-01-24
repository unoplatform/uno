using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
{
	/// <summary>
	/// Copy of the Android ValueAnimator API.
	/// </summary>
	internal interface IValueAnimator : IDisposable
	{
		/// <summary>
		/// Occurs when a frame is ready.
		/// </summary>
		event EventHandler Update;

		/// <summary>
		/// Occurs when animation ends.
		/// </summary>
		event EventHandler AnimationEnd;

		/// <summary>
		/// Occurs when the animation is paused.
		/// </summary>
		event EventHandler AnimationPause;

		/// <summary>
		/// Occurs when the animation is cancelled.
		/// </summary>
		event EventHandler AnimationCancel;

		/// <summary>
		/// Occurs when the animation failed.
		/// </summary>
		event EventHandler AnimationFailed;

		/// <summary>
		/// Gets or sets the animated value. The setter is public because the Java version is public 
		/// </summary>
		/// <value>The animated value.</value>
		object AnimatedValue { get; }

		/// <summary>
		/// Gets or sets the current play time (in milliseconds).
		/// </summary>
		/// <value>The current play time.</value>
		long CurrentPlayTime { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is running.
		/// </summary>
		/// <value><c>true</c> if this instance is running; otherwise, <c>false</c>.</value>
		bool IsRunning { get; }

		/// <summary>
		/// Gets or sets the start delay (in milliseconds).
		/// </summary>
		/// <value>The start delay.</value>
		long StartDelay { get; set; }

		/// <summary>
		/// Gets the duration of the animation (in milliseconds)
		/// </summary>
		long Duration { get; }

		/// <summary>
		/// Start animating.
		/// </summary>
		void Start();

		/// <summary>
		/// Pause this instance.
		/// </summary>
		void Pause();

		/// <summary>
		/// Resume this animation.
		/// </summary>
		void Resume();

		/// <summary>
		/// Cancel this instance.
		/// </summary>
		void Cancel();

		/// <summary>
		/// Sets the duration (in milliseconds).
		/// </summary>
		/// <param name="duration">Duration.</param>
		void SetDuration(long duration);

		/// <summary>
		/// Sets the EasingFunction
		/// </summary>
		/// <param name="easingFunction"></param>
		void SetEasingFunction(IEasingFunction easingFunction);
	}
}

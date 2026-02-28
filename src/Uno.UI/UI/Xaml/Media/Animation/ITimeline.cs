using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media.Animation
{

	/// <summary>
	/// This interface maps to public methods on <see cref="Storyboard"/>, which are implemented internally in Uno on types that inherit
	/// from <see cref="Timeline"/> and called from <see cref="Storyboard"/>.
	/// </summary>
	internal interface ITimeline
	{
		void Begin();
		void Stop();
		void Resume();
		void Pause();
		void Seek(TimeSpan offset);
		void SeekAlignedToLastTick(TimeSpan offset);
		void SkipToFill();
		void Deactivate();
		void RegisterListener(ITimelineListener storyboard);
		void UnregisterListener(ITimelineListener storyboard);

		/// <summary>
		/// Begins the animation in reverse, playing from the end value back to the start value.
		/// Used by Storyboard-level AutoReverse to signal child animations to play in reverse.
		/// </summary>
		void BeginReversed();

		/// <summary>
		/// Skips to the fill state as if the animation had played in reverse.
		/// Sets the animated property to its starting value (the "reversed" end state).
		/// Used by Storyboard-level AutoReverse when SkipToFill is called.
		/// </summary>
		void SkipToFillReversed();
	}
}

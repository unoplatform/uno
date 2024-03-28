using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media.Animation
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
	}
}

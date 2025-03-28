namespace Windows.UI.Xaml.Media.Animation
{
	internal interface ITimelineListener
	{
		void ChildCompleted(Timeline timeline);
		void ChildFailed(Timeline timeline);
	}
}

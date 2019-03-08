namespace Windows.UI.Xaml.Controls
{
	public interface IAnimatedVisualSource
	{
		void Update(AnimatedVisualPlayer player);
		void Load();
		void Unload();
		void Play(bool looped);
		void Stop();
		void Pause();
		void Resume();
	}
}

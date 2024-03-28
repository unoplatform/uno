namespace Windows.UI.Xaml
{
	/// <summary>
	/// Defines constants that specify whether XAML controls play sounds.
	/// </summary>
	public enum ElementSoundPlayerState
	{
		/// <summary>
		/// The platform determines whether or not sounds are played.
		/// </summary>
		Auto,

		/// <summary>
		/// Sounds are never played on any platform.
		/// </summary>
		Off,

		/// <summary>
		/// Sounds are played on all platforms.
		/// </summary>
		On,
	}
}

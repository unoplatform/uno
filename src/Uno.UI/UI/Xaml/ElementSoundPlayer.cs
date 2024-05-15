using System;
using Uno;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Represents a player for XAML control sounds.
	/// </summary>
	partial class ElementSoundPlayer
	{
		/// <summary>
		/// Gets or sets the volume of the sounds played by the Play method.
		/// </summary>
		public static double Volume
		{
			get => ElementSoundPlayerService.Instance.Volume;
			set
			{
				if (value < 0.0 || value > 1.0)
				{
					throw new ArgumentOutOfRangeException(
						nameof(value),
						"Volume value must be between 0.0 and 1.0");
				}
				ElementSoundPlayerService.Instance.Volume = value;
			}
		}

		/// <summary>
		/// Gets or sets a value that specifies whether the system plays control sounds.
		/// </summary>
		public static ElementSoundPlayerState State
		{
			get => ElementSoundPlayerService.Instance.PlayerState;
			set => ElementSoundPlayerService.Instance.PlayerState = value;
		}

		/// <summary>
		/// Gets or sets a value that indicates whether spatial audio is on, off, or handled automatically.
		/// </summary>
		public static ElementSpatialAudioMode SpatialAudioMode
		{
			get => ElementSoundPlayerService.Instance.SpatialAudioMode;
			set => ElementSoundPlayerService.Instance.SpatialAudioMode = value;
		}

		[NotImplemented]
		public static void Play(ElementSoundKind sound) => ElementSoundPlayerService.Instance.Play(sound);

		internal static void RequestInteractionSoundForElement(ElementSoundKind soundToPlay, DependencyObject element) =>
			ElementSoundPlayerService.Instance.RequestInteractionSoundForElement(soundToPlay, element);

		internal static ElementSoundMode GetEffectiveSoundMode(DependencyObject element) =>
			ElementSoundPlayerService.Instance.GetEffectiveSoundMode(element);
	}
}

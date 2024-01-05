using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Content;
using Windows.Media.Playback;

namespace Uno.Media.Playback
{
	public class AudioPlayerBroadcastReceiver : BroadcastReceiver
	{
		private readonly MediaPlayer _player;

		public AudioPlayerBroadcastReceiver(MediaPlayer player)
		{
			_player = player;
		}
		public override void OnReceive(Context? context, Intent? intent)
		{
			if (Android.Media.AudioManager.ActionAudioBecomingNoisy.Equals(intent!.Action))
			{
				_player?.Pause();
			}
		}
	}
}

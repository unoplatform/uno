#nullable enable

using System;
using System.Threading.Tasks;
using System.Timers;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

using _MediaPlayer = Windows.Media.Playback.MediaPlayer; // alias to avoid same name root namespace from ios/macos

namespace Microsoft.UI.Xaml.Controls
{
	public partial class MediaTransportControls : Control
	{
		private _MediaPlayer? _mediaPlayer;
		private SerialDisposable _mediaPlayerSubscriptions = new();

		internal void SetMediaPlayer(_MediaPlayer mediaPlayer)
		{
			_mediaPlayerSubscriptions.Disposable = null;

			_mediaPlayer = mediaPlayer;

			if (IsLoaded)
			{
				BindMediaPlayer();
			}
		}

		internal void SetMediaPlayerElement(MediaPlayerElement mediaPlayerElement)
		{
			_mpe = mediaPlayerElement;
		}

		private void BindMediaPlayer(bool updateAllVisualAndPropertyStates = true)
		{
			if (_mediaPlayer is null)
			{
				return;
			}

			var disposables = new CompositeDisposable();
			_mediaPlayerSubscriptions.Disposable = disposables;

			//Bind(_mediaPlayer, x => x.MediaOpened += OnMediaOpened, x => x.MediaOpened -= OnMediaOpened);
			//Bind(_mediaPlayer, x => x.MediaFailed += OnMediaFailed, x => x.MediaFailed -= OnMediaFailed);
			Bind(_mediaPlayer, x => x.VolumeChanged += OnPlayerVolumeChanged, x => x.VolumeChanged -= OnPlayerVolumeChanged);
			//IFC_RETURN(spMediaPlayerExt->add_SourceChanged(
			//IFC_RETURN(spMediaPlayerExt->add_IsMutedChanged(
			Bind(_mediaPlayer.PlaybackSession, x => x.PositionChanged += OnPositionChanged, x => x.PositionChanged -= OnPositionChanged);
			//Bind(_mediaPlayer.PlaybackSession, x => x.DownloadProgressChanged += OnDownloadProgressChanged, x => x.PlaybackStateChanged -= OnDownloadProgressChanged);
			Bind(_mediaPlayer.PlaybackSession, x => x.PlaybackStateChanged += OnPlaybackStateChanged, x => x.PlaybackStateChanged -= OnPlaybackStateChanged);
			Bind(_mediaPlayer.PlaybackSession, x => x.NaturalDurationChanged += OnNaturalDurationChanged, x => x.NaturalDurationChanged -= OnNaturalDurationChanged);
			Bind(_mediaPlayer.PlaybackSession, x => x.BufferingProgressChanged += OnBufferingProgressChanged, x => x.BufferingProgressChanged -= OnBufferingProgressChanged);
			//IFC_RETURN(spPlayCommandBehaviour->add_IsEnabledChanged(
			//IFC_RETURN(spPauseCommandBehaviour->add_IsEnabledChanged(
			//IFC_RETURN(spNextCommandBehaviour->add_IsEnabledChanged(
			//IFC_RETURN(spPrevCommandBehaviour->add_IsEnabledChanged(
			//IFC_RETURN(spFastforwardCommandBehaviour->add_IsEnabledChanged(
			//IFC_RETURN(spRewindCommandBehaviour->add_IsEnabledChanged(
			//IFC_RETURN(spPositionBehaviour->add_IsEnabledChanged(
			//IFC_RETURN(spRateBehaviour->add_IsEnabledChanged(
			//IFC_RETURN(spAutoRepeatBehaviour->add_IsEnabledChanged(
			//IFC_RETURN(spBreakManager->add_BreakStarted(
			//IFC_RETURN(spBreakManager->add_BreakEnded(
			//IFC_RETURN(spBreakManager->add_BreakSkipped(
			//IFC_RETURN(spBreakPlaybackSession->add_PlaybackStateChanged(
			//IFC_RETURN(spBreakPlaybackSession->add_PositionChanged(
			//IFC_RETURN(spBreakPlaybackSession->add_DownloadProgressChanged(
			//IFC_RETURN(MediaPlayerExtension_AddIsLoopingEnabledChanged(

			if (updateAllVisualAndPropertyStates)
			{
				UpdateVisualState();
				UpdateMediaControlAllStates();
			}

			void Bind<T>(T? target, Action<T> addHandler, Action<T> removeHandler)
			{
				if (target is { })
				{
					addHandler(target);
					disposables.Add(() => removeHandler(target));
				}
			}
		}

		private void OnPlaybackStateChanged(MediaPlaybackSession sender, object args)
		{
			if (sender is { } session)
			{
				var currentState = (MediaPlaybackState)args;
				var previousIsPlaying = m_isPlaying;
				var previousIsBuffering = m_isBuffering;

				m_sourceLoaded = currentState
					is not MediaPlaybackState.None
					and not MediaPlaybackState.Opening;
				if (currentState != MediaPlaybackState.Buffering)
				{
					m_isPlaying = currentState == MediaPlaybackState.Playing;
				}

				m_isBuffering = currentState == MediaPlaybackState.Buffering;

#if !HAS_UNO
				// If playing state changed, toggle position timer
				// to avoid ticking if position is not updating
				if (previousIsPlaying != m_isPlaying)
				{
					if (m_isPlaying)
					{
						StartPositionUpdateTimer();
					}
					else
					{
						StopPositionUpdateTimer();
					}
				}
#endif
				if ((previousIsPlaying != m_isPlaying || previousIsBuffering != m_isBuffering) && !m_isInScrubMode)
				{
					if ((m_isPlaying && !m_isBuffering || m_shouldDismissControlPanel) /*&& !m_isthruScrubber*/)
					{
						m_shouldDismissControlPanel = true;
						HideControlPanel();
					}
					else
					{
						ShowControlPanel();
					}
				}
#if !HAS_UNO
				if (!m_isPlaying)
				{
					IFC(ResetTrickMode());
				}
				// Timing issues still Natural duration values zero even after source loaded.
				if (m_sourceLoaded && m_naturalDuration.TimeSpan.Duration == 0)
				{
					wf::TimeSpan value;
					IFC(spPlaybackSession->get_NaturalDuration(&value));
					m_naturalDuration.TimeSpan = value;
				}
#endif
				UpdateVisualState();
			}
		}

		private void OnBufferingProgressChanged(MediaPlaybackSession sender, object args)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				if (m_tpDownloadProgressIndicator is not null && args is double value)
				{
					m_tpDownloadProgressIndicator.Value = value;
				}
			});
		}

		private void OnNaturalDurationChanged(MediaPlaybackSession sender, object args)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var duration = args as TimeSpan? ?? TimeSpan.Zero;

				if (m_tpMediaPositionSlider is not null)
				{
					m_tpMediaPositionSlider.Minimum = 0;
					m_tpMediaPositionSlider.Maximum = duration.TotalSeconds;
				}

				if (_mediaPlayer is not null
					and { PlaybackSession.PlaybackState: not MediaPlaybackState.Playing and not MediaPlaybackState.Paused })
				{
					m_tpTimeRemainingElement.Maybe<TextBlock>(p => p.Text = FormatTime(duration));
				}
			});
		}

		private void OnPositionChanged(MediaPlaybackSession sender, object args)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var elapsed = args as TimeSpan? ?? TimeSpan.Zero;
				m_tpTimeElapsedElement.Maybe<TextBlock>(p => p.Text = FormatTime(elapsed));
				if (
#if __ANDROID__ || __APPLE_UIKIT__
					!m_isInScrubMode &&
#endif
					m_tpMediaPositionSlider is not null)
				{
					m_positionUpdateUIOnly = true;
					m_tpMediaPositionSlider.Value = elapsed.TotalSeconds;
					m_positionUpdateUIOnly = false;
				}
				if (_mediaPlayer is not null)
				{
					var remaining = _mediaPlayer.PlaybackSession.NaturalDuration - elapsed;
					m_tpTimeRemainingElement.Maybe<TextBlock>(p => p.Text = FormatTime(remaining));
					_ = UpdateTimePosition(elapsed);
				}
			});
		}

		private void OnPositionSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			double newSliderValue = 0.0;
			TimeSpan newMediaPosition;
			//bool isMediaClosed = false;

			if (m_transportControlsEnabled && _mediaPlayer is { })
			{
				// If slider was updated internally in response to Position DP change,
				// do not update the DP again.
				if (m_positionUpdateUIOnly)
				{
					return;
				}

				//if (MTCParent.MediaPlayerElement == m_parentType)
				//{
				//	isMediaClosed = IsMediaStateClosedFromMPE();
				//}

				// If user tried to set the slider while in Closed state or for live content,
				// do not update the DP, but refresh Position UI (snap slider back to 0 position, etc).
				//if (isMediaClosed || IsLiveContent())
				//{
				//	UpdatePositionUI();
				//	return;
				//}

				newSliderValue = e.NewValue;
				//newMediaPosition = TimeSpan.FromTicks(
				//	(newSliderValue - m_positionSliderMinimum) / (m_positionSliderMaximum - m_positionSliderMinimum) *
				//	_mediaPlayer.PlaybackSession.NaturalDuration.Ticks
				//);
				newMediaPosition = TimeSpan.FromSeconds(newSliderValue);

				EnterScrubbingMode();
				SetPosition(newMediaPosition);
				//FireThumbnailEvent();
				m_isthruScrubber = true;
			}
		}

		private void OnPositionSliderPressed(object sender, PointerRoutedEventArgs e)
		{
			//ShowHideThumbnail(true);
			//EnterScrubbingMode();

			if (m_transportControlsEnabled && _mediaPlayer is { })
			{
				_wasPlaying = _mediaPlayer.PlaybackSession.IsPlaying;
				EnterScrubbingMode();

				_mediaPlayer.Pause();
			}
		}

		private void OnPositionSliderReleased(object sender, PointerRoutedEventArgs e)
		{
			//ShowHideThumbnail(false);
			//ExitScrubbingMode();

			if (m_tpMediaPositionSlider is null || double.IsNaN(m_tpMediaPositionSlider.Value))
			{
				return;
			}

			if (m_transportControlsEnabled && _mediaPlayer is { })
			{
				if (m_isPlaying || m_isBuffering)
				{
					EnterScrubbingMode();
					_mediaPlayer.Pause();
				}

				SetPosition(TimeSpan.FromSeconds(m_tpMediaPositionSlider.Value));
				ExitScrubbingMode();

				if (_wasPlaying)
				{
					_mediaPlayer.Play();
				}
			}
		}

		public async Task UpdateTimePosition(TimeSpan elapsed)
		{
			if (_mediaPlayer is not null
				&& _mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing
				&& !_mediaPlayer.PlaybackSession.IsUpdateTimePosition
				&& elapsed != TimeSpan.Zero
				&& _mediaPlayer.PlaybackSession.UpdateTimePositionRate != 0
				&& _mediaPlayer.PlaybackSession.UpdateTimePositionRate != 1)
			{
				_mediaPlayer.PlaybackSession.IsUpdateTimePosition = true;
				elapsed += TimeSpan.FromSeconds(_mediaPlayer.PlaybackSession.UpdateTimePositionRate);
				_mediaPlayer.PlaybackSession.Position = elapsed;
				await Task.Delay(250);
				_mediaPlayer.PlaybackSession.IsUpdateTimePosition = false;
			}
		}

		private void ResetProgressSlider()
		{
			if (m_tpTimeElapsedElement is TextBlock timeElapsedElement)
			{
				timeElapsedElement.Text = FormatTime(TimeSpan.Zero);
			}
			if (m_tpMediaPositionSlider is not null)
			{
				m_positionUpdateUIOnly = true;
				m_tpMediaPositionSlider.Value = 0;
				m_positionUpdateUIOnly = false;
			}
			if (_mediaPlayer is not null)
			{
				if (m_tpTimeRemainingElement is TextBlock timeRemainingElement)
				{
					timeRemainingElement.Text = FormatTime(_mediaPlayer.PlaybackSession.NaturalDuration);
				}
				_mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
				_mediaPlayer.PlaybackSession.PositionFromPlayer = TimeSpan.Zero;
			}
		}

		private string FormatTime(TimeSpan time)
			=> $"{time.TotalHours:0}:{time.Minutes:00}:{time.Seconds:00}";

		private void PlayPause(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer is not null)
			{
				var _isRFRequested = _isRewindForewardRequested;

				ResetRewindForewardRequested();
				if (_mediaPlayer.PlaybackSession.IsPlaying)
				{
					_mediaPlayer.Pause();
					//if RewindForewardRequested, keep playing
					if (_isRFRequested)
					{
						_mediaPlayer.Play();
					}
				}
				else
				{
					_mediaPlayer.Play();
				}
			}
		}

		private void Stop(object sender, RoutedEventArgs e)
		{
			m_isInScrubMode = false;

			ResetRewindForewardRequested();
			ResetProgressSlider();
			_mediaPlayer?.Pause();
			_mediaPlayer?.Stop();
		}

		private void SkipBackward(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer is null)
			{
				return;
			}

			_mediaPlayer.PlaybackSession.Position -= TimeSpan.FromSeconds(10);
		}

		private void SkipForward(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer is null)
			{
				return;
			}

			_mediaPlayer.PlaybackSession.Position += TimeSpan.FromSeconds(30);
		}

		private void ForwardButton(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer is null)
			{
				return;
			}
			_isRewindForewardRequested = true;
			if (_isVolumeRewindRequestedAndAudioIsPlaying != null && _mediaPlayer.IsMuted)
			{
				_isVolumeRewindRequestedAndAudioIsPlaying = null;
			}

#if __SKIA__ || __WASM__
			//For wasm use the PlaybackRate instead the UpdateTimePositionRate
			_mediaPlayer.PlaybackRate =
									_mediaPlayer.PlaybackRate <= 1 ? 2 : /*To stop the Rewind*/
									_mediaPlayer.PlaybackRate > 8 ? _mediaPlayer.PlaybackRate : /*Set limit to x4 Forward*/
									_mediaPlayer.PlaybackRate * 2; /*Keep current Forward*/
			_mediaPlayer.PlaybackSession.UpdateTimePositionRate = 1;
#else
			_mediaPlayer.PlaybackSession.UpdateTimePositionRate =
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate < 1 ? 1 : /*To stop the Rewind*/
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate < 4 ? _mediaPlayer.PlaybackSession.UpdateTimePositionRate * 2 : /*Set limit to x4 Forward*/
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate; /*Keep current Forward*/
#endif
		}

		private void ResetRewindForewardRequested()
		{
			if (_mediaPlayer is null)
			{
				return;
			}
			_mediaPlayer.PlaybackSession.UpdateTimePositionRate = 1;
			_mediaPlayer.PlaybackRate = 1;

			if (_isVolumeRewindRequestedAndAudioIsPlaying != null)
			{
				_mediaPlayer.Volume = (double)_isVolumeRewindRequestedAndAudioIsPlaying;
			}
			_isVolumeRewindRequestedAndAudioIsPlaying = null;
			_isRewindForewardRequested = false;
		}

		private void RewindButton(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer is null)
			{
				return;
			}

			_isRewindForewardRequested = true;
			if (_isVolumeRewindRequestedAndAudioIsPlaying == null && _mediaPlayer.Volume != 0)
			{
				_isVolumeRewindRequestedAndAudioIsPlaying = _mediaPlayer.Volume;
				_mediaPlayer.Volume = 0;
			}
#if __SKIA__ || __WASM__
			if (_mediaPlayer.PlaybackRate != 1)
			{
				_mediaPlayer.PlaybackRate = 1;
			}
#endif
			_mediaPlayer.PlaybackSession.UpdateTimePositionRate =
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate >= 0 ? -1 : /*To start the Rewind*/
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate < -8 ? _mediaPlayer.PlaybackSession.UpdateTimePositionRate : /*Set limit to x4 Rewind*/
				_mediaPlayer.PlaybackSession.UpdateTimePositionRate * 2;
		}

		private void OnVolumeChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (_mediaPlayer is not null)
			{
				_mediaPlayer.Volume = e.NewValue / 100.0;
			}

			UpdateVolumeMuteStates();
			ResetControlsVisibilityTimer();
		}

		private void OnPlayerVolumeChanged(_MediaPlayer sender, object e)
		{
			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				var percent = Math.Round(sender.Volume * 100);
				if (m_tpTHVolumeSlider != null && Math.Abs(m_tpTHVolumeSlider.Value - percent) > 0.1)
				{
					m_tpTHVolumeSlider.Value = percent;
				}
			});
		}

		private void ToggleMute(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer is not null)
			{
				_mediaPlayer.IsMuted = !_mediaPlayer.IsMuted;
			}

			UpdateVolumeMuteStates(isExplicitMuteToggle: true);
			ResetControlsVisibilityTimer();
		}

		private void EnterScrubbingMode()
		{
			if (m_transportControlsEnabled && _mediaPlayer is { })
			{
				if (
					//!m_isAudioOnly &&
					//!IsLiveContent() &&
					!m_isInScrubMode)
				{
					m_currentPlaybackRate = _mediaPlayer.PlaybackSession.PlaybackRate;
					_mediaPlayer.PlaybackSession.PlaybackRate = 0;
					//EnableValueChangedEventThrottlingOnSliderAutomation(false);
					m_isInScrubMode = true;
				}
			}
		}

		private void ExitScrubbingMode()
		{
			if (m_transportControlsEnabled && _mediaPlayer is { })
			{
				if (
					//!m_isAudioOnly &&
					//!IsLiveContent() &&
					m_isInScrubMode)
				{
					_mediaPlayer.PlaybackSession.PlaybackRate = m_currentPlaybackRate;
					//EnableValueChangedEventThrottlingOnSliderAutomation(true);
					m_isInScrubMode = false;
				}
			}
		}
	}
}

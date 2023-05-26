using System;
using System.Timers;
using Uno.UI.Converters;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.UI.Xaml.Controls.MediaPlayer.Internal;
using System.Drawing;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Automation;
using DirectUI;
using System.Runtime.Intrinsics.Arm;
using Windows.Media.Casting;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using Windows.UI.Input;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#elif __ANDROID__
using Uno.UI;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls
	{
		private static class TemplateParts
		{
			public const string TimeElapsedElement = nameof(TimeElapsedElement); // TextBlock
			public const string TimeRemainingElement = nameof(TimeRemainingElement); // TextBlock
			public const string ProgressSlider = nameof(ProgressSlider); // Slider
			public const string PlayPauseButton = nameof(PlayPauseButton); // AppBarButton
			public const string PlayPauseButtonOnLeft = nameof(PlayPauseButtonOnLeft); // AppBarButton
			public const string FullWindowButton = nameof(FullWindowButton); // AppBarButton
			public const string ZoomButton = nameof(ZoomButton); // AppBarButton
			public const string ErrorTextBlock = nameof(ErrorTextBlock); // TextBlock
			public const string MediaControlsCommandBar = nameof(MediaControlsCommandBar); // CommandBar
			public const string VolumeFlyout = nameof(VolumeFlyout); // Flyout
			public const string HorizontalVolumeSlider = nameof(HorizontalVolumeSlider); // ?
			public const string VerticalVolumeSlider = nameof(VerticalVolumeSlider); // ?
			public const string VolumeSlider = nameof(VolumeSlider); // Slider
			public const string AudioSelectionButton = nameof(AudioSelectionButton); // ?
			public const string AudioTracksSelectionButton = nameof(AudioTracksSelectionButton); // AppBarButton
			public const string AvailableAudioTracksMenuFlyout = nameof(AvailableAudioTracksMenuFlyout); // ?
			public const string AvailableAudioTracksMenuFlyoutTarget = nameof(AvailableAudioTracksMenuFlyoutTarget); // ?
			public const string CCSelectionButton = nameof(CCSelectionButton); // AppBarButton
			public const string PlaybackRateButton = nameof(PlaybackRateButton); // AppBarButton
			public const string VolumeButton = nameof(VolumeButton); // ?
			public const string AudioMuteButton = nameof(AudioMuteButton); // AppBarButton
			public const string VolumeMuteButton = nameof(VolumeMuteButton); // AppBarButton
			public const string BufferingProgressBar = nameof(BufferingProgressBar); // ProgressBar
			public const string FastForwardButton = nameof(FastForwardButton); // AppBarButton
			public const string RewindButton = nameof(RewindButton); // AppBarButton
			public const string StopButton = nameof(StopButton); // AppBarButton
			public const string CastButton = nameof(CastButton); // AppBarButton
			public const string SkipForwardButton = nameof(SkipForwardButton); // AppBarButton
			public const string SkipBackwardButton = nameof(SkipBackwardButton); // AppBarButton
			public const string NextTrackButton = nameof(NextTrackButton); // AppBarButton
			public const string PreviousTrackButton = nameof(PreviousTrackButton); // AppBarButton
			public const string RepeatButton = nameof(RepeatButton); // AppBarToggleButton
			public const string CompactOverlayButton = nameof(CompactOverlayButton); // AppBarButton
			public const string LeftSeparator = nameof(LeftSeparator); // AppBarSeparator
			public const string RightSeparator = nameof(RightSeparator); // AppBarSeparator

			// Used by uno only:
			public const string RootGrid = nameof(RootGrid);
			public const string MediaTransportControls_Timeline_Border = nameof(MediaTransportControls_Timeline_Border);

			// MediaControlsCommandBar template children
			public const string MoreButton = nameof(MoreButton); // Button
			// ProgressSlider template children
			public const string DownloadProgressIndicator = nameof(DownloadProgressIndicator); // ProgressBar
			public const string HorizontalThumb = nameof(HorizontalThumb); // Thumb
		}

		private static class UIAKeys
		{
			// MediaElement Transport Controls: UI Automation Name / Tooltip text
			public const string UIA_MEDIA_PLAY = nameof(UIA_MEDIA_PLAY); // "Play"
			public const string UIA_MEDIA_PAUSE = nameof(UIA_MEDIA_PAUSE); // "Pause"
			public const string UIA_MEDIA_TIME_ELAPSED = nameof(UIA_MEDIA_TIME_ELAPSED); // "Time elapsed"
			public const string UIA_MEDIA_TIME_REMAINING = nameof(UIA_MEDIA_TIME_REMAINING); // "Time remaining"
			public const string UIA_MEDIA_DOWNLOAD_PROGRESS = nameof(UIA_MEDIA_DOWNLOAD_PROGRESS); // "Download Progress"
			public const string UIA_MEDIA_BUFFERING_PROGRESS = nameof(UIA_MEDIA_BUFFERING_PROGRESS); // "Buffering Progress"
			public const string UIA_MEDIA_SEEK = nameof(UIA_MEDIA_SEEK); // "Seek"
			public const string UIA_MEDIA_MUTE = nameof(UIA_MEDIA_MUTE); // "Mute"
			public const string UIA_MEDIA_UNMUTE = nameof(UIA_MEDIA_UNMUTE); // "Unmute"
			public const string UIA_MEDIA_VOLUME = nameof(UIA_MEDIA_VOLUME); // "Volume"
			public const string UIA_MEDIA_ERROR = nameof(UIA_MEDIA_ERROR); // "Error"
			public const string TEXT_MEDIA_AUDIO_TRACK_UNTITLED = nameof(TEXT_MEDIA_AUDIO_TRACK_UNTITLED); // "untitled"
			public const string TEXT_MEDIA_AUDIO_TRACK_SELECTED = nameof(TEXT_MEDIA_AUDIO_TRACK_SELECTED); // "(On)" - Appended to name of currently selected audio track
			public const string TEXT_MEDIA_AUDIO_TRACK_SEPARATOR = nameof(TEXT_MEDIA_AUDIO_TRACK_SEPARATOR); // " - " - Used to separate pieces of metadata in audio track name
			public const string UIA_MEDIA_FULLSCREEN = nameof(UIA_MEDIA_FULLSCREEN); // "Full Screen"
			public const string UIA_MEDIA_EXIT_FULLSCREEN = nameof(UIA_MEDIA_EXIT_FULLSCREEN); // "Exit Full Screen"
			public const string UIA_MEDIA_AUDIO_SELECTION = nameof(UIA_MEDIA_AUDIO_SELECTION); // "Show audio selection menu"
			public const string UIA_MEDIA_CC_SELECTION = nameof(UIA_MEDIA_CC_SELECTION); // "Show closed caption menu"
			public const string TEXT_MEDIA_CC_OFF = nameof(TEXT_MEDIA_CC_OFF); // "Off"
			public const string UIA_MEDIA_PLAYBACKRATE = nameof(UIA_MEDIA_PLAYBACKRATE); // "Show playback rate list"
			public const string UIA_MEDIA_FASTFORWARD = nameof(UIA_MEDIA_FASTFORWARD); // "Fast forward"
			public const string UIA_MEDIA_REWIND = nameof(UIA_MEDIA_REWIND); // "Rewind"
			public const string UIA_MEDIA_STOP = nameof(UIA_MEDIA_STOP); // "Stop"
			public const string UIA_MEDIA_CAST = nameof(UIA_MEDIA_CAST); // "Cast to Device"
			public const string UIA_MEDIA_ASPECTRATIO = nameof(UIA_MEDIA_ASPECTRATIO); // "Aspect Ratio"
			public const string UIA_MEDIA_SKIPBACKWARD = nameof(UIA_MEDIA_SKIPBACKWARD); // "Skip Backward"
			public const string UIA_MEDIA_SKIPFORWARD = nameof(UIA_MEDIA_SKIPFORWARD); // "Skip Forward"
			public const string UIA_MEDIA_NEXTRACK = nameof(UIA_MEDIA_NEXTRACK); // "Next Track"
			public const string UIA_MEDIA_PREVIOUSTRACK = nameof(UIA_MEDIA_PREVIOUSTRACK); // "Previous Track"
			public const string UIA_MEDIA_FASTFORWARD_2X = nameof(UIA_MEDIA_FASTFORWARD_2X); // "Fast forward in 2X"
			public const string UIA_MEDIA_FASTFORWARD_4X = nameof(UIA_MEDIA_FASTFORWARD_4X); // "Fast forward in 4X"
			public const string UIA_MEDIA_FASTFORWARD_8X = nameof(UIA_MEDIA_FASTFORWARD_8X); // "Fast forward in 8X"
			public const string UIA_MEDIA_FASTFORWARD_16X = nameof(UIA_MEDIA_FASTFORWARD_16X); // "Fast forward in 16X"
			public const string UIA_MEDIA_REWIND_2X = nameof(UIA_MEDIA_REWIND_2X); // "Rewind in 2X"
			public const string UIA_MEDIA_REWIND_4X = nameof(UIA_MEDIA_REWIND_4X); // "Rewind in 4X"
			public const string UIA_MEDIA_REWIND_8X = nameof(UIA_MEDIA_REWIND_8X); // "Rewind in 8X"
			public const string UIA_MEDIA_REWIND_16X = nameof(UIA_MEDIA_REWIND_16X); // "Rewind in 16X"
			public const string UIA_MEDIA_REPEAT_NONE = nameof(UIA_MEDIA_REPEAT_NONE); // "Repeat None"
			public const string UIA_MEDIA_REPEAT_ONE = nameof(UIA_MEDIA_REPEAT_ONE); // "Repeat One"
			public const string UIA_MEDIA_REPEAT_ALL = nameof(UIA_MEDIA_REPEAT_ALL); // "Repeat All"
			public const string UIA_MEDIA_MINIVIEW = nameof(UIA_MEDIA_MINIVIEW); // "Enter MiniView"
			public const string UIA_MEDIA_EXIT_MINIVIEW = nameof(UIA_MEDIA_EXIT_MINIVIEW); // "Exit MiniView"
			public const string UIA_LESS_BUTTON = nameof(UIA_LESS_BUTTON); // "Less app bar"
			public const string UIA_AP_APPBAR_BUTTON = nameof(UIA_AP_APPBAR_BUTTON); // "app bar button"
			public const string UIA_AP_APPBAR_TOGGLEBUTTON = nameof(UIA_AP_APPBAR_TOGGLEBUTTON); // "app bar toggle button"
			public const string UIA_AP_MEDIAPLAYERELEMENT = nameof(UIA_AP_MEDIAPLAYERELEMENT); // "media player" - Localized control type for the video output of MediaPlayerElement (and MediaElement)
		}

		private static class VisualState
		{
			public class ControlPanelVisibilityStates
			{
				public const string ControlPanelFadeIn = nameof(ControlPanelFadeIn);
				public const string ControlPanelFadeOut = nameof(ControlPanelFadeOut);
			}
			public class MediaStates
			{
				public const string Normal = nameof(Normal);
				public const string Buffering = nameof(Buffering);
				public const string Loading = nameof(Loading);
				public const string Error = nameof(Error);
				public const string Disabled = nameof(Disabled);
			}
			public class AudioSelectionAvailablityStates
			{
				public const string AudioSelectionAvailable = nameof(AudioSelectionAvailable);
				public const string AudioSelectionUnavailable = nameof(AudioSelectionUnavailable);
			}
			public class CCSelectionAvailablityStates
			{
				public const string CCSelectionAvailable = nameof(CCSelectionAvailable);
				public const string CCSelectionUnavailable = nameof(CCSelectionUnavailable);
			}
			public class FocusStates
			{
				public const string Focused = nameof(Focused);
				public const string Unfocused = nameof(Unfocused);
				public const string PointerFocused = nameof(PointerFocused);
			}
			public class MediaTransportControlMode
			{
				public const string NormalMode = nameof(NormalMode);
				public const string CompactMode = nameof(CompactMode);
			}
			public class PlayPauseStates
			{
				public const string PlayState = nameof(PlayState);
				public const string PauseState = nameof(PauseState);
			}
			public class VolumeMuteStates
			{
				public const string VolumeState = nameof(VolumeState);
				public const string MuteState = nameof(MuteState);
			}
			public class FullWindowStates
			{
				public const string NonFullWindowState = nameof(NonFullWindowState);
				public const string FullWindowState = nameof(FullWindowState);
			}
			public class RepeatStates
			{
				public const string RepeatNoneState = nameof(RepeatNoneState);
				public const string RepeatOneState = nameof(RepeatOneState);
				public const string RepeatAllState = nameof(RepeatAllState);
			}
		}
	}

	[TemplatePart(Name = "RootGrid", Type = typeof(Grid))]
	[TemplatePart(Name = "PlayPauseButton", Type = typeof(Button))]
	[TemplatePart(Name = "PlayPauseButtonOnLeft", Type = typeof(Button))]
	[TemplatePart(Name = "VolumeMuteButton", Type = typeof(Button))]
	[TemplatePart(Name = "AudioMuteButton", Type = typeof(Button))]
	[TemplatePart(Name = "VolumeSlider", Type = typeof(Slider))]
	[TemplatePart(Name = "FullWindowButton", Type = typeof(Button))]
	[TemplatePart(Name = "CastButton", Type = typeof(Button))]
	[TemplatePart(Name = "ZoomButton", Type = typeof(Button))]
	[TemplatePart(Name = "PlaybackRateButton", Type = typeof(Button))]
	[TemplatePart(Name = "PlaybackRateButton", Type = typeof(Button))]
	[TemplatePart(Name = "SkipForwardButton", Type = typeof(Button))]
	[TemplatePart(Name = "NextTrackButton", Type = typeof(Button))]
	[TemplatePart(Name = "FastForwardButton", Type = typeof(Button))]
	[TemplatePart(Name = "RewindButton", Type = typeof(Button))]
	[TemplatePart(Name = "PreviousTrackButton", Type = typeof(Button))]
	[TemplatePart(Name = "SkipBackwardButton", Type = typeof(Button))]
	[TemplatePart(Name = "StopButton", Type = typeof(Button))]
	[TemplatePart(Name = "AudioTracksSelectionButton", Type = typeof(Button))]
	[TemplatePart(Name = "CCSelectionButton", Type = typeof(Button))]
	[TemplatePart(Name = "TimeElapsedElement", Type = typeof(TextBlock))]
	[TemplatePart(Name = "TimeRemainingElement", Type = typeof(TextBlock))]
	[TemplatePart(Name = "ProgressSlider", Type = typeof(Slider))]
	[TemplatePart(Name = "BufferingProgressBar", Type = typeof(ProgressBar))]
	[TemplatePart(Name = "DownloadProgressIndicator", Type = typeof(ProgressBar))]
	[TemplatePart(Name = "ControlPanelGrid", Type = typeof(Grid))]
	[TemplatePart(Name = ControlPanelBorderName, Type = typeof(Border))]
	public partial class MediaTransportControls : Control
	{
		#region Template Parts
		//
		// References to control parts we need to manipulate
		//
		private Grid _rootGrid;
		private Border _timelineContainer;
		private ProgressBar _downloadProgressIndicator;
		private Grid _controlPanelGrid;
		private Border _controlPanelBorder;

		//todo@xy: wip: restore CS0169
#pragma warning disable CS0169 // The private field 'class member' is never used
		// Reference to the control panel grid
		private Grid m_tpControlPanelGrid;

		// Reference to the media position slider.
		private Slider m_tpMediaPositionSlider;

		// Reference to the horizontal volume slider (audio-only mode audio slider).
		private Slider m_tpHorizontalVolumeSlider;

		// Reference to the vertical volume slider (video-mode audio slider).
		private Slider m_tpVerticalVolumeSlider;

		// Reference to the Threshold Volume slider (video-mode & audio-mode slider).
		private Slider m_tpTHVolumeSlider;

		// Refererence to currently active volume slider
		private Slider m_tpActiveVolumeSlider;

		// Reference to download progress indicator, which is a part in the MediaSlider template
		private ProgressBar m_tpDownloadProgressIndicator;

		// Reference to the buffering indeterminate progress bar
		private ProgressBar m_tpBufferingProgressBar;

		// Reference to the PlayPause button used in Blue and Threshold
		private ButtonBase m_tpPlayPauseButton;

		// Reference to the PlayPause button used only in Threshold
		private ButtonBase m_tpTHLeftSidePlayPauseButton;

		// Reference to the Audio Selection button
		private Button m_tpAudioSelectionButton;

		// Reference to the Audio Selection button for Threshold
		private Button m_tpTHAudioTrackSelectionButton;

		// Reference to the Available Audiotracks flyout
		private MenuFlyout m_tpAvailableAudioTracksMenuFlyout;

		// Reference to the Available Audiotracks flyout target
		private FrameworkElement m_tpAvailableAudioTracksMenuFlyoutTarget;

		// Reference to the Close Captioning Selection button
		private Button m_tpCCSelectionButton;

		// Reference to the Available Close Captioning tracks flyout
		private MenuFlyout m_tpAvailableCCTracksMenuFlyout;

		// Reference to the Play Rate Selection button
		private Button m_tpPlaybackRateButton;

		// Reference to the Available Play Rate List flyout
		private MenuFlyout m_tpAvailablePlaybackRateMenuFlyout;

		// Reference to the Video volume button
		private ToggleButton m_tpVideoVolumeButton;

		// Reference to the Audio-mute button for Blue and Mute button for Video/Audio in Threshold
		private ButtonBase m_tpMuteButton;

		// Reference to the Threshold volume button
		private ButtonBase m_tpTHVolumeButton;

		// Reference to the Full Window button
		private ButtonBase m_tpFullWindowButton;

		// Reference to the Zoom button
		private ButtonBase m_tpZoomButton;

		// Reference to currently active volume button
		private ToggleButton m_tpActiveVolumeButton;

		// Reference to Time Elapsed / -30 sec seek button or Time Elapsed TextBlock
		private FrameworkElement m_tpTimeElapsedElement;

		// Reference to Time Remaining / +30 sec seek button or Time Remaining TextBlock
		private FrameworkElement m_tpTimeRemainingElement;

		// Reference to the fast forward button
		private Button m_tpFastForwardButton;

		// Reference to the rewind button
		private Button m_tpFastRewindButton;

		// Reference to the stop button
		private Button m_tpStopButton;

		// Reference to the cast button
		private Button m_tpCastButton;

		// Reference to the Skip Forward button
		private Button m_tpSkipForwardButton;

		// Reference to the Skip Backward button
		private Button m_tpSkipBackwardButton;

		// Reference to the Next Track button
		private Button m_tpNextTrackButton;

		// Reference to the Previous Track button
		private Button m_tpPreviousTrackButton;

		// Reference to currently Repeat button
		private ToggleButton m_tpRepeatButton;

		// Reference to the Mini View button
		private Button m_tpCompactOverlayButton;

		// Reference to the Left AppBarSeparator
		private AppBarSeparator m_tpLeftAppBarSeparator;

		// Reference to the Right AppBarSeparator
		private AppBarSeparator m_tpRightAppBarSeparator;

		// Reference to the Image thumbnail preview
		private Image m_tpThumbnailImage;

		// Reference to the Time Elapsed preview
		private TextBlock m_tpTimeElapsedPreview;

		// Reference to Error TextBlock
		private TextBlock m_tpErrorTextBlock;

		// Dispatcher timer responsible for updating clock and position slider
		private DispatcherTimer m_tpPositionUpdateTimer;

		// Dispatcher timer responsible for hiding vertical volume host border
		private DispatcherTimer m_tpHideVerticalVolumeTimer;

		// Dispatcher timer responsible for hiding UI control panel
		private DispatcherTimer m_tpHideControlPanelTimer;

		// Dispatcher timer to detect the pointer move ends.
		private DispatcherTimer m_tpPointerMoveEndTimer;

		// Reference to the Visibility Border element.
		private Border m_tpControlPanelVisibilityBorder;

		// Reference to the CommandBar Element.
		private CommandBar m_tpCommandBar;

		// Reference to the CommandBar Element.
		private FlyoutBase m_tpVolumeFlyout;

		// Reference to the VisualStateGroup
		private VisualStateGroup m_tpVisibilityStatesGroup;
#pragma warning restore CS0169 // The private field 'class member' is never used

		#endregion

		private MediaPlayerElement _mpe;
		private Timer _controlsVisibilityTimer;
		private DispatcherTimer _controlsVisibilityTimer;
		private CompositeDisposable _loadedSubscriptions;

		private bool _wasPlaying;
		private bool _isInteractive;
		private bool _isShowingControls = true;

		public MediaTransportControls() : base()
		{
			_controlsVisibilityTimer = new()
			{
				Interval = TimeSpan.FromSeconds(3)
			};

			_controlsVisibilityTimer.Tick += ControlsVisibilityTimerElapsed;

			DefaultStyleKey = typeof(MediaTransportControls);
			Loaded += MediaTransportControls_Loaded;
			Unloaded += MediaTransportControls_Unloaded;
		}

		private void MediaTransportControls_Unloaded(object sender, RoutedEventArgs e)
		{
			_rootGrid = this.GetTemplateChild(RootGridName) as Grid;
			if (_rootGrid != null)
			{
				_rootGrid.Tapped -= OnRootGridTapped;
			}

			_controlPanelGrid = this.GetTemplateChild(ControlPanelGridName) as Grid;
			if (_controlPanelGrid != null)
			{
				_controlPanelGrid.Tapped -= OnPaneGridTapped;
			}
		}

		private void MediaTransportControls_Loaded(object sender, RoutedEventArgs e)
		{
			_rootGrid = this.GetTemplateChild(RootGridName) as Grid;
			if (_rootGrid != null)
			{
				_rootGrid.Tapped += OnRootGridTapped;
			}

			_controlPanelGrid = this.GetTemplateChild(ControlPanelGridName) as Grid;
			if (_controlPanelGrid != null)
			{
				_controlPanelGrid.Tapped += OnPaneGridTapped;
			}
		}

		internal void SetMediaPlayerElement(MediaPlayerElement mediaPlayerElement)
		{
			_mpe = mediaPlayerElement;
		}

		private void ControlsVisibilityTimerElapsed(object sender, object args)
		{
			_controlsVisibilityTimer.Stop();

			if (ShowAndHideAutomatically)
			{
				Hide();
			}
		}

		private void ResetControlsVisibilityTimer()
		{
			if (ShowAndHideAutomatically)
			{
				_controlsVisibilityTimer.Stop();
				_controlsVisibilityTimer.Start();
			}
		}

		private void CancelControlsVisibilityTimer()
		{
			Show();
			_controlsVisibilityTimer.Stop();
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			DeinitializeTransportControls();

			
			HookupPartsAndHandlers();
			if (_mediaPlayer is not null)
			{
				BindMediaPlayer();
			}
			if (IsLoaded)
			{
				BindToControlEvents();
			}

			//InitializeVisualState();
			UpdateVisualStates(useTransition: false);
			UpdateMediaControlAllStates();

			// fixme@xy
			{
				_rootGrid = this.GetTemplateChild(RootGridName) as Grid;
				_controlPanelBorder = this.GetTemplateChild(ControlPanelBorderName) as Border;

				UpdateMediaTransportControlMode();
			}
		}



		internal override void OnLayoutUpdated()
		{
			base.OnLayoutUpdated();

			OnControlsBoundsChanged();
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			BindToControlEvents();
		}

		private void BindToControlEvents()
		{
			_loadedSubscriptions = new();

			if (_rootGrid is not null)
			{
				_rootGrid.Tapped += OnRootGridTapped;
				_rootGrid.PointerMoved += OnRootGridPointerMoved;

				_loadedSubscriptions.Add(() =>
				{
					_rootGrid.Tapped -= OnRootGridTapped;
					_rootGrid.PointerMoved -= OnRootGridPointerMoved;
				});
			}

			if (_fullWindowButton is not null)
			{
				_fullWindowButton.Tapped += FullWindowButtonTapped;

				_loadedSubscriptions.Add(() => _fullWindowButton.Tapped -= FullWindowButtonTapped);
			}

			if (_zoomButton is not null)
			{
				_zoomButton.Tapped += ZoomButtonTapped;

				_loadedSubscriptions.Add(() => _zoomButton.Tapped -= ZoomButtonTapped);
			}

			if (_playbackRateButton is not null)
			{
				_playbackRateButton.Tapped += PlaybackRateButtonTapped;

				_loadedSubscriptions.Add(() => _playbackRateButton.Tapped -= PlaybackRateButtonTapped);
			}

			if (_compactOverlayButton is not null)
			{
				_compactOverlayButton.Click += UpdateCompactOverlayMode;

				_loadedSubscriptions.Add(() => _compactOverlayButton.Click -= UpdateCompactOverlayMode);
			}

			if (_controlPanelGrid is not null)
			{
				_controlPanelGrid.SizeChanged += ControlPanelGridSizeChanged;

				_loadedSubscriptions.Add(() => _controlPanelGrid.SizeChanged -= ControlPanelGridSizeChanged);
			}

			if (_controlPanelBorder is not null)
			{
				_controlPanelBorder.SizeChanged += ControlPanelBorderSizeChanged;

				_loadedSubscriptions.Add(() => _controlPanelBorder.SizeChanged -= ControlPanelBorderSizeChanged);
			}

			if (_repeatVideoButton is not null)
			{
				_repeatVideoButton.Tapped += IsRepeatEnabledButtonTapped;

				_loadedSubscriptions.Add(() => _repeatVideoButton.Tapped -= IsRepeatEnabledButtonTapped);
			}

			if (_nextTrackButton is not null)
			{
				_nextTrackButton.Tapped -= NextTrackButtonTapped;

				_loadedSubscriptions.Add(() => _nextTrackButton.Tapped -= NextTrackButtonTapped);
			}

			if (_previousTrackButton is not null)
			{
				_previousTrackButton.Tapped -= PreviousTrackButtonTapped;

				_loadedSubscriptions.Add(() => _previousTrackButton.Tapped -= PreviousTrackButtonTapped);
			}

			// Register on visual state changes to update the layout in extensions
			foreach (var groups in VisualStateManager.GetVisualStateGroups(this.GetTemplateRoot()))
			{
				foreach (var state in groups.States)
				{
					if (state.Name is "ControlPanelFadeOut")
					{
						foreach (var child in state.Storyboard.Children)
						{
							// Update the layout on opacity completed
							if (child.PropertyInfo?.LeafPropertyName == "Opacity")
							{
								child.Completed += Storyboard_Completed;

								_loadedSubscriptions.Add(() => child.Completed += Storyboard_Completed);
							}
						}
					}
				}
			}
		}

		private void HookupPartsAndHandlers()
		{
			InitializeTemplateChild(TemplateParts.TimeElapsedElement, UIAKeys.UIA_MEDIA_TIME_ELAPSED, out m_tpTimeElapsedElement);
			InitializeTemplateChild(TemplateParts.TimeRemainingElement, UIAKeys.UIA_MEDIA_TIME_REMAINING, out m_tpTimeRemainingElement);
			if (InitializeTemplateChild(TemplateParts.ProgressSlider, UIAKeys.UIA_MEDIA_SEEK, out m_tpMediaPositionSlider))
			{
				m_tpMediaPositionSlider.RegisterDisposablePropertyChangedCallback(Slider.TemplateProperty, OnSliderTemplateChanged);
				m_tpMediaPositionSlider.Tapped += TappedProgressSlider;

				m_tpDownloadProgressIndicator = m_tpMediaPositionSlider.GetTemplateChild<ProgressBar>(TemplateParts.DownloadProgressIndicator);
			}
			if (InitializeTemplateChild(TemplateParts.PlayPauseButton, UIAKeys.UIA_MEDIA_PLAY, out m_tpPlayPauseButton))
			{
				m_tpPlayPauseButton.Click += PlayPause;
			}
			if (InitializeTemplateChild(TemplateParts.PlayPauseButtonOnLeft, UIAKeys.UIA_MEDIA_PLAY, out m_tpTHLeftSidePlayPauseButton))
			{
				m_tpTHLeftSidePlayPauseButton.Click += PlayPause;
			}
			if (InitializeTemplateChild(TemplateParts.FullWindowButton, UIAKeys.UIA_MEDIA_FULLSCREEN, out m_tpFullWindowButton))
			{
				m_tpFullWindowButton.Tapped += FullWindowButtonTapped;
			}
			if (InitializeTemplateChild(TemplateParts.ZoomButton, UIAKeys.UIA_MEDIA_ASPECTRATIO, out m_tpZoomButton))
			{
				m_tpZoomButton.Tapped += ZoomButtonTapped;
			}
			InitializeTemplateChild(TemplateParts.ErrorTextBlock, UIAKeys.UIA_MEDIA_ERROR, out m_tpErrorTextBlock);
			if (InitializeTemplateChild(TemplateParts.MediaControlsCommandBar, null, out m_tpCommandBar))
			{
				m_tpCommandBar.Loaded += OnCommandBarLoaded;
			}
			InitializeTemplateChild(TemplateParts.VolumeFlyout, null, out m_tpVolumeFlyout);

			HookupVolumeAndProgressPartsAndHandlers();
			MoreControls();

			InitializeTemplateChild(TemplateParts.MediaTransportControls_Timeline_Border, null, out _timelineContainer);
			if (InitializeTemplateChild(TemplateParts.RootGrid, null, out _rootGrid))
			{
				_rootGrid.Tapped += OnRootGridTapped;
			}
		}
		private void HookupVolumeAndProgressPartsAndHandlers()
		{
			InitializeTemplateChild(TemplateParts.HorizontalVolumeSlider, UIAKeys.UIA_MEDIA_VOLUME, out m_tpHorizontalVolumeSlider);
			InitializeTemplateChild(TemplateParts.VerticalVolumeSlider, UIAKeys.UIA_MEDIA_VOLUME, out m_tpVerticalVolumeSlider);
			if (InitializeTemplateChild(TemplateParts.VolumeSlider, UIAKeys.UIA_MEDIA_VOLUME, out m_tpTHVolumeSlider))
			{
				m_tpTHVolumeSlider.Maximum = 100;
				m_tpTHVolumeSlider.Value = 100;

				m_tpTHVolumeSlider.ValueChanged += OnVolumeChanged;
			}
			InitializeTemplateChild(TemplateParts.AudioSelectionButton, UIAKeys.UIA_MEDIA_AUDIO_SELECTION, out m_tpAudioSelectionButton);
			InitializeTemplateChild(TemplateParts.AudioTracksSelectionButton, UIAKeys.UIA_MEDIA_AUDIO_SELECTION, out m_tpTHAudioTrackSelectionButton);
			if (InitializeTemplateChild(TemplateParts.AvailableAudioTracksMenuFlyout, null, out m_tpAvailableAudioTracksMenuFlyout))
			{
				m_tpAvailableAudioTracksMenuFlyout.ShouldConstrainToRootBounds = true;
			}
			InitializeTemplateChild(TemplateParts.AvailableAudioTracksMenuFlyoutTarget, null, out m_tpAudioSelectionButton);
			InitializeTemplateChild(TemplateParts.CCSelectionButton, UIAKeys.UIA_MEDIA_CC_SELECTION, out m_tpCCSelectionButton);
			if (InitializeTemplateChild(TemplateParts.PlaybackRateButton, UIAKeys.UIA_MEDIA_PLAYBACKRATE, out m_tpPlaybackRateButton))
			{
				m_tpPlaybackRateButton.Click += PlaybackRateButtonTapped;
			}
			InitializeTemplateChild(TemplateParts.VolumeButton, UIAKeys.UIA_MEDIA_VOLUME, out m_tpVideoVolumeButton);
			if (InitializeTemplateChild(TemplateParts.AudioMuteButton, UIAKeys.UIA_MEDIA_MUTE, out m_tpMuteButton))
			{
				m_tpMuteButton.Click += ToggleMute;
			}
			InitializeTemplateChild(TemplateParts.VolumeMuteButton, UIAKeys.UIA_MEDIA_MUTE, out m_tpTHVolumeButton);
			InitializeTemplateChild(TemplateParts.BufferingProgressBar, UIAKeys.UIA_MEDIA_BUFFERING_PROGRESS, out m_tpBufferingProgressBar);
			if (InitializeTemplateChild(TemplateParts.FastForwardButton, UIAKeys.UIA_MEDIA_FASTFORWARD, out m_tpFastForwardButton))
			{
				m_tpFastForwardButton.Click += ForwardButton;
			}
			if (InitializeTemplateChild(TemplateParts.RewindButton, UIAKeys.UIA_MEDIA_REWIND, out m_tpFastRewindButton))
			{
				m_tpFastRewindButton.Click += RewindButton;
			}
			if (InitializeTemplateChild(TemplateParts.StopButton, UIAKeys.UIA_MEDIA_STOP, out m_tpStopButton))
			{
				m_tpStopButton.Click += Stop;
			}
			InitializeTemplateChild(TemplateParts.CastButton, UIAKeys.UIA_MEDIA_CAST, out m_tpCastButton);
		}
		private void MoreControls()
		{
			if (InitializeTemplateChild(TemplateParts.SkipForwardButton, UIAKeys.UIA_MEDIA_SKIPFORWARD, out m_tpSkipForwardButton))
			{
				m_tpSkipForwardButton.Click += SkipForward;
			}
			if (InitializeTemplateChild(TemplateParts.SkipBackwardButton, UIAKeys.UIA_MEDIA_SKIPBACKWARD, out m_tpSkipBackwardButton))
			{
				m_tpSkipBackwardButton.Click += SkipBackward;
			}
			if (InitializeTemplateChild(TemplateParts.NextTrackButton, UIAKeys.UIA_MEDIA_NEXTRACK, out m_tpNextTrackButton))
			{
				m_tpNextTrackButton.Click += NextTrackButtonTapped;
			}
			if (InitializeTemplateChild(TemplateParts.PreviousTrackButton, UIAKeys.UIA_MEDIA_NEXTRACK, out m_tpPreviousTrackButton))
			{
				m_tpPreviousTrackButton.Click += PreviousTrackButtonTapped;
			}
			if (InitializeTemplateChild(TemplateParts.RepeatButton, UIAKeys.UIA_MEDIA_REPEAT_NONE, out m_tpRepeatButton))
			{
				m_tpRepeatButton.Click += IsRepeatEnabledButtonTapped;
			}
			if (InitializeTemplateChild(TemplateParts.CompactOverlayButton, UIAKeys.UIA_MEDIA_MINIVIEW, out m_tpCompactOverlayButton))
			{
				m_tpCompactOverlayButton.Click += UpdateMediaTransportControlMode;
			}
			InitializeTemplateChild(TemplateParts.LeftSeparator, null, out m_tpLeftAppBarSeparator);
			InitializeTemplateChild(TemplateParts.RightSeparator, null, out m_tpRightAppBarSeparator);
		}

		private void OnCommandBarLoaded(object sender, RoutedEventArgs e)
		{
			HideMoreButtonIfNecessary();
			HideCastButtonIfNecessary();

			m_tpCommandBar.Loaded -= OnCommandBarLoaded;
		}
		private void HideMoreButtonIfNecessary()
		{
			if (m_tpCommandBar is { SecondaryCommands.Count: 0 })
			{
				if (m_tpCommandBar.GetTemplateChild<UIElement>(TemplateParts.MoreButton) is { } moreButton)
				{
					moreButton.Visibility = Visibility.Collapsed;
				}
			}
		}
		private void HideCastButtonIfNecessary()
		{
			if (m_tpCastButton is { })
			{
#if false // not implemented
				var deviceSelector = CastingDevice.GetDeviceSelector(
					CastingPlaybackTypes.Audio |
					CastingPlaybackTypes.Video |
					CastingPlaybackTypes.Picture);
				if (string.IsNullOrEmpty(deviceSelector))
#endif
				{
					m_tpCastButton.Visibility = Visibility.Collapsed;
				}
			}
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_loadedSubscriptions?.Dispose();
			_loadedSubscriptions = null;
		}

		private void Storyboard_Completed(object sender, object e)
			=> OnControlsBoundsChanged();

		private void ControlPanelGridSizeChanged(object sender, SizeChangedEventArgs args)
		{
			OnControlsBoundsChanged();
		}

		private static void ControlPanelBorderSizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (sender is Border border)
			{
				border.Clip = new RectangleGeometry { Rect = new Rect(0, 0, args.NewSize.Width, args.NewSize.Height) };
			}
		}

		private void DeinitializeTransportControls()
		{
			// Release Play button's event handlers
			UnhookButtonClick(m_tpPlayPauseButton, PlayPause);
			UnhookButtonClick(m_tpTHLeftSidePlayPauseButton, PlayPause);

			// Release Audio Selection button's event handlers
			//IFC(DetachHandler(m_epAudioSelectionButtonClickHandler, m_tpAudioSelectionButton));

			// Release Audio Selection button's event handlers for Threshold
			//IFC(DetachHandler(m_epAudioTrackSelectionButtonClickHandler, m_tpTHAudioTrackSelectionButton));

			// Release Closed Caption Selection button's event handlers
			//IFC(DetachHandler(m_epCCSelectionButtonClickHandler, m_tpCCSelectionButton));

			// Release Play Rate Selection button's event handlers
			UnhookButtonClick(m_tpPlaybackRateButton, PlaybackRateButtonTapped);

			// Release Video Mute/Volume button's event handlers
			//IFC(DetachHandler(m_epVolumeButtonClickHandler, m_tpVideoVolumeButton));

			// Release Audio Mute button's event handlers
			UnhookButtonClick(m_tpMuteButton, ToggleMute);

			// Release Full Window button's event handlers
			//IFC(DetachHandler(m_epFullWindowButtonClickHandler, m_tpFullWindowButton));

			// Release Zoom button's event handlers
			//IFC(DetachHandler(m_epZoomButtonClickHandler, m_tpZoomButton));

			// Release other Media button's event handlers
			UnhookButtonClick(m_tpFastForwardButton, ForwardButton);
			UnhookButtonClick(m_tpFastRewindButton, RewindButton);
			UnhookButtonClick(m_tpStopButton, Stop);
			//IFC(DetachHandler(m_epCastButtonClickHandler, m_tpCastButton));
			UnhookButtonClick(m_tpSkipForwardButton, SkipForward);
			UnhookButtonClick(m_tpSkipBackwardButton, SkipBackward);

			//IFC(DetachHandler(m_epNextTrackButtonClickHandler, m_tpNextTrackButton));
			//IFC(DetachHandler(m_epPreviousTrackButtonClickHandler, m_tpPreviousTrackButton));
			//IFC(DetachHandler(m_epRepeatButtonClickHandler, m_tpRepeatButton));
			//IFC(DetachHandler(m_epCompactOverlayButtonClickHandler, m_tpCompactOverlayButton));

			// Release Position Slider's event handlers
			if (m_tpMediaPositionSlider is { })
			{
				m_tpMediaPositionSlider.Tapped -= TappedProgressSlider;
				//IFC(DetachHandler(m_epProgressSliderSizeChangedHandler, m_tpMediaPositionSlider));
				//IFC(DetachHandler(m_epProgressSliderFocusDisengagedHandler, m_tpMediaPositionSlider));
				//IFC(DetachHandler(m_epPositionChangedHandler, m_tpMediaPositionSlider));
				//{
				//	auto spMediaPositionSlider = m_tpMediaPositionSlider.GetSafeReference();

				//	if (spMediaPositionSlider)
				//	{
				//		IFC(spMediaPositionSlider.Cast<Slider>()->remove_PointerPressed(m_positionSliderPressedEventToken));
				//		IFC(spMediaPositionSlider.Cast<Slider>()->remove_PointerReleased(m_positionSliderReleasedEventToken));
				//		IFC(spMediaPositionSlider.Cast<Slider>()->remove_KeyDown(m_positionSliderKeyDownEventToken));
				//		IFC(spMediaPositionSlider.Cast<Slider>()->remove_KeyUp(m_positionSliderKeyUpEventToken));
				//	}
				//}
			}

			//if (m_spCastingDevicePicker)
			//{
			//	if (m_castingDeviceSelectedToken.value != 0)
			//	{
			//		IFC(m_spCastingDevicePicker->remove_CastingDeviceSelected(m_castingDeviceSelectedToken));
			//	}
			//	if (m_castingPickerDismissedToken.value != 0)
			//	{
			//		IFC(m_spCastingDevicePicker->remove_CastingDevicePickerDismissed(m_castingPickerDismissedToken));
			//		m_castingPickerDismissedToken.value = 0;
			//	}
			//	if (m_spCastingConnection && m_castingConnectStateChangeToken.value != 0)
			//	{
			//		IFC(m_spCastingConnection->remove_StateChanged(m_castingConnectStateChangeToken));
			//		m_castingConnectStateChangeToken.value = 0;
			//	}
			//	m_spCastingDevicePicker = nullptr;
			//}

			//// Release Horizontal volume slider's event handlers
			//IFC(DetachHandler(m_epHorizontalVolumeChangedHandler, m_tpHorizontalVolumeSlider));

			// Release Threshold volume slider's event handlers
			if (m_tpTHVolumeSlider is { })
			{
				m_tpTHVolumeSlider.ValueChanged -= OnVolumeChanged;
				//IFC(DetachHandler(m_volumeSliderPointerWheelChangedHandler, m_tpTHVolumeSlider));
			}

			//// Release Video volume button's event handlers
			//IFC(DetachHandler(m_epVolumeButtonGotFocusHandler, m_tpVideoVolumeButton));

			//// Release Vertical volume slider's event handlers
			//IFC(DetachHandler(m_epVerticalVolumeChangedHandler, m_tpVerticalVolumeSlider));

			//// Release control panel grid's event handlers
			//IFC(DetachHandler(m_epControlPanelEnteredHandler, m_tpControlPanelGrid));
			//IFC(DetachHandler(m_epControlPanelExitedHandler, m_tpControlPanelGrid));
			//IFC(DetachHandler(m_epControlPanelTappedHandler, m_tpControlPanelGrid));
			//IFC(DetachHandler(m_epControlPanelCaptureLostHandler, m_tpControlPanelGrid));
			//IFC(DetachHandler(m_epControlPanelGotFocusHandler, m_tpControlPanelGrid));
			//IFC(DetachHandler(m_epControlPanelLostFocusHandler, m_tpControlPanelGrid));

			//IFC(DetachHandler(m_epBorderSizeChangedHandler, m_tpControlPanelVisibilityBorder));
			//IFC(DetachHandler(m_visibilityStateChangedEventHandler, m_tpVisibilityStatesGroup));

			//// Release Position Timer's event handlers
			//IFC(DetachHandler(m_epPositionUpdateTimerTickHandler, m_tpPositionUpdateTimer));

			//// Release Control Panel Hide Timer's event handlers
			//IFC(DetachHandler(m_epHideControlPanelTimerTickHandler, m_tpHideControlPanelTimer));

			//// Release Pointer Move Timer's event handlers
			//IFC(DetachHandler(m_epPointerMoveEndTimerTickHandler, m_tpPointerMoveEndTimer));

			//if (MTCParent_MediaElement == m_parentType)
			//{
			//	IFC(DeinitializeTransportControlsFromME());
			//}
			//else if (MTCParent_MediaPlayerElement == m_parentType)
			{
				DeinitializeTransportControlsFromMPE();
			}

			//IFC(DetachHandler(m_epFlyoutOpenedHandler, m_tpVolumeFlyout));
			//IFC(DetachHandler(m_epFlyoutClosedHandler, m_tpVolumeFlyout));

			//DXamlCore::GetCurrent()->GetLayoutBoundsHelperNoRef()->RemoveLayoutBoundsChangedCallback(&m_tokLayoutBoundsChanged);

			_loadedSubscriptions?.Dispose();
		}

		private void FullWindowButtonTapped(object sender, RoutedEventArgs e)
		{
			_mpe.IsFullWindow = !_mpe.IsFullWindow;
			UpdateFullWindowStates();
		}
		private void PlaybackRateButtonTapped(object sender, RoutedEventArgs e)
		{
			_mpe.MediaPlayer.PlaybackRate += 0.25;
		}
		private void IsRepeatEnabledButtonTapped(object sender, RoutedEventArgs e)
		{
			if (_mpe?.MediaPlayer is null)
			{
				return;
			}

			_mpe.MediaPlayer.IsLoopingEnabled = !_mpe.MediaPlayer.IsLoopingEnabled;
			UpdateRepeatStates();
		}
		private void PreviousTrackButtonTapped(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
			_mpe.MediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
		}
		private void NextTrackButtonTapped(object sender, RoutedEventArgs e)
		{
			_mediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.NaturalDuration;
			_mpe.MediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.NaturalDuration;
		}
		private void ZoomButtonTapped(object sender, RoutedEventArgs e)
		{
			if (_mpe.Stretch == Stretch.Uniform)
			{
				_mpe.Stretch = Stretch.UniformToFill;
			}
			else
			{
				_mpe.Stretch = Stretch.Uniform;
			}
		}

		public void Show()
		{
			_isShowingControls = true;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				VisualStateManager.GoToState(this, VisualState.ControlPanelVisibilityStates.ControlPanelFadeIn, false);
			});

			// Adjust layout bounds immediately
			OnControlsBoundsChanged();

			if (ShowAndHideAutomatically)
			{
				ResetControlsVisibilityTimer();
			}
		}

		public void Hide()
		{
			_isShowingControls = false;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				if (_mediaPlayer.PlaybackSession.IsPlaying)
				{
					VisualStateManager.GoToState(this, VisualState.ControlPanelVisibilityStates.ControlPanelFadeOut, false);
				}
			});
		}

		private void OnControlsBoundsChanged()
		{
			var root = (XamlRoot?.Content as UIElement);
			if (root is null)
			{
				return;
			}

			var bounds = new Rect(
				0,
				0,
				_controlPanelGrid.ActualWidth,
				_isShowingControls ? _controlPanelGrid.ActualHeight : 0);

			var transportBounds = TransformToVisual(root).TransformBounds(bounds);
			_mediaPlayer?.SetTransportControlBounds(transportBounds);
		}

		private void OnPaneGridTapped(object sender, TappedRoutedEventArgs e)
		{
			if (ShowAndHideAutomatically)
			{
				ResetControlsVisibilityTimer();
			}
			e.Handled = true;
		}
		private void OnRootGridTapped(object sender, TappedRoutedEventArgs e)
		{
			if (e.PointerDeviceType == PointerDeviceType.Touch)
			{
				if (_isShowingControls)
				{
					_controlsVisibilityTimer.Stop();
					Hide();
				}
				else
				{
					Show();
				}
			}
		}

		private void OnRootGridPointerMoved(object sender, PointerRoutedEventArgs e)
		{
			Show();
		}

		private void UpdateCompactOverlayMode(object sender, RoutedEventArgs e)
		{
			_mpe.ToogleCompactOverlay(!_mpe.IsCompactOverlay);
		}

		private void UpdateMediaTransportControlMode(object sender, RoutedEventArgs e)
		{
			VisualStateManager.GoToState(this, IsCompact ? "CompactMode" : "NormalMode", true);
			OnControlsBoundsChanged();
		}
		private void UpdateMediaTransportControlMode()
		{
			VisualStateManager.GoToState(this, IsCompact ? "CompactMode" : "NormalMode", true);
			OnControlsBoundsChanged();
		}

		private static void OnIsCompactChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is MediaTransportControls mtc)
			{
				mtc.UpdateMediaTransportControlMode();
			}
		}

		private static void OnShowAndHideAutomaticallyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if ((bool)args.NewValue)
			{
				((MediaTransportControls)dependencyObject).ResetControlsVisibilityTimer();
			}
			else
			{
				((MediaTransportControls)dependencyObject).CancelControlsVisibilityTimer();
			}
		}

		private static void OnIsSeekBarVisibleChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			((MediaTransportControls)dependencyObject)._timelineContainer.Visibility = (bool)args.NewValue ? Visibility.Visible : Visibility.Collapsed;
		}

		private static void OnIsSeekEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			VisualStateManager.GoToState(((MediaTransportControls)dependencyObject).m_tpMediaPositionSlider, (bool)args.NewValue ? "Normal" : "Disabled", false);
		}

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			UpdateMediaControlState(args.Property);
		}

		private void UpdateMediaControlAllStates()
		{
			UpdateMediaControlState(IsFullWindowButtonVisibleProperty);
			UpdateMediaControlState(IsZoomButtonVisibleProperty);
			UpdateMediaControlState(IsSeekBarVisibleProperty);
			UpdateMediaControlState(IsVolumeButtonVisibleProperty);
			UpdateMediaControlState(IsFullWindowEnabledProperty);
			UpdateMediaControlState(IsVolumeEnabledProperty);
			UpdateMediaControlState(IsZoomEnabledProperty);
			UpdateMediaControlState(IsSeekEnabledProperty);
			UpdateMediaControlState(IsPlaybackRateButtonVisibleProperty);
			UpdateMediaControlState(IsPlaybackRateEnabledProperty);
			UpdateMediaControlState(IsFastForwardButtonVisibleProperty);
			UpdateMediaControlState(IsFastForwardEnabledProperty);
			UpdateMediaControlState(IsFastRewindEnabledProperty);
			UpdateMediaControlState(IsFastRewindButtonVisibleProperty);
			UpdateMediaControlState(IsStopEnabledProperty);
			UpdateMediaControlState(IsStopButtonVisibleProperty);
			UpdateMediaControlState(IsCompactProperty);
			UpdateMediaControlState(IsSkipForwardEnabledProperty);
			UpdateMediaControlState(IsSkipBackwardEnabledProperty);
			UpdateMediaControlState(IsSkipForwardButtonVisibleProperty);
			UpdateMediaControlState(IsSkipBackwardButtonVisibleProperty);
			UpdateMediaControlState(IsNextTrackButtonVisibleProperty);
			UpdateMediaControlState(IsPreviousTrackButtonVisibleProperty);
			UpdateMediaControlState(IsRepeatButtonVisibleProperty);
			UpdateMediaControlState(IsRepeatEnabledProperty);
			UpdateMediaControlState(IsCompactOverlayButtonVisibleProperty);
			UpdateMediaControlState(IsCompactOverlayEnabledProperty);
		}
		private void UpdateMediaControlState(DependencyProperty property)
		{
			switch (property)
			{
				case var _ when property == IsCompactProperty:
					UpdateMediaTransportControlMode();
					break;

				case var _ when property == IsRepeatButtonVisibleProperty:
					BindVisibility(m_tpRepeatButton, IsRepeatButtonVisible);
					break;
				case var _ when property == IsRepeatEnabledProperty:
					BindIsEnabled(m_tpRepeatButton, IsRepeatEnabled);
					break;
				case var _ when property == IsVolumeButtonVisibleProperty:
					BindVisibility(m_tpTHVolumeButton, IsVolumeButtonVisible);
					break;
				case var _ when property == IsVolumeEnabledProperty:
					BindIsEnabled(m_tpTHVolumeButton, IsVolumeEnabled);
					break;
				case var _ when property == IsFullWindowButtonVisibleProperty:
					BindVisibility(m_tpFullWindowButton, IsFullWindowButtonVisible);
					break;
				case var _ when property == IsFullWindowEnabledProperty:
					BindIsEnabled(m_tpFullWindowButton, IsFullWindowEnabled);
					break;
				case var _ when property == IsZoomButtonVisibleProperty:
					BindVisibility(m_tpZoomButton, IsZoomButtonVisible);
					break;
				case var _ when property == IsZoomEnabledProperty:
					BindIsEnabled(m_tpZoomButton, IsZoomEnabled);
					break;
				case var _ when property == IsPlaybackRateButtonVisibleProperty:
					BindVisibility(m_tpPlaybackRateButton, IsPlaybackRateButtonVisible);
					break;
				case var _ when property == IsPlaybackRateEnabledProperty:
					BindIsEnabled(m_tpPlaybackRateButton, IsPlaybackRateEnabled);
					break;
				case var _ when property == IsFastForwardButtonVisibleProperty:
					BindVisibility(m_tpFastForwardButton, IsFastForwardButtonVisible);
					break;
				case var _ when property == IsFastForwardEnabledProperty:
					BindIsEnabled(m_tpFastForwardButton, IsFastForwardEnabled);
					break;
				case var _ when property == IsFastRewindButtonVisibleProperty:
					BindVisibility(m_tpFastRewindButton, IsFastRewindButtonVisible);
					break;
				case var _ when property == IsFastRewindEnabledProperty:
					BindIsEnabled(m_tpFastRewindButton, IsFastRewindEnabled);
					break;
				case var _ when property == IsStopButtonVisibleProperty:
					BindVisibility(m_tpStopButton, IsStopButtonVisible);
					break;
				case var _ when property == IsStopEnabledProperty:
					BindIsEnabled(m_tpStopButton, IsStopEnabled);
					break;
				case var _ when property == IsSeekBarVisibleProperty:
					BindVisibility(_timelineContainer, IsSeekBarVisible);
					break;
				case var _ when property == IsSeekEnabledProperty:
					BindIsEnabled(_timelineContainer, IsSeekEnabled);
					break;
				case var _ when property == IsSkipForwardButtonVisibleProperty:
					BindVisibility(m_tpSkipForwardButton, IsSkipForwardButtonVisible);
					break;
				case var _ when property == IsSkipForwardEnabledProperty:
					BindIsEnabled(m_tpSkipForwardButton, IsSkipForwardEnabled);
					break;
				case var _ when property == IsSkipBackwardButtonVisibleProperty:
					BindVisibility(m_tpSkipBackwardButton, IsSkipBackwardButtonVisible);
					break;
				case var _ when property == IsSkipBackwardEnabledProperty:
					BindIsEnabled(m_tpSkipBackwardButton, IsSkipBackwardEnabled);
					break;
				case var _ when property == IsNextTrackButtonVisibleProperty:
					BindVisibility(m_tpNextTrackButton, IsNextTrackButtonVisible);
					break;
				case var _ when property == IsPreviousTrackButtonVisibleProperty:
					BindVisibility(m_tpPreviousTrackButton, IsPreviousTrackButtonVisible);
					break;
				case var _ when property == IsCompactOverlayButtonVisibleProperty:
					BindVisibility(m_tpCompactOverlayButton, IsCompactOverlayButtonVisible);
					break;
				case var _ when property == IsCompactOverlayEnabledProperty:
					BindIsEnabled(m_tpCompactOverlayButton, IsCompactOverlayEnabled);
					break;
			}

			void BindVisibility(FrameworkElement target, bool value)
			{
				if (target is { })
				{
					target.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
				}
			}
			void BindIsEnabled(FrameworkElement target, bool value)
			{
				if (target is { })
				{
					target.IsEnabled = value;
				}
			}
		}

		private void UpdateVisualStates(bool useTransition = true)
		{
			//UpdateControlPanelVisibilityStates(useTransition);
			UpdateMediaStates(useTransition);
			//UpdateAudioSelectionAvailablityStates(useTransition);
			//UpdateCCSelectionAvailablityStates(useTransition);
			//UpdateFocusStates(useTransition);
			UpdateMediaTransportControlMode(useTransition);
			UpdatePlayPauseStates(useTransition);
			UpdateVolumeMuteStates(useTransition);
			UpdateFullWindowStates(useTransition);
			UpdateRepeatStates(useTransition);
		}
		private void UpdateMediaStates(bool useTransition = true)
		{
			if (_mpe?.MediaPlayer?.PlaybackSession is { } session)
			{
				var state = session.PlaybackState switch
				{
					MediaPlaybackState.None => VisualState.MediaStates.Disabled,
					MediaPlaybackState.Opening => VisualState.MediaStates.Loading,
					MediaPlaybackState.Buffering => VisualState.MediaStates.Buffering,
					MediaPlaybackState.Playing or
					MediaPlaybackState.Paused => VisualState.MediaStates.Normal,

					_ => null,
				};

				if (state != null)
				{
					VisualStateManager.GoToState(this, state, useTransition);
				}
			}
		}
		private void UpdateMediaTransportControlMode(bool useTransition = true)
		{
			var state = IsCompact
				? VisualState.MediaTransportControlMode.CompactMode
				: VisualState.MediaTransportControlMode.NormalMode;
			VisualStateManager.GoToState(this, state, useTransition);

			var uiaKey = IsCompact
				? UIAKeys.UIA_MEDIA_EXIT_MINIVIEW
				: UIAKeys.UIA_MEDIA_MINIVIEW;
			SetAutomationNameAndTooltip(m_tpCompactOverlayButton, uiaKey);
		}
		private void UpdatePlayPauseStates(bool useTransition = true)
		{
			if (_mpe?.MediaPlayer is null)
			{
				return;
			}

			// todo@xy: verify & test logic here

			var state = _mpe.MediaPlayer.PlaybackSession.IsPlaying
				? VisualState.PlayPauseStates.PauseState
				: VisualState.PlayPauseStates.PlayState;
			VisualStateManager.GoToState(this, state, useTransition);

			var uiaKey = _mpe.MediaPlayer.PlaybackSession.IsPlaying
				? UIAKeys.UIA_MEDIA_PAUSE
				: UIAKeys.UIA_MEDIA_PLAY;
			SetAutomationNameAndTooltip(m_tpPlayPauseButton, uiaKey);
			SetAutomationNameAndTooltip(m_tpTHLeftSidePlayPauseButton, uiaKey);
		}
		private void UpdateVolumeMuteStates(bool isExplicitMuteToggle = false, bool useTransition = true)
		{
			if (_mediaPlayer is null)
			{
				return;
			}

			// We can be in mute state for 2 reasons:
			// - volume is set to 0
			// - muted
			// While the volume is at 0, we can click on the mute button (indicated by isExplicitMuteToggle)
			// and it will still toggle its state visually.
			var isMuted = isExplicitMuteToggle
				? _mediaPlayer.IsMuted
				: _mediaPlayer.IsMuted || _mediaPlayer.Volume == 0;

			var state = isMuted
				? VisualState.VolumeMuteStates.MuteState
				: VisualState.VolumeMuteStates.VolumeState;
			VisualStateManager.GoToState(this, state, useTransition);

			var uiaKey = isMuted
				? UIAKeys.UIA_MEDIA_UNMUTE
				: UIAKeys.UIA_MEDIA_MUTE;
			SetAutomationNameAndTooltip(m_tpMuteButton, uiaKey);
		}
		private void UpdateFullWindowStates(bool useTransition = true)
		{
			var state = _mpe.IsFullWindow
				? VisualState.FullWindowStates.FullWindowState
				: VisualState.FullWindowStates.NonFullWindowState;
			VisualStateManager.GoToState(this, state, useTransition);

			var uiaKey = _mpe.IsFullWindow
				? UIAKeys.UIA_MEDIA_EXIT_FULLSCREEN
				: UIAKeys.UIA_MEDIA_FULLSCREEN;
			SetAutomationNameAndTooltip(m_tpFullWindowButton, uiaKey);
		}
		private void UpdateRepeatStates(bool useTransition = true)
		{
			if (_mpe?.MediaPlayer is null)
			{
				return;
			}

			var state = _mpe.MediaPlayer.IsLoopingEnabled
				? VisualState.RepeatStates.RepeatAllState
				: VisualState.RepeatStates.RepeatNoneState;
			VisualStateManager.GoToState(this, state, useTransition);

			var uiaKey = _mpe.MediaPlayer.IsLoopingEnabled
				? UIAKeys.UIA_MEDIA_REPEAT_ALL
				: UIAKeys.UIA_MEDIA_REPEAT_NONE;
			SetAutomationNameAndTooltip(m_tpRepeatButton, uiaKey);
		}

		private bool InitializeTemplateChild<T>(string childName, string uiaKey, out T child) where T : class, DependencyObject
		{
			child = GetTemplateChild<T>(childName);
			if (child is { } && uiaKey is { })
			{
				SetAutomationNameAndTooltip(child, uiaKey);
			}

			return child != null;
		}
		private void SetAutomationNameAndTooltip(DependencyObject target, string uiaKey)
		{
			if (target is { })
			{
				var value = DXamlCore.Current.GetLocalizedResourceString(uiaKey);
				AutomationProperties.SetName(target, value);
				ToolTipService.SetToolTip(target, value);
			}
		}


		void UnhookButtonClick(ButtonBase button, RoutedEventHandler handler)
		{
			if (button != null)
			{
				button.Click -= handler;
			}
		}
	}
}

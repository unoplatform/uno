#nullable enable

using System.Collections.Generic;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls;

public partial class MediaTransportControls // Strings
{
	private static class TemplateParts
	{
		#region Uno-specific

		public const string RootGrid = nameof(RootGrid);
		public const string ControlPanel_ControlPanelVisibilityStates_Border = nameof(ControlPanel_ControlPanelVisibilityStates_Border);
		public const string MediaTransportControls_Timeline_Border = nameof(MediaTransportControls_Timeline_Border);
		public const string PlaybackRateListView = nameof(PlaybackRateListView);
		public const string PlaybackRateFlyout = nameof(PlaybackRateFlyout);

		#endregion

		public const string ControlPanelGrid = nameof(ControlPanelGrid); // Grid
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

public partial class MediaTransportControls // Definitions
{
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CA1823 // Avoid unused private fields
	// HNS Hunderds of Nano Seconds used for conversion in timer duration
	private const uint HNSPerSecond = 10000000;

	// Control Panel Timout in secs, after timeout Control Panel will be hide.
	private const double ControlPanelDisplayTimeoutInSecs = 3.0;

	// Vertical Volume bar Timout in secs, after timout Vertical Volume bar will be hide.
	private const double VerticalVolumeDisplayTimeoutInSecs = 3.0;

	// Timer frequecy in second to update Seek bar.
	private const double SeekbarPositionUpdateFreqInSecs = 0.250;

	// Elapsed-Remaining Button used seeking interval defined in HNS
	private const long TimeButtonUsedSeekIntervalInHNS = 300000000;

	// Maximum Time String length unsed in the Time Buttons.
	// [H]H:mm:ss time string has up to 8 chars; also include terminating '\0'
	private const uint MaxTimeButtonTextLength = 9;

	// Maximum Processed Language length
	// include terminating '\0'
	private const uint MaxProcessedLanguageNameLength = 50;

	// Maximum Dropout levels used in WinBlue
	private const uint MaxDropuOutLevels = 10;

	// Maximum PlayRate Counts
	private const uint AvailablePlaybackRateCount = 5;

	private static readonly IReadOnlyCollection<double> AvailablePlaybackRateList = new[] { 0.25, 0.5, 1.0, 1.5, 2.0 };

	// Skip forward/Skip Backward time interval defined in Seconds
	private const uint SkipForwardInSecs = 30;
	private const uint SkipBackwardInSecs = 10;

	private const int VolumeSliderWheelScrollStep = 2;

	private const int MinSupportedPlayRate = 2;
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore CA1823 // Avoid unused private fields
}

[TemplatePart(Name = TemplateParts.ControlPanelGrid, Type = typeof(Grid))]
[TemplatePart(Name = TemplateParts.TimeElapsedElement, Type = typeof(TextBlock))]
[TemplatePart(Name = TemplateParts.TimeRemainingElement, Type = typeof(TextBlock))]
[TemplatePart(Name = TemplateParts.ProgressSlider, Type = typeof(Slider))]
[TemplatePart(Name = TemplateParts.PlayPauseButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.PlayPauseButtonOnLeft, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.FullWindowButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.ZoomButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.ErrorTextBlock, Type = typeof(TextBlock))]
[TemplatePart(Name = TemplateParts.MediaControlsCommandBar, Type = typeof(CommandBar))]
[TemplatePart(Name = TemplateParts.VolumeFlyout, Type = typeof(Flyout))]
[TemplatePart(Name = TemplateParts.HorizontalVolumeSlider, Type = typeof(Slider))]
[TemplatePart(Name = TemplateParts.VerticalVolumeSlider, Type = typeof(Slider))]
[TemplatePart(Name = TemplateParts.VolumeSlider, Type = typeof(Slider))]
[TemplatePart(Name = TemplateParts.AudioSelectionButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.AudioTracksSelectionButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.AvailableAudioTracksMenuFlyout, Type = typeof(MenuFlyout))]
[TemplatePart(Name = TemplateParts.AvailableAudioTracksMenuFlyoutTarget, Type = typeof(FrameworkElement))]
[TemplatePart(Name = TemplateParts.CCSelectionButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.PlaybackRateButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.VolumeButton, Type = typeof(ToggleButton))]
[TemplatePart(Name = TemplateParts.AudioMuteButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.VolumeMuteButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.BufferingProgressBar, Type = typeof(ProgressBar))]
[TemplatePart(Name = TemplateParts.FastForwardButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.RewindButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.StopButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.CastButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.SkipForwardButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.SkipBackwardButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.NextTrackButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.PreviousTrackButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.RepeatButton, Type = typeof(AppBarToggleButton))]
[TemplatePart(Name = TemplateParts.CompactOverlayButton, Type = typeof(AppBarButton))]
[TemplatePart(Name = TemplateParts.LeftSeparator, Type = typeof(AppBarSeparator))]
[TemplatePart(Name = TemplateParts.RightSeparator, Type = typeof(AppBarSeparator))]
[TemplatePart(Name = TemplateParts.MoreButton, Type = typeof(Button))]
[TemplatePart(Name = TemplateParts.DownloadProgressIndicator, Type = typeof(ProgressBar))]
[TemplatePart(Name = TemplateParts.HorizontalThumb, Type = typeof(Thumb))]
public partial class MediaTransportControls // Template Parts
{
	//
	// References to control parts we need to manipulate
	//
	#region Uno-specific

	private Grid? _rootGrid;
	private Border? _timelineContainer;
	private Border? _controlPanelBorder;
	private Thumb? _progressSliderThumb;
	private Flyout? _playbackRateFlyout;
	private ListView? _playbackRateListView;

	#endregion

	// Reference to the control panel grid
	private Grid? m_tpControlPanelGrid;

	// Reference to the media position slider.
	private Slider? m_tpMediaPositionSlider;

	// Reference to the horizontal volume slider (audio-only mode audio slider).
	private Slider? m_tpHorizontalVolumeSlider;

	// Reference to the vertical volume slider (video-mode audio slider).
	private Slider? m_tpVerticalVolumeSlider;

	// Reference to the Threshold Volume slider (video-mode & audio-mode slider).
	private Slider? m_tpTHVolumeSlider;

	// Reference to currently active volume slider
	//private Slider m_tpActiveVolumeSlider;

	// Reference to download progress indicator, which is a part in the MediaSlider template
	private ProgressBar? m_tpDownloadProgressIndicator;

	// Reference to the buffering indeterminate progress bar
	private ProgressBar? m_tpBufferingProgressBar;

	// Reference to the PlayPause button used in Blue and Threshold
	private ButtonBase? m_tpPlayPauseButton;

	// Reference to the PlayPause button used only in Threshold
	private ButtonBase? m_tpTHLeftSidePlayPauseButton;

	// Reference to the Audio Selection button
	private Button? m_tpAudioSelectionButton;

	// Reference to the Audio Selection button for Threshold
	private Button? m_tpTHAudioTrackSelectionButton;

	// Reference to the Available Audiotracks flyout
	private MenuFlyout? m_tpAvailableAudioTracksMenuFlyout;

	// Reference to the Available Audiotracks flyout target
	//private FrameworkElement m_tpAvailableAudioTracksMenuFlyoutTarget;

	// Reference to the Close Captioning Selection button
	private Button? m_tpCCSelectionButton;

	// Reference to the Available Close Captioning tracks flyout
	//private MenuFlyout m_tpAvailableCCTracksMenuFlyout;

	// Reference to the Play Rate Selection button
	private Button? m_tpPlaybackRateButton;

	// Reference to the Available Play Rate List flyout
	//private MenuFlyout m_tpAvailablePlaybackRateMenuFlyout;

	// Reference to the Video volume button
	private ToggleButton? m_tpVideoVolumeButton;

	// Reference to the Audio-mute button for Blue and Mute button for Video/Audio in Threshold
	private ButtonBase? m_tpMuteButton;

	// Reference to the Threshold volume button
	private ButtonBase? m_tpTHVolumeButton;

	// Reference to the Full Window button
	private ButtonBase? m_tpFullWindowButton;

	// Reference to the Zoom button
	private ButtonBase? m_tpZoomButton;

	// Reference to currently active volume button
	//private ToggleButton m_tpActiveVolumeButton;

	// Reference to Time Elapsed / -30 sec seek button or Time Elapsed TextBlock
	private FrameworkElement? m_tpTimeElapsedElement;

	// Reference to Time Remaining / +30 sec seek button or Time Remaining TextBlock
	private FrameworkElement? m_tpTimeRemainingElement;

	// Reference to the fast forward button
	private Button? m_tpFastForwardButton;

	// Reference to the rewind button
	private Button? m_tpFastRewindButton;

	// Reference to the stop button
	private Button? m_tpStopButton;

	// Reference to the cast button
	private Button? m_tpCastButton;

	// Reference to the Skip Forward button
	private Button? m_tpSkipForwardButton;

	// Reference to the Skip Backward button
	private Button? m_tpSkipBackwardButton;

	// Reference to the Next Track button
	private Button? m_tpNextTrackButton;

	// Reference to the Previous Track button
	private Button? m_tpPreviousTrackButton;

	// Reference to currently Repeat button
	private ToggleButton? m_tpRepeatButton;

	// Reference to the Mini View button
	private Button? m_tpCompactOverlayButton;

	// Reference to the Left AppBarSeparator
	private AppBarSeparator? m_tpLeftAppBarSeparator;

	// Reference to the Right AppBarSeparator
	private AppBarSeparator? m_tpRightAppBarSeparator;

	// Reference to the Image thumbnail preview
	//private Image m_tpThumbnailImage;

	// Reference to the Time Elapsed preview
	//private TextBlock m_tpTimeElapsedPreview;

	// Reference to Error TextBlock
	private TextBlock? m_tpErrorTextBlock;

	// Dispatcher timer responsible for updating clock and position slider
	//private DispatcherTimer m_tpPositionUpdateTimer;

	// Dispatcher timer responsible for hiding vertical volume host border
	//private DispatcherTimer m_tpHideVerticalVolumeTimer;

	// Dispatcher timer responsible for hiding UI control panel
	private DispatcherTimer? m_tpHideControlPanelTimer;

	// Dispatcher timer to detect the pointer move ends.
	private DispatcherTimer m_tpPointerMoveEndTimer;

	// Reference to the Visibility Border element.
	//private Border m_tpControlPanelVisibilityBorder;

	// Reference to the CommandBar Element.
	private CommandBar? m_tpCommandBar;

	// Reference to the CommandBar Element.
	private FlyoutBase? m_tpVolumeFlyout;

	// Reference to the VisualStateGroup
	//private VisualStateGroup m_tpVisibilityStatesGroup;
}

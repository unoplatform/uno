#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DirectUI;
using Uno.Disposables;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Uno.Extensions;
using Windows.Foundation.Metadata;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using System.Reflection.Emit;
using System.Reflection;
#endif


#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls
	{
		private static class TemplateParts
		{
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
			public const string PlaybackRateListView = nameof(PlaybackRateListView); // AppBarButton
			public const string PlaybackRateFlyout = nameof(PlaybackRateFlyout); // AppBarButton

			public const string LeftSeparator = nameof(LeftSeparator); // AppBarSeparator
			public const string RightSeparator = nameof(RightSeparator); // AppBarSeparator

			// Used by uno only:
			public const string RootGrid = nameof(RootGrid);
			public const string ControlPanel_ControlPanelVisibilityStates_Border = nameof(ControlPanel_ControlPanelVisibilityStates_Border);
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
	[TemplatePart(Name = "ControlPanel_ControlPanelVisibilityStates_Border", Type = typeof(Border))]


	[TemplatePart(Name = "RepeatButton", Type = typeof(Button))]
	[TemplatePart(Name = "VolumeFlyout", Type = typeof(Flyout))]
	[TemplatePart(Name = "PlaybackRateFlyout", Type = typeof(Flyout))]
	[TemplatePart(Name = "PlaybackRateListView", Type = typeof(ListView))]
	[TemplatePart(Name = "CompactOverlayButton", Type = typeof(Button))]
	[TemplatePart(Name = "MediaTransportControls_Timeline_Border", Type = typeof(Border))]
	//[TemplatePart(Name = "HorizontalThumb", Type = typeof(Grid))]

	public partial class MediaTransportControls : Control
	{
		#region Template Parts
		//
		// References to control parts we need to manipulate
		//
		private Grid? _rootGrid;
		private Border? _timelineContainer;
		private Border? _controlPanelBorder;
		private Thumb? _sliderThumb;

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

		// Reference to the PlayBack ListView of rates
		private ListView? m_tpPlaybackRateListView;

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
		//private DispatcherTimer m_tpPointerMoveEndTimer;

		// Reference to the Visibility Border element.
		//private Border m_tpControlPanelVisibilityBorder;

		// Reference to the CommandBar Element.
		private CommandBar? m_tpCommandBar;

		// Reference to the CommandBar Element.
		private FlyoutBase? m_tpVolumeFlyout;

		// Reference to the VisualStateGroup
		//private VisualStateGroup m_tpVisibilityStatesGroup;

		private Flyout? m_tpPlaybackRateFlyout;

		#endregion

		private MediaPlayerElement? _mpe;
		private readonly SerialDisposable _subscriptions = new();

		private bool _wasPlaying;
		private bool _isTemplateApplied;
		private bool _isShowingControls = true;
		private bool _isShowingControlVolumeOrPlaybackRate;

		public MediaTransportControls()
		{
			DefaultStyleKey = typeof(MediaTransportControls);

			m_tpHideControlPanelTimer = new() { Interval = TimeSpan.FromSeconds(3) };
		}
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			DeinitializeTransportControls();

			HookupPartsAndHandlers();
			_isTemplateApplied = true;

			if (IsLoaded)
			{
				BindToControlEvents();
				BindMediaPlayer();
			}

			UpdateAllVisualStates(useTransition: false);
			UpdateMediaControlAllStates(); // dependency-properties states
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			BindToControlEvents();
			BindMediaPlayer();
		}
		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			DeinitializeTransportControls();
		}
		internal override void OnLayoutUpdated()
		{
			base.OnLayoutUpdated();

			OnControlsBoundsChanged();
		}


		private void HookupPartsAndHandlers()
		{
			InitializeTemplateChild(TemplateParts.RootGrid, null, out _rootGrid);
			InitializeTemplateChild(TemplateParts.ControlPanelGrid, null, out m_tpControlPanelGrid);

			InitializeTemplateChild(TemplateParts.TimeElapsedElement, UIAKeys.UIA_MEDIA_TIME_ELAPSED, out m_tpTimeElapsedElement);
			InitializeTemplateChild(TemplateParts.TimeRemainingElement, UIAKeys.UIA_MEDIA_TIME_REMAINING, out m_tpTimeRemainingElement);
			if (InitializeTemplateChild(TemplateParts.ProgressSlider, UIAKeys.UIA_MEDIA_SEEK, out m_tpMediaPositionSlider))
			{
				m_tpDownloadProgressIndicator = m_tpMediaPositionSlider.GetTemplateChild<ProgressBar>(TemplateParts.DownloadProgressIndicator);
				_sliderThumb = m_tpMediaPositionSlider.GetTemplateChild<Thumb>(TemplateParts.HorizontalThumb);
			}
			InitializeTemplateChild(TemplateParts.PlayPauseButton, UIAKeys.UIA_MEDIA_PLAY, out m_tpPlayPauseButton);
			InitializeTemplateChild(TemplateParts.PlayPauseButtonOnLeft, UIAKeys.UIA_MEDIA_PLAY, out m_tpTHLeftSidePlayPauseButton);
			InitializeTemplateChild(TemplateParts.FullWindowButton, UIAKeys.UIA_MEDIA_FULLSCREEN, out m_tpFullWindowButton);
			InitializeTemplateChild(TemplateParts.ZoomButton, UIAKeys.UIA_MEDIA_ASPECTRATIO, out m_tpZoomButton);
			InitializeTemplateChild(TemplateParts.ErrorTextBlock, UIAKeys.UIA_MEDIA_ERROR, out m_tpErrorTextBlock);
			InitializeTemplateChild(TemplateParts.MediaControlsCommandBar, null, out m_tpCommandBar);
			InitializeTemplateChild(TemplateParts.VolumeFlyout, null, out m_tpVolumeFlyout);

			HookupVolumeAndProgressPartsAndHandlers();
			MoreControls();

			InitializeTemplateChild(TemplateParts.MediaTransportControls_Timeline_Border, null, out _timelineContainer);
			InitializeTemplateChild(TemplateParts.ControlPanel_ControlPanelVisibilityStates_Border, null, out _controlPanelBorder);
		}
		private void HookupVolumeAndProgressPartsAndHandlers()
		{
			InitializeTemplateChild(TemplateParts.HorizontalVolumeSlider, UIAKeys.UIA_MEDIA_VOLUME, out m_tpHorizontalVolumeSlider);
			InitializeTemplateChild(TemplateParts.VerticalVolumeSlider, UIAKeys.UIA_MEDIA_VOLUME, out m_tpVerticalVolumeSlider);
			if (InitializeTemplateChild(TemplateParts.VolumeSlider, UIAKeys.UIA_MEDIA_VOLUME, out m_tpTHVolumeSlider))
			{
				m_tpTHVolumeSlider.Maximum = 100;
				m_tpTHVolumeSlider.Value = 100;
			}
			InitializeTemplateChild(TemplateParts.AudioSelectionButton, UIAKeys.UIA_MEDIA_AUDIO_SELECTION, out m_tpAudioSelectionButton);
			InitializeTemplateChild(TemplateParts.AudioTracksSelectionButton, UIAKeys.UIA_MEDIA_AUDIO_SELECTION, out m_tpTHAudioTrackSelectionButton);
			if (InitializeTemplateChild(TemplateParts.AvailableAudioTracksMenuFlyout, null, out m_tpAvailableAudioTracksMenuFlyout))
			{
				m_tpAvailableAudioTracksMenuFlyout.ShouldConstrainToRootBounds = true;
			}
			InitializeTemplateChild(TemplateParts.AvailableAudioTracksMenuFlyoutTarget, null, out m_tpAudioSelectionButton);
			InitializeTemplateChild(TemplateParts.CCSelectionButton, UIAKeys.UIA_MEDIA_CC_SELECTION, out m_tpCCSelectionButton);
			InitializeTemplateChild(TemplateParts.PlaybackRateButton, UIAKeys.UIA_MEDIA_PLAYBACKRATE, out m_tpPlaybackRateButton);
			InitializeTemplateChild(TemplateParts.VolumeButton, UIAKeys.UIA_MEDIA_VOLUME, out m_tpVideoVolumeButton);
			InitializeTemplateChild(TemplateParts.AudioMuteButton, UIAKeys.UIA_MEDIA_MUTE, out m_tpMuteButton);
			InitializeTemplateChild(TemplateParts.VolumeMuteButton, UIAKeys.UIA_MEDIA_MUTE, out m_tpTHVolumeButton);
			InitializeTemplateChild(TemplateParts.BufferingProgressBar, UIAKeys.UIA_MEDIA_BUFFERING_PROGRESS, out m_tpBufferingProgressBar);
			InitializeTemplateChild(TemplateParts.FastForwardButton, UIAKeys.UIA_MEDIA_FASTFORWARD, out m_tpFastForwardButton);
			InitializeTemplateChild(TemplateParts.RewindButton, UIAKeys.UIA_MEDIA_REWIND, out m_tpFastRewindButton);
			InitializeTemplateChild(TemplateParts.StopButton, UIAKeys.UIA_MEDIA_STOP, out m_tpStopButton);
			InitializeTemplateChild(TemplateParts.CastButton, UIAKeys.UIA_MEDIA_CAST, out m_tpCastButton);
			InitializeTemplateChild(TemplateParts.PlaybackRateFlyout, UIAKeys.UIA_MEDIA_PLAYBACKRATE, out m_tpPlaybackRateFlyout);
			InitializePlaybackRateListView();
		}

		private void MoreControls()
		{
			InitializeTemplateChild(TemplateParts.SkipForwardButton, UIAKeys.UIA_MEDIA_SKIPFORWARD, out m_tpSkipForwardButton);
			InitializeTemplateChild(TemplateParts.SkipBackwardButton, UIAKeys.UIA_MEDIA_SKIPBACKWARD, out m_tpSkipBackwardButton);
			InitializeTemplateChild(TemplateParts.NextTrackButton, UIAKeys.UIA_MEDIA_NEXTRACK, out m_tpNextTrackButton);
			InitializeTemplateChild(TemplateParts.PreviousTrackButton, UIAKeys.UIA_MEDIA_NEXTRACK, out m_tpPreviousTrackButton);
			InitializeTemplateChild(TemplateParts.RepeatButton, UIAKeys.UIA_MEDIA_REPEAT_NONE, out m_tpRepeatButton);
			InitializeTemplateChild(TemplateParts.CompactOverlayButton, UIAKeys.UIA_MEDIA_MINIVIEW, out m_tpCompactOverlayButton);
			InitializeTemplateChild(TemplateParts.LeftSeparator, null, out m_tpLeftAppBarSeparator);
			InitializeTemplateChild(TemplateParts.RightSeparator, null, out m_tpRightAppBarSeparator);
		}
		private void BindToControlEvents()
		{
			if (!_isTemplateApplied)
			{
				return;
			}

			var disposables = new CompositeDisposable();
			_subscriptions.Disposable = disposables;

			Bind(m_tpHideControlPanelTimer, x => x.Tick += ControlsVisibilityTimerElapsed, x => x.Tick -= ControlsVisibilityTimerElapsed);

			BindTapped(_rootGrid, OnRootGridTapped);
			Bind(_rootGrid, x => x.PointerMoved += OnRootGridPointerMoved, x => x.PointerMoved -= OnRootGridPointerMoved);
			BindLoaded(m_tpCommandBar, OnCommandBarLoaded, invokeHandlerIfAlreadyLoaded: true);
			BindSizeChanged(m_tpControlPanelGrid, ControlPanelGridSizeChanged);
			BindTapped(m_tpControlPanelGrid, OnPaneGridTapped);
			BindSizeChanged(_controlPanelBorder, ControlPanelBorderSizeChanged);
			BindTapped(m_tpMediaPositionSlider, TappedProgressSlider);
			Bind(_sliderThumb, x => x.DragStarted += ThumbOnDragStarted, x => x.DragStarted -= ThumbOnDragStarted);
			Bind(_sliderThumb, x => x.DragCompleted += ThumbOnDragCompleted, x => x.DragCompleted -= ThumbOnDragCompleted);

			BindButtonClick(m_tpTHLeftSidePlayPauseButton, PlayPause);
			BindButtonClick(m_tpMuteButton, ToggleMute);
			Bind(m_tpTHVolumeSlider, x => x.ValueChanged += OnVolumeChanged, x => x.ValueChanged -= OnVolumeChanged);
			// MediaControlsCommandBar\PrimaryCommands
			//BindButtonClick(m_tpCCSelectionButton, null);
			//BindButtonClick(m_tpTHAudioTrackSelectionButton, null);
			// - LeftSeparator
			BindButtonClick(m_tpStopButton, Stop);
			BindButtonClick(m_tpSkipBackwardButton, SkipBackward);
			BindButtonClick(m_tpPreviousTrackButton, PreviousTrackButtonTapped);
			BindButtonClick(m_tpFastRewindButton, RewindButton);
			BindButtonClick(m_tpPlayPauseButton, PlayPause);
			BindButtonClick(m_tpFastForwardButton, ForwardButton);
			BindButtonClick(m_tpNextTrackButton, NextTrackButtonTapped);
			BindButtonClick(m_tpSkipForwardButton, SkipForward);
			BindButtonClick(m_tpPlaybackRateButton, ResetControlsVisibilityTimerAndVolumeOrPlaybackVisibility);
			BindButtonClick(m_tpTHVolumeButton, ResetControlsVisibilityTimerAndVolumeOrPlaybackVisibility);

			Bind(m_tpTHVolumeSlider, x => x.ValueChanged += ResetVolumeOrPlaybackVisibility, x => x.ValueChanged -= ResetVolumeOrPlaybackVisibility);
			Bind(m_tpTHVolumeSlider, x => x.PointerExited += ResetControlsVisibilityTimerAndVolumeOrPlaybackVisibility, x => x.PointerExited -= ResetControlsVisibilityTimerAndVolumeOrPlaybackVisibility);
			Bind(m_tpTHVolumeSlider, x => x.PointerEntered += CancelControlsVisibilityTimerAndVolumeOrPlaybackVisibility, x => x.PointerEntered -= CancelControlsVisibilityTimerAndVolumeOrPlaybackVisibility);

			Bind(m_tpPlaybackRateListView, x => x.SelectionChanged += PlaybackRateListView_SelectionChanged, x => x.SelectionChanged -= PlaybackRateListView_SelectionChanged);
			Bind(m_tpPlaybackRateListView, x => x.PointerExited += ResetControlsVisibilityTimerAndVolumeOrPlaybackVisibility, x => x.PointerExited -= ResetControlsVisibilityTimerAndVolumeOrPlaybackVisibility);
			Bind(m_tpPlaybackRateListView, x => x.PointerEntered += CancelControlsVisibilityTimerAndVolumeOrPlaybackVisibility, x => x.PointerEntered -= CancelControlsVisibilityTimerAndVolumeOrPlaybackVisibility);

			// - RightSeparator
			BindButtonClick(m_tpRepeatButton, RepeatButtonTapped);
			BindButtonClick(m_tpZoomButton, ZoomButtonTapped);
			//BindButtonClick(m_tpCastButton, null);
			BindButtonClick(m_tpCompactOverlayButton, UpdateCompactOverlayMode);
			BindButtonClick(m_tpFullWindowButton, FullWindowButtonTapped);

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
								disposables.Add(() => child.Completed -= Storyboard_Completed);
							}
						}
					}
				}
			}

			void BindTapped(UIElement? target, TappedEventHandler handler)
			{
				if (target is { })
				{
					target.Tapped += handler;
					disposables.Add(() => target.Tapped -= handler);
				}
			}
			void BindLoaded(FrameworkElement? target, RoutedEventHandler handler, bool invokeHandlerIfAlreadyLoaded = false)
			{
				if (target is { })
				{
					// register before invoking so that fire-once handler can self-unsubscribed

					target.Loaded += handler;
					disposables.Add(() => target.Loaded -= handler);

					if (invokeHandlerIfAlreadyLoaded && target.IsLoaded)
					{
						handler.Invoke(target, default);
					}
				}
			}
			void BindSizeChanged(FrameworkElement? target, SizeChangedEventHandler handler)
			{
				if (target is { })
				{
					target.SizeChanged += handler;
					disposables.Add(() => target.SizeChanged -= handler);
				}
			}
			void BindButtonClick(ButtonBase? target, RoutedEventHandler handler)
			{
				if (target is { })
				{
					target.Click += handler;
					disposables.Add(() => target.Click -= handler);
				}
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
		private void DeinitializeTransportControls()
		{
			_subscriptions.Disposable = null;
			_mediaPlayerSubscriptions.Disposable = null;
		}

		private void ControlsVisibilityTimerElapsed(object? sender, object args)
		{
			m_tpHideControlPanelTimer?.Stop();

			if (ShowAndHideAutomatically && !_isShowingControlVolumeOrPlaybackRate)
			{
				Hide();
			}
		}

		private void ResetControlsVisibilityTimer()
		{
			if (ShowAndHideAutomatically && m_tpHideControlPanelTimer is not null)
			{
				m_tpHideControlPanelTimer.Stop();
				m_tpHideControlPanelTimer.Start();
			}
		}

		private void CancelControlsVisibilityTimer()
		{
			Show();
			m_tpHideControlPanelTimer?.Stop();
		}

		private void OnCommandBarLoaded(object? sender, RoutedEventArgs e)
		{
			if (m_tpCommandBar is not null)
			{
				m_tpCommandBar.Loaded -= OnCommandBarLoaded;
			}

			HideMoreButtonIfNecessary();
			HideCastButtonIfNecessary();
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

		private void Storyboard_Completed(object? sender, object e) => OnControlsBoundsChanged();

		public void Show()
		{
			_isShowingControls = true;

			_ = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				UpdateControlPanelVisibilityStates(useTransition: false);
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
				if (_mediaPlayer is { PlaybackSession.IsPlaying: true })
				{
					UpdateControlPanelVisibilityStates(useTransition: false);
				}
				if (m_tpPlaybackRateButton is { }
					&& m_tpPlaybackRateFlyout is { }
					&& m_tpVolumeFlyout is { })
				{
					if (m_tpPlaybackRateButton is AppBarButton playbackRateAppBarButton)
					{
						playbackRateAppBarButton.Flyout.Hide();
					}
					else
					{
						m_tpPlaybackRateFlyout.Hide();
					}
					m_tpVolumeFlyout.Hide();
				}
			});
		}

		private void OnControlsBoundsChanged()
		{
			if (m_tpControlPanelGrid is { } &&
				_mediaPlayer is { } &&
				XamlRoot?.Content is UIElement root)
			{
				var bounds = new Rect(
					0,
					0,
					m_tpControlPanelGrid.ActualWidth,
					_isShowingControls ? m_tpControlPanelGrid.ActualHeight : 0
				);
				var transportBounds = TransformToVisual(root).TransformBounds(bounds);

				_mediaPlayer.SetTransportControlBounds(transportBounds);
			}
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
			if (_isShowingControlVolumeOrPlaybackRate)
			{
				_isShowingControlVolumeOrPlaybackRate = false;
				if (ShowAndHideAutomatically)
				{
					ResetControlsVisibilityTimer();
				}
			}
			if (e.PointerDeviceType == PointerDeviceType.Touch)
			{
				if (_isShowingControls)
				{
					m_tpHideControlPanelTimer?.Stop();
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
			if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
			{
				Show();
			}
		}

		private void ControlPanelGridSizeChanged(object sender, SizeChangedEventArgs args)
		{
			OnControlsBoundsChanged();
		}

		private static void ControlPanelBorderSizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (sender is Border border)
			{
				border.Clip = new RectangleGeometry
				{
					Rect = new Rect(default, args.NewSize)
				};
			}
		}

		private void FullWindowButtonTapped(object sender, RoutedEventArgs e)
		{
			if (_mpe is not null)
			{
				_mpe.IsFullWindow = !_mpe.IsFullWindow;
			}

			UpdateFullWindowStates();
		}

		private void PlaybackRateListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is ListView listView)
			{
				if (listView.SelectedItem is not null)
				{
					if (listView.SelectedItem is Windows.UI.Xaml.Controls.ListViewItem item)
					{
						_isShowingControlVolumeOrPlaybackRate = false;
						ResetControlsVisibilityTimer();
						if (m_tpPlaybackRateButton is { }
							&& m_tpPlaybackRateFlyout is { })
						{
							if (m_tpPlaybackRateButton is AppBarButton playbackRateAppBarButton)
							{
								playbackRateAppBarButton.Flyout.Hide();
							}
							else
							{
								m_tpPlaybackRateFlyout.Hide();
							}
							if (_mpe is not null && _mpe.MediaPlayer is not null)
							{
								_mpe.MediaPlayer.PlaybackRate = double.Parse(item.Content + "", CultureInfo.InvariantCulture);
							}
						}
					}
				}
			}
		}

		private void ResetControlsVisibilityTimerAndVolumeOrPlaybackVisibility(object sender, RoutedEventArgs e)
		{
			_isShowingControlVolumeOrPlaybackRate = false;
			ResetControlsVisibilityTimer();
		}

		private void ResetVolumeOrPlaybackVisibility(object sender, RangeBaseValueChangedEventArgs e)
		{
			_isShowingControlVolumeOrPlaybackRate = false;
		}

		private void CancelControlsVisibilityTimerAndVolumeOrPlaybackVisibility(object sender, PointerRoutedEventArgs e)
		{
			_isShowingControlVolumeOrPlaybackRate = true;
			CancelControlsVisibilityTimer();
		}

		private void RepeatButtonTapped(object sender, RoutedEventArgs e)
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
			if (_mediaPlayer is not null)
			{
				_mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
				_mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
			}
		}

		private void NextTrackButtonTapped(object sender, RoutedEventArgs e)
		{
			if (_mediaPlayer is not null)
			{
				_mediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.NaturalDuration;
				_mediaPlayer.PlaybackSession.Position = _mediaPlayer.PlaybackSession.NaturalDuration;
			}
		}
		private void ZoomButtonTapped(object sender, RoutedEventArgs e)
		{
			if (_mpe is not null)
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
		}

		private void UpdateCompactOverlayMode(object sender, RoutedEventArgs e)
		{
			IsCompact = !IsCompact;

			if (_mpe is not null)
			{
				_mpe.ToggleCompactOverlay(IsCompact);
			}
		}


		// properties changed handlers
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
					UpdateMediaTransportControlModeStates();
					OnControlsBoundsChanged();
					break;
				case var _ when property == ShowAndHideAutomaticallyProperty:
					OnShowAndHideAutomaticallyChanged();
					break;

				case var _ when property == IsRepeatButtonVisibleProperty:
					BindVisibility(m_tpRepeatButton, IsImplemented(typeof(MediaPlayer), "IsLoopingEnabled") && IsRepeatButtonVisible, property);
					break;
				case var _ when property == IsRepeatEnabledProperty:
					BindIsEnabled(m_tpRepeatButton, IsRepeatEnabled);
					break;
				case var _ when property == IsVolumeButtonVisibleProperty:
					BindVisibility(m_tpTHVolumeButton, IsImplemented(typeof(MediaPlayer), "Volume") && IsVolumeButtonVisible, property);
					break;
				case var _ when property == IsVolumeEnabledProperty:
					BindIsEnabled(m_tpTHVolumeButton, IsVolumeEnabled);
					break;
				case var _ when property == IsFullWindowButtonVisibleProperty:
					BindVisibility(m_tpFullWindowButton, IsFullWindowButtonVisible, property);
					break;
				case var _ when property == IsFullWindowEnabledProperty:
					BindIsEnabled(m_tpFullWindowButton, IsFullWindowEnabled);
					break;
				case var _ when property == IsZoomButtonVisibleProperty:
					BindVisibility(m_tpZoomButton, IsZoomButtonVisible, property);
					break;
				case var _ when property == IsZoomEnabledProperty:
					BindIsEnabled(m_tpZoomButton, IsZoomEnabled);
					break;
				case var _ when property == IsPlaybackRateButtonVisibleProperty:
					BindVisibility(m_tpPlaybackRateButton, IsImplemented(typeof(MediaPlayer), "PlaybackRate") && IsPlaybackRateButtonVisible, property);
					break;
				case var _ when property == IsPlaybackRateEnabledProperty:
					BindIsEnabled(m_tpPlaybackRateButton, IsPlaybackRateEnabled);
					break;
				case var _ when property == IsFastForwardButtonVisibleProperty:
					BindVisibility(m_tpFastForwardButton, IsFastForwardButtonVisible, property);
					break;
				case var _ when property == IsFastForwardEnabledProperty:
					BindIsEnabled(m_tpFastForwardButton, IsFastForwardEnabled);
					break;
				case var _ when property == IsFastRewindButtonVisibleProperty:
					BindVisibility(m_tpFastRewindButton, IsFastRewindButtonVisible, property);
					break;
				case var _ when property == IsFastRewindEnabledProperty:
					BindIsEnabled(m_tpFastRewindButton, IsFastRewindEnabled);
					break;
				case var _ when property == IsStopButtonVisibleProperty:
					BindVisibility(m_tpStopButton, IsStopButtonVisible, property);
					break;
				case var _ when property == IsStopEnabledProperty:
					BindIsEnabled(m_tpStopButton, IsStopEnabled);
					break;
				case var _ when property == IsSeekBarVisibleProperty:
					BindVisibility(_timelineContainer, IsSeekBarVisible, property);
					break;
				case var _ when property == IsSeekEnabledProperty:
					BindIsEnabled(_timelineContainer, IsSeekEnabled);
					break;
				case var _ when property == IsSkipForwardButtonVisibleProperty:
					BindVisibility(m_tpSkipForwardButton, IsSkipForwardButtonVisible, property);
					break;
				case var _ when property == IsSkipForwardEnabledProperty:
					BindIsEnabled(m_tpSkipForwardButton, IsSkipForwardEnabled);
					break;
				case var _ when property == IsSkipBackwardButtonVisibleProperty:
					BindVisibility(m_tpSkipBackwardButton, IsSkipBackwardButtonVisible, property);
					break;
				case var _ when property == IsSkipBackwardEnabledProperty:
					BindIsEnabled(m_tpSkipBackwardButton, IsSkipBackwardEnabled);
					break;
				case var _ when property == IsNextTrackButtonVisibleProperty:
					BindVisibility(m_tpNextTrackButton, IsNextTrackButtonVisible, property);
					break;
				case var _ when property == IsPreviousTrackButtonVisibleProperty:
					BindVisibility(m_tpPreviousTrackButton, IsPreviousTrackButtonVisible, property);
					break;
				case var _ when property == IsCompactOverlayButtonVisibleProperty:
					BindVisibility(m_tpCompactOverlayButton, IsCompactOverlayButtonVisible, property);
					break;
				case var _ when property == IsCompactOverlayEnabledProperty:
					BindIsEnabled(m_tpCompactOverlayButton, IsCompactOverlayEnabled);
					break;
			}

			void BindVisibility(FrameworkElement? target, bool value, DependencyProperty property)
			{
				if (target is { })
				{
					if (!IsImplemented(typeof(MediaTransportControls), property.Name))
					{
						target.Visibility = Visibility.Collapsed;
						return;
					}
					target.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
				}
			}
			void BindIsEnabled(FrameworkElement? target, bool value)
			{
				if (target is { })
				{
					target.IsEnabled = value;
				}
			}
		}
		private void OnShowAndHideAutomaticallyChanged()
		{
			if (ShowAndHideAutomatically)
			{
				ResetControlsVisibilityTimer();
			}
			else
			{
				CancelControlsVisibilityTimer();
			}
		}

		private bool IsImplemented(Type type, string property)
		{
			return ApiInformation.IsPropertyPresent(type.FullName, property);
		}

		// visual states
		private void UpdateAllVisualStates(bool useTransition = true)
		{
			// all visual states are listed below: // unused/not-implemented ones are commented out
			UpdateControlPanelVisibilityStates(useTransition);
			UpdateMediaStates(useTransition);
			//UpdateAudioSelectionAvailablityStates(useTransition);
			//UpdateCCSelectionAvailablityStates(useTransition);
			//UpdateFocusStates(useTransition);
			UpdateMediaTransportControlModeStates(useTransition);
			UpdatePlayPauseStates(useTransition);
			UpdateVolumeMuteStates(useTransition);
			UpdateFullWindowStates(useTransition);
			UpdateRepeatStates(useTransition);
		}
		private void UpdateControlPanelVisibilityStates(bool useTransition = true)
		{
			var state = _isShowingControls
				? VisualState.ControlPanelVisibilityStates.ControlPanelFadeIn
				: VisualState.ControlPanelVisibilityStates.ControlPanelFadeOut;
			VisualStateManager.GoToState(this, state, useTransition);
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
		private void UpdateMediaTransportControlModeStates(bool useTransition = true)
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
			// While the volume is at 0, we can click on the mute button (indicated by isExplicitMuteToggle=true)
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
			if (_mpe is not null)
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

		// helper methods
		private bool InitializeTemplateChild<T>(
			string childName,
			string? uiaKey,
			[NotNullWhen(true)] out T? child) where T : class, DependencyObject
		{
			child = GetTemplateChild<T>(childName);
			if (child is { } && uiaKey is { })
			{
				SetAutomationNameAndTooltip(child, uiaKey);
			}

			return child != null;
		}

		private void InitializePlaybackRateListView()
		{

			if (m_tpPlaybackRateButton is AppBarButton playbackRateAppBarButton)
			{
				m_tpPlaybackRateListView = new ListView();
				m_tpPlaybackRateListView.VerticalAlignment = VerticalAlignment.Top;
				m_tpPlaybackRateListView.HorizontalAlignment = HorizontalAlignment.Center;
				m_tpPlaybackRateListView.Margin = new Thickness(0);
				m_tpPlaybackRateListView.Items.AddRange(new List<ListViewItem>() {
													new() { Content = "0.25" },
													new() { Content = "0.5" },
													new() { Content = "1" },
													new() { Content = "1.5" },
													new() { Content = "2" }});
				m_tpPlaybackRateFlyout = new Flyout();
				m_tpPlaybackRateFlyout.FlyoutPresenterStyle = (Style)Application.Current.Resources["FlyoutStyle"];
				m_tpPlaybackRateFlyout.ShouldConstrainToRootBounds = false;
				m_tpPlaybackRateFlyout.Content = m_tpPlaybackRateListView;

				playbackRateAppBarButton.Flyout = m_tpPlaybackRateFlyout;
			}
			else
			{
				InitializeTemplateChild(TemplateParts.PlaybackRateListView, UIAKeys.UIA_MEDIA_PLAYBACKRATE, out m_tpPlaybackRateListView);
			}
			if (m_tpPlaybackRateFlyout is { })
			{
#if __SKIA__
				m_tpPlaybackRateFlyout.Placement = FlyoutPlacementMode.RightEdgeAlignedTop;
#else
				m_tpPlaybackRateFlyout.Placement = FlyoutPlacementMode.Top;
#endif
			}
		}

		private void SetAutomationNameAndTooltip(DependencyObject? target, string uiaKey)
		{
			if (target is { })
			{
				var value = DXamlCore.Current.GetLocalizedResourceString(uiaKey);
				AutomationProperties.SetName(target, value);
				ToolTipService.SetToolTip(target, value);
			}
		}
	}
}

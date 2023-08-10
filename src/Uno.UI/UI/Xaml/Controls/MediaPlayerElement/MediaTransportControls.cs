#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using DirectUI;
using Uno.Disposables;
using Uno.Extensions;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using Uno.UI.Xaml.Core;
#endif

#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif

using _MediaPlayer = Windows.Media.Playback.MediaPlayer; // alias to avoid same name root namespace from ios/macos

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls : Control
	{
		private MediaPlayerElement? _mpe;
		private readonly SerialDisposable _subscriptions = new();

#pragma warning disable CS0649
		private bool m_transportControlsEnabled = true; // not-implemented
		private bool m_controlPanelIsVisible;
		private bool m_shouldDismissControlPanel;

		private bool m_isPointerMove;
		private bool m_controlPanelHasPointerOver;
		private bool m_rootHasPointerPressed;
		private bool m_isFlyoutOpen;
		private bool m_isInScrubMode;
		//private bool m_isthruScrubber;
		private bool m_positionUpdateUIOnly; // If true, update the Position slider value only - do not set underlying ME.Position DP (used to differentiate position update from user vs video playing)

		private bool m_sourceLoaded;
		private bool m_isPlaying;
		private bool m_isBuffering;
		private double m_currentPlaybackRate;
#pragma warning restore CS0649

		private bool _wasPlaying;
		private bool _isTemplateApplied; // indicates if the template parts have been resolved
		private double? _isVolumeRewindRequestedAndAudioIsPlaying; // indicate the volume when the Play/Pause/Stop/Forward need to restart the audio
		private bool _isRewindForewardRequested; // indicates that the Pause need to pause the playback and not the video

		public MediaTransportControls()
		{
			DefaultStyleKey = typeof(MediaTransportControls);

			m_tpHideControlPanelTimer = new() { Interval = TimeSpan.FromSeconds(ControlPanelDisplayTimeoutInSecs) };
			m_tpPointerMoveEndTimer = new()
			{
#if HAS_UNO
				Interval = TimeSpan.FromSeconds(0.250), // throttle frequency at which this get spammed
#else
				Interval = TimeSpan.Zero
#endif
			};
		}
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			// Detach any existing handlers
			DeinitializeTransportControls();

			HookupPartsAndHandlers();
			_isTemplateApplied = true;

			if (IsLoaded)
			{
				// uno-specific: event subcriptions are extracted out from HookupPartsAndHandlers() to ensure proper disposal
				BindToControlEvents();
				BindMediaPlayer(updateAllVisualAndPropertyStates: false);
			}

			// Initialize the visual state
			InitializeVisualState();

			// Update MediaControl States (dependency-properties states)
			UpdateMediaControlAllStates();
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

		// template child setup
		private void HookupPartsAndHandlers()
		{
			InitializeTemplateChild(TemplateParts.RootGrid, null, out _rootGrid);
			InitializeTemplateChild(TemplateParts.ControlPanelGrid, null, out m_tpControlPanelGrid);

			InitializeTemplateChild(TemplateParts.TimeElapsedElement, UIAKeys.UIA_MEDIA_TIME_ELAPSED, out m_tpTimeElapsedElement);
			InitializeTemplateChild(TemplateParts.TimeRemainingElement, UIAKeys.UIA_MEDIA_TIME_REMAINING, out m_tpTimeRemainingElement);
			if (InitializeTemplateChild(TemplateParts.ProgressSlider, UIAKeys.UIA_MEDIA_SEEK, out m_tpMediaPositionSlider))
			{
				m_tpDownloadProgressIndicator = m_tpMediaPositionSlider.GetTemplateChild<ProgressBar>(TemplateParts.DownloadProgressIndicator);
				_progressSliderThumb = m_tpMediaPositionSlider.GetTemplateChild<Thumb>(TemplateParts.HorizontalThumb);
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

			// uno specifics
			InitializeTemplateChild(TemplateParts.MediaTransportControls_Timeline_Border, null, out _timelineContainer);
			InitializeTemplateChild(TemplateParts.ControlPanel_ControlPanelVisibilityStates_Border, null, out _controlPanelBorder);
			InitializeTemplateChild(TemplateParts.PlaybackRateFlyout, UIAKeys.UIA_MEDIA_PLAYBACKRATE, out _playbackRateFlyout);
			InitializePlaybackRateListView();
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
		}
		private void MoreControls()
		{
			InitializeTemplateChild(TemplateParts.SkipForwardButton, UIAKeys.UIA_MEDIA_SKIPFORWARD, out m_tpSkipForwardButton);
			InitializeTemplateChild(TemplateParts.SkipBackwardButton, UIAKeys.UIA_MEDIA_SKIPBACKWARD, out m_tpSkipBackwardButton);
			InitializeTemplateChild(TemplateParts.NextTrackButton, UIAKeys.UIA_MEDIA_NEXTRACK, out m_tpNextTrackButton);
			InitializeTemplateChild(TemplateParts.PreviousTrackButton, UIAKeys.UIA_MEDIA_PREVIOUSTRACK, out m_tpPreviousTrackButton);
			InitializeTemplateChild(TemplateParts.RepeatButton, UIAKeys.UIA_MEDIA_REPEAT_NONE, out m_tpRepeatButton);
			InitializeTemplateChild(TemplateParts.CompactOverlayButton, UIAKeys.UIA_MEDIA_MINIVIEW, out m_tpCompactOverlayButton);
			InitializeTemplateChild(TemplateParts.LeftSeparator, null, out m_tpLeftAppBarSeparator);
			InitializeTemplateChild(TemplateParts.RightSeparator, null, out m_tpRightAppBarSeparator);
		}
		private void InitializePlaybackRateListView()
		{
			if (m_tpPlaybackRateButton is AppBarButton)
			{
				_playbackRateListView = new ListView();
				_playbackRateListView.HorizontalAlignment = HorizontalAlignment.Center;
				_playbackRateListView.VerticalAlignment = VerticalAlignment.Top;
				_playbackRateListView.Margin = new Thickness(0);
				_playbackRateListView.Items.AddRange(AvailablePlaybackRateList.Select(x => new ListViewItem { Content = $"{x:0.##}" }));

				_playbackRateFlyout = new Flyout();
				_playbackRateFlyout.FlyoutPresenterStyle = (Style)Application.Current.Resources["FlyoutStyle"];
				_playbackRateFlyout.ShouldConstrainToRootBounds = false;
				_playbackRateFlyout.Content = _playbackRateListView;

				m_tpPlaybackRateButton.Flyout = _playbackRateFlyout;
			}
			else
			{
				InitializeTemplateChild(TemplateParts.PlaybackRateListView, UIAKeys.UIA_MEDIA_PLAYBACKRATE, out _playbackRateListView);
			}
			if (_playbackRateFlyout is { })
			{
#if __SKIA__
				_playbackRateFlyout.Placement = FlyoutPlacementMode.RightEdgeAlignedTop;
#else
				_playbackRateFlyout.Placement = FlyoutPlacementMode.Top;
#endif
			}
		}

		// events un/subscription
		private void BindToControlEvents()
		{
			if (!_isTemplateApplied)
			{
				return;
			}

			var disposables = new CompositeDisposable();
			_subscriptions.Disposable = disposables;

			Bind(m_tpHideControlPanelTimer, x => x.Tick += OnHideControlPanelTimerTick, x => x.Tick -= OnHideControlPanelTimerTick);
			Bind(m_tpPointerMoveEndTimer, x => x.Tick += OnPointerMoveEndTimerTick, x => x.Tick -= OnPointerMoveEndTimerTick);

			Bind(this, x => x.PointerExited += OnRootExited, x => x.PointerExited -= OnRootExited);
			Bind(this, x => x.PointerPressed += OnRootPressed, x => x.PointerPressed -= OnRootPressed);
			Bind(this, x => x.PointerReleased += OnRootReleased, x => x.PointerReleased -= OnRootReleased);
			Bind(this, x => x.PointerCaptureLost += OnRootCaptureLost, x => x.PointerCaptureLost -= OnRootCaptureLost);
			Bind(this, x => x.PointerMoved += OnRootMoved, x => x.PointerMoved -= OnRootMoved);

			BindLoaded(m_tpCommandBar, OnCommandBarLoaded, invokeHandlerIfAlreadyLoaded: true);
			BindSizeChanged(m_tpControlPanelGrid, ControlPanelGridSizeChanged);
			Bind(m_tpControlPanelGrid, x => x.PointerEntered += OnControlPanelEntered, x => x.PointerExited -= OnControlPanelEntered);
			Bind(m_tpControlPanelGrid, x => x.PointerExited += OnControlPanelExited, x => x.PointerExited -= OnControlPanelExited);
			Bind(m_tpControlPanelGrid, x => x.PointerCaptureLost += OnControlPanelCaptureLost, x => x.PointerCaptureLost -= OnControlPanelCaptureLost);
#if !HAS_UNO
			Bind(m_tpControlPanelGrid, x => x.GotFocus += OnControlPanelGotFocus, x => x.GotFocus -= OnControlPanelGotFocus);
			Bind(m_tpControlPanelGrid, x => x.LostFocus += OnControlPanelLostFocus, x => x.LostFocus -= OnControlPanelLostFocus);
#endif
			BindSizeChanged(_controlPanelBorder, ControlPanelBorderSizeChanged);

			// Interactive parts of MTC, but outside of MediaControlsCommandBar:
			Bind(m_tpMediaPositionSlider, x => x.ValueChanged += OnMediaPositionSliderValueChanged, x => x.ValueChanged -= OnMediaPositionSliderValueChanged);
			Bind(_progressSliderThumb, x => x.DragCompleted += ThumbOnDragCompleted, x => x.DragCompleted -= ThumbOnDragCompleted);
			Bind(_progressSliderThumb, x => x.DragStarted += ThumbOnDragStarted, x => x.DragStarted -= ThumbOnDragStarted);
			BindButtonClick(m_tpTHLeftSidePlayPauseButton, PlayPause);

			// MediaControlsCommandBar\PrimaryCommands:
#if !HAS_UNO
			BindButtonClick(m_tpCCSelectionButton, null);
			BindButtonClick(m_tpTHAudioTrackSelectionButton, null);
#endif
			// --- LeftSeparator ---
			BindButtonClick(m_tpStopButton, Stop);
			BindButtonClick(m_tpSkipBackwardButton, SkipBackward);
			BindButtonClick(m_tpPreviousTrackButton, PreviousTrackButtonTapped);
			BindButtonClick(m_tpFastRewindButton, RewindButton);
			BindButtonClick(m_tpPlayPauseButton, PlayPause);
			BindButtonClick(m_tpFastForwardButton, ForwardButton);
			BindButtonClick(m_tpNextTrackButton, NextTrackButtonTapped);
			BindButtonClick(m_tpSkipForwardButton, SkipForward);
			// --- RightSeparator ---
			BindButtonClick(m_tpRepeatButton, RepeatButtonTapped);
			BindButtonClick(m_tpZoomButton, ZoomButtonTapped);
#if !HAS_UNO
			BindButtonClick(m_tpCastButton, null);
#endif
			BindButtonClick(m_tpCompactOverlayButton, UpdateCompactOverlayMode);
			BindButtonClick(m_tpFullWindowButton, FullWindowButtonTapped);

			// Flyout nested:
			BindFlyout(m_tpVolumeFlyout, OnFlyoutOpened, OnFlyoutClosed);
			BindButtonClick(m_tpMuteButton, ToggleMute);
			Bind(m_tpTHVolumeSlider, x => x.ValueChanged += OnVolumeChanged, x => x.ValueChanged -= OnVolumeChanged);
			BindFlyout(_playbackRateFlyout, OnFlyoutOpened, OnFlyoutClosed);
			Bind(_playbackRateListView, x => x.SelectionChanged += PlaybackRateListView_SelectionChanged, x => x.SelectionChanged -= PlaybackRateListView_SelectionChanged);

			// Register on visual state changes to update the layout in extensions
			foreach (var groups in VisualStateManager.GetVisualStateGroups(this.GetTemplateRoot()))
			{
				foreach (var state in groups.States)
				{
					if (state.Name is VisualState.ControlPanelVisibilityStates.ControlPanelFadeOut)
					{
						foreach (var child in state.Storyboard.Children)
						{
							// Update the layout on opacity completed
							if (child.PropertyInfo?.LeafPropertyName == nameof(UIElement.Opacity))
							{
								child.Completed += Storyboard_Completed;
								disposables.Add(() => child.Completed -= Storyboard_Completed);
							}
						}
					}
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
			void BindFlyout(FlyoutBase? target, EventHandler<object> openedHandler, EventHandler<object> closedHandler)
			{
				if (target is { })
				{
					target.Opened += openedHandler;
					target.Closed += closedHandler;
					disposables.Add(() =>
					{
						target.Opened -= openedHandler;
						target.Closed -= closedHandler;
					});
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

		/* ShowAndHideAutomatically mechanism: // note: these are based from observation for reference only, and should not be viewed as the truth
			- while playing & ShowAndHideAutomatically, the MTC will auto-hide after a set time. (see: m_tpHideControlPanelTimer.Interval)
				^ performing any action will reset this timer. [mobile: ignoring PointerEnter/Exit]
				^ having any flyout opened will disable this auto-hide.
			- while MTC is hidden, setting ShowAndHideAutomatically=true will show the MTC immediately.
			- while playing & MTC is visible, setting ShowAndHideAutomatically=true will begin the timer.
			- tapping the video will shows the MTC.
			[desktop-specific]:
			- if ShowAndHideAutomatically, PointerEnter/Moved in the video will auto-shows the MTC and begin/reset the timer.
				^ presumably, clicking any button should also reset the timer, but this is already encompassed in the above rule.
				^ hovering over the button flyouts will also disable auto-hide.
			- if ShowAndHideAutomatically, while the cursor is on MTC, auto-hide is disabled.
			[mobile-specific]: // PointerEnter/Exit are obviously not well-supported here
		 */

		private void OnHideControlPanelTimerTick(object? sender, object args)
		{
			m_tpHideControlPanelTimer?.Stop();

			if (IsInLiveTree)
			{
				HideControlPanel();
			}
		}
		private void OnPointerMoveEndTimerTick(object? sender, object args)
		{
			m_isPointerMove = false;
			if (m_tpPointerMoveEndTimer is { })
			{
				m_tpPointerMoveEndTimer.Stop();
			}
			StartControlPanelHideTimer();
		}
		private void ResetControlsVisibilityTimer()
		{
			if (m_tpHideControlPanelTimer is { } &&
				ShowAndHideAutomatically)
			{
				m_tpHideControlPanelTimer.Stop();
				m_tpHideControlPanelTimer.Start();
			}
		}
		private void StartControlPanelHideTimer()
		{
			if (m_transportControlsEnabled)
			{
				if (m_tpHideControlPanelTimer is { } &&
					ShouldHideControlPanel())
				{
					m_tpHideControlPanelTimer.Start();
				}
			}
		}
		private void StopControlPanelHideTimer()
		{
			if (m_transportControlsEnabled)
			{
				if (m_tpHideControlPanelTimer is { })
				{
					m_tpHideControlPanelTimer?.Stop();
				}
			}
		}
		private void ShowControlPanel()
		{
			if (m_transportControlsEnabled)
			{
				if (!m_controlPanelIsVisible)
				{
					m_controlPanelIsVisible = true;
#if !HAS_UNO
					if (!m_isVSStateChangeExternal) // Skip if Visual State already happen through external
					{
						m_controlPanelVisibilityChanged = TRUE;
					}
#endif
				}

#if !HAS_UNO
				ShowControlPanelFromMPE();

				// Resume position updates now that CP is visible
				StartPositionUpdateTimer();
#endif

				// Immediately start the timer to hide control panel
				StartControlPanelHideTimer();

				UpdateVisualState();

#if !HAS_UNO
				m_isVSStateChangeExternal = FALSE;
#endif

#if HAS_UNO
				// Adjust layout bounds immediately
				OnControlsBoundsChanged();
#endif
			}
		}
		private void HideControlPanel(bool hideImmediately = false)
		{
			if (m_transportControlsEnabled)
			{
				if (m_tpHideControlPanelTimer is { } && _mpe is { })
				{
					if (hideImmediately || ShouldHideControlPanel() /*|| m_isVSStateChangeExternal*/)
					{
						// Both CP and Vertical Volume will be hiddden, so stop their hide timers.
						StopControlPanelHideTimer();

#if !HAS_UNO
						//IFC(StopVerticalVolumeHideTimer());

						// Stop position updates now that CP is not visible
						//IFC(StopPositionUpdateTimer());

						// Flag vertical volume to hide so that it won't get displayed
						// next time the ControlPanel becomes visible
						//if (m_verticalVolumeIsVisible)
						//{
						//	m_verticalVolumeIsVisible = FALSE;
						//	m_verticalVolumeVisibilityChanged = TRUE;
						//}
#endif

						// Flag control panel itself to hide
						m_controlPanelIsVisible = false;
#if !HAS_UNO
						//if (!m_isVSStateChangeExternal) // Skip if Visual State already happen through external
						//{
						//	m_controlPanelVisibilityChanged = TRUE;
						//}

						//HideControlPanelFromMPE();
#endif

						UpdateVisualState();
					}
				}

				m_shouldDismissControlPanel = false;
#if !HAS_UNO
				//m_isthruScrubber = false;
				//m_isVSStateChangeExternal = false;
#endif
			}
		}
		public void Show() => ShowControlPanel();
		public void Hide() => HideControlPanel(hideImmediately: true);

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
#if !HAS_UNO
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

		private void OnRootExited(object sender, PointerRoutedEventArgs e)
		{
			if (m_transportControlsEnabled)
			{
				// If user presses pointer over root then drags it off while holding down
				// we get neither Released nor CaptureLost on the root. Thus, unset
				// m_rootHasPointerPressed whenever pointer leaves root.
				// For consistency, also enforce Pressed is FALSE for vertical volume host.
				m_rootHasPointerPressed = false;

				// If pointer exited the root area, it is no longer over the
				// vertical volume or the control panel, enforce this here.
				m_controlPanelHasPointerOver = false;

				StartControlPanelHideTimer();
			}
		}
		private void OnRootPressed(object sender, PointerRoutedEventArgs e)
		{
			if (m_transportControlsEnabled)
			{
				m_rootHasPointerPressed = true;
				StopControlPanelHideTimer();

				// Any click over media area should bring up control panel.
				ShowControlPanel();
			}
		}
		private void OnRootReleased(object sender, PointerRoutedEventArgs e)
		{
			if (m_transportControlsEnabled)
			{
				m_rootHasPointerPressed = false;
				StartControlPanelHideTimer();
			}
		}
		private void OnRootCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			if (m_transportControlsEnabled)
			{
				m_rootHasPointerPressed = false;
				StartControlPanelHideTimer();
			}
		}
		private void OnRootMoved(object sender, PointerRoutedEventArgs e)
		{
			if (e.Pointer.PointerDeviceType is PointerDeviceType.Touch)
			{
				return;
			}

			if (m_transportControlsEnabled)
			{
				// Check flags to minimize work in this frquently called handler
				if (
#if !HAS_UNO
					!m_isAudioOnly &&
#endif
					!m_controlPanelIsVisible &&
					ShowAndHideAutomatically /* ignore if when auto hide/show is disabled */
#if !HAS_UNO
					&& !m_hasError
#endif
					)
				{
					ShowControlPanel();
				}

				if (m_tpPointerMoveEndTimer is { })
				{
					m_isPointerMove = true;
					// timer to detect when pointer move ends.
					m_tpPointerMoveEndTimer.Stop();
					m_tpPointerMoveEndTimer.Start();
				}
			}
		}

		private void OnControlPanelEntered(object sender, PointerRoutedEventArgs e)
		{
			if (m_transportControlsEnabled)
			{
				m_controlPanelHasPointerOver = true;
				StopControlPanelHideTimer();
			}
		}
		private void OnControlPanelExited(object sender, PointerRoutedEventArgs e)
		{
			if (m_transportControlsEnabled)
			{
				m_controlPanelHasPointerOver = false;
				StartControlPanelHideTimer();
			}
		}
		private void OnControlPanelCaptureLost(object sender, PointerRoutedEventArgs e)
		{
			if (m_transportControlsEnabled)
			{
				//
				// 1. Update PointerPresed state.
				//
				// If capture was lost on control panel, it is safe to say
				// pointer is not pressed over ME, since only the controls
				// making up the panel could possibly take capture.
				//
				m_rootHasPointerPressed = false;

				//
				// 2. Update PointerOver state.
				//
				// If volume slider is dragged with pointer outside the vertical volume host,
				// PointerExited event is not fired, however when pointer is released we will
				// get a CaptureLost event.
				//
				var spPointerPointWhenCaptureLost = e.GetCurrentPoint(null);
				var pointWhenCaptureLost = spPointerPointWhenCaptureLost.Position;

				// Check if control panel grid is still hit
				m_controlPanelHasPointerOver = HitTestHelper(pointWhenCaptureLost, m_tpControlPanelGrid);

				// Kick off timers as needed based on updated PointerOver state
				if (!m_controlPanelHasPointerOver)
				{
					StartControlPanelHideTimer();
				}
			}
		}

		private void OnControlsBoundsChanged()
		{
			if (m_tpControlPanelGrid is { } &&
				_mediaPlayer is { } &&
				XamlRoot?.Content is UIElement root)
			{
				var slot = m_tpControlPanelGrid
					.TransformToVisual(m_tpControlPanelGrid.Parent as UIElement)
					.TransformBounds(m_tpControlPanelGrid.LayoutSlotWithMarginsAndAlignments);
				slot.Height += m_tpControlPanelGrid.Padding.Top
								+ m_tpControlPanelGrid.Padding.Bottom
								+ Margin.Top
								+ Margin.Bottom;
				if (!m_controlPanelIsVisible)
				{
					slot.Height = 0;
				}
				_mediaPlayer.SetTransportControlBounds(slot);
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
		private void Storyboard_Completed(object? sender, object e) => OnControlsBoundsChanged();
		private void OnFlyoutOpened(object? sender, object e)
		{
			m_isFlyoutOpen = true;
		}
		private void OnFlyoutClosed(object? sender, object e)
		{
			m_isFlyoutOpen = false;
			ResetControlsVisibilityTimer();
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
						ResetControlsVisibilityTimer();

						if (m_tpPlaybackRateButton is { }
							&& _playbackRateFlyout is { })
						{
							if (m_tpPlaybackRateButton is AppBarButton playbackRateAppBarButton)
							{
								playbackRateAppBarButton.Flyout.Hide();
							}
							else
							{
								_playbackRateFlyout.Hide();
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
		private void RepeatButtonTapped(object sender, RoutedEventArgs e)
		{
			if (_mpe?.MediaPlayer is null ||
				!ApiInformation.IsPropertyPresent(typeof(_MediaPlayer), nameof(_MediaPlayer.IsLoopingEnabled)))
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
					BindVisibility(m_tpRepeatButton, IsRepeatButtonVisible && ApiInformation.IsPropertyPresent(typeof(_MediaPlayer), nameof(_MediaPlayer.IsLoopingEnabled)));
					break;
				case var _ when property == IsRepeatEnabledProperty:
					BindIsEnabled(m_tpRepeatButton, IsRepeatEnabled && ApiInformation.IsPropertyPresent(typeof(_MediaPlayer), nameof(_MediaPlayer.IsLoopingEnabled)));
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
					BindVisibility(m_tpPlaybackRateButton, IsPlaybackRateButtonVisible && ApiInformation.IsPropertyPresent(typeof(_MediaPlayer), nameof(_MediaPlayer.PlaybackRate)));
					break;
				case var _ when property == IsPlaybackRateEnabledProperty:
					BindIsEnabled(m_tpPlaybackRateButton, IsPlaybackRateEnabled && ApiInformation.IsPropertyPresent(typeof(_MediaPlayer), nameof(_MediaPlayer.PlaybackRate)));
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

			void BindVisibility(FrameworkElement? target, bool value)
			{
				if (target is { })
				{
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
			if (!m_controlPanelIsVisible && !ShowAndHideAutomatically)
			{
				ShowControlPanel();
				return;
			}

			if (m_controlPanelIsVisible)
			{
				if (_mediaPlayer?.PlaybackSession.IsPlaying == true)
				{
					// while playing & MTC is visible, setting ShowAndHideAutomatically=true will begin the timer.
					ResetControlsVisibilityTimer();
				}
			}
			else
			{
				// while MTC is hidden, setting ShowAndHideAutomatically=true will show the MTC immediately.
				ShowControlPanel();
			}
		}

		// visual states
		private void InitializeVisualState()
		{
#if !HAS_UNO
			if (MTCParent_MediaElement == m_parentType)
			{
				IFC(InitializeVisualStateFromME());
			}
			else if (MTCParent_MediaPlayerElement == m_parentType)
			{
				IFC(InitializeVisualStateFromMPE());
			}

			IFC(InitializeVolume());

			if (MTCParent_MediaElement == m_parentType)
			{
				// Make sure we have the latest playback item
				IFC(UpdatePlaybackItemReference());
			}

			IFC(UpdateRepeatButtonUI());

			Update UI
			IFC(UpdatePlayPauseUI());
			IFC(UpdateFullWindowUI());
			IFC(UpdatePositionUI());
			IFC(UpdateDownloadProgressUI());
			IFC(UpdateErrorUI());

			if (m_tpMediaPositionSlider)
			{
				IFC(m_tpMediaPositionSlider.Cast<Slider>()->get_Minimum(&m_positionSliderMinimum));
				IFC(m_tpMediaPositionSlider.Cast<Slider>()->get_Maximum(&m_positionSliderMaximum));
			}

			// We could have switched into or out of audio mode, which changes the controls that are displayed.
			IFC(UpdateAudioSelectionUI());
			IFC(UpdateIsMutedUI());
			IFC(UpdateVolumeUI());
			IFC(CalculateDropOutLevel());
#endif

			// ShowControlPanel() calls UpdateVisualState()
			ShowControlPanel();
		}
		internal override void UpdateVisualState(bool useTransitions = true)
		{
			// all visual states are listed below: // unused/not-implemented ones are commented out
			UpdateControlPanelVisibilityStates(useTransitions);
			UpdateMediaStates(useTransitions);
			//UpdateAudioSelectionAvailablityStates(useTransitions);
			//UpdateCCSelectionAvailablityStates(useTransitions);
			//UpdateFocusStates(useTransitions);
			UpdateMediaTransportControlModeStates(useTransitions);
			UpdatePlayPauseStates(useTransitions);
			UpdateVolumeMuteStates(useTransitions);
			UpdateFullWindowStates(useTransitions);
			UpdateRepeatStates(useTransitions);
		}
		private void UpdateControlPanelVisibilityStates(bool useTransitions = true)
		{
			var state = m_controlPanelIsVisible
				? VisualState.ControlPanelVisibilityStates.ControlPanelFadeIn
				: VisualState.ControlPanelVisibilityStates.ControlPanelFadeOut;
			VisualStateManager.GoToState(this, state, useTransitions);
		}
		private void UpdateMediaStates(bool useTransitions = true)
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
					VisualStateManager.GoToState(this, state, useTransitions);
				}
			}
		}
		private void UpdateMediaTransportControlModeStates(bool useTransitions = true)
		{
			var state = IsCompact
				? VisualState.MediaTransportControlMode.CompactMode
				: VisualState.MediaTransportControlMode.NormalMode;
			VisualStateManager.GoToState(this, state, useTransitions);

			var uiaKey = IsCompact
				? UIAKeys.UIA_MEDIA_EXIT_MINIVIEW
				: UIAKeys.UIA_MEDIA_MINIVIEW;
			SetAutomationNameAndTooltip(m_tpCompactOverlayButton, uiaKey);
		}
		private void UpdatePlayPauseStates(bool useTransitions = true)
		{
			if (_mpe?.MediaPlayer is null)
			{
				return;
			}

			var isPlaying = m_isPlaying || (m_isInScrubMode && _wasPlaying);
			var state = isPlaying
				? VisualState.PlayPauseStates.PauseState
				: VisualState.PlayPauseStates.PlayState;
			VisualStateManager.GoToState(this, state, useTransitions);

			var uiaKey = isPlaying
				? UIAKeys.UIA_MEDIA_PAUSE
				: UIAKeys.UIA_MEDIA_PLAY;
			SetAutomationNameAndTooltip(m_tpPlayPauseButton, uiaKey);
			SetAutomationNameAndTooltip(m_tpTHLeftSidePlayPauseButton, uiaKey);
		}
		private void UpdateVolumeMuteStates(bool isExplicitMuteToggle = false, bool useTransitions = true)
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
			VisualStateManager.GoToState(this, state, useTransitions);

			var uiaKey = isMuted
				? UIAKeys.UIA_MEDIA_UNMUTE
				: UIAKeys.UIA_MEDIA_MUTE;
			SetAutomationNameAndTooltip(m_tpMuteButton, uiaKey);
		}
		private void UpdateFullWindowStates(bool useTransitions = true)
		{
			if (_mpe is not null)
			{
				var state = _mpe.IsFullWindow
					? VisualState.FullWindowStates.FullWindowState
					: VisualState.FullWindowStates.NonFullWindowState;
				VisualStateManager.GoToState(this, state, useTransitions);

				var uiaKey = _mpe.IsFullWindow
					? UIAKeys.UIA_MEDIA_EXIT_FULLSCREEN
					: UIAKeys.UIA_MEDIA_FULLSCREEN;
				SetAutomationNameAndTooltip(m_tpFullWindowButton, uiaKey);
			}
		}
		private void UpdateRepeatStates(bool useTransitions = true)
		{
			if (_mpe?.MediaPlayer is null ||
				!ApiInformation.IsPropertyPresent(typeof(_MediaPlayer), nameof(_MediaPlayer.IsLoopingEnabled)))
			{
				return;
			}

			var state = _mpe.MediaPlayer.IsLoopingEnabled
				? VisualState.RepeatStates.RepeatAllState
				: VisualState.RepeatStates.RepeatNoneState;
			VisualStateManager.GoToState(this, state, useTransitions);

			var uiaKey = _mpe.MediaPlayer.IsLoopingEnabled
				? UIAKeys.UIA_MEDIA_REPEAT_ALL
				: UIAKeys.UIA_MEDIA_REPEAT_NONE;
			SetAutomationNameAndTooltip(m_tpRepeatButton, uiaKey);
		}
	}

	partial class MediaTransportControls // helper methods
	{
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
		private void SetAutomationNameAndTooltip(DependencyObject? target, string uiaKey)
		{
			if (target is { })
			{
				var value = DXamlCore.Current.GetLocalizedResourceString(uiaKey);
				AutomationProperties.SetName(target, value);
				ToolTipService.SetToolTip(target, value);
			}
		}

		/// <summary>
		/// Helper to check if conditions are met to hide control panel.
		/// </summary>
		private bool ShouldHideControlPanel()
		{
			var isAutoShowHide = ShowAndHideAutomatically;
			var result =
				m_controlPanelIsVisible &&
#if !HAS_UNO
				!m_isAudioOnly &&
				!m_hasError &&
#endif
				(m_shouldDismissControlPanel || !m_controlPanelHasPointerOver) &&
				!m_rootHasPointerPressed &&
#if !HAS_UNO
				// Do not need to check this on the Xbox only if commandbar should exist in the template.
				(!m_controlsHaveKeyOrProgFocus || (XboxUtility::IsOnXbox() && m_tpCommandBar.Get())) &&

				!m_verticalVolumeHasKeyOrProgFocus &&
#endif
				ShouldHideControlPanelWhilePlaying() &&
				!m_isFlyoutOpen &&
				!m_isPointerMove &&

				// Hide MTC only if auto hide/Show is enabled
				isAutoShowHide;

			return result;
		}

		/// <summary>
		/// Helper to check if conditions are met to hide control panel while playing.
		/// It should stay if we aren't playing video.
		/// </summary>
		private bool ShouldHideControlPanelWhilePlaying()
		{
			return (m_isPlaying && !m_isBuffering)
				|| (m_shouldDismissControlPanel);
		}

		/// <summary>
		/// Helper to hit test pElement against point.
		/// </summary>
		private bool HitTestHelper(Point point, UIElement? pElement)
		{
			if (pElement is { })
			{
				var spElements = VisualTreeHelper.FindElementsInHostCoordinates(point, pElement, includeAllElements: m_tpCommandBar is { } ? false : true);
				foreach (var spElement in spElements)
				{
					if (pElement == spElement)
					{
						return true;
					}
				}
			}

			return false;
		}
	}
}

#if __ANDROID__ || __IOS__ || __MACOS__

using System;
using Windows.Media.Playback;

namespace Windows.UI.Xaml.Controls
{
	public partial class MediaTransportControls
	{
		#region IsFullWindowButtonVisible Property

		public bool IsFullWindowButtonVisible
		{
			get { return (bool)GetValue(IsFullWindowButtonVisibleProperty); }
			set { SetValue(IsFullWindowButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsFullWindowButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsFullWindowButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsPlaybackRateEnabled Property

		public bool IsPlaybackRateEnabled
		{
			get { return (bool)GetValue(IsPlaybackRateEnabledProperty); }
			set { SetValue(IsPlaybackRateEnabledProperty, value); }
		}

		public static DependencyProperty IsPlaybackRateEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsPlaybackRateEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsFastRewindEnabled Property

		public bool IsFastRewindEnabled
		{
			get { return (bool)GetValue(IsFastRewindEnabledProperty); }
			set { SetValue(IsFastRewindEnabledProperty, value); }
		}

		public static DependencyProperty IsFastRewindEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsFastRewindEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsFastRewindButtonVisible Property

		public bool IsFastRewindButtonVisible
		{
			get { return (bool)GetValue(IsFastRewindButtonVisibleProperty); }
			set { SetValue(IsFastRewindButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsFastRewindButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsFastRewindButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsFullWindowEnabled Property

		public bool IsFullWindowEnabled
		{
			get { return (bool)GetValue(IsFullWindowEnabledProperty); }
			set { SetValue(IsFullWindowEnabledProperty, value); }
		}

		public static DependencyProperty IsFullWindowEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsFullWindowEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsFastForwardEnabled Property

		public bool IsFastForwardEnabled
		{
			get { return (bool)GetValue(IsFastForwardEnabledProperty); }
			set { SetValue(IsFastForwardEnabledProperty, value); }
		}

		public static DependencyProperty IsFastForwardEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsFastForwardEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsCompact Property

		public bool IsCompact
		{
			get { return (bool)GetValue(IsCompactProperty); }
			set { SetValue(IsCompactProperty, value); }
		}

		public static DependencyProperty IsCompactProperty { get; } =
			DependencyProperty.Register(
				nameof(IsCompact),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(false, OnIsCompactChanged));

		#endregion

		#region IsSeekEnabled Property

		public bool IsSeekEnabled
		{
			get { return (bool)GetValue(IsSeekEnabledProperty); }
			set { SetValue(IsSeekEnabledProperty, value); }
		}

		public static DependencyProperty IsSeekEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSeekEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true, OnIsSeekEnabledChanged));

		#endregion

		#region IsStopEnabled Property

		public bool IsStopEnabled
		{
			get { return (bool)GetValue(IsStopEnabledProperty); }
			set { SetValue(IsStopEnabledProperty, value); }
		}

		public static DependencyProperty IsStopEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsStopEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsFastForwardButtonVisible Property

		public bool IsFastForwardButtonVisible
		{
			get { return (bool)GetValue(IsFastForwardButtonVisibleProperty); }
			set { SetValue(IsFastForwardButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsFastForwardButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsFastForwardButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsStopButtonVisible Property

		public bool IsStopButtonVisible
		{
			get { return (bool)GetValue(IsStopButtonVisibleProperty); }
			set { SetValue(IsStopButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsStopButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsStopButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsVolumeButtonVisible Property

		public bool IsVolumeButtonVisible
		{
			get { return (bool)GetValue(IsVolumeButtonVisibleProperty); }
			set { SetValue(IsVolumeButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsVolumeButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsVolumeButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsSeekBarVisible Property

		public bool IsSeekBarVisible
		{
			get { return (bool)GetValue(IsSeekBarVisibleProperty); }
			set { SetValue(IsSeekBarVisibleProperty, value); }
		}

		public static DependencyProperty IsSeekBarVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSeekBarVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true, OnIsSeekBarVisibleChanged));

		#endregion

		#region IsPlaybackRateButtonVisible Property

		public bool IsPlaybackRateButtonVisible
		{
			get { return (bool)GetValue(IsPlaybackRateButtonVisibleProperty); }
			set { SetValue(IsPlaybackRateButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsPlaybackRateButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsPlaybackRateButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsZoomEnabled Property

		public bool IsZoomEnabled
		{
			get { return (bool)GetValue(IsZoomEnabledProperty); }
			set { SetValue(IsZoomEnabledProperty, value); }
		}

		public static DependencyProperty IsZoomEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsZoomEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsZoomButtonVisible Property

		public bool IsZoomButtonVisible
		{
			get { return (bool)GetValue(IsZoomButtonVisibleProperty); }
			set { SetValue(IsZoomButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsZoomButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsZoomButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsVolumeEnabled Property

		public bool IsVolumeEnabled
		{
			get { return (bool)GetValue(IsVolumeEnabledProperty); }
			set { SetValue(IsVolumeEnabledProperty, value); }
		}

		public static DependencyProperty IsVolumeEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsVolumeEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsSkipBackwardButtonVisible Property

		public bool IsSkipBackwardButtonVisible
		{
			get { return (bool)GetValue(IsSkipBackwardButtonVisibleProperty); }
			set { SetValue(IsSkipBackwardButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsSkipBackwardButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSkipBackwardButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsNextTrackButtonVisible Property

		public bool IsNextTrackButtonVisible
		{
			get { return (bool)GetValue(IsNextTrackButtonVisibleProperty); }
			set { SetValue(IsNextTrackButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsNextTrackButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsNextTrackButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region FastPlayFallbackBehaviour Property

		public Media.FastPlayFallbackBehaviour FastPlayFallbackBehaviour
		{
			get { return (Media.FastPlayFallbackBehaviour)GetValue(FastPlayFallbackBehaviourProperty); }
			set { SetValue(FastPlayFallbackBehaviourProperty, value); }
		}

		public static DependencyProperty FastPlayFallbackBehaviourProperty { get; } =
			DependencyProperty.Register(
				nameof(FastPlayFallbackBehaviour),
				typeof(Media.FastPlayFallbackBehaviour),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(default(Media.FastPlayFallbackBehaviour)));

		#endregion

		#region IsSkipForwardEnabled Property

		public bool IsSkipForwardEnabled
		{
			get { return (bool)GetValue(IsSkipForwardEnabledProperty); }
			set { SetValue(IsSkipForwardEnabledProperty, value); }
		}

		public static DependencyProperty IsSkipForwardEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSkipForwardEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsPreviousTrackButtonVisible Property

		public bool IsPreviousTrackButtonVisible
		{
			get { return (bool)GetValue(IsPreviousTrackButtonVisibleProperty); }
			set { SetValue(IsPreviousTrackButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsPreviousTrackButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsPreviousTrackButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsSkipForwardButtonVisible Property

		public bool IsSkipForwardButtonVisible
		{
			get { return (bool)GetValue(IsSkipForwardButtonVisibleProperty); }
			set { SetValue(IsSkipForwardButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsSkipForwardButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSkipForwardButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsSkipBackwardEnabled Property

		public bool IsSkipBackwardEnabled
		{
			get { return (bool)GetValue(IsSkipBackwardEnabledProperty); }
			set { SetValue(IsSkipBackwardEnabledProperty, value); }
		}

		public static DependencyProperty IsSkipBackwardEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSkipBackwardEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region ShowAndHideAutomatically Property

		public bool ShowAndHideAutomatically
		{
			get { return (bool)GetValue(ShowAndHideAutomaticallyProperty); }
			set { SetValue(ShowAndHideAutomaticallyProperty, value); }
		}

		public static DependencyProperty ShowAndHideAutomaticallyProperty { get; } =
			DependencyProperty.Register(
				nameof(ShowAndHideAutomatically),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true, OnShowAndHideAutomaticallyChanged));

		#endregion

		#region IsRepeatButtonVisible Property

		public bool IsRepeatButtonVisible
		{
			get { return (bool)GetValue(IsRepeatButtonVisibleProperty); }
			set { SetValue(IsRepeatButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsRepeatButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsRepeatButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsRepeatEnabled Property

		public bool IsRepeatEnabled
		{
			get { return (bool)GetValue(IsRepeatEnabledProperty); }
			set { SetValue(IsRepeatEnabledProperty, value); }
		}

		public static DependencyProperty IsRepeatEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsRepeatEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsCompactOverlayEnabled Property

		public bool IsCompactOverlayEnabled
		{
			get { return (bool)GetValue(IsCompactOverlayEnabledProperty); }
			set { SetValue(IsCompactOverlayEnabledProperty, value); }
		}

		public static DependencyProperty IsCompactOverlayEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsCompactOverlayEnabled),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion

		#region IsCompactOverlayButtonVisible Property

		public bool IsCompactOverlayButtonVisible
		{
			get { return (bool)GetValue(IsCompactOverlayButtonVisibleProperty); }
			set { SetValue(IsCompactOverlayButtonVisibleProperty, value); }
		}

		public static DependencyProperty IsCompactOverlayButtonVisibleProperty { get; } =
			DependencyProperty.Register(
				nameof(IsCompactOverlayButtonVisible),
				typeof(bool),
				typeof(MediaTransportControls),
				new FrameworkPropertyMetadata(true));

		#endregion
	}
}
#endif

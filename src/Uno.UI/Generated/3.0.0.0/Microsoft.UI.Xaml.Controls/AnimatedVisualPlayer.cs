#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AnimatedVisualPlayer : global::Windows.UI.Xaml.FrameworkElement
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.Stretch Stretch
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Stretch)this.GetValue(StretchProperty);
			}
			set
			{
				this.SetValue(StretchProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.Controls.IAnimatedVisualSource Source
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.IAnimatedVisualSource)this.GetValue(SourceProperty);
			}
			set
			{
				this.SetValue(SourceProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  double PlaybackRate
		{
			get
			{
				return (double)this.GetValue(PlaybackRateProperty);
			}
			set
			{
				this.SetValue(PlaybackRateProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.DataTemplate FallbackContent
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(FallbackContentProperty);
			}
			set
			{
				this.SetValue(FallbackContentProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AutoPlay
		{
			get
			{
				return (bool)this.GetValue(AutoPlayProperty);
			}
			set
			{
				this.SetValue(AutoPlayProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  object Diagnostics
		{
			get
			{
				return (object)this.GetValue(DiagnosticsProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan Duration
		{
			get
			{
				return (global::System.TimeSpan)this.GetValue(DurationProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsAnimatedVisualLoaded
		{
			get
			{
				return (bool)this.GetValue(IsAnimatedVisualLoadedProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPlaying
		{
			get
			{
				return (bool)this.GetValue(IsPlayingProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionObject ProgressObject
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionObject AnimatedVisualPlayer.ProgressObject is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=CompositionObject%20AnimatedVisualPlayer.ProgressObject");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.Controls.PlayerAnimationOptimization AnimationOptimization
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.PlayerAnimationOptimization)this.GetValue(AnimationOptimizationProperty);
			}
			set
			{
				this.SetValue(AnimationOptimizationProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty AutoPlayProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(AutoPlay), typeof(bool), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty DiagnosticsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Diagnostics), typeof(object), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty DurationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Duration), typeof(global::System.TimeSpan), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(global::System.TimeSpan)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty FallbackContentProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(FallbackContent), typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsAnimatedVisualLoadedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsAnimatedVisualLoaded), typeof(bool), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsPlayingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsPlaying), typeof(bool), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty PlaybackRateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(PlaybackRate), typeof(double), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SourceProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Source), typeof(global::Microsoft.UI.Xaml.Controls.IAnimatedVisualSource), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Controls.IAnimatedVisualSource)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty StretchProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Stretch), typeof(global::Windows.UI.Xaml.Media.Stretch), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Stretch)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty AnimationOptimizationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(AnimationOptimization), typeof(global::Microsoft.UI.Xaml.Controls.PlayerAnimationOptimization), 
			typeof(global::Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Controls.PlayerAnimationOptimization)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AnimatedVisualPlayer() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer", "AnimatedVisualPlayer.AnimatedVisualPlayer()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.AnimatedVisualPlayer()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.Diagnostics.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.Duration.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.Source.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.Source.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.FallbackContent.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.FallbackContent.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.AutoPlay.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.AutoPlay.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.IsAnimatedVisualLoaded.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.IsPlaying.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.PlaybackRate.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.PlaybackRate.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.ProgressObject.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.Stretch.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.Stretch.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Pause()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer", "void AnimatedVisualPlayer.Pause()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction PlayAsync( double fromProgress,  double toProgress,  bool looped)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction AnimatedVisualPlayer.PlayAsync(double fromProgress, double toProgress, bool looped) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20AnimatedVisualPlayer.PlayAsync%28double%20fromProgress%2C%20double%20toProgress%2C%20bool%20looped%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Resume()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer", "void AnimatedVisualPlayer.Resume()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetProgress( double progress)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer", "void AnimatedVisualPlayer.SetProgress(double progress)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Stop()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer", "void AnimatedVisualPlayer.Stop()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.AnimationOptimization.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.AnimationOptimization.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.AnimationOptimizationProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.AutoPlayProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.DiagnosticsProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.DurationProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.FallbackContentProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.IsAnimatedVisualLoadedProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.IsPlayingProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.PlaybackRateProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.SourceProperty.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.AnimatedVisualPlayer.StretchProperty.get
	}
}

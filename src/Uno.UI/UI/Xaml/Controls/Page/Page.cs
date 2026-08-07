using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class Page : UserControl
{
#if !UNO_HAS_BORDER_VISUAL
	private readonly BorderLayerRenderer _borderRenderer;
#endif

	public Page()
	{
#if !UNO_HAS_BORDER_VISUAL
		_borderRenderer = new BorderLayerRenderer(this);
#endif
	}

#if UNO_HAS_BORDER_VISUAL
	private protected override ContainerVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateBorderVisual();
#endif

#if !UNO_HAS_BORDER_VISUAL
	private void UpdateBorder()
	{
		_borderRenderer.Update();
	}
#endif

	protected internal virtual void OnNavigatedFrom(NavigationEventArgs e) { }

	protected internal virtual void OnNavigatedTo(NavigationEventArgs e) { }

	protected internal virtual void OnNavigatingFrom(NavigatingCancelEventArgs e) { }

	// MUX Reference Page_Partial.cpp
	internal void AppBarClosedSizeChanged()
	{
		InvalidateMeasure();
	}

	#region TopAppBar
	public AppBar TopAppBar
	{
		get => (AppBar)this.GetValue(TopAppBarProperty);
		set => this.SetValue(TopAppBarProperty, value);
	}

	public static DependencyProperty TopAppBarProperty { get; } =
		DependencyProperty.Register(
			"TopAppBar",
			typeof(AppBar),
			typeof(Page),
			new FrameworkPropertyMetadata(
				default(AppBar),
				FrameworkPropertyMetadataOptions.ValueInheritsDataContext
			)
		);
	#endregion

	#region BottomAppBar
	public AppBar BottomAppBar
	{
		get => (AppBar)this.GetValue(BottomAppBarProperty);
		set => this.SetValue(BottomAppBarProperty, value);
	}

	public static DependencyProperty BottomAppBarProperty { get; } =
		DependencyProperty.Register(
			"BottomAppBar",
			typeof(AppBar),
			typeof(Page),
			new FrameworkPropertyMetadata(
				default(AppBar),
				FrameworkPropertyMetadataOptions.ValueInheritsDataContext
			)
		);
	#endregion

	#region Frame

	public
#if __APPLE_UIKIT__
		new
#endif
		Frame Frame
	{
		get => (Frame)this.GetValue(FrameProperty);
		internal set => this.SetValue(FrameProperty, value);
	}

	public static DependencyProperty FrameProperty { get; } =
		DependencyProperty.Register(
			"Frame",
			typeof(Frame),
			typeof(Page),
			new FrameworkPropertyMetadata(
				default(Frame),
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext
			)
		);

	#endregion

	/// <summary>
	/// Gets or sets the navigation mode that indicates whether this Page is cached,
	/// and the period of time that the cache entry should persist.
	/// </summary>
	/// <remarks>
	/// To enable a page to be cached, set NavigationCacheMode to either Enabled or Required.
	/// The difference in behavior is that Enabled might not be cached if the frame's cache
	/// size limit (CacheSize) is exceeded, whereas Required always generates an entry
	/// no matter the size limit.
	/// </remarks>
	public NavigationCacheMode NavigationCacheMode
	{
		get => (NavigationCacheMode)GetValue(NavigationCacheModeProperty);
		set => SetValue(NavigationCacheModeProperty, value);
	}

	internal static DependencyProperty NavigationCacheModeProperty { get; } =
		DependencyProperty.Register(
			nameof(NavigationCacheMode),
			typeof(NavigationCacheMode),
			typeof(Page),
			new FrameworkPropertyMetadata(NavigationCacheMode.Disabled));

	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
	{
#if UNO_HAS_BORDER_VISUAL
		this.UpdateBackground();
#else
		UpdateBorder();
#endif
	}
}

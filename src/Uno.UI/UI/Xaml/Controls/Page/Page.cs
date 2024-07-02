using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

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
	private protected override ShapeVisual CreateElementVisual() => Compositor.GetSharedCompositor().CreateBorderVisual();
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

	#region TopAppBar
	[Uno.NotImplemented]
	public AppBar TopAppBar
	{
		get => (AppBar)this.GetValue(TopAppBarProperty);
		set => this.SetValue(TopAppBarProperty, value);
	}

	[Uno.NotImplemented]
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
	[Uno.NotImplemented]
	public AppBar BottomAppBar
	{
		get => (AppBar)this.GetValue(BottomAppBarProperty);
		set => this.SetValue(BottomAppBarProperty, value);
	}

	[Uno.NotImplemented]
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
#if __IOS__ || __MACOS__
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

	public NavigationCacheMode NavigationCacheMode { get; set; }

	protected override void OnBackgroundChanged(DependencyPropertyChangedEventArgs e)
	{
#if UNO_HAS_BORDER_VISUAL
		this.UpdateBackground();
#else
		UpdateBorder();
#endif
	}
}

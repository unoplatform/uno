#nullable enable

using System.Collections.Generic;
using System.Numerics;
using DirectUI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemPresenter : IBorderInfoProvider
{
	// Inline state fill of the under-content layer; drawn as the BorderVisual background so BackgroundTransition applies to it.
	private Brush? m_chromeBackground;

	// Over-content layer, hosted by the presenter visual or the secondary chrome visual.
	private ShapeVisual? m_overContentLayerVisual;

	private protected override ContainerVisual CreateElementVisual() => new ListViewBaseItemChromeVisual(Compositor.GetSharedCompositor());

	private ListViewBaseItemChromeVisual ChromeVisual => (ListViewBaseItemChromeVisual)Visual;

	// WinUI never renders the presenter through ContentPresenterRenderContent (hwwalk.cpp:1513-1526): only the chrome layers draw.
	Brush? IBorderInfoProvider.Background => m_chromeBackground;

	BackgroundSizing IBorderInfoProvider.BackgroundSizing => BackgroundSizing.OuterBorderEdge;

	Brush? IBorderInfoProvider.BorderBrush => null;

	Thickness IBorderInfoProvider.BorderThickness => default;

	// Keeps WinUI's rounded clip of the presenter children.
	CornerRadius IBorderInfoProvider.CornerRadius => CornerRadius;

	BorderVisual IBorderInfoProvider.BorderVisual => ChromeVisual;

	internal override void OnArrangeVisual(Rect rect, Rect? clip)
	{
		base.OnArrangeVisual(rect, clip);

		// The layers are sized from the chrome bounds.
		RenderLayers();
	}

	/// <summary>
	/// Configures every chrome layer, as the WinUI render walk would on the next frame.
	/// </summary>
	/// <remarks>Uno-specific: replaces the hwwalk.cpp RenderLayer calls (PrimaryChrome_Pre, PrimaryChrome_Post, SecondaryChrome_Post).</remarks>
	private void RenderLayers()
	{
		// Uno-specific: configured eagerly from property changes, so an unlinked presenter draws nothing instead of raising the wrong-parent error.
		var canRender = m_pParentListViewBaseItemNoRef is not null;

		var underContentLayer = new ChromeContentRenderer(this);
		m_chromeBackground = null;
		if (canRender)
		{
			RenderLayer(underContentLayer, ListViewBaseItemChromeLayerPosition.PrimaryChrome_Pre);
		}

		ChromeVisual.SetUnderContentShapes(underContentLayer.Shapes, underContentLayer.BackgroundIndex);
		this.UpdateBackground();

		var overContentLayer = new ChromeContentRenderer(this);
		ContainerVisual? overContentLayerHost = null;
		if (canRender)
		{
			// MUX Reference hwwalk.cpp, lines 1644-1661: the post layer is skipped when rounded corners clip the children.
			if (!RequiresCompNodeForRoundedCorners())
			{
				RenderLayer(overContentLayer, ListViewBaseItemChromeLayerPosition.PrimaryChrome_Post);
				overContentLayerHost = Visual;
			}

			if (overContentLayer.Shapes is null && m_pSecondaryChrome is { } secondaryChrome)
			{
				RenderLayer(overContentLayer, ListViewBaseItemChromeLayerPosition.SecondaryChrome_Post);
				overContentLayerHost = secondaryChrome.Visual;
			}
		}

		SetOverContentLayer(overContentLayer.Shapes is null ? null : overContentLayerHost, overContentLayer.Shapes);
	}

	private void SetOverContentLayer(ContainerVisual? host, List<CompositionSpriteShape>? shapes)
	{
		if (m_overContentLayerVisual is null && shapes is null)
		{
			return;
		}

		var layer = m_overContentLayerVisual ??= CreateOverContentLayerVisual();

		foreach (var shape in layer.Shapes)
		{
			ListViewBaseItemChromeVisual.ReleaseShapeGeometry((CompositionSpriteShape)shape);
		}

		layer.Shapes.Clear();

		if (layer.Parent is { } parent && parent != host)
		{
			parent.Children.Remove(layer);
		}

		if (host is null || shapes is null)
		{
			return;
		}

		layer.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
		foreach (var shape in shapes)
		{
			layer.Shapes.Add(shape);
		}

		if (layer.Parent is null)
		{
			host.Children.InsertAtTop(layer);
		}
	}

	private ShapeVisual CreateOverContentLayerVisual()
	{
		var layer = Visual.Compositor.CreateShapeVisual();
		// Above the content, like the post-children render step. Child visuals are never hit-tested.
		layer.ZIndex = int.MaxValue;
#if DEBUG
		layer.Comment = "#chromeOverContentLayer";
#endif
		return layer;
	}

	// MUX Reference framework.cpp, lines 2622-2655 (UpdateRequiresCompNodeForRoundedCorners)
	private bool RequiresCompNodeForRoundedCorners()
	{
		var cornerRadius = CornerRadius;

		var hasRoundedCorner =
			cornerRadius.TopLeft != 0
			|| cornerRadius.TopRight != 0
			|| cornerRadius.BottomLeft != 0
			|| cornerRadius.BottomRight != 0;

		var hasChildren = GetChildren().Count > 0;

		return hasRoundedCorner && hasChildren;
	}

	// TODO Uno: WinUI handshakes m_isFocusVisualDrawnByFocusManager with CustomizeFocusRectangle per render walk;
	// Uno configures the layers eagerly, so it derives the same answer from the focus state.
	private bool IsFocusVisualDrawnByFocusManager()
		=> m_visualStates.HasState(FocusStates.Focused)
			&& GetParentListViewBaseItemNoRef().UseSystemFocusVisuals
			&& !ShouldDrawDottedLinesFocusVisual();

	// MUX Reference BaseContentRenderer.cpp, lines 1518-1600 (continuous rectangles only)
	private void RenderFocusRectangle(ChromeContentRenderer pContentRenderer, FocusRectangleOptions focusOptions)
	{
		// If bounds haven't been set by the caller, default to just use the element bounds
		if (focusOptions.bounds.Width == 0.0f && focusOptions.bounds.Height == 0.0f)
		{
			focusOptions.bounds = new Rect(0, 0, ActualWidth, ActualHeight);
		}

		// TODO Uno: FocusRectangleOptions.isContinuous; the chrome only renders continuous rectangles (dotted lines are not supported).
		var bounds = focusOptions.bounds;
		if (focusOptions.drawFirst)
		{
			if (focusOptions.firstBrush is not null)
			{
				AddBorder(pContentRenderer, bounds, focusOptions.firstThickness, focusOptions.firstBrush, this);
			}

			CSizeUtil.Deflate(ref bounds, focusOptions.firstThickness);
		}
		if (focusOptions.drawSecond && focusOptions.secondBrush is not null)
		{
			AddBorder(pContentRenderer, bounds, focusOptions.secondThickness, focusOptions.secondBrush, this);
		}
	}

	/// <summary>
	/// Collects the shapes of one chrome layer.
	/// </summary>
	/// <remarks>Uno-specific: stands in for WinUI's IContentRenderer.</remarks>
	private sealed class ChromeContentRenderer(ListViewBaseItemPresenter owner)
	{
		internal List<CompositionSpriteShape>? Shapes { get; private set; }

		// Position of the element background (the Inline state fill) among the shapes.
		internal int BackgroundIndex { get; private set; } = int.MaxValue;

		internal void AddRectangle(Rect bounds, Brush brush)
			=> AddShape(brush, BorderVisual.BuildRoundRectPath(bounds, default));

		internal void AddBorder(Rect bounds, Thickness thickness, Brush brush)
		{
			if (thickness.Left <= 0 && thickness.Top <= 0 && thickness.Right <= 0 && thickness.Bottom <= 0)
			{
				return;
			}

			var inner = bounds;
			CSizeUtil.Deflate(ref inner, thickness);
			AddShape(brush, BorderVisual.BuildRoundRectRingPath(bounds, default, inner, default));
		}

		internal void AddElementBackground(Brush brush)
		{
			BackgroundIndex = Shapes?.Count ?? 0;
			owner.m_chromeBackground = brush;
		}

		private void AddShape(Brush brush, Uno.UI.Composition.Drawing.IGeometry path)
		{
			var compositor = owner.Visual.Compositor;
			var geometry = compositor.CreatePathGeometry();
			BorderVisual.SetPath(geometry, path);

			var shape = compositor.CreateSpriteShape(geometry);
			shape.FillBrush = brush.GetOrCreateCompositionBrush(compositor);

			(Shapes ??= new()).Add(shape);
		}
	}

	/// <summary>
	/// Stops every running chrome animation and runs its completion action synchronously
	/// (e.g. removes a check box or selection indicator that was fading out).
	/// </summary>
	/// <remarks>Uno-specific: recycled containers leave the tree, so their animations cannot be left to complete.</remarks>
	internal void FlushChromeAnimations()
	{
		ClearAnimation(m_pointerPressedAnimation);

		if (m_reorderHintAnimation.tpStoryboard is not null)
		{
			OnReorderHintReturnCompleted(null, null);
		}

		ClearAnimation(m_dragDropAnimation);

		if (m_multiSelectAnimation.tpStoryboard is not null)
		{
			OnMultiSelectCompleted(null, null);
		}

		if (m_indicatorSelectAnimation.tpStoryboard is not null)
		{
			OnIndicatorSelectCompleted(null, null);
		}

		if (m_selectionIndicatorAnimation.tpStoryboard is not null)
		{
			OnSelectionIndicatorCompleted(null, null);
		}
	}
}

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI;
using static Uno.UI.Xaml.Internal.Inlined;
using WinUICoreServices = global::Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Xaml.Islands;

internal partial class XamlIslandRoot
{
	private ContentRoot _contentRoot = null!;
	private bool _isVisible;
	private Size _previousIslandSize;

	internal void InitializeRoot(WinUICoreServices coreServices)
	{
		_contentRoot = coreServices.ContentRootCoordinator.CreateContentRoot(ContentRootType.XamlIslandRoot, Colors.Transparent, this);
		_contentRoot.XamlIslandRoot = this;
	}

	internal ContentRoot ContentRoot => _contentRoot;

	private void SetPublicRootVisual(
		UIElement? rootVisual,
		ScrollViewer? rootScrollViewer,
		ContentPresenter? contentPresenter) =>
		_contentRoot.VisualTree.SetPublicRootVisual(rootVisual, rootScrollViewer, contentPresenter);

	/// <summary>
	/// Overriding virtual to add specific logic to measure pass.
	/// This behavior is the same as that of the Canvas.
	/// </summary>
	/// <param name="availableSize">Available size.</param>
	/// <returns>Desired size.</returns>
	protected override Size MeasureOverride(Size availableSize)
	{
		Size desiredSize = default;
		var children = Children;
		for (var i = 0; i < children.Count; i++)
		{
			var child = children[i];
			if (child != null)
			{
				// Measure child to the plugin size
				child.Measure(availableSize);

				// The first child is the content, which is what we want the desired size to be
				if (i == 0)
				{
					desiredSize = child.DesiredSize;
				}
			}
		}

		// TODO: Uno specific - implement composition content.
		// Notify the comp content of a new desired/requested size
		//if (auto compContent = GetCompositionContent())
		//{
		//	IFC_RETURN(compContent->put_RequestedSize({ desiredSize.width, desiredSize.height }));
		//}
		return desiredSize;
	}

	/// <summary>
	/// Overriding CFrameworkElement virtual to add specific logic to arrange pass.
	/// The root visual always arranges the children with the finalSize. This ensures that
	/// children of the root visual are always arranged at the plugin size.
	/// </summary>
	/// <param name="finalSize">Final arranged size.</param>
	/// <returns>Final size.</returns>
	protected override Size ArrangeOverride(Size finalSize)
	{
		var children = Children;
		for (var i = 0; i < children.Count; i++)
		{
			var child = children[i];

			if (child == null)
			{
				continue;
			}

			var childDesiredSize = child.DesiredSize;
			var childRect = new Rect(
				0,
				0,
				Math.Max(finalSize.Width, childDesiredSize.Width),
				Math.Max(finalSize.Height, childDesiredSize.Height));
			child.Arrange(childRect);
		}

		// TODO: Uno implement this
		//// Notify the XAML render walk the content has changed size
		//GetContext()->UpdateXamlIslandRootTargetSize(this);
		return finalSize;
	}

	/// <remarks>
	/// The Width and Height are set in DesktopWindow.cs immediately after native window
	/// size changes.
	/// </remarks>
	public Size GetSize() => new Size(Width, Height);

	public new bool IsVisible() => _isVisible;

	// TODO Uno: Implement focus on island.
	public bool TrySetFocus() => true;

	internal ContentIsland? ContentIsland => ContentRoot?.CompositionContent;

	internal ContentIslandEnvironment? ContentIslandEnvironment { get; set; }

	internal void OnStateChanged()
	{
		// In the scope if this handler, we may find the the XamlRoot has changed in various ways.  This RAII
		// helper ensures we call CContentRoot::RaisePendingXamlRootChangedEventIfNeeded before exiting this function.
		using var xamlRootChangedEventGuard = Disposable.Create(() => _contentRoot.RaisePendingXamlRootChangedEventIfNeeded(true));

		UpdateRasterizationScale();
		OnIslandActualSizeChanged();

		bool isSiteVisible = false;
		// Don't consider content visibility. That's a separate concept and that property is exposed to apps. The site
		// visibility is the equivalent of CoreWindow visibility in XamlIslands mode.    
		if (ContentIsland is { } contentIsland)
		{
			isSiteVisible = contentIsland.IsSiteVisible;
		}

		if (_isVisible != isSiteVisible)
		{
			_isVisible = isSiteVisible;

			if (_contentRoot != null)
			{
				_contentRoot.AddPendingXamlRootChangedEvent(ContentRoot.ChangeType.IsVisible);

				// TODO MZ: Implement this maybe
				//CoreServices coreServices = GetContext();
				//coreServices.OnVisibilityChanged(false /* isStartingUp */, false /* freezeDWMSnapshotIfHidden */);
			}
		}
	}

	private void UpdateRasterizationScale()
	{
		// We may have been already closed, bail out.
		// m_contentRoot may be null if we reset the window content to null before getting a StateChanged notification.
		if (ContentRoot is { } contentRoot)
		{
			var visualTree = contentRoot.VisualTree;
			if (visualTree.RootScale is { } rootScale)
			{
				rootScale.UpdateSystemScale();
			}
		}
	}

	private void OnIslandActualSizeChanged()
	{
		var newSize = GetSize();
		if (!IsCloseReal(_previousIslandSize.Width, newSize.Width) || !IsCloseReal(_previousIslandSize.Height, newSize.Height))
		{
			InvalidateMeasure();

			var parent = this.GetParentInternal(false);
			if (parent is not null)
			{
				parent.InvalidateMeasure();
			}

			FxCallbacks.XamlIslandRoot_OnSizeChanged(this);

			if (_contentRoot != null)
			{
				_contentRoot.AddPendingXamlRootChangedEvent(ContentRoot.ChangeType.Size);
			}
			else
			{
				// The XamlIslandRoot itself will release its visual tree immediately when it leaves the live tree,
				// but will be kept alive until the end of the frame. If any CompositionContent::StateChanged events
				// are raised in the meantime, we will ignore them.
				// MUX_ASSERT(!IsActive());
			}

			// TODO Uno: Implement later
			//float newArea = newSize.Width * newSize.Height;
			//if (IsLessThanReal(m_maxIslandArea, newArea))
			//{
			//	m_maxIslandArea = newArea;

			//	InstrumentationNewAreaMax(newSize.Width, newSize.Height);
			//}

			_previousIslandSize = newSize;
		}
	}

	internal static void OnSizeChangedStatic(XamlIslandRoot xamlIslandRoot)
	{
		// TODO MZ: Implement this maybe
		//xamlIslandRoot.ContentManager.OnWindowSizeChanged();
	}
}

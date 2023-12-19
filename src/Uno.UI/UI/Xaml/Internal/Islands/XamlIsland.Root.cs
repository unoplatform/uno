#nullable enable

using System;
using Uno.UI.Xaml.Core;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using WinUICoreServices = global::Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Xaml.Islands;

internal partial class XamlIsland
{
	private ContentRoot _contentRoot = null!;

	internal void InitializeRoot(WinUICoreServices coreServices)
	{
		_contentRoot = coreServices.ContentRootCoordinator.CreateContentRoot(ContentRootType.XamlIsland, Colors.Transparent, this);
		_contentRoot.XamlIslandRoot = this;

		this.Background = new SolidColorBrush(Colors.Transparent);
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

	public Size GetSize() => new Size(ActualWidth, ActualHeight);
}

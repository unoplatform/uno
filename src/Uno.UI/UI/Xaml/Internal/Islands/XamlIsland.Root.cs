#nullable enable

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
		foreach (var child in Children)
		{
			if (child != null)
			{
				// Measure child to the plugin size
				child.Measure(availableSize);
			}
		}

		return new Size();
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
		foreach (var child in Children)
		{
			if (child == null)
			{
				continue;
			}

			var x = child.GetOffsetX();
			var y = child.GetOffsetY();

			if (true)//child.GetIsArrangeDirty() || child.GetIsOnArrangeDirtyPath())
			{
				//child.EnsureLayoutStorage();

				var childRect = new Rect(x, y, finalSize.Width, finalSize.Height);

				child.Arrange(childRect);
			}
		}

		return finalSize;
	}
}

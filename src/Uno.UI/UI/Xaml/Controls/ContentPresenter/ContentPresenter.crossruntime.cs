using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	private bool _isRemovingContentTemplateRoot;

	/// <remarks>
	/// MUX: CContentPresenter::ApplyTemplate parents a UIElement Content during Measure, so a presenter layout
	/// can't reach never claims it. Uno parents eagerly, but must keep that outcome: a template can bind the same
	/// element to several presenters and show only one of them, as BreadcrumbBarItem does.
	/// </remarks>
	private static bool CanBeMeasured(UIElement element)
		=> element.IsActiveInVisualTree && element.Visibility != Visibility.Collapsed && element.AreAllAncestorsVisible();

	private protected override void ApplyTemplate(out bool addedVisuals)
	{
		base.ApplyTemplate(out addedVisuals);

		// Uno specific: WinUI fails if the element is still parented elsewhere, while UIElement.AddChild moves it,
		// which two presenters being laid out would keep doing to each other.
		if (!_isRemovingContentTemplateRoot
			&& !HasTemplateChild()
			&& Content is UIElement content
			&& this.ResolveContentTemplate() is null
			&& (content.GetParent() is not UIElement parent || !CanBeMeasured(parent)))
		{
			ContentTemplateRoot = content;
		}
	}

	partial void RegisterContentTemplateRoot() => AddChild(ContentTemplateRoot);

	partial void UnregisterContentTemplateRoot()
	{
		// Unloaded handlers can run a layout pass while the root is being replaced.
		_isRemovingContentTemplateRoot = true;
		try
		{
			RemoveChild(ContentTemplateRoot);
		}
		finally
		{
			_isRemovingContentTemplateRoot = false;
		}
	}
}

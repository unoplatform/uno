#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	partial void RegisterContentTemplateRoot() => AddTemplateChild(ContentTemplateRoot);

	partial void UnregisterContentTemplateRoot() => RemoveTemplateChild();

	private protected virtual UIElement? GetTemplateChildNoRef() => ContentTemplateRoot;

	private protected virtual void AddTemplateChild(UIElement child) => AddChild(child);

	private protected virtual void RemoveTemplateChild()
	{
		if (ContentTemplateRoot is { } root)
		{
			RemoveChild(root);
		}
	}
}

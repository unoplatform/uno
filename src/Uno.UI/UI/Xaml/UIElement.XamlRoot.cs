#nullable enable

using System;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml;

public partial class UIElement : DependencyObject, IXUidProvider, IUIElement
{
	private ManagedWeakReference? _visualTreeCacheWeakReference;

	public XamlRoot? XamlRoot
	{
		get
		{
			var visualTree = VisualTree.GetForElement(this);
			if (visualTree is not null)
			{
				var xamlRoot = visualTree.GetOrCreateXamlRoot();
				return xamlRoot;
			}

			return null;
		}
		set
		{
			var xamlRoot = XamlRoot;
			if (xamlRoot == value)
			{
				return;
			}

			if (xamlRoot is not null)
			{
				throw new InvalidOperationException("Cannot change XamlRoot for existing element");
			}

			this.SetVisualTree(value!.VisualTree);
		}
	}

	internal VisualTree? VisualTreeCache
	{
		get => _visualTreeCacheWeakReference?.IsDisposed == false ?
			_visualTreeCacheWeakReference.Target as VisualTree : null;
		set => _visualTreeCacheWeakReference = WeakReferencePool.RentWeakReference(this, value);
	}
}

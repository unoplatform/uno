#nullable enable

using System;
using Uno.UI.DataBinding;
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
			if (XamlRoot == value)
			{
				return;
			}

			if (XamlRoot is not null)
			{
				throw new InvalidOperationException("Cannot change XamlRoot for existing element");
			}

			// TODO: It should be possible to set XamlRoot when still null. https://github.com/unoplatform/uno/issues/8978
		}
	}

	internal VisualTree? VisualTreeCache
	{
		get => _visualTreeCacheWeakReference?.IsDisposed == false ?
			_visualTreeCacheWeakReference.Target as VisualTree : null;
		set => _visualTreeCacheWeakReference = WeakReferencePool.RentWeakReference(this, value);
	}
}

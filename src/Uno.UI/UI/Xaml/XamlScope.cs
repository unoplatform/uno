using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// This is a helper to support resolution of StaticResource/ThemeResource references using the 'Xaml-tree scope' similar to UWP, as
	/// opposed to the visual tree. In particular it allows correct resolution during template resolution, where the visual tree may be
	/// arbitrarily distant from the xaml tree.
	/// </summary>
	internal class XamlScope
	{
		private readonly ImmutableStack<ManagedWeakReference> _resourceSources;

		public IEnumerable<ManagedWeakReference> Sources => _resourceSources;

		private XamlScope(ImmutableStack<ManagedWeakReference> sources)
		{
			_resourceSources = sources;
		}

		public XamlScope Push(ManagedWeakReference source) => new XamlScope(_resourceSources.Push(source));

		public XamlScope Pop() => new XamlScope(_resourceSources.Pop());

		public static XamlScope Create() => new XamlScope(ImmutableStack.Create<ManagedWeakReference>());
	}
}

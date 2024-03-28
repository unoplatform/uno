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
	internal record XamlScope(ImmutableStack<ManagedWeakReference> Sources)
	{
		public XamlScope Push(ManagedWeakReference source)
			=> new(Sources.Push(source));

		public XamlScope Pop()
			=> new(Sources.Pop());

		public static XamlScope Create()
			=> new(ImmutableStack.Create<ManagedWeakReference>());
	}
}

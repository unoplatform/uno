#nullable enable

using System.ComponentModel;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml
{
	public abstract partial class SetterBase : DependencyObject, IMultiParentShareableDependencyObject
	{
		internal SetterBase()
		{
			IsAutoPropertyInheritanceEnabled = false;
		}

		internal abstract void ApplyTo(DependencyObject o);

		// There shouldn't be a DependencyObject parameter. This can be removed in Uno 6 once we remove `Setter<T>`
		internal abstract bool TryGetSetterValue(out object? value, DependencyObject @do);

		internal virtual void OnStringPropertyChanged(string name) { }

		public bool IsSealed
		{
			get; private set;
		}

		internal void Seal()
			=> IsSealed = true;
	}
}


#nullable enable

namespace Microsoft.UI.Xaml
{
	public abstract partial class SetterBase : DependencyObject, IMultiParentShareableDependencyObject
	{
		internal SetterBase()
		{
			IsAutoPropertyInheritanceEnabled = false;
		}

		internal abstract void ApplyTo(DependencyObject o);

		internal abstract bool TryGetSetterValue(out object? value);

		public bool IsSealed
		{
			get; private set;
		}

		internal void Seal()
			=> IsSealed = true;
	}
}


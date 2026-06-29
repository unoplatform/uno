#nullable enable

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

		internal abstract bool TryGetSetterValue(out object? value);

		partial void OnDataContextChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			this.Log().Debug("SetterBase.DataContextChanged");
		}

		public bool IsSealed
		{
			get; private set;
		}

		internal void Seal()
			=> IsSealed = true;
	}
}


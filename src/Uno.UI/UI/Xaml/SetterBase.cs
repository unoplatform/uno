#nullable enable

using System.ComponentModel;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Windows.UI.Xaml
{
	public abstract partial class SetterBase
	{
		internal SetterBase()
		{
			IsAutoPropertyInheritanceEnabled = false;
		}

		internal abstract void ApplyTo(DependencyObject o);

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


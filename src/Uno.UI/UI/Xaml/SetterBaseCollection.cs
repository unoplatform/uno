#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml
{
	public sealed partial class SetterBaseCollection : DependencyObjectCollection<SetterBase>
	{
		public SetterBaseCollection()
		{

		}

		internal SetterBaseCollection(DependencyObject parent, bool isAutoPropertyInheritanceEnabled)
			: base(parent, isAutoPropertyInheritanceEnabled: false)
		{
		}

		public bool IsSealed { get; private set; }

		internal void Seal()
			=> IsSealed = true;

		private protected override void ValidateItem(SetterBase item)
		{
			base.ValidateItem(item);

			if (IsSealed)
			{
				throw new InvalidOperationException($"The SetterBaseCollection is sealed and cannot be modified");
			}
		}

		private protected override void OnAdded(SetterBase setterBase)
		{
			base.OnAdded(setterBase);

			if (setterBase is Setter setter && setter.Target is null)
			{
				setterBase.Seal();
			}
		}
	}
}

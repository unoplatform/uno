using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// Defines the Xaml owner of a visual tree, used to determine the target of Xaml-defined events.
	/// </summary>
	public class XamlInfo
	{
		private readonly ManagedWeakReference _owner;

		public XamlInfo(DependencyObject xamlOwner)
		{
			if (xamlOwner is IWeakReferenceProvider provider)
			{
				_owner = provider.WeakReference;
			}
			else
			{
				throw new NotSupportedException($"The provided reference must be an " + nameof(IWeakReferenceProvider));
			}
		}

		/// <summary>
		/// Gets the top-level owner of the treem when defined from a xaml file
		/// </summary>
		public DependencyObject Owner => _owner.Target as DependencyObject;

		public static XamlInfo GetXamlInfo(DependencyObject obj)
			=> (XamlInfo)obj.GetValue(XamlInfoProperty);

		public static void SetXamlInfo(DependencyObject obj, XamlInfo owner)
			=> obj.SetValue(XamlInfoProperty, owner);

		public static DependencyProperty XamlInfoProperty
		{
			[DynamicDependency(nameof(GetXamlInfo))]
			[DynamicDependency(nameof(SetXamlInfo))]
			get;
		} = DependencyProperty.RegisterAttached(
				name: nameof(XamlInfo),
				propertyType: typeof(XamlInfo),
				ownerType: typeof(XamlInfo),
				typeMetadata: new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.Inherits
				)
			);
	}
}

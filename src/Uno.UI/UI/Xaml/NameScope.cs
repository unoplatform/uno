using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class NameScope : INameScope
	{
		private Dictionary<string, ManagedWeakReference> _names = new Dictionary<string, ManagedWeakReference>();
		private ManagedWeakReference _ownerRef;

		public NameScope()
		{
		}

		public DependencyObject Owner
		{
			get => _ownerRef?.Target as DependencyObject;
			set
			{
				if (_ownerRef is { })
				{
					WeakReferencePool.ReturnWeakReference(this, _ownerRef);
				}

				_ownerRef = WeakReferencePool.RentWeakReference(this, value);
			}
		}

		public object FindName(string name)
		{
			return _names.TryGetValue(name, out var element)
				? element.Target
				: null;
		}

		public void RegisterName(string name, object scopedElement)
		{
			if (_names.ContainsKey(name))
			{
				this.Log().Warn($"The name [{name}] already exists in the current XAML scope");
			}

			_names[name] = WeakReferencePool.RentWeakReference(this, scopedElement);
		}

		public void UnregisterName(string name)
		{
			_names.Remove(name);
		}

		#region NameScope attached property

		/// <summary>
		/// Provides the attached property set accessor for the NameScope attached property.
		/// </summary>
		/// <param name="dependencyObject">Object to change XAML namescope for.</param>
		/// <param name="value">The new XAML namescope, using an interface cast.</param>
		public static void SetNameScope(DependencyObject dependencyObject, INameScope value)
		{
			dependencyObject.SetValue(NameScopeProperty, value);
		}

		/// <summary>
		/// Provides the attached property get accessor for the NameScope attached property.
		/// </summary>
		/// <param name="dependencyObject">The object to get the XAML namescope from.</param>
		/// <returns>A XAML namescope, as an INameScope instance.</returns>
		public static INameScope GetNameScope(DependencyObject dependencyObject)
		{
			return (INameScope)dependencyObject.GetValue(NameScopeProperty);
		}

		/// <summary>
		/// Identifies the NameScope attached property.
		/// </summary>
		public static DependencyProperty NameScopeProperty
		{
			[DynamicDependency(nameof(GetNameScope))]
			[DynamicDependency(nameof(SetNameScope))]
			get;
		} = DependencyProperty.RegisterAttached(
				"NameScope",
				typeof(INameScope),
				typeof(NameScope),
				new FrameworkPropertyMetadata(
					null,
					// This property is inherited to ensure the NameScope is available to all children.
					// This differs from WPF's implementation, which doesn't seem to inherit this property, 
					// but still appears to have the effect we want.
					FrameworkPropertyMetadataOptions.Inherits
				)
			);

		#endregion

		/// <summary>
		/// Search for a named element in all available namescopes, preferring scopes that are 'closest' in the hierarchy.
		/// </summary>
		internal static object FindInNamescopes(DependencyObject caller, string name)
		{
			var parent = caller;
			while (parent != null)
			{
				var scope = NameScope.GetNameScope(parent);
				var target = scope?.FindName(name);

				if (target != null)
				{
					return target;
				}

				var newParent = parent.GetParent() as DependencyObject;

				if (newParent is null && !(parent is UIElement))
				{
					// This case is about handling ElementName Bindings on non-UIElement
					// dependency objects (e.g. XAML Behaviors triggers). Those objects
					// cannot have a parent set, and in order to find ancestor scopes
					// (DataTemplate inside a DataTemplate) we need to find a known ancestor
					// through the NameScope owner.

					if (scope?.Owner is DependencyObject owner)
					{
						return FindInNamescopes(owner, name);
					}
				}

				parent = newParent;
			}

			return null;
		}
	}
}

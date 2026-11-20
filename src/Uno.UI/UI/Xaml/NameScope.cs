using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class NameScope : INameScope
	{
		private Dictionary<string, ManagedWeakReference> _names = new Dictionary<string, ManagedWeakReference>();
		private Dictionary<string, ManagedWeakReference> _codeNames;
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

		/// <summary>
		/// Registers an element whose name was set in code when it entered the live tree. Kept apart from
		/// the XAML-registered names so that only ElementName resolution sees it. The last one registered wins,
		/// as in WinUI's name table.
		/// </summary>
		internal void RegisterCodeName(string name, DependencyObject element)
		{
			_codeNames ??= new Dictionary<string, ManagedWeakReference>();

			if (_codeNames.TryGetValue(name, out var existing))
			{
				if (ReferenceEquals(existing.Target, element))
				{
					return;
				}

				WeakReferencePool.ReturnWeakReference(this, existing);
			}

			_codeNames[name] = WeakReferencePool.RentWeakReference(this, element);
		}

		internal void UnregisterCodeName(string name, DependencyObject element)
		{
			if (_codeNames is not null
				&& _codeNames.TryGetValue(name, out var existing)
				&& (existing.Target is null || ReferenceEquals(existing.Target, element)))
			{
				_codeNames.Remove(name);
				WeakReferencePool.ReturnWeakReference(this, existing);

				if (_codeNames.Count == 0)
				{
					_codeNames = null;
				}
			}
		}

		// A code name is registered after the XAML ones were parsed, so it overwrites them in WinUI's single table.
		private object FindNameOrCodeName(string name)
			=> (_codeNames is not null && _codeNames.TryGetValue(name, out var element) ? element.Target : null)
				?? FindName(name);

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
			UIElement lastElement = null;
			var sawNameScope = false;
			while (parent != null)
			{
				var scope = NameScope.GetNameScope(parent);
				sawNameScope |= scope is not null;
				var target = scope is NameScope nameScope ? nameScope.FindNameOrCodeName(name) : scope?.FindName(name);

				if (target != null)
				{
					return target;
				}

				if (parent is UIElement element)
				{
					lastElement = element;
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

			// The per-tree scope only holds names of elements outside any namescope, so only such a caller sees them.
			return sawNameScope
				? null
				: VisualTree.GetForElement(lastElement, VisualTree.LookupOptions.NoFallback)?.CodeNameScope?.FindNameOrCodeName(name);
		}
	}
}

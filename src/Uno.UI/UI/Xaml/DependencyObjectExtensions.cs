using Uno.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Runtime.CompilerServices;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Diagnostics.Eventing;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml
{
	public static partial class DependencyObjectExtensions
	{
		private static ConditionalWeakTable<object, AttachedDependencyObject> _objectData
			= new ConditionalWeakTable<object, AttachedDependencyObject>();

		private static DependencyObjectStore GetStore(object instance)
		{
			if (instance is IDependencyObjectStoreProvider provider)
			{
				return provider.Store;
			}

			return GetAttachedStore(instance);
		}

		/// <summary>
		/// Gets the attached dependency object for the specified instance.
		/// </summary>
		/// <returns>A new DependencyObject if none exists, otherwise the existing one.</returns>
		internal static DependencyObjectStore GetAttachedStore(object instance)
		{
			return ((IDependencyObjectStoreProvider)GetAttachedDependencyObject(instance)).Store;
		}

		/// <summary>
		/// Provides a DependencyObject proxy for a non-dependency object for DataBinding and x:Bind purposes
		/// </summary>
		/// <param name="instance"></param>
		/// <returns></returns>
		internal static AttachedDependencyObject GetAttachedDependencyObject(object instance)
			=> _objectData.GetValue(instance, i => new AttachedDependencyObject(i));

		/// <summary>
		/// Gets the Unique ID of the specified dependency object.
		/// </summary>
		/// <param name="dependencyObject">The possible object id</param>
		/// <returns>A unique ID</returns>
		internal static long GetDependencyObjectId(this object dependencyObject)
		{
			return GetStore(dependencyObject).GetHashCode();
		}

		/// <summary>
		/// Gets the parent dependency object, if any.
		/// </summary>
		/// <param name="dependencyObject"></param>
		/// <returns></returns>
		internal static object GetParent(this object dependencyObject)
		{
			return GetStore(dependencyObject).Parent;
		}

		/// <summary>
		/// Gets the parent dependency object, if any.
		/// </summary>
		/// <param name="dependencyObject"></param>
		/// <returns></returns>
		internal static object GetParent(this IDependencyObjectStoreProvider provider)
			=> provider.Store.Parent;

		/// <summary>
		/// Enables the use of hard references for internal variables to improve the performance
		/// </summary>
		[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
		internal static void StoreTryEnableHardReferences(this IDependencyObjectStoreProvider provider)
			=> provider.Store.TryEnableHardReferences();

		/// <summary>
		/// Disables the use of hard references for internal variables to improve the performance
		/// </summary>
		[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
		internal static void StoreDisableHardReferences(this IDependencyObjectStoreProvider provider)
			=> provider.Store.DisableHardReferences();

		/// <summary>
		/// Gets the implicit style for the current object
		/// </summary>
		[global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
		internal static Style StoreGetImplicitStyle(this IDependencyObjectStoreProvider provider, in SpecializedResourceDictionary.ResourceKey styleKey)
			=> provider.Store.GetImplicitStyle(styleKey);

		internal static IEnumerable<object> GetParents(this object dependencyObject)
		{
			var parent = dependencyObject.GetParent();
			while (parent != null)
			{
				yield return parent;
				parent = parent.GetParent();
			}
		}

		internal static bool HasParent(this object dependencyObject, DependencyObject searchedParent)
		{
			var parent = dependencyObject.GetParent();
			while (parent != null)
			{
				if (ReferenceEquals(parent, searchedParent))
				{
					return true;
				}

				parent = parent.GetParent();
			}

			return false;
		}

		/// <summary>
		/// Set the parent of the specified dependency object
		/// </summary>
		/// <remarks>
		/// This method will create a weak attached DependencyObjectStore if the object is
		/// not an <see cref="IDependencyObjectStoreProvider"/>
		/// </remarks>
		internal static void SetParent(this object dependencyObject, object parent)
			=> GetStore(dependencyObject).Parent = parent;

		/// <summary>
		/// Set the parent of the specified dependency object
		/// </summary>
		internal static void SetParent(this IDependencyObjectStoreProvider storeProvider, object parent)
			=> storeProvider.Store.Parent = parent;

		/// <summary>
		/// Tries to set the parent of the specified dependency object
		/// </summary>
		/// <returns>true if the parent could be set, otherwise false</returns>
		internal static bool TrySetParent(this object dependencyObject, object parent)
		{
			if (dependencyObject is IDependencyObjectStoreProvider provider)
			{
				SetParent(provider, parent);
				return true;
			}

			return false;
		}

		internal static void SetLogicalParent(this FrameworkElement element, DependencyObject logicalParent)
		{
			// UWP distinguishes between the 'logical parent' (or inheritance parent) and the 'visual parent' of an element. Uno already
			// recognises this distinction on some targets, but for targets using CoerceHitTestVisibility() for hit testing, the pointer
			// implementation depends upon the logical parent (ie DepObjStore.Parent) being identical to the visual parent, because it
			// piggybacks on the DP inheritance mechanism. Therefore we use LogicalParentOverride as a workaround to modify the publicly-visible
			// FrameworkElement.Parent without affecting DP propagation.
			element.LogicalParentOverride = logicalParent;
		}

		/// <summary>
		/// Parts of the internal UWP API of DependencyObject
		/// Determines if a DO is a parent of another DO
		/// </summary>
		internal static bool IsAncestorOf(this DependencyObject ancestor, DependencyObject descendant)
		{
			if (descendant is null)
			{
				return false;
			}

			var current = descendant.GetParent();
			while (current != null && ancestor != current)
			{
				current = current.GetParent();
			}

			return (ancestor == current);
		}

		/// <summary>
		/// Creates a SetValue precedence scoped override. All calls to SetValue
		/// on the specified instance will be set to the specified precedence.
		/// </summary>
		/// <param name="instance">The instance to override</param>
		/// <param name="precedence">The precedence to set</param>
		/// <returns>A disposable to dispose to cancel the override.</returns>
		internal static IDisposable OverrideLocalPrecedence(this object instance, DependencyPropertyValuePrecedences precedence)
		{
			return GetStore(instance).OverrideLocalPrecedence(precedence);
		}


		/// <summary>
		/// Gets the value for the specified dependency property on the specified instance.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <returns>The dependency property value</returns>
		public static object GetValue(this object instance, DependencyProperty property)
		{
			return GetStore(instance).GetValue(property);
		}

		/// <summary>
		/// Get the value for the specified dependency property on the specific instance at the specified precedence level
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <param name="precedence">The value precedence to fetch</param>
		/// <returns></returns>
		public static object GetValue(this object instance, DependencyProperty property, DependencyPropertyValuePrecedences? precedence)
		{
			return GetStore(instance).GetValue(property);
		}

		/// <summary>
		/// Returns the local value of a dependency property, if a local value is set.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		public static object ReadLocalValue(this object instance, DependencyProperty property)
		{
			return GetStore(instance).ReadLocalValue(property);
		}

		internal static object GetBaseValue(this object instance, DependencyProperty property)
		{
			var (value, _) = GetStore(instance).GetBaseValue(property);
			return value;
		}

		/// <summary>
		/// Clears the value for the specified dependency property on the specified instance.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <param name="precedence">The value precedence to assign</param>
		internal static void ClearValue(this DependencyObject instance, DependencyProperty property, DependencyPropertyValuePrecedences precedence)
		{
			SetValue(instance, property, DependencyProperty.UnsetValue, precedence);
		}

		/// <summary>
		/// Sets the value of the specified dependency property on the specified instance.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <param name="value">The value to set</param>
		public static void SetValue(this object instance, DependencyProperty property, object value)
		{
			GetStore(instance).SetValue(property, value, DependencyPropertyValuePrecedences.Local);
		}

		/// <summary>
		/// Sets the value of the specified dependency property on the specified instance.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <param name="value">The value to set</param>
		/// <param name="precedence">The value precedence to assign</param>
		public static void SetValue(this DependencyObject instance, DependencyProperty property, object value, DependencyPropertyValuePrecedences? precedence)
		{
			GetStore(instance).SetValue(property, value, precedence ?? DependencyPropertyValuePrecedences.Local);
		}

		/// <summary>
		/// Sets the value of the specified dependency property on the specified instance.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <param name="value">The value to set</param>
		/// <param name="precedence">The value precedence to assign</param>
		public static void SetValue(this object instance, DependencyProperty property, object value, DependencyPropertyValuePrecedences? precedence)
		{
			GetStore(instance).SetValue(property, value, precedence ?? DependencyPropertyValuePrecedences.Local);
		}

		/// <summary>
		/// Coerces the value of the specified dependency property.
		/// This is accomplished by invoking any CoerceValueCallback function specified in
		/// property metadata for the dependency property as it exists on the calling DependencyObject.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		internal static void CoerceValue(this IDependencyObjectStoreProvider storeProvider, DependencyProperty property)
		{
			storeProvider.Store.CoerceValue(property);
		}

		/// <summary>
		/// Register for changes dependency property changes notifications.
		/// </summary>
		/// <param name="instance">The instance that owns the property</param>
		/// <param name="property">The property to observe</param>
		/// <param name="callback">The callback</param>
		/// <returns>A disposable that will unregister the callback when disposed.</returns>
		public static IDisposable RegisterDisposablePropertyChangedCallback(this object instance, DependencyProperty property, PropertyChangedCallback callback)
		{
			return GetStore(instance).RegisterPropertyChangedCallback(property, callback);
		}

		/// <summary>
		/// Register for changes all dependency properties changes notifications for the specified instance.
		/// </summary>
		/// <param name="instance">The instance for which to observe properties changes</param>
		/// <param name="callback">The callback</param>
		/// <returns>A disposable that will unregister the callback when disposed.</returns>
		public static IDisposable RegisterDisposablePropertyChangedCallback(this object instance, ExplicitPropertyChangedCallback handler)
		{
			return GetStore(instance).RegisterPropertyChangedCallback(handler);
		}

		/// <summary>
		/// Registers a notification function for listening to changes to a tree of DependencyProperties relative to this DependencyObject instance.
		/// </summary>
		/// <param name="instance">The DependencyObject instance the dependency property tree starts at.</param>
		/// <param name="callback">A callback based on the PropertyChangedCallback delegate, which the system invokes when the value of any of the specified properties changes.</param>
		/// <param name="properties">The tree of dependency property to register for property-changed notification.</param>
		/// <returns>A disposable that will unregister the callback when disposed.</returns>
		/// <remarks>
		/// <para>Each node of the dependency property tree is represented by an array describing the path of the dependency property relative to the given dependency object instance.</para>
		/// <para>For example, to register for notifications to changes to the Color of a TextBlock's Foreground:</para>
		/// <code>var disposable = myTextBlock.RegisterDisposableNestedPropertyChangedCallback(callback, new [] { TextBlock.ForegroundProperty, SolidColorBrush.ColorProperty });</code>
		/// </remarks>
		internal static IDisposable RegisterDisposableNestedPropertyChangedCallback(this DependencyObject instance, PropertyChangedCallback callback, params DependencyProperty[][] properties)
		{
			if (instance == null)
			{
				return Disposable.Empty;
			}

			var disposables = properties
				.Where(Enumerable.Any)
				.GroupBy(Enumerable.First, propertyPath => propertyPath.Skip(1).ToArray())
				.Where(Enumerable.Any)
				.Where(group => group.Key != null)
				.Select(group =>
				{
					var property = group.Key;
					var subProperties = group.ToArray();

					// Validate the property owner type
					if (!instance.GetType().Is(property.OwnerType) && !property.IsAttached)
					{
						return Disposable.Empty;
					}

					var childDisposable = new SerialDisposable();

					childDisposable.Disposable = (instance.GetValue(property) as DependencyObject)?.RegisterDisposableNestedPropertyChangedCallback(callback, subProperties);

					var disposable = instance.RegisterDisposablePropertyChangedCallback(property, (s, e) =>
					{
						callback(s, e);

						childDisposable.Disposable = s?.RegisterDisposableNestedPropertyChangedCallback(callback, subProperties);
					});

					return new CompositeDisposable(disposable, childDisposable);
				});
			return new CompositeDisposable(disposables);
		}

		/// <summary>
		/// Registers to parent changes.
		/// </summary>
		/// <param name="instance">The target dependency object</param>
		/// <param name="key">A key to be passed to the callback parameter.</param>
		/// <param name="handler">A callback to be called</param>
		/// <returns>A disposable that cancels the subscription.</returns>
		internal static IDisposable RegisterParentChangedCallback(this DependencyObject instance, object key, ParentChangedCallback handler)
		{
			return GetStore(instance).RegisterParentChangedCallback(key, handler);
		}

		/// <summary>
		/// Registers to parent changes.
		/// </summary>
		/// <param name="instance">The target dependency object</param>
		/// <param name="key">A key to be passed to the callback parameter.</param>
		/// <param name="handler">A callback to be called</param>
		/// <returns>A disposable that cancels the subscription.</returns>
		internal static void RegisterParentChangedCallbackStrong(this DependencyObject instance, object key, ParentChangedCallback handler)
			=> GetStore(instance).RegisterParentChangedCallbackStrong(key, handler);

		/// <summary>
		/// Determines if the specified dependency property is set.
		/// A property is set whenever a value (including null) is assigned to it.
		/// </summary>
		/// <param name="dependencyObject">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to test</param>
		/// <returns>True if the dependency property is set. False otherwise.</returns>
		internal static bool IsDependencyPropertySet(this DependencyObject dependencyObject, DependencyProperty property)
		{
			return GetStore(dependencyObject)
				.GetCurrentHighestValuePrecedence(property) != DependencyPropertyValuePrecedences.DefaultValue;
		}

		/// <summary>
		/// True if a value is set on the property with <see cref="DependencyPropertyValuePrecedences.Local"/> precedence or higher, false otherwise.
		/// </summary>
		/// <param name="dependencyObject">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to test</param>
		internal static bool IsDependencyPropertyLocallySet(this DependencyObject dependencyObject, DependencyProperty property) =>
			GetStore(dependencyObject).GetCurrentHighestValuePrecedence(property) <= DependencyPropertyValuePrecedences.Local;

		internal static DependencyPropertyValuePrecedences GetCurrentHighestValuePrecedence(this DependencyObject dependencyObject, DependencyProperty property)
		{
			return GetStore(dependencyObject).GetCurrentHighestValuePrecedence(property);
		}

		internal static DependencyPropertyValuePrecedences GetBaseValueSource(this DependencyObject dependencyObject, DependencyProperty property)
		{
			var precedence = dependencyObject.GetCurrentHighestValuePrecedence(property);
			// TODO: Bring this closer to CFrameworkElement::IsPropertySetByStyle from WinUI.
			if (precedence is DependencyPropertyValuePrecedences.DefaultStyle or DependencyPropertyValuePrecedences.ImplicitStyle or DependencyPropertyValuePrecedences.ExplicitStyle)
			{
				return DependencyPropertyValuePrecedences.ExplicitStyle;
			}
			else if (precedence == DependencyPropertyValuePrecedences.DefaultValue)
			{
				return DependencyPropertyValuePrecedences.DefaultValue;
			}
			else
			{
				return DependencyPropertyValuePrecedences.Local;
			}
		}

		internal static void InvalidateMeasure(this DependencyObject d)
		{
			var uielement = d as UIElement ?? d.GetParents().OfType<UIElement>().FirstOrDefault();
			uielement?.InvalidateMeasure();
		}

		internal static void InvalidateArrange(this DependencyObject d)
		{
			var uielement = d as UIElement ?? d.GetParents().OfType<UIElement>().FirstOrDefault();
			uielement?.InvalidateArrange();
		}

		/// <summary>
		/// See <see cref="DependencyObjectStore.RegisterPropertyChangedCallbackStrong(ExplicitPropertyChangedCallback)"/> for more details
		/// </summary>
		internal static void RegisterPropertyChangedCallbackStrong(this IDependencyObjectStoreProvider storeProvider, ExplicitPropertyChangedCallback handler)
			=> storeProvider.Store.RegisterPropertyChangedCallbackStrong(handler);

		// TODO Uno: MUX uses a IsRightToLeft virtual method on DependencyObject. This
		// allows for some customization - e.g. glyphs should not respect this.
		internal static bool IsRightToLeft(this DependencyObject dependencyObject) =>
			dependencyObject is FrameworkElement fw && fw.FlowDirection == FlowDirection.RightToLeft;

		internal static DependencyObject GetTemplatedParent(this DependencyObject @do)
		{
			return (@do as IDependencyObjectStoreProvider)?.Store.GetTemplatedParent2();
		}

		internal static void SetTemplatedParent(this DependencyObject @do, DependencyObject tp)
		{
			(@do as IDependencyObjectStoreProvider)?.Store.SetTemplatedParent2(tp);
		}

		internal static bool HasLocalOrModifierValue(this DependencyObject @do, DependencyProperty dp)
		{
			return (@do as IDependencyObjectStoreProvider).Store.HasLocalOrModifierValue(dp);
		}
	}
}

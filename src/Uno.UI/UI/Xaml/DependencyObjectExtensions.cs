using Uno.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using System.Runtime.CompilerServices;
using Uno.Extensions;
using Uno.Logging;
using Uno.Diagnostics.Eventing;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml
{
	public static class DependencyObjectExtensions
	{
		private static ConditionalWeakTable<object, DependencyObject> _objectData
			= new ConditionalWeakTable<object, DependencyObject>();

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
			return ((IDependencyObjectStoreProvider)_objectData.GetValue(instance, i => new AttachedDependencyObject(i))).Store;
		}

		/// <summary>
		/// Gets the Unique ID of the specified dependency object.
		/// </summary>
		/// <param name="dependencyObject">The possible object id</param>
		/// <returns>A unique ID</returns>
		internal static long GetDependencyObjectId(this object dependencyObject)
		{
			return GetStore(dependencyObject).ObjectId;
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

		internal static IEnumerable<object> GetParents(this object dependencyObject)
		{
			var parent = dependencyObject.GetParent();
			while (parent != null)
			{
				yield return parent;
				parent = parent.GetParent();
			}
		}

		/// <summary>
		/// Set the parent of the specified dependency object
		/// </summary>
		internal static void SetParent(this object dependencyObject, object parent)
		{
			GetStore(dependencyObject).Parent = parent;
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
			return GetStore(instance).GetValue(property, precedence);
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

		/// <summary>
		/// Gets the value for the specified dependency property on the specific instance at
		/// the specified precedence.  As opposed to GetValue, this will not fall back to the highest
		/// precedence if this precedence is currently unset and will return unset value.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <param name="precedence">The value precedence at which to fetch a value</param>
		/// <returns></returns>
		internal static object GetPrecedenceSpecificValue(this DependencyObject instance, DependencyProperty property, DependencyPropertyValuePrecedences precedence)
		{
			return GetStore(instance).GetValue(property, precedence, true);
		}


		internal static void PropagateInheritedProperties(this DependencyObject instance)
		{
			GetStore(instance).PropagateInheritedProperties();
		}

		/// <summary>
		/// Get the value for the specified dependency property on the specific instance at 
		/// the highest precedence level under the specified one.
		/// E.G. If a property has a value both on the Animation, Local and Default 
		/// precedences, and the given precedence is Animation, then the Local value is returned.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <param name="precedence">The value precedence under which to fetch a value</param>
		/// <returns></returns>
		internal static (object value, DependencyPropertyValuePrecedences precedence) GetValueUnderPrecedence(this DependencyObject instance, DependencyProperty property, DependencyPropertyValuePrecedences precedence)
		{
			return GetStore(instance).GetValueUnderPrecedence(property, precedence);
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
		internal static void CoerceValue(this object instance, DependencyProperty property)
		{
			GetStore(instance).CoerceValue(property);
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

			return properties
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
				})
				.Apply(disposables => new CompositeDisposable(disposables));
		}

		/// <summary>
		/// Register for changes all dependency properties changes notifications for the specified instance.
		/// </summary>
		/// <param name="instance">The instance for which to observe properties changes</param>
		/// <param name="handler">The callback</param>
		/// <returns>A disposable that will unregister the callback when disposed.</returns>
		internal static IDisposable RegisterInheritedPropertyChangedCallback(this object instance, ExplicitPropertyChangedCallback handler)
		{
			return GetStore(instance).RegisterInheritedPropertyChangedCallback(handler);
		}

		/// <summary>
		/// Register for compiled bindings updates propagation
		/// </summary>
		/// <param name="instance">The instance for which to observe compiled bindings updates</param>
		/// <param name="handler">The callback</param>
		/// <returns>A disposable that will unregister the callback when disposed.</returns>
		internal static IDisposable RegisterCompiledBindingsUpdateCallback(this object instance, Action handler) 
			=> GetStore(instance).RegisterCompiledBindingsUpdateCallback(handler);

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

		internal static DependencyPropertyValuePrecedences GetCurrentHighestValuePrecedence(this DependencyObject dependencyObject, DependencyProperty property)
		{
			return GetStore(dependencyObject).GetCurrentHighestValuePrecedence(property);
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

		internal static void InvalidateRender(this DependencyObject d)
		{
			var uielement = d as UIElement ?? d.GetParents().OfType<UIElement>().FirstOrDefault();
			uielement?.InvalidateRender();
		}
	}
}

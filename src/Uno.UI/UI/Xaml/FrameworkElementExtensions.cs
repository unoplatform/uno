#if !SILVERLIGHT
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;

#if NETFX_CORE
using Windows.UI.Xaml;
using IFrameworkElement = Windows.UI.Xaml.FrameworkElement;
using IBinder = Windows.UI.Xaml.FrameworkElement;
using Windows.UI.Xaml.Data;
#elif XAMARIN
using Windows.UI.Xaml.Data;
using Uno.UI.DataBinding;
#else
using Windows.UI.Xaml.Data;
using Uno.UI.DataBinding;
#endif

namespace Windows.UI.Xaml
{
	public static class FrameworkElementExtensions
	{
		public static T Style<T>(this T element, Style style) where T : IFrameworkElement
		{
#if NETFX_CORE
			style.ApplyTo(element);
#else
			element.Style = style;
#endif
			return element;
		}

		public static T Binding<T>(this T element, string property, string propertyPath, string converter) where T : IDependencyObjectStoreProvider
		{
			var dependencyProperty = GetDependencyProperty(element, property);
			var path = new PropertyPath(propertyPath.Replace("].[", "]["));
			var binding = new Windows.UI.Xaml.Data.Binding { Path = path, Converter = ResourceHelper.FindConverter(converter) };

			(element as IDependencyObjectStoreProvider).Store.SetBinding(dependencyProperty, binding);

			return element;
		}

		public static T Binding<T>(this T element, string property, string propertyPath, object source, BindingMode mode) where T : DependencyObject
		{
			return element.Binding(property,
				new Data.Binding()
				{
					Path = propertyPath,
					Source = source,
					Mode = mode
				}
			);
		}

		public static T Binding<T>(this T element, string property, BindingBase binding) where T : DependencyObject
		{
#if NETFX_CORE
			var dependencyProperty = GetDependencyProperty(element, property);

			element.SetBinding(dependencyProperty, binding);
#else
			(element as IDependencyObjectStoreProvider).Store.SetBinding(property, binding);
#endif

			return element;
		}

		private static DependencyProperty GetDependencyProperty(object element, string propertyName)
		{
			var dependencyProperty = GetDependencyPropertyFromProperties(element, propertyName);

			if (dependencyProperty == null)
			{
				dependencyProperty = GetDependencyPropertyFromFields(element, propertyName);
			}

			if (dependencyProperty == null)
			{
				throw new InvalidOperationException("Unable to find the dependency property [{0}]".InvariantCultureFormat(propertyName));
			}

			return dependencyProperty;
		}

		private static DependencyProperty GetDependencyPropertyFromFields(object element, string property)
		{
			FieldInfo fieldInfo = null;
			var currentType = element.GetType();
			do
			{
				fieldInfo = currentType.GetTypeInfo().GetDeclaredField(property + "Property");

				if (fieldInfo == null)
				{
					currentType = currentType.GetTypeInfo().BaseType;
				}

			}
			while (currentType != null && fieldInfo == null);

			if (fieldInfo != null)
			{
				var dependencyProperty = (DependencyProperty)fieldInfo.GetValue(null);
				return dependencyProperty;
			}

			return null;
		}

		private static DependencyProperty GetDependencyPropertyFromProperties(object element, string property)
		{
			PropertyInfo propertyInfo = null;
			var currentType = element.GetType();

			do
			{
				propertyInfo = currentType.GetTypeInfo().GetDeclaredProperty(property + "Property");

				if (propertyInfo == null)
				{
					currentType = currentType.GetTypeInfo().BaseType;
				}

			} while (currentType != null && propertyInfo == null);

			if (propertyInfo != null)
			{
				var dependencyProperty = (DependencyProperty)propertyInfo.GetMethod.Invoke(null, new object[0]);
				return dependencyProperty;
			}


			return null;
		}

		public static T Binding<T>(this T element, string property, string propertyPath) where T : DependencyObject
		{
			var path = new PropertyPath(propertyPath.Replace("].[", "]["));
			var binding = new Windows.UI.Xaml.Data.Binding { Path = path };

			return element.Binding(property, binding);
		}

		public static Windows.UI.Xaml.Documents.Run Binding(
			this Windows.UI.Xaml.Documents.Run element,
			string property,
			string propertyPath
		)
		{
			propertyPath = propertyPath.Replace("].[", "][");

			if (property == "Text")
			{
				var templateString = "<Run xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" Text=\"{{Binding {0}}}\" />";

				return (Windows.UI.Xaml.Documents.Run)Windows.UI.Xaml.Markup.XamlReader.Load(templateString.InvariantCultureFormat(propertyPath));
			}
			else
			{
				var path = new PropertyPath(propertyPath);
				var binding = new Windows.UI.Xaml.Data.Binding { Path = path };

				var dependencyProperty = GetDependencyProperty(element, property);

				BindingOperations.SetBinding(element, dependencyProperty, binding);

				return element;
			}
		}

		public static T Margin<T>(this T element, Thickness margin) where T : IFrameworkElement
		{
			element.Margin = margin;
			return element;
		}

		public static T Name<T>(this T element, string name) where T : IFrameworkElement
		{
			element.Name = name;
			return element;
		}

		public static T MaxWidth<T>(this T element, float maxWidth) where T : IFrameworkElement
		{
			element.MaxWidth = maxWidth;
			return element;
		}

		public static T MaxHeight<T>(this T element, float maxHeight) where T : IFrameworkElement
		{
			element.MaxHeight = maxHeight;
			return element;
		}

		public static T Margin<T>(this T element, float left, float top, float right, float bottom)
			where T : IFrameworkElement
		{
			return element.Margin(new Thickness(left, top, right, bottom));
		}

		public static T Margin<T>(this T element, float leftRight, float topBottom)
			where T : IFrameworkElement
		{
			return element.Margin(new Thickness(leftRight, topBottom, leftRight, topBottom));
		}

		/// <summary>
		/// Bind property on <param name="element"/> to a property on <param name="source"/> of the same name.
		/// </summary>
		internal static T BindToEquivalentProperty<T>(this T element, object source, string property, BindingMode bindingMode = BindingMode.OneWay) where T : DependencyObject
		{
			return element.Binding(property, property, source, bindingMode);
		}

		internal static Thickness? GetPadding(this IFrameworkElement frameworkElement)
		{
			switch (frameworkElement)
			{
				case Grid g:
					return g.Padding;

				case StackPanel sp:
					return sp.Padding;

				case Control c:
					return c.Padding;

				case ContentPresenter cp:
					return cp.Padding;

				case Border b:
					return b.Padding;

				case Panel p:
					return p.Padding;
			}

			return null;
		}
	}
}
#endif

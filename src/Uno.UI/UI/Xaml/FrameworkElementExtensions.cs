#if !SILVERLIGHT
using System;
using System.Reflection;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using Uno.UI;

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
#endif

namespace Windows.UI.Xaml
{
	internal static class FrameworkElementExtensions
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
				var dependencyProperty = (DependencyProperty)propertyInfo.GetMethod.Invoke(null, Array.Empty<object>());
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
			return element.Margin(new Thickness(leftRight, topBottom));
		}

		/// <summary>
		/// Bind property on <param name="element"/> to a property on <param name="source"/> of the same name.
		/// </summary>
		internal static T BindToEquivalentProperty<T>(this T element, object source, string property, BindingMode bindingMode = BindingMode.OneWay) where T : DependencyObject
		{
			return element.Binding(property, property, source, bindingMode);
		}

		internal static bool TryGetPadding(this IFrameworkElement frameworkElement, out Thickness padding)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Grid_Available && frameworkElement is Grid g)
			{
				padding = g.Padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_StackPanel_Available && frameworkElement is StackPanel sp)
			{
				padding = sp.Padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_RelativePanel_Available && frameworkElement is RelativePanel rp)
			{
				padding = rp.Padding;
				return true;
			}

			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_LayoutPanel_Available && frameworkElement is Microsoft/* UWP don't rename */.UI.Xaml.Controls.LayoutPanel lp)
			{
				padding = lp.Padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Control_Available && frameworkElement is Control c)
			{
				padding = c.Padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_ContentPresenter_Available && frameworkElement is ContentPresenter cp)
			{
				padding = cp.Padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Border_Available && frameworkElement is Border b)
			{
				padding = b.Padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_ItemsPresenter_Available && frameworkElement is ItemsPresenter ip)
			{
				padding = ip.Padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_TextBlock_Available && frameworkElement is TextBlock tb)
			{
				padding = tb.Padding;
				return true;
			}

			padding = default;
			return false;
		}

		internal static bool TrySetPadding(this IFrameworkElement frameworkElement, Thickness padding)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_TextBlock_Available && frameworkElement is Grid g)
			{
				g.Padding = padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_StackPanel_Available && frameworkElement is StackPanel sp)
			{
				sp.Padding = padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_RelativePanel_Available && frameworkElement is RelativePanel rp)
			{
				rp.Padding = padding;
				return true;
			}

			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_LayoutPanel_Available && frameworkElement is Microsoft/* UWP don't rename */.UI.Xaml.Controls.LayoutPanel lp)
			{
				lp.Padding = padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Control_Available && frameworkElement is Control c)
			{
				c.Padding = padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_ContentPresenter_Available && frameworkElement is ContentPresenter cp)
			{
				cp.Padding = padding;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Border_Available && frameworkElement is Border b)
			{
				b.Padding = padding;
				return true;
			}

			return false;
		}

#if __WASM__
		/// <summary>
		/// This method retrieves the actual border thickness of given FrameworkElement.
		/// Note that for Control.BorderThickness is not actually applied unless
		/// it is used somewhere within the ControlTemplate.
		/// </summary>
		/// <param name="frameworkElement">Framework element.</param>
		/// <param name="borderThickness">Thickness.</param>
		/// <returns>Whether the given element has an actual border.</returns>
		internal static bool TryGetActualBorderThickness(this IFrameworkElement frameworkElement, out Thickness borderThickness)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Panel_Available && frameworkElement is Panel { BorderBrushInternal: not null } p)
			{
				borderThickness = p.BorderThicknessInternal;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_ContentPresenter_Available && frameworkElement is ContentPresenter { BorderBrush: not null } cp)
			{
				borderThickness = cp.BorderThickness;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Border_Available && frameworkElement is Border { BorderBrush: not null } b)
			{
				borderThickness = b.BorderThickness;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_CalendarViewDayItem_Available && frameworkElement is CalendarViewDayItem calendarViewDayItem)
			{
				borderThickness = calendarViewDayItem.EffectiveBorderThickness;
				return true;
			}

			borderThickness = default;
			return false;
		}
#endif

		internal static bool TryGetBorderThickness(this IFrameworkElement frameworkElement, out Thickness borderThickness)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Panel_Available && frameworkElement is Panel p)
			{
				borderThickness = p.BorderThicknessInternal;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Control_Available && frameworkElement is Control c)
			{
				borderThickness = c.BorderThickness;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_ContentPresenter_Available && frameworkElement is ContentPresenter cp)
			{
				borderThickness = cp.BorderThickness;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Border_Available && frameworkElement is Border b)
			{
				borderThickness = b.BorderThickness;
				return true;
			}

			borderThickness = default;
			return false;
		}

		internal static bool TrySetBorderThickness(this IFrameworkElement frameworkElement, Thickness borderThickness)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Grid_Available && frameworkElement is Grid g)
			{
				g.BorderThickness = borderThickness;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_StackPanel_Available && frameworkElement is StackPanel sp)
			{
				sp.BorderThickness = borderThickness;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_RelativePanel_Available && frameworkElement is RelativePanel rp)
			{
				rp.BorderThickness = borderThickness;
				return true;
			}

			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_LayoutPanel_Available && frameworkElement is Microsoft/* UWP don't rename */.UI.Xaml.Controls.LayoutPanel lp)
			{
				lp.BorderThickness = borderThickness;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Control_Available && frameworkElement is Control c)
			{
				c.BorderThickness = borderThickness;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_ContentPresenter_Available && frameworkElement is ContentPresenter cp)
			{
				cp.BorderThickness = borderThickness;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Border_Available && frameworkElement is Border b)
			{
				b.BorderThickness = borderThickness;
				return true;
			}

			return false;
		}

		internal static bool TryGetCornerRadius(this IFrameworkElement frameworkElement, out CornerRadius cornerRadius)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Grid_Available && frameworkElement is Grid g)
			{
				cornerRadius = g.CornerRadius;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_StackPanel_Available && frameworkElement is StackPanel sp)
			{
				cornerRadius = sp.CornerRadius;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_RelativePanel_Available && frameworkElement is RelativePanel rp)
			{
				cornerRadius = rp.CornerRadius;
				return true;
			}

			if (__LinkerHints.Is_Microsoft_UI_Xaml_Controls_LayoutPanel_Available && frameworkElement is Microsoft/* UWP don't rename */.UI.Xaml.Controls.LayoutPanel lp)
			{
				cornerRadius = lp.CornerRadius;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Control_Available && frameworkElement is Control c)
			{
				cornerRadius = c.CornerRadius;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_ContentPresenter_Available && frameworkElement is ContentPresenter cp)
			{
				cornerRadius = cp.CornerRadius;
				return true;
			}

			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_Border_Available && frameworkElement is Border b)
			{
				cornerRadius = b.CornerRadius;
				return true;
			}

			cornerRadius = default;
			return false;
		}
	}
}
#endif

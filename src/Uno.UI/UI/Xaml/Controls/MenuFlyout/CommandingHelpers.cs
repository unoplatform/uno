using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;

using ICommand = System.Windows.Input.ICommand;

namespace Windows.UI.Xaml.Controls
{
	public class CommandingHelpers
	{
		class IconSourceToIconSourceElementConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, string language)
			{
				if (value != null)
				{
					var valueAsI = value;

					IconSource valueAsIconSource;
					IconSourceElement returnValueAsIconElement = new IconSourceElement();

					valueAsIconSource = valueAsI as IconSource;

					returnValueAsIconElement.IconSource = valueAsIconSource;

					return returnValueAsIconElement;
				}
				else
				{
					return null;
				}
			}

			public object ConvertBack(object value, Type targetType, object parameter, string language)
			{
				throw new NotImplementedException();
			}
		}

		class KeyboardAcceleratorCopyConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, string language)
			{
				if (value != null)
				{
					object valueAsI = value;

					IList<KeyboardAccelerator> valueAsKeyboardAccelerators;
					var returnValueAsKeyboardAcceleratorCollection = new DependencyObjectCollection<KeyboardAccelerator>();

					valueAsKeyboardAccelerators = valueAsI as IList<KeyboardAccelerator>;
					int keyboardAcceleratorCount;

					keyboardAcceleratorCount = valueAsKeyboardAccelerators.Count;

					// Keyboard accelerators can't have two parents,
					// so we'll need to copy them and bind to the original properties
					// instead of assigning them.
					// We set up bindings so that modifications to the app-defined accelerators
					// will propagate to the accelerators that are used by the framework.
					for (int i = 0; i < keyboardAcceleratorCount; i++)
					{
						KeyboardAccelerator keyboardAccelerator = valueAsKeyboardAccelerators[i];
						KeyboardAccelerator keyboardAcceleratorCopy = new KeyboardAccelerator();

						keyboardAcceleratorCopy.SetBinding(KeyboardAccelerator.IsEnabledProperty, new Binding { Path = "IsEnabled", Source = keyboardAccelerator });
						keyboardAcceleratorCopy.SetBinding(KeyboardAccelerator.KeyProperty, new Binding { Path = "Key", Source = keyboardAccelerator });
						keyboardAcceleratorCopy.SetBinding(KeyboardAccelerator.ModifiersProperty, new Binding { Path = "Modifiers", Source = keyboardAccelerator });
						keyboardAcceleratorCopy.SetBinding(KeyboardAccelerator.ScopeOwnerProperty, new Binding { Path = "ScopeOwner", Source = keyboardAccelerator });
					}

					return returnValueAsKeyboardAcceleratorCollection;
				}
				else
				{
					return null;
				}
			}

			public object ConvertBack(object value, Type targetType, object parameter, string language)
			{
				throw new NotImplementedException();
			}
		}

		internal static void BindToLabelPropertyIfUnset(
			 ICommand uiCommand,
			 DependencyObject target,
			 DependencyProperty labelProperty)
		{
			string localLabel = null;
			var localLabelAsI = target.ReadLocalValue(labelProperty);

			if (localLabelAsI != null)
			{
				localLabel = localLabelAsI.ToString();
			}

			if (localLabelAsI == DependencyProperty.UnsetValue || string.IsNullOrEmpty(localLabel))
			{
				if (target is IDependencyObjectStoreProvider dosp)
				{
					dosp.Store.SetBinding(labelProperty, new Binding { Path = "Label", Source = uiCommand });
				}
			}
		}

		internal static void BindToIconPropertyIfUnset(
			 XamlUICommand uiCommand,
			 DependencyObject target,
			 DependencyProperty iconProperty)
		{
			IconElement localIcon;
			object localIconAsI = target.ReadLocalValue(iconProperty);

			localIcon = localIconAsI as IconElement;

			if (localIconAsI == DependencyProperty.UnsetValue || localIcon == null)
			{
				if (target is IDependencyObjectStoreProvider dosp)
				{
					IconSourceToIconSourceElementConverter converter = new IconSourceToIconSourceElementConverter();
					dosp.Store.SetBinding(iconProperty, new Binding { Path = "IconSource", Source = uiCommand, Converter = converter });
				}
			}
		}

		internal static void BindToIconSourcePropertyIfUnset(
			 XamlUICommand uiCommand,
			 DependencyObject target,
			 DependencyProperty iconSourceProperty)
		{
			object localIconSourceAsI;
			IconSource localIconSource;
			localIconSourceAsI = target.ReadLocalValue(iconSourceProperty);

			localIconSource = localIconSourceAsI as IconSource;

			if (localIconSourceAsI == DependencyProperty.UnsetValue || localIconSource == null)
			{
				if (target is IDependencyObjectStoreProvider dosp)
				{
					dosp.Store.SetBinding(iconSourceProperty, new Binding { Path = "IconSource", Source = uiCommand });
				}
			}
		}

		internal static void BindToKeyboardAcceleratorsIfUnset(
			 XamlUICommand uiCommand,
			 UIElement target)
		{
			IList<KeyboardAccelerator> targetKeyboardAccelerators;
			int targetKeyboardAcceleratorCount;

			targetKeyboardAccelerators = target.KeyboardAccelerators;
			targetKeyboardAcceleratorCount = targetKeyboardAccelerators.Count;

			if (targetKeyboardAcceleratorCount == 0)
			{
				var converter = new KeyboardAcceleratorCopyConverter();
				target.SetBinding(UIElement.KeyboardAcceleratorsProperty, new Binding { Path = "KeyboardAccelerators", Source = uiCommand, Converter = converter });
			}
		}

		internal static void BindToAccessKeyIfUnset(
			 XamlUICommand uiCommand,
			 UIElement target)
		{
			string localAccessKey;
			localAccessKey = target.AccessKey;

			if (localAccessKey == null || string.IsNullOrEmpty(localAccessKey))
			{
				target.SetBinding(UIElement.AccessKeyProperty, new Binding { Path = "AccessKey", Source = uiCommand });
			}
		}

		internal static void BindToDescriptionPropertiesIfUnset(
			 XamlUICommand uiCommand,
			 FrameworkElement target)
		{
			string localHelpText = AutomationProperties.GetHelpText(target);

			if (localHelpText == null || string.IsNullOrEmpty(localHelpText))
			{
				target.SetBinding(AutomationProperties.HelpTextProperty, new Binding { Path = "Description", Source = uiCommand });
			}

			object localToolTipAsI;
			localToolTipAsI = ToolTipService.GetToolTip(target);

			string localToolTipAsString = null;
			ToolTip localToolTip = null;

			if (localToolTipAsI != null)
			{
				localToolTipAsString = localToolTipAsI.ToString();
				localToolTip = localToolTipAsI as ToolTip;
			}

			if ((localToolTipAsString == null || string.IsNullOrEmpty(localToolTipAsString)) && localToolTip == null)
			{
				target.SetBinding(ToolTipService.ToolTipProperty, new Binding { Path = "Description", Source = uiCommand });
			}
		}

		internal static void ClearBindingIfSet(
			 ICommand uiCommand,
			 FrameworkElement target,
			 DependencyProperty targetProperty)
		{
			BindingExpression bindingExpression;
			bindingExpression = target.GetBindingExpression(targetProperty);

			if (bindingExpression != null)
			{
				object bindingSource;
				bindingSource = bindingExpression.ParentBinding.Source;

				if (bindingSource != null && bindingSource == uiCommand)
				{
					target.ClearValue(targetProperty);
				}
			}
		}
	}
}

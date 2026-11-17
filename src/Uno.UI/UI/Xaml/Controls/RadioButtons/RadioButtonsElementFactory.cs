using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	internal class RadioButtonsElementFactory : ElementFactory
	{
		private IElementFactory m_itemTemplateWrapper = null;

		// MUX Reference RadioButtonsElementFactory.cpp (UserElementFactory), main
		internal void UserElementFactory(object newValue)
		{
			if (newValue is DataTemplate dataTemplate)
			{
				m_itemTemplateWrapper = new ItemTemplateWrapper(dataTemplate);
			}
			else if (newValue is DataTemplateSelector selector)
			{
				m_itemTemplateWrapper = new ItemTemplateWrapper(selector);
			}
			else if (newValue is IElementFactory customElementFactory)
			{
				m_itemTemplateWrapper = customElementFactory;
			}
#if HAS_UNO
			// TODO Uno: remove with IElementFactoryShim.
			else if (newValue is IElementFactoryShim shim)
			{
				m_itemTemplateWrapper = new ElementFactoryShimAdapter(shim);
			}
#endif
		}

		private protected override UIElement GetElementCore(global::Microsoft.UI.Xaml.ElementFactoryGetArgs args)
		{
			object GetContent(IElementFactory itemTemplateWrapper)
			{
				if (itemTemplateWrapper != null)
				{
					return itemTemplateWrapper.GetElement(args);
				}
				return args.Data;
			}

			var newContent = GetContent(m_itemTemplateWrapper);

			// Element is already a RadioButton, so we just return it.
			var radioButton = newContent as RadioButton;
			if (radioButton != null)
			{
				return radioButton;
			}

			// Element is not a RadioButton. We'll wrap it in a RadioButton now.
			var newRadioButton = new RadioButton();
			newRadioButton.Content = args.Data;

			// If a user provided item template exists, we pass the template down to the ContentPresenter of the RadioButton.
			var itemTemplateWrapper = m_itemTemplateWrapper as ItemTemplateWrapper;
			if (itemTemplateWrapper != null)
			{
				newRadioButton.ContentTemplate = itemTemplateWrapper.Template;
			}

			return newRadioButton;
		}

		private protected override void RecycleElementCore(global::Microsoft.UI.Xaml.ElementFactoryRecycleArgs args)
		{

		}
	}
}

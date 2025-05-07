using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	internal class RadioButtonsElementFactory : ElementFactory
	{
		private IElementFactoryShim m_itemTemplateWrapper = null;

		internal void UserElementFactory(object newValue)
		{
			m_itemTemplateWrapper = newValue as IElementFactoryShim;
			if (m_itemTemplateWrapper == null)
			{
				// ItemTemplate set does not implement IElementFactoryShim. We also want to support DataTemplate.
				var dataTemplate = newValue as DataTemplate;
				if (dataTemplate != null)
				{
					m_itemTemplateWrapper = new ItemTemplateWrapper(dataTemplate);
				}
			}
		}

		protected override UIElement GetElementCore(ElementFactoryGetArgs args)
		{
			object GetContent(IElementFactoryShim itemTemplateWrapper)
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

		protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
		{

		}
	}
}

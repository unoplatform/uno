using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation;
using static Uno.UI.FeatureConfiguration;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class SelectorItem
{
	//---------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Returns a plain text string to provide a default AutomationProperties.Name
	//      in the absence of an explicitly defined one
	//
	//---------------------------------------------------------------------------
	internal override string GetPlainText()
	{
		string strPlainText = null;

		var contentTemplateRoot = ContentTemplateRoot;

		if (contentTemplateRoot != null)
		{
			// we have the first child of the content. Check whether it has an automation name

			strPlainText = AutomationProperties.GetName(contentTemplateRoot);

			// fallback: use getplain text on it
			if (string.IsNullOrEmpty(strPlainText))
			{
				var contentTemplateRootAsIFE = contentTemplateRoot as FrameworkElement;

				strPlainText = null;

				if (contentTemplateRootAsIFE is not null)
				{
					strPlainText = contentTemplateRootAsIFE.GetPlainText();
				}
			}

			// fallback, use GetPlainText on the contentpresenter, who has some special logic to account for old templates
			if (string.IsNullOrEmpty(strPlainText))
			{
				var contentTemplateRootAsIFE = contentTemplateRoot as FrameworkElement;

				strPlainText = null;

				if (contentTemplateRootAsIFE is not null)
				{
					var pParent = contentTemplateRootAsIFE.Parent;
					if (pParent is ContentPresenter cp)
					{
						strPlainText = cp.GetTextBlockText();
					}
				}
			}
		}

		return strPlainText;
	}
}

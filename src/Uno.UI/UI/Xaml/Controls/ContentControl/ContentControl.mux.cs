using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls;

public partial class ContentControl
{
	internal override string GetPlainText()
	{
		var content = Content;

		if (content is not null)
		{
			return FrameworkElement.GetStringFromObject(content);
		}

		return null;
	}
}

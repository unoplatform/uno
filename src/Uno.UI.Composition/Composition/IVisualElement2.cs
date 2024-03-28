#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Composition
{
	public partial interface IVisualElement2
	{
		public Visual GetVisualInternal();
	}
}

using System;
using System.Linq;

namespace Windows.UI
{
	public sealed partial class UIContentRoot
	{
		private UIContentRoot()
		{
		}

		public UIContext UIContext { get; } = UIContext.GetForCurrentThread();
	}
}

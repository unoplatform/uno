#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace Uno.UI.Toolkit
{
    public static class UICommandExtensions
    {
		public static void SetDestructive(this UICommand command, bool isDestructive)
		{
#if __IOS__
			if (command == null)
			{
				return;
			}

			command.IsDestructive = isDestructive;
#endif
		}
	}
}

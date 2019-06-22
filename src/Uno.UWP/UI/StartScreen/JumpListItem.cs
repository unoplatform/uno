#if __ANDROID__ || __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.StartScreen
{
	public partial class JumpListItem
	{
		public Uri Logo { get; set; }

		public string GroupName { get; set; }

		public string DisplayName { get; set; }

		public string Description { get; set; }

		public string Arguments { get; }

		public JumpListItemKind Kind { get; }

		public bool RemovedByUser { get; }

		public static JumpListItem CreateWithArguments(string arguments, string displayName)
		{
			return null;
		}

		public static JumpListItem CreateSeparator()
		{
			return null;
		}
	}
}
#endif

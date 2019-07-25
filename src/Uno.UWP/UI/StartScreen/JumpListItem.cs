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
		private JumpListItem(string arguments)
		{
			Arguments = arguments;
		}

		public Uri Logo { get; set; }

		public string GroupName { get; set; }

		public string DisplayName { get; set; }

		public string Description { get; set; }

		public string Arguments { get; }

		public JumpListItemKind Kind => JumpListItemKind.Arguments;

		public bool RemovedByUser => false;

		public static JumpListItem CreateWithArguments(string arguments, string displayName)
		{
			return new JumpListItem(arguments) { DisplayName = displayName };
		}

		public static JumpListItem CreateSeparator()
		{
			return null;
		}
	}
}
#endif

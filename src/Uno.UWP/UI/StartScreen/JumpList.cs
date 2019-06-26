#if __ANDROID__ || __IOS__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
		private JumpList() => Initialize();

		public JumpListSystemGroupKind SystemGroupKind { get; set; }

		public IList<JumpListItem> Items { get; } = new List<JumpListItem>();

		public IAsyncAction SaveAsync()
		{
			throw new NotImplementedException();
		}

		public static IAsyncOperation<JumpList> LoadCurrentAsync()
		{
			throw new NotImplementedException();
		}		
	}
}
#endif

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.UI.StartScreen
{
	public partial class JumpList
	{
#if __ANDROID__ || __IOS__
		private JumpList() => Init();

		public JumpListSystemGroupKind SystemGroupKind { get; set; }

		public IList<JumpListItem> Items { get; } = new List<JumpListItem>();

		public IAsyncAction SaveAsync() => InternalSaveAsync();

		public static IAsyncOperation<JumpList> LoadCurrentAsync() =>
			Task.FromResult(new JumpList()).AsAsyncOperation();
#else
		public static bool IsSupported() => false;
#endif
	}
}

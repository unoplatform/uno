#if NET7_0_OR_GREATER
using System.Runtime.InteropServices.JavaScript;

namespace __Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal partial class DragDropExtension
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension.registerNoOp")]
			internal static partial void RegisterNoOp();
		}
	}
}
#endif

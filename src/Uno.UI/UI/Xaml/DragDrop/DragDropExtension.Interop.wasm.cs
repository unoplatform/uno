using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.ApplicationModel.DataTransfer.DragDrop.Core
{
	internal partial class DragDropExtension
	{
		internal static partial class NativeMethods
		{
			[JSImport("globalThis.Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension.registerNoOp")]
			internal static partial void RegisterNoOp();

			[JSImport("globalThis.Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension.retrieveFiles")]
			internal static partial Task<string> RetrieveFilesAsync(int[] itemIds);

			[JSImport("globalThis.Windows.ApplicationModel.DataTransfer.DragDrop.Core.DragDropExtension.retrieveText")]
			internal static partial Task<string> RetrieveTextAsync(int itemId);
		}
	}
}

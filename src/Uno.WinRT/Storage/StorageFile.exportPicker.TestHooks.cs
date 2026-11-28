#nullable enable

using System;
using System.Threading.Tasks;

namespace Windows.Storage
{
	partial class StorageFile
	{
		// For the runtime tests of the iOS save picker's export sheet. The runtime tests are compiled against the reference assembly and run on
		// every platform, so these exist everywhere.
#if __IOS__
		internal static Task ExportSheetsIdle => ExportSheet.Idle;

		internal static int ExportSheetsAskedFor => ExportSheet.AskedFor;

		internal static int ExportSheetsPresented => ExportSheet.Presented;

		internal static bool IsExportSheetShown => ExportSheet.IsShown;

		internal static Func<Task> PresentOtherSheet(bool documentPicker) => ExportSheet.PresentOther(documentPicker);

		internal static Task DismissExportSheetThroughPresenter() => ExportSheet.DismissShownThroughPresenter();

		internal static Task FinishExportSheet(global::Windows.Storage.Provider.FileUpdateStatus status) => ExportSheet.FinishShown(status);
#else
		internal static Task ExportSheetsIdle => Task.CompletedTask;

		internal static int ExportSheetsAskedFor => 0;

		internal static int ExportSheetsPresented => 0;

		internal static bool IsExportSheetShown => false;

		internal static Func<Task> PresentOtherSheet(bool documentPicker) => throw new PlatformNotSupportedException();

		internal static Task DismissExportSheetThroughPresenter() => throw new PlatformNotSupportedException();

		internal static Task FinishExportSheet(global::Windows.Storage.Provider.FileUpdateStatus status) => throw new PlatformNotSupportedException();
#endif
	}
}

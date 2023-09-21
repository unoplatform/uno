using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;

namespace Windows.Storage.Pickers
{
	public static class FilePickerHelper
	{
		public static IDisposable RegisterOnBeforeStartActivity(FileOpenPicker picker, Intent intent)
		   => picker.RegisterOnBeforeStartActivity(intent);

		public static IDisposable RegisterOnBeforeStartActivity(FileSavePicker picker, Intent intent)
			=> picker.RegisterOnBeforeStartActivity(intent);
	}
}

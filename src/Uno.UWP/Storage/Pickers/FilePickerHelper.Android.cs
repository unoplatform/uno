using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;

namespace Windows.Storage.Pickers
{
	/// <summary>
	/// Enables the user to extend the internal intent
	/// </summary>
	public static class FilePickerHelper
	{
		public static void RegisterOnBeforeStartActivity(FileOpenPicker picker, Action<Intent> intentAction)
		   => picker.RegisterOnBeforeStartActivity(intentAction);

		public static void RegisterOnBeforeStartActivity(FileSavePicker picker, Action<Intent> intentAction)
			=> picker.RegisterOnBeforeStartActivity(intentAction);
	}
}

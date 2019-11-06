using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_Storage.StorageFileTests
{
	[Uno.UI.Samples.Controls.SampleControlInfo("StorageFile", "StorageFile_DateCreated", description: "Testing attribute DateCreated of StorageFile")]
	public sealed partial class StorageFile_DateCreated : UserControl
    {
        public DateCreated()
        {
            this.InitializeComponent();
        }

		static string _name;
		static DateTimeOffset _date;

		private void sfdButtonCreate_Click(object sender, RoutedEventArgs e)
		{
			// creating
			_date = DateTimeOffset.Now;
			_name = _date.ToString("hhMMss") + ".txt";
			Windows.Storage.StorageFolder fold = Windows.Storage.ApplicationData.Current.LocalFolder;
			System.IO.File.Create(fold.Path + "\\" + _name);
			//Windows.Storage.StorageFile file;
			//file = await Windows.Storage.StorageFile.GetFileFromPathAsync(fold.Path + "\\" + _name);
			//file = await fold.CreateFileAsync(_name);
			sfdResult.Text = "created";
		}


		private async void sfdButtonTest_Click(object sender, RoutedEventArgs e)
		{
			Windows.Storage.StorageFolder fold = Windows.Storage.ApplicationData.Current.LocalFolder;
			Windows.Storage.StorageFile file = await fold.GetFileAsync(_name);

			DateTimeOffset filedate = file.DateCreated;
			if (Math.Abs((filedate - _date).TotalSeconds) < 2)
			{
				sfdResult.Text = "success";
			}
		}
	}
}

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage
{
	[TestClass]
	public class Given_StorageFolder
	{
		[TestMethod]
		public async Task When_CreateFile_FailIfExists()
		{
			var folder = ApplicationData.Current.LocalFolder;

			var filename = $"{Guid.NewGuid()}.txt";

			await folder.CreateFileAsync(filename);

			await Assert.ThrowsExceptionAsync<Exception>(() => folder.CreateFileAsync(filename).AsTask());
		}

		[TestMethod]
		public async Task When_CreateFile_ReplaceExisting()
		{
			var folder = ApplicationData.Current.LocalFolder;

			var filename = $"{Guid.NewGuid()}.txt";

			var file = await folder.CreateFileAsync(filename);

			using (var sw = new StreamWriter(await file.OpenStreamForWriteAsync()))
			{
				sw.Write("Failed !");
			}

			file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

			using (var sw = new StreamWriter(await file.OpenStreamForWriteAsync()))
			{
				sw.Write("OK !");
			}

			using (var sr = new StreamReader(await file.OpenStreamForReadAsync()))
			{
				Assert.AreEqual("OK !", sr.ReadToEnd());
			}
		}
	}
}

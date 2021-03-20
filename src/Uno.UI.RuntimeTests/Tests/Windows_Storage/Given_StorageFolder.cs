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

			await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => folder.CreateFileAsync(filename).AsTask());
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

		[TestMethod]
		public async Task When_CreateFolder_GenerateUnique_Not_Exists()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				// Create some other folder
				await folder.CreateFolderAsync("something");

				var createdFolder = await folder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName);

				// No suffix should be generated
				Assert.AreEqual("test", createdFolder.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFolder_GenerateUnique_Exists()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFolderAsync("test");

				var createdFolder = await folder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (2)", createdFolder.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFile_GenerateUnique_Exists_Fake_Extension()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFolderAsync("test.txt");

				var createdFolder = await folder.CreateFolderAsync("test.txt", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test.txt (2)", createdFolder.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFolder_GenerateUnique_Exists_Increments()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFolderAsync("test");

				await folder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName);
				var createdFolder = await folder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (3)", createdFolder.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFolder_GenerateUnique_Multiple()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFolderAsync("test");

				for (int i = 2; i < 20; i++)
				{
					await folder.CreateFolderAsync($"test ({i})");
				}

				var createdFolder = await folder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (20)", createdFolder.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFolder_GenerateUnique_Multiple_With_Gap()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFolderAsync("test");

				for (int i = 2; i < 20; i++)
				{
					await folder.CreateFolderAsync($"test ({i})");
				}

				var gapFolder = await folder.GetFolderAsync("test (14)");
				await gapFolder.DeleteAsync();

				var createdFolder = await folder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (14)", createdFolder.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFolder_GenerateUnique_Exists_File()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFolderAsync("test");
				await folder.CreateFileAsync("test (2)");

				var createdFolder = await folder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (3)", createdFolder.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		[TestMethod]
		public async Task When_CreateFolder_GenerateUnique_Exists_File_With_Extension()
		{
			var path = "";
			try
			{
				path = GetRandomFolderPath();
				var folder = await StorageFolder.GetFolderFromPathAsync(path);

				await folder.CreateFolderAsync("test");
				await folder.CreateFileAsync("test (2).txt");

				var createdFile = await folder.CreateFolderAsync("test", CreationCollisionOption.GenerateUniqueName);

				Assert.AreEqual("test (2)", createdFile.Name);
			}
			finally
			{
				Directory.Delete(path, true);
			}
		}

		private string GetRandomFolderPath()
		{
			var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, Guid.NewGuid().ToString());
			Directory.CreateDirectory(path);
			return path;
		}
	}
}

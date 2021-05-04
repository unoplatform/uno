using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests
{
	[TestClass]
	public class Given_FileIO
	{
		[TestMethod]
		public async Task When_WriteTextAsyncNoEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				targetFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(GenerateRandomFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteTextAsync(targetFile, contents);

				var realContents = File.ReadAllText(targetFile.Path);
				Assert.AreEqual(contents, realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_WriteLinesAsyncNoEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var lines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				targetFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(GenerateRandomFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteLinesAsync(targetFile, lines);

				var realContents = File.ReadAllLines(targetFile.Path);
				CollectionAssert.AreEqual(realContents, lines);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_WriteTextAsyncWithEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				targetFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(GenerateRandomFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteTextAsync(targetFile, contents, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);

				var realContents = File.ReadAllText(targetFile.Path, Encoding.BigEndianUnicode);
				Assert.AreEqual(contents, realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_WriteLinesAsyncWithEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var lines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				targetFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(GenerateRandomFileName(), CreationCollisionOption.ReplaceExisting);
				await FileIO.WriteLinesAsync(targetFile, lines, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);

				var realContents = File.ReadAllLines(targetFile.Path, Encoding.Unicode);
				CollectionAssert.AreEqual(realContents, lines);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_AppendTextAsyncNoEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var originalContent = "First line3527535205šěššýétžščžíé";
				var appendedContent = "New line33251164535205věšřěšřčžčšžž";

				var fileName = GenerateRandomFileName();
				var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
				File.WriteAllText(filePath, originalContent);
				targetFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
				await FileIO.AppendTextAsync(targetFile, appendedContent);

				var realContents = File.ReadAllText(targetFile.Path);
				Assert.AreEqual(originalContent + appendedContent, realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_AppendTextAsyncRecognizesEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var originalContent = "First line3527535205šěššýétžščžíé";
				var appendedContent = "New line33251164535205věšřěšřčžčšžž";

				var fileName = GenerateRandomFileName();
				var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
				File.WriteAllText(filePath, originalContent, Encoding.Unicode);
				targetFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
				await FileIO.AppendTextAsync(targetFile, appendedContent);

				var realContents = File.ReadAllText(targetFile.Path, Encoding.Unicode);
				Assert.AreEqual(originalContent + appendedContent, realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_AppendTextAsyncWithEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var originalContent = "First line3527535205šěššýétžščžíé";
				var appendedContent = "New line33251164535205věšřěšřčžčšžž";

				var fileName = GenerateRandomFileName();
				var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
				File.WriteAllText(filePath, originalContent, Encoding.BigEndianUnicode);
				targetFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
				await FileIO.AppendTextAsync(targetFile, appendedContent, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);

				var realContents = File.ReadAllText(targetFile.Path, Encoding.BigEndianUnicode);
				Assert.AreEqual(originalContent + appendedContent, realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_AppendLinesAsyncNoEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var firstLines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				var appendedLines = new[]
				{
					"New line",
					"33251164535205",
					"věšřěšřčžčšžž"
				};
				var fileName = GenerateRandomFileName();
				var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
				File.WriteAllLines(filePath, firstLines);
				targetFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
				await FileIO.AppendLinesAsync(targetFile, appendedLines);

				var realContents = File.ReadAllLines(targetFile.Path);
				CollectionAssert.AreEqual(firstLines.Concat(appendedLines).ToArray(), realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_AppendLinesAsyncRecognizesEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var firstLines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				var appendedLines = new[]
				{
					"New line",
					"33251164535205",
					"věšřěšřčžčšžž"
				};
				var fileName = GenerateRandomFileName();
				var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
				File.WriteAllLines(filePath, firstLines, Encoding.BigEndianUnicode);
				targetFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
				await FileIO.AppendLinesAsync(targetFile, appendedLines);

				var realContents = File.ReadAllLines(targetFile.Path, Encoding.BigEndianUnicode);
				CollectionAssert.AreEqual(firstLines.Concat(appendedLines).ToArray(), realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_AppendLinesAsyncWithEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var firstLines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				var appendedLines = new[]
				{
					"New line",
					"33251164535205",
					"věšřěšřčžčšžž"
				};
				var fileName = GenerateRandomFileName();
				var filePath = Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
				File.WriteAllLines(filePath, firstLines, Encoding.Unicode);
				targetFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);
				await FileIO.AppendLinesAsync(targetFile, appendedLines, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);

				var realContents = File.ReadAllLines(targetFile.Path, Encoding.Unicode);
				CollectionAssert.AreEqual(firstLines.Concat(appendedLines).ToArray(), realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_ReadTextAsyncNoEncoding()
		{
			IStorageFile sourceFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				var fileName = GenerateRandomFileName();
				File.WriteAllText(Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName), contents);
				sourceFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

				var realContents = await FileIO.ReadTextAsync(sourceFile);
				Assert.AreEqual(contents, realContents);
			}
			finally
			{
				DeleteFile(sourceFile);
			}
		}

		[TestMethod]
		public async Task When_ReadLinesAsyncNoEncoding()
		{
			IStorageFile sourceFile = null;
			try
			{
				var lines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				var fileName = GenerateRandomFileName();
				File.WriteAllLines(Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName), lines);
				sourceFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

				var realContents = await FileIO.ReadLinesAsync(sourceFile);
				CollectionAssert.AreEqual(realContents.ToArray(), lines);
			}
			finally
			{
				DeleteFile(sourceFile);
			}
		}

		[TestMethod]
		public async Task When_ReadTextAsyncWithEncoding()
		{
			IStorageFile sourceFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				var fileName = GenerateRandomFileName();
				File.WriteAllText(Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName), contents, Encoding.Unicode);
				sourceFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

				var realContents = await FileIO.ReadTextAsync(sourceFile, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
				Assert.AreEqual(contents, realContents);
			}
			finally
			{
				DeleteFile(sourceFile);
			}
		}

		[TestMethod]
		public async Task When_ReadLinesAsyncWithEncoding()
		{
			IStorageFile sourceFile = null;
			try
			{
				var lines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				var fileName = GenerateRandomFileName();
				File.WriteAllLines(Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName), lines, Encoding.BigEndianUnicode);
				sourceFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

				var realContents = await FileIO.ReadLinesAsync(sourceFile, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);
				CollectionAssert.AreEqual(realContents.ToArray(), lines);
			}
			finally
			{
				DeleteFile(sourceFile);
			}
		}

		[TestMethod]
		public async Task When_ReadTextAsyncCanRecognizeEncoding()
		{
			IStorageFile sourceFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				var fileName = GenerateRandomFileName();
				File.WriteAllText(Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName), contents, Encoding.BigEndianUnicode);
				sourceFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

				var realContents = await FileIO.ReadTextAsync(sourceFile);
				Assert.AreEqual(contents, realContents);
			}
			finally
			{
				DeleteFile(sourceFile);
			}
		}

		[TestMethod]
		public async Task When_ReadLinesAsyncCanRecognizeEncoding()
		{
			IStorageFile sourceFile = null;
			try
			{
				var lines = new[]
				{
					"First line",
					"3527535205",
					"šěššýétžščžíé"
				};
				var fileName = GenerateRandomFileName();
				File.WriteAllLines(Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName), lines, Encoding.Unicode);
				sourceFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

				var realContents = await FileIO.ReadLinesAsync(sourceFile);
				CollectionAssert.AreEqual(realContents.ToArray(), lines);
			}
			finally
			{
				DeleteFile(sourceFile);
			}
		}

		[TestMethod]
		public async Task When_WriteBytesAsync()
		{
			IStorageFile targetFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				var bytes = Encoding.UTF8.GetBytes(contents);
				var fileName = GenerateRandomFileName();
				targetFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName);
				await FileIO.WriteBytesAsync(targetFile, bytes);

				var realContents = File.ReadAllBytes(targetFile.Path);
				CollectionAssert.AreEqual(bytes, realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_WriteBufferAsync()
		{
			IStorageFile targetFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				var bytes = Encoding.UTF8.GetBytes(contents);
				var buffer = bytes.AsBuffer();
				var fileName = GenerateRandomFileName();
				targetFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName);
				await FileIO.WriteBufferAsync(targetFile, buffer);

				var realContents = File.ReadAllBytes(targetFile.Path);
				CollectionAssert.AreEqual(bytes, realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_ReadBufferAsync()
		{
			IStorageFile sourceFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				var bytes = Encoding.UTF8.GetBytes(contents);
				var fileName = GenerateRandomFileName();
				var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, fileName);
				File.WriteAllBytes(path, bytes);
				sourceFile = await ApplicationData.Current.LocalFolder.GetFileAsync(fileName);

				var buffer = await FileIO.ReadBufferAsync(sourceFile);
				var realBytes = buffer.ToArray();
				CollectionAssert.AreEqual(bytes, realBytes);
			}
			finally
			{
				DeleteFile(sourceFile);
			}
		}

		[TestMethod]
		public async Task When_Large_WriteBytesAsync()
		{
			IStorageFile targetFile = null;
			try
			{
				var contents = new string('a', 200000) + new string('b', 200000);
				var bytes = Encoding.UTF8.GetBytes(contents);
				var fileName = GenerateRandomFileName();
				targetFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(fileName);
				await FileIO.WriteBytesAsync(targetFile, bytes);

				var realContents = File.ReadAllBytes(targetFile.Path);
				CollectionAssert.AreEqual(bytes, realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		private string GenerateRandomFileName() => $"{Guid.NewGuid()}.txt";

		private void DeleteFile(IStorageFile file)
		{
			if (file != null)
			{
				File.Delete(file.Path);
			}
		}
	}
}

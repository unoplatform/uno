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
	public class Given_PathIO
	{
		[TestMethod]
		public async Task When_WriteTextAsyncNoEncoding()
		{
			StorageFile targetFile = null;
			try
			{
				var contents = "Hello world!\r\n__127538\t+ěčšřěřšěřt";
				targetFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(GenerateRandomFileName(), CreationCollisionOption.ReplaceExisting);
				await PathIO.WriteTextAsync(targetFile.Path, contents);

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
				await PathIO.WriteLinesAsync(targetFile.Path, lines);

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
				await PathIO.WriteTextAsync(targetFile.Path, contents, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);

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
				await PathIO.WriteLinesAsync(targetFile.Path, lines, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);

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
				await PathIO.AppendTextAsync(targetFile.Path, appendedContent);

				var realContents = File.ReadAllText(targetFile.Path);
				Assert.AreEqual(originalContent + appendedContent, realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_AppendTextAsyncIgnoresEncoding()
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
				await PathIO.AppendTextAsync(targetFile.Path, appendedContent);

				var realContents = File.ReadAllBytes(targetFile.Path).Skip(2).ToArray(); // Skip BOM at start
				var expectedContent = Encoding.Unicode.GetBytes(originalContent).Concat(Encoding.Default.GetBytes(appendedContent)).ToArray();
				CollectionAssert.AreEqual(expectedContent, realContents);
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
				await PathIO.AppendTextAsync(targetFile.Path, appendedContent, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);

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
				await PathIO.AppendLinesAsync(targetFile.Path, appendedLines);

				var realContents = File.ReadAllLines(targetFile.Path);
				CollectionAssert.AreEqual(firstLines.Concat(appendedLines).ToArray(), realContents);
			}
			finally
			{
				DeleteFile(targetFile);
			}
		}

		[TestMethod]
		public async Task When_AppendLinesAsyncIgnoresEncoding()
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
				await PathIO.AppendLinesAsync(targetFile.Path, appendedLines);

				var firstLinesBytes = Encoding.BigEndianUnicode.GetBytes(string.Join(Environment.NewLine, firstLines) + Environment.NewLine);
				var appendedLinesBytes = Encoding.Default.GetBytes(string.Join(Environment.NewLine, appendedLines) + Environment.NewLine);
				var expectedContents = firstLinesBytes.Concat(appendedLinesBytes).ToArray();
				var realContents = File.ReadAllBytes(targetFile.Path).Skip(2).ToArray();
				CollectionAssert.AreEqual(expectedContents, realContents);
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
				await PathIO.AppendLinesAsync(targetFile.Path, appendedLines, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);

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

				var realContents = await PathIO.ReadTextAsync(sourceFile.Path);
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

				var realContents = await PathIO.ReadLinesAsync(sourceFile.Path);
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

				var realContents = await PathIO.ReadTextAsync(sourceFile.Path, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
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

				var realContents = await PathIO.ReadLinesAsync(sourceFile.Path, Windows.Storage.Streams.UnicodeEncoding.Utf16BE);
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

				var realContents = await PathIO.ReadTextAsync(sourceFile.Path);
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

				var realContents = await PathIO.ReadLinesAsync(sourceFile.Path);
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
				await PathIO.WriteBytesAsync(targetFile.Path, bytes);

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
				await PathIO.WriteBufferAsync(targetFile.Path, buffer);

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

				var buffer = await PathIO.ReadBufferAsync(sourceFile.Path);
				var realBytes = buffer.ToArray();
				CollectionAssert.AreEqual(bytes, realBytes);
			}
			finally
			{
				DeleteFile(sourceFile);
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

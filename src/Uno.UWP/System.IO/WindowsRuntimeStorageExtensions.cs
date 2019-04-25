#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
using System.Security;
using System.Threading.Tasks;
using Windows.Storage;

namespace System.IO
{
	public static class WindowsRuntimeStorageExtensions
	{
		public static async Task<Stream> OpenStreamForReadAsync(this IStorageFile windowsRuntimeFile)
		{
			return File.OpenRead(windowsRuntimeFile.Path);
		}

		public static async Task<Stream> OpenStreamForReadAsync(this IStorageFolder rootDirectory, string relativePath)
		{
			return File.OpenRead(Path.Combine(rootDirectory.Path, relativePath));
		}

		public static async Task<Stream> OpenStreamForWriteAsync(this IStorageFile windowsRuntimeFile)
		{
			return File.OpenWrite(windowsRuntimeFile.Path);
		}

		public static async Task<Stream> OpenStreamForWriteAsync(this IStorageFolder rootDirectory, string relativePath, CreationCollisionOption creationCollisionOption)
		{
			FileMode mode;
			FileAccess access;

			switch(creationCollisionOption)
			{
				case CreationCollisionOption.FailIfExists:
					mode = FileMode.CreateNew;
					access = FileAccess.Write;
					break;

				case CreationCollisionOption.GenerateUniqueName:
					mode = FileMode.CreateNew;
					access = FileAccess.Write;
					break;

				case CreationCollisionOption.OpenIfExists:
					mode = FileMode.OpenOrCreate;
					access = FileAccess.Write;
					break;

				case CreationCollisionOption.ReplaceExisting:
					mode = FileMode.Truncate;
					access = FileAccess.Write;
					break;
				default:
					throw new NotSupportedException($"The {creationCollisionOption} CreationCollisionOption is not supported");
			}

			Directory.CreateDirectory(rootDirectory.Path);
			return File.Open(Path.Combine(rootDirectory.Path, relativePath), mode, access);
		}
	}
}

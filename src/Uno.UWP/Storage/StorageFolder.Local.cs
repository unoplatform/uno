#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously


#if __IOS__
using UIKit;
using Foundation;
#endif


using System;
using System.IO;
using Uno.Extensions;
using Windows.Foundation;

namespace Windows.Storage
{
	partial class StorageFolder
	{
		private sealed class Local : ImplementationBase
		{
			public Local(string path)
				: base(path)
			{
			}

			public override IAsyncOperation<StorageFolder> CreateFolderAsync(string folderName, CreationCollisionOption option) =>
				AsyncOperation.FromTask(async ct =>
				{
					await TryInitializeStorage();

					var path = global::System.IO.Path.Combine(Path, folderName);
					DirectoryInfo di;
					switch (option)
					{
						case CreationCollisionOption.ReplaceExisting:
							if (Directory.Exists(path))
							{
								Directory.Delete(path, true);
							}

							di = Directory.CreateDirectory(path);
							return new StorageFolder(di.Name, path);

						case CreationCollisionOption.FailIfExists:
							if (Directory.Exists(path))
							{
								throw new UnauthorizedAccessException();
							}

							di = Directory.CreateDirectory(path);
							return new StorageFolder(di.Name, path);

						case CreationCollisionOption.OpenIfExists:
							if (Directory.Exists(path))
							{
								return new StorageFolder(folderName, path);
							}

							di = Directory.CreateDirectory(path);
							return new StorageFolder(di.Name, path);

						case CreationCollisionOption.GenerateUniqueName:
							if (Directory.Exists(path))
							{
								di = Directory.CreateDirectory(path += Guid.NewGuid().ToStringInvariant());
								return new StorageFolder(di.Name, path);
							}
							else
							{
								di = Directory.CreateDirectory(path);
								return new StorageFolder(di.Name, path);
							}
					}

					return null;
				});

			public override IAsyncOperation<StorageFolder> GetFolderAsync(string name) =>
				AsyncOperation.FromTask(async ct =>
				{
					await TryInitializeStorage();

					var itemPath = global::System.IO.Path.Combine(Path, name);

					var directoryExists = Directory.Exists(itemPath);

					if (!directoryExists)
					{
						throw new FileNotFoundException(itemPath);
					}

					return await StorageFolder.GetFolderFromPathAsync(itemPath);
				});
		}
	}
}

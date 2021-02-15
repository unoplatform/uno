#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

#if __IOS__
using UIKit;
using Foundation;
#endif

using System.Collections.Generic;
using Windows.Foundation;

namespace Windows.Storage
{
	public partial class StorageFolder
	{
		private abstract class ImplementationBase
		{
			protected ImplementationBase(string path)
				=> Path = path;

			public void InitOwner(StorageFolder owner)
				=> Owner = owner; // Lazy initialized to avoid delegate in StorageFolder ctor

			protected StorageFolder Owner { get; private set; } = null!; // Should probably be private

			public virtual string Path { get; protected set; }

			public IAsyncOperation<StorageFolder> CreateFolderAsync(string folderName) => CreateFolderAsync(folderName, CreationCollisionOption.FailIfExists);

			public abstract IAsyncOperation<StorageFolder> CreateFolderAsync(string folderName, CreationCollisionOption option);

			public abstract IAsyncOperation<StorageFolder> GetFolderAsync(string name);
		}
	}
}

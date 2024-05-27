#if __IOS__ || __ANDROID__
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Toolkit;

public partial class MediaGallery
{
	public async Task<bool> CheckAccessAsync() => await CheckAccessPlatformAsync();

	public async Task SaveAsync(MediaFileType type, byte[] data, string targetFileName)
	{
		using var memoryStream = new MemoryStream(data);
		await SaveAsync(type, memoryStream, targetFileName);
	}

	public async Task SaveAsync(MediaFileType type, Stream stream, string targetFileName) =>
		await SavePlatformAsync(type, stream, targetFileName);

	public async Task SaveAsync(MediaFileType type, string sourceFilePath, string targetFileName)
	{
		using var fileStream = System.IO.File.OpenRead(sourceFilePath);
		await SaveAsync(type, fileStream, targetFileName);
	}
}
#endif

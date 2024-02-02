#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Android.Content.Res;
using System.Threading.Tasks;

namespace Uno.UI.Toolkit;

partial class StorageFileHelper
{
	private static ICollection<string>? _scannedFiles { get; set; }

	private static Task<bool> FileExistsInPackage(string fileName)
	{
		//Look in assets first***
		var context = global::Android.App.Application.Context;

		//Get only the filename with extension and apply internal UNO naming rules so we are able to find the compiled resources by replacing '.' and '-' by underscore.
		var normalizedFileName = Path.GetFileNameWithoutExtension(fileName).Replace('.', '_').Replace('-', '_') + Path.GetExtension(fileName);
		if (_scannedFiles is null)
		{
			_scannedFiles = new List<string>();
			ScanPackageAssets(_scannedFiles);
		}

		if (_scannedFiles.Contains(normalizedFileName))
		{
			return Task.FromResult(true);
		}

		//If not asset found and we detect "/" in filename means we are looking for a nested resource file.
		//In this case we need to replace '/, -, .' by '_' and remove filename extension before trying to pull it from app resources.
		var normalizedResName = fileName.ToLowerInvariant();
		var nameArray = normalizedResName.Split("/");
		normalizedResName = Path.GetFileNameWithoutExtension(normalizedResName).Replace('.', '_').Replace('-', '_');
		if (nameArray.Length > 1)
		{
			//Replace original filename in our array by our normalized resource filename.
			nameArray[nameArray.Length - 1] = normalizedResName;
			//Join our normalized elements without extension
			normalizedResName = string.Join('_', nameArray);
		}

		//Look in drawable resources***
		var resources = context.Resources;
		int resId = resources?.GetIdentifier(normalizedResName, "drawable", context.PackageName) ?? 0;
		if (resId == 0)
		{
			//Look in mipmap resources***
			resId = resources?.GetIdentifier(normalizedResName, "mipmap", context.PackageName) ?? 0;
		}

		return Task.FromResult(resId != 0);
	}

	/// <summary>
	/// This method will scan for all the assets within current package
	/// </summary>
	/// <param name="scannedFiles">scanned files list</param>
	/// <param name="rootPath">root path</param>
	private static bool ScanPackageAssets(ICollection<string> scannedFiles, string rootPath = "")
	{
		try
		{
			var paths = global::Android.App.Application.Context.Assets?.List(rootPath);
			if (paths?.Length > 0)
			{
				foreach (var file in paths)
				{
					string path = string.IsNullOrEmpty(rootPath) ? file : Path.Combine(rootPath, file);
					if (!ScanPackageAssets(scannedFiles, path))
					{
						return false;
					}

					if (path.Contains('.'))
					{
						scannedFiles.Add(Path.GetFileName(path));
					}
				}
			}
			return true;
		}
		catch (IOException)
		{
			return false;
		}
	}
}


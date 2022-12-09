#nullable enable
#if __ANDROID__
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Android.Content.Res;
using System.Threading.Tasks;
namespace Windows.Storage.Helpers;

partial class StorageFileHelper
{
	private static Task<bool> FileExistsInPackage(string fileName)
	{
		//Look in assets first***
		var context = global::Android.App.Application.Context;
		var assets = context.Assets;

		//Get only the filename with extension and apply internal UNO naming rules so we are able to find the compiled resources by replacing '.' and '-' by underscore.
		var normalizedFileName = Path.GetFileNameWithoutExtension(fileName).Replace('.', '_').Replace('-', '_') + Path.GetExtension(fileName);
		var files = new List<string>();
		ScanPackageAssets(assets!, files);
		if (files.Contains(normalizedFileName))
		{
			return Task.FromResult(true);
		}

		//If not asset found and we detect "/" in filename means we are looking for a nested resource file.
		//In this case we need to replace '/, -, .' by '_' and remove filename extension before trying to pull it from app resources.
		var normalizedResName = fileName.ToLowerInvariant();
		var nameArray = normalizedResName.Split("/")?.ToList();
		normalizedResName = Path.GetFileNameWithoutExtension(normalizedResName).Replace('.', '_').Replace('-', '_');
		if (nameArray?.Count > 1)
		{
			//Replace original filename in our array by our normalized resource filename.
			nameArray[nameArray.Count - 1] = normalizedResName;
			//Join our normalized elements without extension
			normalizedResName = string.Join("_", nameArray);
		}

		//Look in drawable resources***
		var resources = context.Resources;
		int resId = 0;
		resId = resources?.GetIdentifier(normalizedResName, "drawable", context.PackageName) ?? 0;
		if (resId != 0)
		{
			return Task.FromResult(true);
		}

		//Look in mipmap resources***
		resId = resources?.GetIdentifier(normalizedResName, "mipmap", context.PackageName) ?? 0;
		if (resId != 0)
		{
			return Task.FromResult(true);
		}

		return Task.FromResult(false);
	}


	//This method will scan for all the assets within current package
	//This method will return a list of {file}.{extension}
	private static bool ScanPackageAssets(AssetManager assets, List<string> files, string rootPath = "")
	{
		try
		{
			var Paths = assets?.List(rootPath);
			if (Paths?.Length > 0)
			{
				foreach (var file in Paths)
				{
					string path = string.IsNullOrWhiteSpace(rootPath) ? file : Path.Combine(rootPath, file);
					if (!ScanPackageAssets(assets!, files, path))
					{
						return false;
					}

					if (path.Contains('.'))
					{
						files.Add(Path.GetFileNameWithoutExtension(path) + Path.GetExtension(path));
					}
				}
			}
			return true;
		}
		catch
		{
			//in case of IOException just return false
			return false;
		}
	}
}

#endif

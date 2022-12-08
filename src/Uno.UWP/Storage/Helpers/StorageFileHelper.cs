using System.Reflection;
using System.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if __ANDROID__
using Android.Content.Res;
#elif __IOS__ || MACCATALYST || MACOS
using Foundation;
#endif

namespace Windows.Storage.Helpers;

public static class StorageFileHelper
{
	/// <summary>
	/// Determines if a file exists within application package
	/// </summary>
	/// <param name="filename">relative file path</param>
	/// <returns>true if file exists</returns>
	public static bool Exists(string filename)
	{
#if __ANDROID__
		//Look in assets first***
		var context = global::Android.App.Application.Context;
		var assets = context.Assets;

		//Get only the filename with extension and apply internal UNO naming rules so we are able to find the compiled resources by replacing '.' and '-' by underscore.
		var normalizedFileName = Path.GetFileNameWithoutExtension(filename).Replace('.', '_').Replace('-','_') + Path.GetExtension(filename);
		var files = new List<string>();
		ScanPackageAssets(assets, files);
		if (files.Contains(normalizedFileName))
		{
			return true;
		}

		//If not asset found and we detect "/" in filename means we are looking for a nested resource file.
		//In this case we need to replace '/, -, .' by '_' and remove filename extension before trying to pull it from app resources.
		var normalizedResName = filename.ToLower(Thread.CurrentThread.CurrentCulture);
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
			return true;
		}

		//Look in mipmap resources***
		resId = resources?.GetIdentifier(normalizedResName, "mipmap", context.PackageName) ?? 0;
		if (resId != 0)
		{
			return true;
		}



		return false;
#elif __IOS__ || MACCATALYST || MACOS
		var directoryName = global::System.IO.Path.GetDirectoryName(filename) + string.Empty;
		var fileName = global::System.IO.Path.GetFileNameWithoutExtension(filename);
		var fileExtension = global::System.IO.Path.GetExtension(filename);

		var resourcePathname = NSBundle.MainBundle.PathForResource(global::System.IO.Path.Combine(directoryName, fileName), fileExtension.Substring(1));

		return resourcePathname != null;
#elif WINDOWS
		var executingPath = Assembly.GetExecutingAssembly().Location;
		if (!string.IsNullOrWhiteSpace(executingPath))
		{
			var path = Path.GetDirectoryName(executingPath);
			if (path is not null &&
				!string.IsNullOrWhiteSpace(path))
			{
				var fullPath = Path.Combine(path, filename);
				return File.Exists(fullPath);
			}
		}
		return true;
#else
		return true;
#endif

	}

#if __ANDROID__
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
					if (!ScanPackageAssets(assets, files, path))
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
			return false;
		}
	}
#endif
}

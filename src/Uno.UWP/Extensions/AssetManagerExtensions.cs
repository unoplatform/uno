using System;
using Android.Content.Res;

namespace Uno.Extensions;

internal static class AssetManagerExtensions
{
	internal static bool TryOpen(this AssetManager assetManager, string path, out System.IO.Stream? stream)
	{
		try
		{
			stream = assetManager.Open(path);
			return true;
		}
		catch (Exception)
		{
			stream = null;
			return false;
		}
	}
}

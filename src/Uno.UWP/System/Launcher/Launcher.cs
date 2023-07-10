using System;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Uno.Extensions;
using Windows.UI.Core;
using System.Runtime.CompilerServices;

#if __SKIA__ || __MACOS__
using Windows.Storage;
#endif

namespace Windows.System;

/// <summary>
/// Starts the default app associated with the specified file or URI.
/// </summary>
public static partial class Launcher
{
#if __ANDROID__ || __IOS__ || __MACOS__
	private const string MicrosoftUriPrefix = "ms-";

	private static bool IsSpecialUri(Uri uri) => uri.Scheme.StartsWith(MicrosoftUriPrefix, StringComparison.InvariantCultureIgnoreCase);
#endif

	/// <summary>
	/// Starts the default app associated with the URI scheme name for the specified URI.
	/// </summary>
	/// <param name="uri">The URI.</param>
	/// <returns>Returns true if the default app for the URI scheme was launched; false otherwise.</returns>
	/// <exception cref="ArgumentNullException">If the provided URI is null.</exception>
	/// <exception cref="InvalidOperationException">If the method is called from a non-UI thread.</exception>
	public static IAsyncOperation<bool> LaunchUriAsync(Uri uri)
	{
#if __IOS__ || __ANDROID__ || __WASM__ || __MACOS__ || __SKIA__

		if (uri is null)
		{
			// this exception might not be in line with UWP which seems to throw AccessViolationException... for some reason.
			throw new ArgumentNullException(nameof(uri));
		}

		EnsureUIThread();

		return LaunchUriPlatformAsync(uri).AsAsyncOperation();
#else
		if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
		{
			typeof(Launcher).Log().Error($"{nameof(LaunchUriAsync)} is not implemented on this platform.");
		}

		return Task.FromResult(false).AsAsyncOperation();
#endif
	}

#if __ANDROID__ || __IOS__ || __MACOS__ || __SKIA__
	/// <summary>
	/// Asynchronously query whether an app can be activated for the specified URI and launch type.
	/// </summary>
	/// <param name="uri">The URI for which to query support.</param>
	/// <param name="launchQuerySupportType">The type of launch for which to query support.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException">If the provided URI is null.</exception>
	public static IAsyncOperation<LaunchQuerySupportStatus> QueryUriSupportAsync(
		Uri uri,
		LaunchQuerySupportType launchQuerySupportType)
	{
		if (uri is null)
		{
			// counterintuitively, UWP throws plain Exception with HRESULT when passed in Uri is null
			throw new ArgumentNullException(nameof(uri));
		}

		// this method may run on the background thread on UWP
		if (CoreDispatcher.Main.HasThreadAccess)
		{
			return QueryUriSupportPlatformAsync(uri, launchQuerySupportType).AsAsyncOperation();
		}
		else
		{
			return CoreDispatcher.Main.RunWithResultAsync(
				priority: CoreDispatcherPriority.Normal,
				task: async () => await QueryUriSupportPlatformAsync(uri, launchQuerySupportType)
			).AsAsyncOperation();
		}
	}
#endif

#if __SKIA__ || __MACOS__
	/// <summary>
	/// Starts the default app associated with the specified file.
	/// </summary>
	/// <param name="file">The file.</param>
	/// <returns>The result of the operation.</returns>
	/// <exception cref="ArgumentNullException">If the provided file is null.</exception>
	public static IAsyncOperation<bool> LaunchFileAsync(IStorageFile file)
	{
		if (file is null)
		{
			throw new ArgumentNullException(nameof(file));
		}

		EnsureUIThread();

		return LaunchFilePlatformAsync(file).AsAsyncOperation();
	}

	/// <summary>
	/// Launches File Explorer and displays the contents of the specified folder.
	/// </summary>
	/// <param name="folder">The folder to display in File Explorer.</param>
	/// <returns>The result of the operation.</returns>
	/// <exception cref="ArgumentNullException">If the provided folder is null.</exception>
	public static IAsyncOperation<bool> LaunchFolderAsync(IStorageFolder folder)
	{
		if (folder is null)
		{
			throw new ArgumentNullException(nameof(folder));
		}

		EnsureUIThread();

		return LaunchFolderPlatformAsync(folder).AsAsyncOperation();
	}

	/// <summary>
	/// Launches File Explorer and displays the contents of the specified folder.
	/// </summary>
	/// <param name="path">A filepath to the folder to open.</param>
	/// <returns>The result of the operation.</returns>
	/// <exception cref="ArgumentNullException">If the provided path is null.</exception>
	public static IAsyncOperation<bool> LaunchFolderPathAsync(string path)
	{
		if (path is null)
		{
			throw new ArgumentNullException(nameof(path));
		}

		EnsureUIThread();

		return LaunchFolderPathPlatformAsync(path).AsAsyncOperation();
	}
#endif

	private static void EnsureUIThread([CallerMemberName] string methodName = null)
	{
#if !__WASM__
		if (!CoreDispatcher.Main.HasThreadAccess)
		{
			if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
			{
				typeof(Launcher).Log().Error($"{nameof(methodName)} must be called on the UI thread");
			}

			// LaunchUriAsync throws the following exception if used on UI thread on UWP
			throw new InvalidOperationException($"{nameof(methodName)} must be called on the UI thread");
		}
#endif
	}
}

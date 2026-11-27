#nullable enable

using System;
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Webkit;

namespace Uno.Storage.Internal;

/// <summary>
/// Serves local files to the app <see cref="Windows.System.Launcher.LaunchFileAsync(Windows.Storage.IStorageFile)"/>
/// hands them to, as Android 7+ refuses <c>file://</c> URIs across apps.
/// </summary>
/// <remarks>
/// The provider is not exported: another app can only read a URI it was explicitly granted.
/// </remarks>
[ContentProvider(
	new[] { "${applicationId}" + AuthoritySuffix },
	Name = "uno.storage.LauncherFileProvider",
	Exported = false,
	GrantUriPermissions = true)]
internal sealed class LauncherFileProvider : ContentProvider
{
	internal const string AuthoritySuffix = ".uno.launcherfileprovider";

	private const string FallbackMimeType = "*/*";

	internal static Android.Net.Uri GetUri(Context context, string path) =>
		new Android.Net.Uri.Builder()
			.Scheme(ContentResolver.SchemeContent)!
			.Authority(context.PackageName + AuthoritySuffix)!
			.Path(path)!
			.Build()!;

	internal static string GetMimeType(string fileName, string? contentType)
	{
		if (!string.IsNullOrWhiteSpace(contentType) && contentType != "application/octet-stream")
		{
			return contentType;
		}

		var extension = MimeTypeMap.GetFileExtensionFromUrl(Android.Net.Uri.Encode(fileName));
		return MimeTypeMap.Singleton?.GetMimeTypeFromExtension(extension?.ToLowerInvariant()) ?? FallbackMimeType;
	}

	public override bool OnCreate() => true;

	public override ParcelFileDescriptor? OpenFile(Android.Net.Uri uri, string mode)
	{
		if (mode != "r")
		{
			throw new Java.Lang.SecurityException("Files shared by the launcher are read-only.");
		}

		return ParcelFileDescriptor.Open(GetFile(uri), ParcelFileMode.ReadOnly);
	}

	public override string? GetType(Android.Net.Uri uri) => GetMimeType(GetFile(uri).Name, null);

	public override ICursor? Query(Android.Net.Uri uri, string[]? projection, string? selection, string[]? selectionArgs, string? sortOrder)
	{
		var file = GetFile(uri);
		var columns = projection ?? new[] { IOpenableColumns.DisplayName, IOpenableColumns.Size };
		var cursor = new MatrixCursor(columns, 1);
		var row = cursor.NewRow()!;

		foreach (var column in columns)
		{
			Java.Lang.Object? value = column switch
			{
				IOpenableColumns.DisplayName => new Java.Lang.String(file.Name),
				IOpenableColumns.Size => Java.Lang.Long.ValueOf(file.Length()),
				_ => null,
			};

			row.Add(column, value);
		}

		return cursor;
	}

	public override Android.Net.Uri? Insert(Android.Net.Uri uri, ContentValues? values) =>
		throw new Java.Lang.UnsupportedOperationException();

	public override int Update(Android.Net.Uri uri, ContentValues? values, string? selection, string[]? selectionArgs) =>
		throw new Java.Lang.UnsupportedOperationException();

	public override int Delete(Android.Net.Uri uri, string? selection, string[]? selectionArgs) =>
		throw new Java.Lang.UnsupportedOperationException();

	private static Java.IO.File GetFile(Android.Net.Uri uri) =>
		new(uri.Path ?? throw new Java.IO.FileNotFoundException(uri.ToString()));
}

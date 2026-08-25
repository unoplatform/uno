#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using Uno.Foundation.Logging;
using Windows.Storage;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppleToastNotificationDeliveryReceiptStore
{
	private const string ReceiptDirectoryName = ".uno-apple-toast-delivery-receipts-v1";
	private const string ReceiptExtension = ".receipt";
	private static readonly object _gate = new();

	public static bool TryPersist(string scheduleIdentifier)
	{
		if (GetReceiptDirectoryPath() is not { } directoryPath)
		{
			return false;
		}
		return TryPersist(scheduleIdentifier, directoryPath);
	}

	internal static bool TryPersist(string scheduleIdentifier, string directoryPath)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		ArgumentException.ThrowIfNullOrEmpty(directoryPath);
		if (!Guid.TryParseExact(scheduleIdentifier, "N", out _))
		{
			return false;
		}
		lock (_gate)
		{
			var path = Path.Combine(directoryPath, scheduleIdentifier + ReceiptExtension);
			if (File.Exists(path))
			{
				return true;
			}
			var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

			try
			{
				Directory.CreateDirectory(directoryPath);
				using (var stream = new FileStream(
					temporaryPath,
					FileMode.CreateNew,
					FileAccess.Write,
					FileShare.Read,
					bufferSize: 1,
					FileOptions.WriteThrough))
				{
					stream.WriteByte(1);
					stream.Flush(flushToDisk: true);
				}
				File.Move(temporaryPath, path);
				return true;
			}
			catch (IOException exception)
			{
				if (File.Exists(path))
				{
					return true;
				}
				LogWarning("Apple scheduled-notification delivery receipt could not be persisted.", exception);
				return false;
			}
			catch (UnauthorizedAccessException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipt could not be persisted.", exception);
				return false;
			}
			catch (SecurityException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipt could not be persisted.", exception);
				return false;
			}
			finally
			{
				TryDeleteTemporaryFile(temporaryPath);
			}
		}
	}

	public static IReadOnlyCollection<string>? GetIdentifiers()
	{
		if (GetReceiptDirectoryPath() is not { } directoryPath)
		{
			return null;
		}
		return GetIdentifiers(directoryPath);
	}

	internal static IReadOnlyCollection<string>? GetIdentifiers(string directoryPath)
	{
		ArgumentException.ThrowIfNullOrEmpty(directoryPath);
		lock (_gate)
		{
			try
			{
				if (!Directory.Exists(directoryPath))
				{
					return Array.Empty<string>();
				}
				return Directory.EnumerateFiles(directoryPath, "*" + ReceiptExtension, SearchOption.TopDirectoryOnly)
					.Select(Path.GetFileNameWithoutExtension)
					.OfType<string>()
					.Where(identifier => Guid.TryParseExact(identifier, "N", out _))
					.Distinct(StringComparer.Ordinal)
					.ToArray();
			}
			catch (IOException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipts could not be read.", exception);
				return null;
			}
			catch (UnauthorizedAccessException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipts could not be read.", exception);
				return null;
			}
			catch (SecurityException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipts could not be read.", exception);
				return null;
			}
		}
	}

	public static bool TryConsume(string scheduleIdentifier)
	{
		if (GetReceiptDirectoryPath() is not { } directoryPath)
		{
			return false;
		}
		return TryConsume(scheduleIdentifier, directoryPath);
	}

	internal static bool TryConsume(string scheduleIdentifier, string directoryPath)
	{
		ArgumentNullException.ThrowIfNull(scheduleIdentifier);
		ArgumentException.ThrowIfNullOrEmpty(directoryPath);
		if (!Guid.TryParseExact(scheduleIdentifier, "N", out _))
		{
			return false;
		}

		lock (_gate)
		{
			try
			{
				File.Delete(Path.Combine(directoryPath, scheduleIdentifier + ReceiptExtension));
				return true;
			}
			catch (IOException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipt could not be consumed.", exception);
				return false;
			}
			catch (UnauthorizedAccessException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipt could not be consumed.", exception);
				return false;
			}
			catch (SecurityException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipt could not be consumed.", exception);
				return false;
			}
		}
	}

	public static bool TryCleanup(IReadOnlyCollection<string> retainedScheduleIdentifiers)
	{
		if (GetReceiptDirectoryPath() is not { } directoryPath)
		{
			return false;
		}
		return TryCleanup(retainedScheduleIdentifiers, directoryPath);
	}

	internal static bool TryCleanup(IReadOnlyCollection<string> retainedScheduleIdentifiers, string directoryPath)
	{
		ArgumentNullException.ThrowIfNull(retainedScheduleIdentifiers);
		ArgumentException.ThrowIfNullOrEmpty(directoryPath);
		var retained = retainedScheduleIdentifiers
			.Where(identifier => Guid.TryParseExact(identifier, "N", out _))
			.ToHashSet(StringComparer.Ordinal);

		lock (_gate)
		{
			try
			{
				if (!Directory.Exists(directoryPath))
				{
					return true;
				}
				foreach (var receiptPath in Directory.EnumerateFiles(directoryPath, "*" + ReceiptExtension, SearchOption.TopDirectoryOnly))
				{
					var identifier = Path.GetFileNameWithoutExtension(receiptPath);
					if (identifier is null || !retained.Contains(identifier))
					{
						File.Delete(receiptPath);
					}
				}
				foreach (var temporaryPath in Directory.EnumerateFiles(directoryPath, "*.tmp", SearchOption.TopDirectoryOnly))
				{
					File.Delete(temporaryPath);
				}
				return true;
			}
			catch (IOException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipts could not be cleaned up.", exception);
				return false;
			}
			catch (UnauthorizedAccessException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipts could not be cleaned up.", exception);
				return false;
			}
			catch (SecurityException exception)
			{
				LogWarning("Apple scheduled-notification delivery receipts could not be cleaned up.", exception);
				return false;
			}
		}
	}

	private static string? GetReceiptDirectoryPath()
	{
		var localFolderPath = ApplicationData.Current.LocalFolder.Path;
		return string.IsNullOrEmpty(localFolderPath)
			? null
			: Path.Combine(localFolderPath, ReceiptDirectoryName);
	}

	private static void TryDeleteTemporaryFile(string path)
	{
		try
		{
			File.Delete(path);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		catch (SecurityException)
		{
		}
	}

	private static void LogWarning(string message, Exception exception)
	{
		if (typeof(AppleToastNotificationDeliveryReceiptStore).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(AppleToastNotificationDeliveryReceiptStore).Log().LogWarning($"{message} {exception.Message}");
		}
	}
}

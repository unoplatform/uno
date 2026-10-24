#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using UniformTypeIdentifiers;
using Uno.Foundation.Logging;
using Uno.Helpers.Theming;
using Uno.Storage.Internal;
using Uno.UI.Dispatching;
using Windows.ApplicationModel.Core;
using Windows.Storage.Provider;
using IOPath = System.IO.Path;

namespace Windows.Storage;

partial class StorageFile
{
	// Staged files are in folders of their own, so that files with the same name don't clash. Whatever an earlier run left
	// there (staged files it didn't delete, or a crash) is deleted before the first file of this run is staged.
	private static readonly Lazy<string> _exportPickerFolder = new(() =>
	{
		var folder = IOPath.Combine(IOPath.GetTempPath(), "Uno.FileSavePicker");
		ExportPickerStorageFile.TryDeleteFolder(folder);
		return folder;
	});

	/// <summary>
	/// Creates an empty file in a new folder of the temporary directory, which <see cref="CachedFileManager.CompleteUpdatesAsync"/> saves
	/// with the system's export sheet.
	/// </summary>
	internal static StorageFile CreateForExportPicker(string fileName)
	{
		var folder = IOPath.Combine(_exportPickerFolder.Value, Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(folder);

		var path = IOPath.Combine(folder, fileName);

		try
		{
			File.Create(path).Dispose();
		}
		catch
		{
			ExportPickerStorageFile.TryDeleteFolder(folder);
			throw;
		}

		return new StorageFile(new ExportPickerStorageFile(path, folder));
	}

	/// <summary>
	/// Presents the export sheet for a file from <see cref="CreateForExportPicker"/>, or returns <see langword="null"/> for any other file.
	/// </summary>
	internal Task<FileUpdateStatus>? ExportAsync(CancellationToken ct)
		=> (Implementation as ExportPickerStorageFile)?.ExportAsync(ct);

	/// <summary>
	/// The file the save picker returns when no extension is registered: a local file staged in a folder of its own, which stays until the app
	/// deletes it, and which <see cref="CachedFileManager.CompleteUpdatesAsync"/> saves a copy of with the export sheet.
	/// </summary>
	private sealed class ExportPickerStorageFile : Local
	{
		private readonly string _folder;
		private readonly object _gate = new();
		private Task<FileUpdateStatus>? _export;

		public ExportPickerStorageFile(string path, string folder)
			: base(path)
		{
			_folder = folder;
		}

		public override StorageProvider Provider => StorageProviders.IosExportPicker;

		// Each step tolerates a folder that is already gone, such as after a completion that found the file gone. The staging folder is
		// deleted also when the file was moved out of it.
		public override Task DeleteAsync(CancellationToken ct, StorageDeleteOption options)
		{
			IgnoreMissingFolder(() => File.Delete(Path));
			IgnoreMissingFolder(() => Directory.Delete(_folder, recursive: true));

			return Task.CompletedTask;
		}

		private static void IgnoreMissingFolder(Action delete)
		{
			try
			{
				delete();
			}
			catch (DirectoryNotFoundException)
			{
				// Nothing is left to delete.
			}
		}

		/// <summary>
		/// Presents the export sheet, which saves a copy of the file. The file stays, so it can be saved again. A call made before an earlier
		/// one for the file ends gets its result, unless the earlier one is cancelled: then it shows its own sheet once that one is gone.
		/// </summary>
		public Task<FileUpdateStatus> ExportAsync(CancellationToken ct)
		{
			lock (_gate)
			{
				return _export is { IsCompleted: false } shown ? JoinAsync(shown, ct) : _export = ExportCoreAsync(ct);
			}
		}

		private async Task<FileUpdateStatus> JoinAsync(Task<FileUpdateStatus> shown, CancellationToken ct)
		{
			try
			{
				return await shown.WaitAsync(ct);
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested)
			{
				return await ExportAsync(ct);
			}
		}

		private async Task<FileUpdateStatus> ExportCoreAsync(CancellationToken ct)
		{
			if (!File.Exists(Path))
			{
				TryDeleteFolder(_folder);
				return FileUpdateStatus.Failed;
			}

			return await ExportSheet.PresentAsync(Path, ct);
		}

		internal static void TryDeleteFolder(string folder)
		{
			try
			{
				if (Directory.Exists(folder))
				{
					Directory.Delete(folder, recursive: true);
				}
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				if (typeof(ExportPickerStorageFile).Log().IsEnabled(LogLevel.Warning))
				{
					typeof(ExportPickerStorageFile).Log().LogWarning($"Could not delete the save picker folder {folder}: {ex.Message}");
				}
			}
		}
	}

	/// <summary>
	/// Presents <see cref="UIDocumentPickerViewController"/> for exporting a copy of a file. Everything but <see cref="PresentAsync"/> and
	/// <see cref="AskedFor"/> runs on the main thread.
	/// </summary>
	private sealed class ExportSheet
	{
		private static readonly TimeSpan _stallWarningTime = TimeSpan.FromSeconds(15);
		private static readonly TimeSpan _watchInterval = TimeSpan.FromMilliseconds(250);

		// Keeps the picker and its delegate alive until the sheet is gone: UIKit holds the delegate weakly.
		private static readonly HashSet<ExportSheet> _shown = new();

		// The document picker appears only once its service runs, seconds after it is presented, and UIKit drops another presentation
		// asked for meanwhile, without calling it back. So each sheet waits until the previous one is gone.
		private static Task _previousGone = Task.CompletedTask;

		private static int _askedFor;
		private static int _presentedCount;

		private readonly TaskCompletionSource<FileUpdateStatus> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _gone = new();
		private readonly string _path;
		private readonly CancellationToken _ct;
		private CancellationTokenRegistration _cancellation;
		private NSUrl? _url;
		private UIDocumentPickerViewController? _picker;
		private bool _isPresented;
		private bool _isDropped;

		private ExportSheet(string path, CancellationToken ct)
		{
			_path = path;
			_ct = ct;
		}

		/// <summary>Completes once the sheets asked for so far, presented or still waiting, are gone.</summary>
		public static Task Idle => WhenIdleAsync();

		/// <summary>Gets how many sheets were asked for.</summary>
		public static int AskedFor => _askedFor;

		/// <summary>Gets how many sheets were presented to UIKit.</summary>
		public static int Presented => _presentedCount;

		/// <summary>Gets whether a sheet is shown.</summary>
		public static bool IsShown => _shown.Any(sheet => sheet._isPresented);

		/// <returns>
		/// <see cref="FileUpdateStatus.Complete"/> when the user saved the file, under its name or another one, and
		/// <see cref="FileUpdateStatus.Incomplete"/> when they closed the sheet without saving it. The task fails with
		/// <see cref="InvalidOperationException"/> when the sheet can't be presented, also when UIKit drops its presentation.
		/// </returns>
		public static Task<FileUpdateStatus> PresentAsync(string path, CancellationToken ct)
		{
			Interlocked.Increment(ref _askedFor);

			var sheet = new ExportSheet(path, ct);
			NativeDispatcher.Main.Enqueue(sheet.Start);
			return sheet._completion.Task;
		}

		private static async Task WhenIdleAsync()
		{
			// A sheet whose Start is still queued runs before this, so it is waited for too.
			var queued = new TaskCompletionSource();
			NativeDispatcher.Main.Enqueue(() => queued.TrySetResult());
			await queued.Task;

			await _previousGone;
		}

		private async void Start()
		{
			var previousGone = _previousGone;
			_previousGone = _gone.Task;

			try
			{
				_cancellation = _ct.Register(() => NativeDispatcher.Main.Enqueue(Cancel));

				await previousGone;
				var presenter = IsCancelled() ? null : await GetPresenterAsync();

				if (presenter is null || IsCancelled())
				{
					SetGone();
					return;
				}

				_url = NSUrl.FromFilename(_path);
				_picker = new UIDocumentPickerViewController([_url], asCopy: true)
				{
					ShouldShowFileExtensions = true,
					Delegate = new PickerDelegate(this),
				};

				_picker.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ?
					UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

				_shown.Add(this);
				_presentedCount++;
				presenter.PresentViewController(_picker, true, OnPresented);
				_ = WatchPresentationAsync(presenter, _picker);
			}
			catch (Exception ex)
			{
				if (_completion.TrySetException(ex))
				{
					_cancellation.Dispose();
				}
				else if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning($"The export sheet failed after it was cancelled: {ex}");
				}

				SetGone();
			}
		}

		// The token's callback only queues the cancellation, so a cancellation requested meanwhile is applied here, before anything is presented.
		private bool IsCancelled()
		{
			if (_ct.IsCancellationRequested)
			{
				Cancel();
			}

			return _completion.Task.IsCompleted;
		}

		private void OnPresented()
		{
			_isPresented = true;

			// A sheet that was cancelled before it appeared, or taken for dropped, is closed now: UIKit ignores a dismissal before then.
			if (_completion.Task.IsCompleted)
			{
				Dismiss();
			}
		}

		private void Cancel()
		{
			if (!_completion.TrySetCanceled(_ct))
			{
				return;
			}

			_cancellation.Dispose();

			// Otherwise the sheet is closed once it appears, or isn't presented at all.
			if (_isPresented)
			{
				Dismiss();
			}
		}

		// The picker has closed itself when its delegate is called.
		private void Finish(FileUpdateStatus status)
		{
			if (_completion.TrySetResult(status))
			{
				_cancellation.Dispose();
			}

			SetGone();
		}

		private void Dismiss()
		{
			if (_picker?.PresentingViewController is { } presenter)
			{
				presenter.DismissViewController(true, SetGone);
			}
			else
			{
				SetGone();
			}
		}

		private void SetGone()
		{
			_shown.Remove(this);

			// Releases the picker, and lets the next sheet present, after the UIKit callback that got here returns. A dropped picker is left to
			// the GC instead, so that OnPresented can still close it if UIKit presents it after all.
			NativeDispatcher.Main.Enqueue(() =>
			{
				if (!_isDropped || _isPresented)
				{
					_picker?.Dispose();
					_picker = null;
				}

				_url?.Dispose();
				_url = null;
				_gone.TrySetResult();
			});
		}

		// UIKit drops a presentation without calling it back if, when the picker's service runs, the controller it was presented from is
		// waiting for another delayed presentation asked for before it (such as a document picker or the share sheet, which don't show in
		// PresentedViewController until then), or has left the window. Neither can be seen beforehand, so this checks until the sheet
		// appears: once the presenter shows another controller, or is out of the window with nothing presented, the sheet won't appear.
		private async Task WatchPresentationAsync(UIViewController presenter, UIDocumentPickerViewController picker)
		{
			var askedAt = Stopwatch.GetTimestamp();
			var warned = false;

			while (true)
			{
				await DelayAsync(_watchInterval);

				if (_isPresented || !_shown.Contains(this))
				{
					return;
				}

				if (GetDropReason(presenter, picker) is { } reason)
				{
					Drop(reason);
					return;
				}

				// A drop that can't be seen, such as for a presentation that never ends, blocks the later sheets, so this says when that seems
				// to happen.
				if (!warned && Stopwatch.GetElapsedTime(askedAt) > _stallWarningTime && this.Log().IsEnabled(LogLevel.Warning))
				{
					warned = true;
					this.Log().LogWarning(
						$"The export sheet for {IOPath.GetFileName(_path)} asked for {_stallWarningTime.TotalSeconds:0} s ago hasn't appeared: " +
						"UIKit may have dropped its presentation. Later export sheets wait for it.");
				}
			}
		}

		private static string? GetDropReason(UIViewController presenter, UIDocumentPickerViewController picker)
		{
			if (presenter.PresentedViewController is { } presented)
			{
				return presented.Handle == picker.Handle ? null : $"{presented.GetType().Name} was still appearing";
			}

			return presenter.ViewIfLoaded?.Window is null ? $"{presenter.GetType().Name}, which it was presented from, left the window first" : null;
		}

		// The call ends with an exception rather than Failed, which is for a file that is gone: this one can be completed again.
		private void Drop(string reason)
		{
			_isDropped = true;

			var exception = new InvalidOperationException(
				$"The export sheet for {IOPath.GetFileName(_path)} wasn't shown: UIKit dropped its presentation, as {reason}.");

			if (_completion.TrySetException(exception))
			{
				_cancellation.Dispose();
			}

			SetGone();
		}

		/// <returns>The controller to present from, or <see langword="null"/> if the sheet was cancelled meanwhile.</returns>
		private async Task<UIViewController?> GetPresenterAsync()
		{
			var waitStart = Stopwatch.GetTimestamp();
			var warned = false;

			while (!IsCancelled())
			{
				var root = UIApplication.SharedApplication.KeyWindow?.RootViewController
					?? throw new InvalidOperationException("The export sheet can't be presented: the app has no key window.");

				// While something is presented full screen, UIKit takes the root's view out of the window.
				if (root.PresentedViewController is null && root.ViewIfLoaded?.Window is null)
				{
					throw new InvalidOperationException("The export sheet can't be presented: the app's window isn't on screen.");
				}

				var controller = root;
				while (controller.PresentedViewController is { } presented)
				{
					controller = presented;
				}

				// UIKit refuses to present from a controller that is still appearing or going away, so this waits for it to settle. A presented
				// controller that isn't in a window yet, such as a document picker waiting for its service, is still appearing.
				if (!controller.IsBeingPresented && !controller.IsBeingDismissed && controller.ViewIfLoaded?.Window is not null)
				{
					return controller;
				}

				if (controller.GetTransitionCoordinator() is not { } coordinator || !await WaitForTransitionAsync(coordinator))
				{
					await DelayAsync(TimeSpan.FromMilliseconds(100));
				}

				if (!warned && Stopwatch.GetElapsedTime(waitStart) > _stallWarningTime && this.Log().IsEnabled(LogLevel.Warning))
				{
					warned = true;
					this.Log().LogWarning(
						$"The export sheet has waited {_stallWarningTime.TotalSeconds:0} s for {controller.GetType().Name} to finish appearing or going away.");
				}
			}

			return null;
		}

		/// <summary>
		/// For the runtime tests: presents a sheet from the top-most controller outside the export sheets' queue, as other code of the app
		/// would: a document picker for opening files, which appears only once its service runs, or a plain controller. The returned function
		/// dismisses it once it has appeared.
		/// </summary>
		public static Func<Task> PresentOther(bool documentPicker)
		{
			var root = UIApplication.SharedApplication.KeyWindow?.RootViewController
				?? throw new InvalidOperationException("The app has no key window.");

			var presenter = root;
			while (presenter.PresentedViewController is { } presented)
			{
				presenter = presented;
			}

			UIViewController other = documentPicker ? new UIDocumentPickerViewController([UTTypes.Data], asCopy: true) : new UIViewController();
			presenter.PresentViewController(other, true, null);

			return async () =>
			{
				UIViewController? otherPresenter;
				while ((otherPresenter = other.PresentingViewController) is null || other.IsBeingPresented)
				{
					await DelayAsync(TimeSpan.FromMilliseconds(100));
				}

				await otherPresenter.DismissViewControllerAsync(true);
				other.Dispose();
			};
		}

		private static async Task<bool> WaitForTransitionAsync(IUIViewControllerTransitionCoordinator coordinator)
		{
			var transition = new TaskCompletionSource();

			// Resumes after UIKit's completion callback, as SetGone does.
			if (!coordinator.AnimateAlongsideTransition(_ => { }, _ => NativeDispatcher.Main.Enqueue(() => transition.TrySetResult())))
			{
				return false;
			}

			await transition.Task;
			return true;
		}

		// Resumes on the main thread.
		private static Task DelayAsync(TimeSpan delay)
		{
			var elapsed = new TaskCompletionSource();
			_ = Task.Delay(delay).ContinueWith(_ => NativeDispatcher.Main.Enqueue(() => elapsed.TrySetResult()), TaskScheduler.Default);
			return elapsed.Task;
		}

		private sealed class PickerDelegate : UIDocumentPickerDelegate
		{
			private readonly ExportSheet _sheet;

			public PickerDelegate(ExportSheet sheet) => _sheet = sheet;

			// Also called when the sheet is swiped down.
			public override void WasCancelled(UIDocumentPickerViewController controller) => _sheet.Finish(FileUpdateStatus.Incomplete);

			public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
				=> _sheet.Finish(urls.Length > 0 ? FileUpdateStatus.Complete : FileUpdateStatus.Incomplete);
		}
	}
}

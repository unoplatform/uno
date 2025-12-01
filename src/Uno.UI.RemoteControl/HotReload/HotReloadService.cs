using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload;

namespace Uno.UI.HotReload;

/// <summary>
/// Provides services for managing hot-reload operations, including pausing and resuming user interface updates during
/// live code changes.
/// </summary>
public partial class HotReloadService
{
	#region Singleton
	private static HotReloadService? _instance;

	/// <summary>
	/// Gets the singleton instance of the HotReloadService.
	/// If this is `null`, is means the application is not hot-reload capable.
	/// </summary>
	/// <remarks>
	/// Use this property to access the global HotReloadService instance throughout the application. This
	/// instance is thread-safe and intended to be shared.
	/// </remarks>
	public static HotReloadService? Instance => _instance;

	private HotReloadService(ClientHotReloadProcessor processor)
	{
		_processor = processor;
	}
	#endregion

	#region Legacy temp compat members
	// For now, this is initialized when the ClientHRProcessor initializes.
	// But this should be replaced ny a module initializer in the main application.
	internal static void Init(ClientHotReloadProcessor processor)
	{
		_instance = new(processor);
	}

	private readonly ClientHotReloadProcessor _processor;

	internal bool IsUIUpdatePaused => _pauseCounter is not 0;

	internal (bool value, string reason) ShouldReloadUi()
		=> _pauseCounter is not 0
			? (false, "type mapping prevent reload")
			: (true, string.Empty);
	#endregion

	private int _pauseCounter;


	/// <summary>
	/// Request the hot-reload engine to not perform any UI updates until the returned handle is disposed.
	/// </summary>
	/// <returns></returns>
	public UIUpdatePauseHandle PauseUIUpdates()
	{
		Interlocked.Increment(ref _pauseCounter);
		return new(this);
	}

	/// <summary>
	/// Specifies the result of attempting to update the user interface after resuming from a paused or suspended state.
	/// </summary>
	/// <remarks>Use this enumeration to determine the outcome of a UI update operation following a resume action.</remarks>
	public enum UIUpdateResumeResult
	{
		/// <summary>
		/// Indicate the UI has been properly updated after resuming and now reflects the latest changes.
		/// </summary>
		UIUpdated = 200,

		/// <summary>
		/// Indicates that the UI ahs not been updated to latest changes because another process is holding the updates.
		/// </summary>
		HoldByAnotherProcess = 401,

		/// <summary>
		/// Indicates that the operation could not be performed because it has already been resumed.
		/// </summary>
		AlreadyResumed = 500
	}

	/// <summary>
	/// Provides a handle that temporarily pauses UI updates and resumes them when disposed or explicitly processed.
	/// </summary>
	/// <remarks>
	/// This handle is intended to be used with a using statement or disposed asynchronously to ensure that
	/// UI updates are properly resumed. Only the first call to resume the UI will have an effect; subsequent calls are
	/// ignored. Thread-safe for concurrent use.
	/// </remarks>
	/// <param name="owner">The processor responsible for managing UI update pause and resume operations. Cannot be null.</param>
	public class UIUpdatePauseHandle(HotReloadService owner) : IAsyncDisposable
	{
		private int _released;

		public async ValueTask<UIUpdateResumeResult> TryProcess()
		{
			if (Interlocked.CompareExchange(ref _released, 1, 0) is not 0)
			{
				return UIUpdateResumeResult.AlreadyResumed;
			}

			if (Interlocked.Decrement(ref owner._pauseCounter) is not 0)
			{
				return UIUpdateResumeResult.HoldByAnotherProcess;
			}

			await owner._processor.UpdateUI(owner._processor._status.ReportLocalStarting([], ClientHotReloadProcessor.HotReloadSource.Manual), [], CancellationToken.None);
			return UIUpdateResumeResult.UIUpdated;
		}

		public async ValueTask DisposeAsync()
			=> await TryProcess();

		~UIUpdatePauseHandle()
			=> _ = TryProcess();
	}
}

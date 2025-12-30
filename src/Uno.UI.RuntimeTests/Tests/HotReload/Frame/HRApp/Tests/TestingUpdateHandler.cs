
using System.Reflection.Metadata;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[assembly: ElementMetadataUpdateHandler(typeof(TestingUpdateHandler))]

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

public static class TestingUpdateHandler
{
	private static TaskCompletionSource _visualTreeUpdateCompletion = new TaskCompletionSource();
	private static TaskCompletionSource<bool> _reloadCompletion = new TaskCompletionSource<bool>();

	/// <summary>
	/// This method is invoked whenever the UI is updated after a Hot Reload operation
	/// Only if ui updating hasn't be paused
	/// </summary>
	/// <param name="updatedTypes"></param>
	public static void AfterVisualTreeUpdate(Type[]? updatedTypes)
	{
		var oldCompletion = _visualTreeUpdateCompletion;
		_visualTreeUpdateCompletion = new TaskCompletionSource();
		oldCompletion.TrySetResult();
	}

	/// <summary>
	/// This method is invoked whenever the UI is updated after a Hot Reload operation
	/// </summary>
	/// <param name="updatedTypes">The types that are updated</param>
	/// <param name="uiUpdated">Whether or not the UI was updated</param>
	public static void ReloadCompleted(Type[]? updatedTypes, bool uiUpdated)
	{
		var oldCompletion = _reloadCompletion;
		_reloadCompletion = new TaskCompletionSource<bool>();
		oldCompletion.TrySetResult(uiUpdated);
	}


	/// <summary>
	/// Gets a task that allows the called to wait for an UI Update to 
	/// complete. This can block indefinitely if type mappings has been paused
	/// see TypeMappings.Pause()
	/// </summary>
	/// <returns></returns>
	public static async Task WaitForVisualTreeUpdate()
	{
		await _visualTreeUpdateCompletion.Task;

		// The previous tasks only indicates the update has been received and processed by visual tree ... not the UI has been updated yet
		await UnitTestsUIContentHelper.WaitForIdle();
	}

	/// <summary>
	/// Gets a task that allows the called to wait for an UI Update to 
	/// complete. This should not block indefinitely even if type mappings has been paused
	/// see TypeMappings.Pause()
	/// </summary>
	/// <returns></returns>
	public static async Task<bool> WaitForReloadCompleted()
	{
		return await _reloadCompletion.Task;
	}
}

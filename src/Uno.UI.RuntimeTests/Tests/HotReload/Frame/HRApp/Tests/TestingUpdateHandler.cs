
using System.Reflection.Metadata;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

[assembly: ElementMetadataUpdateHandler(typeof(TestingUpdateHandler))]

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

public static class TestingUpdateHandler
{
	private static TaskCompletionSource _visualTreeUpdateCompletion = new TaskCompletionSource();

	/// <summary>
	/// This method is invoked whenever the UI is updated after a Hot Reload operation
	/// </summary>
	/// <param name="updatedTypes"></param>
	public static void AfterVisualTreeUpdate(Type[]? updatedTypes)
	{
		var oldCompletion = _visualTreeUpdateCompletion;
		_visualTreeUpdateCompletion = new TaskCompletionSource();
		oldCompletion.TrySetResult();
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
	}
}

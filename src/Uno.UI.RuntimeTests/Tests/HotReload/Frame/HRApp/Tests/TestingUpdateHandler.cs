
using System.Reflection.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

[assembly: ElementMetadataUpdateHandler(typeof(TestingUpdateHandler))]

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

public static class TestingUpdateHandler
{
	private static TaskCompletionSource _visualTreeUpdateCompletion = new TaskCompletionSource();
	public static void BeforeVisualTreeUpdate(Type[]? updatedTypes)
	{
		//throw new Exception("BeforeVisualTreeUpdate");
		typeof(TestingUpdateHandler).Log().LogWarning("--------------------------Before Visual Tree Update");
	}

	public static void AfterVisualTreeUpdate(Type[]? updatedTypes)
	{
		//throw new Exception("AfterVisualTreeUpdate");
		typeof(TestingUpdateHandler).Log().LogWarning("--------------------------After Visual Tree Update");
		var oldCompletion = _visualTreeUpdateCompletion;
		_visualTreeUpdateCompletion = new TaskCompletionSource();
		oldCompletion.TrySetResult();
	}

	public static async Task WaitForVisualTreeUpdate()
	{
		await _visualTreeUpdateCompletion.Task;
	}
}

using System.Reflection.Metadata;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using System.Threading.Tasks;

[assembly: ElementMetadataUpdateHandler(typeof(TextBox), typeof(TextBoxUpdateHandler))]

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;

/// <summary>
/// This is only used for testing the capture/restore functionality of the UI Update phase. As such
/// it doesn't consider whether the Text property is databound or inherited etc
/// </summary>
internal static class TextBoxUpdateHandler
{
	public static void CaptureState(FrameworkElement element, IDictionary<string, object> stateDictionary, Type[]? updatedTypes)
	{
		stateDictionary["text"] = (element as TextBox)?.Text ?? string.Empty;
	}

	public static Task RestoreState(FrameworkElement element, IDictionary<string, object> stateDictionary, Type[]? updatedTypes)
	{
		if (element is TextBox textBox)
		{
			var newText = stateDictionary.TryGetValue("text", out var text) ? text?.ToString() : string.Empty;
			textBox.Text = newText;
		}

		return Task.CompletedTask;
	}
}

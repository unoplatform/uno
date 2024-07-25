using System.Threading;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests.Pages;

public record HR_Frame_Pages_DataContext_VM
{
	private static int _counter = 0;

	private string Value { get; } = $"DataContext #{Interlocked.Increment(ref _counter)}";
}

public record HR_Frame_Pages_DataContext_VM_2 : HR_Frame_Pages_DataContext_VM;

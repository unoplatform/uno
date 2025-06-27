using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Uno.UI.Hosting;

if (NativeLibrary.TryLoad("__Internal", Assembly.GetEntryAssembly()!, unchecked((DllImportSearchPath)0xFFFFFFFF), out var handle))
{
	Console.WriteLine("icuuc loaded!!!!!!!!!!!!!!!!");
	if (NativeLibrary.TryGetExport(handle, nameof(ubrk_countAvailable), out var address))
	{
		unsafe
		{
			var address1 = (delegate* unmanaged<int>)address;
			Console.WriteLine($"ubrk_countAvailable loaded {address1()}!!!!!!!!!!!!!!!!");
		}
	}
	else
	{
		Console.WriteLine("ubrk_countAvailable failed!!!!!!!!!!!!!!!!");
	}
}
else
{
	Console.WriteLine("icuuc failed!!!!!!!!!!!!!!!!");
}
var list = new[]
{
	ubrk_countAvailable
};
for (var index = 0; index < list.Length; index++)
{
	var func = list[index];
	try
	{
		Console.WriteLine($"{index} : {func()}");
	}
	catch (Exception e)
	{
		Console.WriteLine(e);
	}
}

[DllImport("__Internal")]
static extern int ubrk_countAvailable();

var host = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseWebAssembly()
	.Build();

await host.RunAsync();

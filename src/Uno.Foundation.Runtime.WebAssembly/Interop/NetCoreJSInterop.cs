using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

internal sealed class Interop
{
	/// <summary>
	/// .NET5 MonoVM and upward specific internal call.
	/// </summary>
	[Obfuscation(Feature = "renaming", Exclude = true)]
	internal sealed class Runtime
	{
		/// <summary>
		/// Do not use this method directly. Use <see cref="WebAssembly.Runtime.InvokeJS(string)"/> instead.
		/// </summary>
		/// <remarks>
		/// Matches https://github.com/dotnet/runtime/blob/54906ea87c9d8ff3df0b341f02ae255fd58820bd/src/mono/wasm/runtime/driver.c#L417
		/// </remarks>
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static extern string InvokeJS(string str, out int exceptional_result);
	}
}

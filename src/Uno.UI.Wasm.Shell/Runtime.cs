namespace WebAssembly
{
	internal sealed class Runtime
	{
		[System.Runtime.CompilerServices.MethodImplAttribute((System.Runtime.CompilerServices.MethodImplOptions)4096)]
		static extern string InvokeJS(string str, out int exceptional_result);

		public static string InvokeJS(string str)
		{
			return InvokeJS(str, out var _);
		}
	}
}

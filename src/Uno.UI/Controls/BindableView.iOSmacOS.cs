#if true
using System.Collections.Generic;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#else
using NativeHandle = System.IntPtr;
#endif

namespace Uno.UI.Controls;

#if __IOS__
public partial class BindableUIView
#elif __MACOS__
public partial class BindableNSView
#endif
{
	static Dictionary<NativeHandle, Dictionary<NativeHandle,bool>> protocol_cache = new();

	public override bool ConformsToProtocol(NativeHandle protocol)
	{
		var classHandle = ClassHandle;
		bool new_map = false;
		lock (protocol_cache)
		{
			if (!protocol_cache.TryGetValue(classHandle, out var map))
			{
				map = new();
				new_map = true;
				protocol_cache.Add(classHandle, map);
			}
			if (new_map || !map.TryGetValue(protocol, out var result))
			{
				result = base.ConformsToProtocol(protocol);
				map.Add(protocol, result);
			}
			return result;
		}
	}
}
#endif

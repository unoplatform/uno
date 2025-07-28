using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using UIKit;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal class TransientNativePointer
{
	private static readonly Dictionary<IntPtr, TransientNativePointer> _instances = new Dictionary<IntPtr, TransientNativePointer>();
	private static uint _nextAvailablePointerId;

	private readonly IntPtr _nativeId;
	private readonly HashSet<UIView> _leases = new HashSet<UIView>();

	public uint Id { get; }

	// The first frameId can be zero, and we're comparing it when handling
	// TouchesBegan. Setting it to -1 by default allows for handling the
	// "unset" value with a minimal performance cost.
	public long LastManagedOnlyFrameId { get; set; } = -1;

	public bool HadMove { get; set; }

	private TransientNativePointer(IntPtr nativeId)
	{
		_nativeId = nativeId;
		Id = _nextAvailablePointerId++;
	}

	public static TransientNativePointer Get(UIView UIView, UITouch touch)
	{
		if (!_instances.TryGetValue(touch.Handle, out var id))
		{
			_instances[touch.Handle] = id = new TransientNativePointer(touch.Handle);
		}

		id._leases.Add(UIView);

		return id;
	}

	public void Release(UIView element)
	{
		if (_leases.Remove(element) && _leases.Count == 0)
		{
			if (_instances.Remove(_nativeId) && _instances.Count == 0)
			{
				// When all pointers are released, we reset the pointer ID to 0.
				// This is required to detect a DoubleTap where pointer ID must be the same.
				_nextAvailablePointerId = 0;
			}
		}
	}
}


#if UNO_HAS_MANAGED_POINTERS
#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Input;

namespace Uno.UI.Xaml.Core;

internal sealed class DirectManipulationCollection : IEnumerable<DirectManipulation>
{
	// Note: We should optimize this collection to track manipulation per active PointerIdentifier.
	//		 This would involve to add an event to the GestureRecognizer when it starts to handle a pointer.
	//		 We will also have to consider that a manipulation might be associated to 0 (inertial), 1 or more PointerIdentifiers!
	//		 It's also possible to have multiple manipulation (in different states, e.g. one interacting and one inertial) for the same PointerIdentifier!

	private readonly List<DirectManipulation> _instances = new();

	public void Scavenge()
		=> _instances.RemoveAll(static manip => manip.IsCompleted);

	public IEnumerable<DirectManipulation> OfType(PointerDeviceType type)
	{
		return _instances.Where(manip => manip.IsPointerType(type));
	}

	public DirectManipulation? Get(PointerIdentifier identifier)
		=> _instances.FirstOrDefault(manip => manip.IsTracking(identifier));

	public void Add(DirectManipulation manipulation)
		=> _instances.Add(manipulation);

	public void Remove(DirectManipulation manipulation)
		=> _instances.Remove(manipulation);

	/// <inheritdoc />
	public IEnumerator<DirectManipulation> GetEnumerator()
		=> _instances.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> GetEnumerator();

	public void ClearForFatalError()
		=> _instances.Clear();
}
#endif

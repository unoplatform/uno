using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Input
{
	public enum PointerDeviceType
	{
		// WARNING: This enum has a corresponding version in TypeScript!
		// WARNING: This enum is used as index in PointerTypePseudoDictionary, i.e. we are assuming that the values are sequential int starting from 0!

		/// <summary>
		/// A touch-enabled device
		/// </summary>
		Touch = 0,

		/// <summary>
		/// Pen
		/// </summary>
		Pen = 1,

		/// <summary>
		/// Mouse
		/// </summary>
		Mouse = 2,
	}
}

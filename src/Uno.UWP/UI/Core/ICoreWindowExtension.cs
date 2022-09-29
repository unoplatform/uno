#nullable disable

using System;
using System.Linq;
using Windows.Devices.Input;

namespace Windows.UI.Core
{
	internal interface ICoreWindowExtension
	{
		public CoreCursor PointerCursor { get; set; }

		void ReleasePointerCapture(PointerIdentifier pointer);

		void SetPointerCapture(PointerIdentifier pointer);
	}
}

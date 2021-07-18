using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Devices.Radios
{
	public partial class Radio
	{
		public RadioKind Kind { get; internal set; }

		public string Name { get; internal set; }

		public RadioState State { get; internal set; }

	}
}

﻿#if IS_UNIT_TESTS || __SKIA__ || __NETSTD_REFERENCE__ || __TVOS__
namespace Windows.Devices.Midi
{
	public partial class MidiInPort
	{
		/// <summary>
		/// Remove public parameterless constructor,
		/// needed for NET Standard reference check.
		/// </summary>
		private MidiInPort()
		{
		}
	}
}
#endif

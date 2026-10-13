#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Linux;
using Windows.System;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class KeyMapTests
{
	[TestMethod]
	[DataRow(0x0061u, VirtualKey.A)]
	[DataRow(0x0041u, VirtualKey.A)]
	[DataRow(0xff0du, VirtualKey.Enter)]
	[DataRow(0xff8du, VirtualKey.Enter)]
	[DataRow(0xfe20u, VirtualKey.Tab)]
	[DataRow(0xffb1u, VirtualKey.NumberPad1)]
	[DataRow(0xffe1u, VirtualKey.LeftShift)]
	[DataRow(0xffe2u, VirtualKey.RightShift)]
	[DataRow(0x0101f600u, VirtualKey.None)]
	public void KeysymsPreservePhysicalKeyMeaning(uint keysym, VirtualKey expected)
		=> Assert.AreEqual(expected, LinuxKeyMap.FromKeysym(keysym));
}

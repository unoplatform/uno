#nullable enable

using System.Numerics;
using System;
using Android.Views;

namespace Microsoft.UI.Composition
{
	public partial class Visual : global::Microsoft.UI.Composition.CompositionObject
	{
		internal View? NativeOwner { get; set; }
	}
}

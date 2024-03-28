#nullable enable

using System.Numerics;
using System;
using Android.Views;

namespace Windows.UI.Composition
{
	public partial class Visual : global::Windows.UI.Composition.CompositionObject
	{
		internal View? NativeOwner { get; set; }
	}
}

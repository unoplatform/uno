#nullable enable

using System;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// Attribute to control the automatic generation of weak event subscriptions
	/// </summary>
	[System.AttributeUsage(AttributeTargets.Event, Inherited = false, AllowMultiple = false)]
	internal sealed class GeneratedWeakEventAttribute : Attribute
	{

	}
}

#pragma warning disable IDE1006 // Naming Styles

using System;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Describes the changes made to a dependency property
	/// </summary>
	public sealed partial class DependencyPropertyChangedEventArgs : EventArgs
	{
		internal DependencyProperty PropertyInternal;
		internal object NewValueInternal;
		internal object OldValueInternal;
#if __APPLE_UIKIT__ || IS_UNIT_TESTS
		internal DependencyPropertyValuePrecedences NewPrecedenceInternal;
		internal DependencyPropertyValuePrecedences OldPrecedenceInternal;
		internal bool BypassesPropagationInternal;
#endif

		internal DependencyPropertyChangedEventArgs()
		{
		}

		public DependencyProperty Property => PropertyInternal;

		/// <summary>
		/// Gets the new value of the dependency property.
		/// </summary>
		public object NewValue => NewValueInternal;
		/// <summary>
		/// Gets the old value of the dependency property.
		/// </summary>
		public object OldValue => OldValueInternal;

#if __APPLE_UIKIT__ || IS_UNIT_TESTS
		/// <summary>
		/// Gets the dependency property value precedence of the new value
		/// </summary>
		internal DependencyPropertyValuePrecedences NewPrecedence => NewPrecedenceInternal;

		/// <summary>
		/// Gets the dependency property value precedence of the old value
		/// </summary>
		internal DependencyPropertyValuePrecedences OldPrecedence => OldPrecedenceInternal;

		/// <summary>
		/// Is true if an animated value should be ignored when setting the native
		/// value associated to it.  Happens in the scenario of GPU bound animations
		/// in iOS.
		/// </summary>
		internal bool BypassesPropagation => BypassesPropagationInternal;
#endif
	}
}

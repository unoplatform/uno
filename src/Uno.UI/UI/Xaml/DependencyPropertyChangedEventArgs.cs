using System;
using Uno.UI.DataBinding;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using System.Linq;
using System.Threading;
using Uno.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics;

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using View = UIKit.UIView;
#endif

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Describes the changes made to a dependency property
	/// </summary>
	public sealed partial class DependencyPropertyChangedEventArgs : EventArgs
	{
		internal DependencyPropertyChangedEventArgs(
			DependencyProperty property,
			object oldValue,
			object newValue
#if __IOS__ || __MACOS__ || IS_UNIT_TESTS
			, DependencyPropertyValuePrecedences oldPrecedence,
			DependencyPropertyValuePrecedences newPrecedence,
			bool bypassesPropagation
#endif
			)
		{
			Property = property;
			OldValue = oldValue;
			NewValue = newValue;
#if __IOS__ || __MACOS__ || IS_UNIT_TESTS
			OldPrecedence = oldPrecedence;
			NewPrecedence = newPrecedence;
			BypassesPropagation = bypassesPropagation;
#endif
		}

		/// <summary>
		/// Gets the new value of the dependency property.
		/// </summary>
		public object NewValue { get; }

		/// <summary>
		/// Gets the old value of the dependency property.
		/// </summary>
		public object OldValue { get; }

		public DependencyProperty Property { get; }

#if __IOS__ || __MACOS__ || IS_UNIT_TESTS
		/// <summary>
		/// Gets the dependency property value precedence of the new value
		/// </summary>
		internal DependencyPropertyValuePrecedences NewPrecedence
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the dependency property value precedence of the old value
		/// </summary>
		internal DependencyPropertyValuePrecedences OldPrecedence
		{
			get;
			private set;
		}

		/// <summary>
		/// Is true if an animated value should be ignored when setting the native
		/// value associated to it.  Happens in the scenario of GPU bound animations
		/// in iOS.
		/// </summary>
		internal bool BypassesPropagation
		{
			get;
			private set;
		}
#endif
	}
}

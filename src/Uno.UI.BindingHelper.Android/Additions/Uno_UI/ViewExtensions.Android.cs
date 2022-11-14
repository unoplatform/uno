#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Android.Runtime;
using Android.Views;
using Java.Interop;

namespace Uno.UI
{
    partial class UnoViewGroup
	{
		private static readonly JniObjectReference _membersPeerReference;

		private static readonly JniMethodInfo _getMeasuredDimensions;
		private static readonly JniMethodInfo _tryFastRequestLayout;
		private static readonly JniMethodInfo _isLayoutRequested;
		private static readonly JniMethodInfo _requestLayout;

		static UnoViewGroup()
		{
			_membersPeerReference = _members.JniPeerType.PeerReference;

			// Static methods
			_getMeasuredDimensions = _members.StaticMethods.GetMethodInfo("getMeasuredDimensions.(Landroid/view/View;)J");
			_tryFastRequestLayout = _members.StaticMethods.GetMethodInfo("tryFastRequestLayout.(Landroid/view/View;Z)V");

			// Instance methods
			_isLayoutRequested = _members.InstanceMethods.GetMethodInfo("isLayoutRequested.()Z");
			_requestLayout = _members.InstanceMethods.GetMethodInfo("requestLayout.()V");
		}

		/// <summary>
		/// Simplified for performance copy of UnoViewGroup.GetMeasuredDimensions binding
		/// </summary>
		internal unsafe static long GetMeasuredDimensions_Slim(View view)
		{
			var __args = stackalloc JniArgumentValue[1];
			*__args = new(view?.Handle ?? IntPtr.Zero);

			return JniEnvironment.StaticMethods.CallStaticLongMethod(_membersPeerReference, _getMeasuredDimensions, __args);
		}

		/// <summary>
		/// Simplified for performance copy of UnoViewGroup.TryFastRequestLayout binding
		/// </summary>
		internal unsafe static void TryFastRequestLayout_Slim(View view, bool needsForceLayout)
		{
			var __args = stackalloc JniArgumentValue[2];
			*__args = new JniArgumentValue(view?.Handle ?? IntPtr.Zero);
			__args[1] = new JniArgumentValue(needsForceLayout);

			JniEnvironment.StaticMethods.CallStaticVoidMethod(_membersPeerReference, _tryFastRequestLayout, __args);
		}

		/// <summary>
		/// Simplified for performance copy of UnoViewGroup.IsLayoutRequested binding
		/// </summary>
		internal unsafe bool IsLayoutRequested_Slim
			=> JniEnvironment.InstanceMethods.CallNonvirtualBooleanMethod((this as IJavaPeerable).PeerReference, _membersPeerReference, _isLayoutRequested, null);

		internal unsafe void RequestLayout_Slim()
			=> JniEnvironment.InstanceMethods.CallNonvirtualVoidMethod((this as IJavaPeerable).PeerReference, _membersPeerReference, _requestLayout, null);
	}
}
#endif

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

namespace Uno.UI.BindingHelper.Android
{
    internal static class AndroidViewsViewExtensions
	{
		private static readonly JniPeerMembers _members;
		private static readonly JniObjectReference _peerReference;

		private static readonly JniMethodInfo _viewMeasure;

		static AndroidViewsViewExtensions()
		{
			_members = new XAPeerMembers("android/view/View", typeof(View));
			_peerReference = _members.JniPeerType.PeerReference;

			// _members.InstanceMethods.InvokeNonvirtualVoidMethod("measure.(II)V", (Java.Interop.IJavaPeerable)this, __args);
			_viewMeasure = _members.InstanceMethods.GetMethodInfo("measure.(II)V");
		}

		/// <summary>
		/// Simplified for performance copy of View.Measure binding
		/// </summary>
		internal static unsafe void Measure_Slim(View view, int widthMeasureSpec, int heightMeasureSpec)
		{
			var __args = stackalloc JniArgumentValue[2];
			*__args = new(widthMeasureSpec);
			__args[1] = new(heightMeasureSpec);

			var target = (IJavaPeerable)view;

			JniEnvironment.InstanceMethods.CallNonvirtualVoidMethod(target.PeerReference, _peerReference, _viewMeasure, __args);
		}
	}
}
#endif

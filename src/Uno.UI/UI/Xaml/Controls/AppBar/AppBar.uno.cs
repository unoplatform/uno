#if __APPLE_UIKIT__ || __ANDROID__
#define HAS_NATIVE_COMMANDBAR
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar
#if HAS_NATIVE_COMMANDBAR
	: ICustomClippingElement
#endif
{
#pragma warning disable CS0414
#pragma warning disable CS0649
#pragma warning disable CS0169
	//UIElement? m_layoutTransitionElement;
	private bool _isNativeTemplate;
	//UIElement m_parentElementForLTEs;
#pragma warning restore CS0414
#pragma warning restore CS0649
#pragma warning restore CS0169
}

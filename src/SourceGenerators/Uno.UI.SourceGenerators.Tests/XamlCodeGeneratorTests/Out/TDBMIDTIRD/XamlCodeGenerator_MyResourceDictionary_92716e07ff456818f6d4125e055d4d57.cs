﻿// <autogenerated />
#pragma warning disable CS0114
#pragma warning disable CS0108
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.UI;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.Text;
using Uno.Extensions;
using Uno;
using Uno.UI.Helpers;
using Uno.UI.Helpers.Xaml;
using MyProject;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace TestRepro
{
	public sealed partial class MyResourceDictionary : global::Microsoft.UI.Xaml.ResourceDictionary
	{
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_prefix_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
		public void InitializeComponent()
		{
			this[
			"myTemplate"
			] = 
			new global::Uno.UI.Xaml.WeakResourceInitializer(this, __ResourceOwner_1 => 
				new global::Microsoft.UI.Xaml.DataTemplate(__ResourceOwner_1 , __owner => 				new _MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_TestReproMyResourceDictionarySC0().Build(__owner)
				)			)
			;
		}

		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private class _MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_TestReproMyResourceDictionarySC0 
		{
			[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
			private const string __baseUri_prefix_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
			[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
			private const string __baseUri_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
			global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();
			public _View Build(object __ResourceOwner_1)
			{
				_View __rootInstance = null;
				var __that = this;
				__rootInstance = 
				new global::Microsoft.UI.Xaml.Controls.TextBlock
				{
					IsParsing = true,
					Name = "tb",
					// Source 0\MyResourceDictionary.xaml (Line 12:5)
				}
				.MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_XamlApply((MyResourceDictionary_92716e07ff456818f6d4125e055d4d57XamlApplyExtensions.XamlApplyHandler0)(c0 => 
				{
					/* _isTopLevelDictionary:True */
					__that._component_0 = c0;
					__nameScope.RegisterName("tb", c0);
					__that.tb = c0;
					c0.SetBinding(
						global::Microsoft.UI.Xaml.Controls.TextBlock.TextProperty,
						new Microsoft.UI.Xaml.Data.Binding()
						{
							Mode = BindingMode.OneWay,
						}
							.BindingApply(___b => /*defaultBindModeOneWay*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, null, ___ctx => ___ctx is TestRepro.MyModel ___tctx ? (TryGetInstance_xBind_1(___tctx, out var bindResult1) ? (true, bindResult1) : (false, default)) : (false, default), null , new [] {"MyString"}))
					);
					global::Uno.UI.FrameworkElementHelper.SetBaseUri(c0, __baseUri_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57);
					c0.CreationComplete();
				}
				))
				;
				if (__rootInstance is FrameworkElement __fe) 
				{
					var owner = this;
					__fe.Loading += delegate
					{
						_component_0.UpdateResourceBindings();
					}
					;
				}
				if (__rootInstance is DependencyObject d)
				{
					if (global::Microsoft.UI.Xaml.NameScope.GetNameScope(d) == null)
					{
						global::Microsoft.UI.Xaml.NameScope.SetNameScope(d, __nameScope);
						__nameScope.Owner = d;
					}
					global::Uno.UI.FrameworkElementHelper.AddObjectReference(d, this);
				}
				return __rootInstance;
			}
			private global::Microsoft.UI.Xaml.Markup.ComponentHolder _component_0_Holder  = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: true);
			private global::Microsoft.UI.Xaml.Controls.TextBlock _component_0
			{
				get
				{
					return (global::Microsoft.UI.Xaml.Controls.TextBlock)_component_0_Holder.Instance;
				}
				set
				{
					_component_0_Holder.Instance = value;
				}
			}
			private global::Microsoft.UI.Xaml.Data.ElementNameSubject _tbSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
			private global::Microsoft.UI.Xaml.Controls.TextBlock tb
			{
				get
				{
					return (global::Microsoft.UI.Xaml.Controls.TextBlock)_tbSubject.ElementInstance;
				}
				set
				{
					_tbSubject.ElementInstance = value;
				}
			}
			private static bool TryGetInstance_xBind_1(global::TestRepro.MyModel ___tctx, out object o)
			{
				o = null;
				o = ___tctx.MyString;
				return true;
			}
		}
	}
}
namespace MyProject
{
	public sealed partial class GlobalStaticResources
	{
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_prefix_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
		// This non-static inner class is a means of reducing size of AOT compilations by avoiding many accesses to static members from a static callsite, which adds costly class initializer checks each time.
		internal sealed class ResourceDictionarySingleton__MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 : global::Uno.UI.IXamlResourceDictionaryProvider
		{
			private static global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();
			private static global::Uno.UI.IXamlResourceDictionaryProvider __that;
			internal static global::Uno.UI.IXamlResourceDictionaryProvider Instance
			{
				get
				{
					if (__that == null)
					{
						__that = new ResourceDictionarySingleton__MyResourceDictionary_92716e07ff456818f6d4125e055d4d57();
					}

					return __that;
				}
			}

			private readonly global::Uno.UI.Xaml.XamlParseContext __ParseContext_;
			internal static global::Uno.UI.Xaml.XamlParseContext GetParseContext() => ((ResourceDictionarySingleton__MyResourceDictionary_92716e07ff456818f6d4125e055d4d57)Instance).__ParseContext_;

			public ResourceDictionarySingleton__MyResourceDictionary_92716e07ff456818f6d4125e055d4d57()
			{
				__ParseContext_ = global::MyProject.GlobalStaticResources.__ParseContext_;
			}

			// Method for resource myTemplate 
			private object Get_1(object __ResourceOwner_1) =>
				new global::Microsoft.UI.Xaml.DataTemplate(__ResourceOwner_1 , __owner => 				new __Resources._MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_MyResourceDictionaryRDSC1().Build(__owner)
				)				;

			private global::Microsoft.UI.Xaml.ResourceDictionary _MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_ResourceDictionary;

			internal global::Microsoft.UI.Xaml.ResourceDictionary MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_ResourceDictionary
			{
				get
				{
					if (_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_ResourceDictionary == null)
					{
						_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_ResourceDictionary = 
						new global::Microsoft.UI.Xaml.ResourceDictionary
						{
							IsParsing = true,
							[
							"myTemplate"
							] = 
							new global::Uno.UI.Xaml.WeakResourceInitializer(this, __ResourceOwner_1 => 
								new global::Microsoft.UI.Xaml.DataTemplate(__ResourceOwner_1 , __owner => 								new __Resources._MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_MyResourceDictionaryRDSC2().Build(__owner)
								)							)
							,
						}
						;
						_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_ResourceDictionary.Source = new global::System.Uri("ms-resource:///Files/C:/Project/0/MyResourceDictionary.xaml");
						_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_ResourceDictionary.CreationComplete();
					}
					return _MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_ResourceDictionary;
				}
			}

			global::Microsoft.UI.Xaml.ResourceDictionary global::Uno.UI.IXamlResourceDictionaryProvider.GetResourceDictionary() => MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_ResourceDictionary;
		}

		internal static global::Microsoft.UI.Xaml.ResourceDictionary MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_ResourceDictionary => ResourceDictionarySingleton__MyResourceDictionary_92716e07ff456818f6d4125e055d4d57.Instance.GetResourceDictionary();
	}
}
namespace MyProject.__Resources
{
	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
	 class _MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_MyResourceDictionaryRDSC1 
	{
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_prefix_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
		global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();
		public _View Build(object __ResourceOwner_1)
		{
			_View __rootInstance = null;
			var __that = this;
			__rootInstance = 
			new global::Microsoft.UI.Xaml.Controls.TextBlock
			{
				IsParsing = true,
				Name = "tb",
				// Source 0\MyResourceDictionary.xaml (Line 12:5)
			}
			.MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_XamlApply((MyResourceDictionary_92716e07ff456818f6d4125e055d4d57XamlApplyExtensions.XamlApplyHandler0)(c1 => 
			{
				/* _isTopLevelDictionary:True */
				__that._component_0 = c1;
				__nameScope.RegisterName("tb", c1);
				__that.tb = c1;
				c1.SetBinding(
					global::Microsoft.UI.Xaml.Controls.TextBlock.TextProperty,
					new Microsoft.UI.Xaml.Data.Binding()
					{
						Mode = BindingMode.OneWay,
					}
						.BindingApply(___b => /*defaultBindModeOneWay*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, null, ___ctx => ___ctx is TestRepro.MyModel ___tctx ? (TryGetInstance_xBind_2(___tctx, out var bindResult2) ? (true, bindResult2) : (false, default)) : (false, default), null , new [] {"MyString"}))
				);
				global::Uno.UI.FrameworkElementHelper.SetBaseUri(c1, __baseUri_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57);
				c1.CreationComplete();
			}
			))
			;
			if (__rootInstance is FrameworkElement __fe) 
			{
				var owner = this;
				__fe.Loading += delegate
				{
					_component_0.UpdateResourceBindings();
				}
				;
			}
			if (__rootInstance is DependencyObject d)
			{
				if (global::Microsoft.UI.Xaml.NameScope.GetNameScope(d) == null)
				{
					global::Microsoft.UI.Xaml.NameScope.SetNameScope(d, __nameScope);
					__nameScope.Owner = d;
				}
				global::Uno.UI.FrameworkElementHelper.AddObjectReference(d, this);
			}
			return __rootInstance;
		}
		private global::Microsoft.UI.Xaml.Markup.ComponentHolder _component_0_Holder  = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: true);
		private global::Microsoft.UI.Xaml.Controls.TextBlock _component_0
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.TextBlock)_component_0_Holder.Instance;
			}
			set
			{
				_component_0_Holder.Instance = value;
			}
		}
		private global::Microsoft.UI.Xaml.Data.ElementNameSubject _tbSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
		private global::Microsoft.UI.Xaml.Controls.TextBlock tb
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.TextBlock)_tbSubject.ElementInstance;
			}
			set
			{
				_tbSubject.ElementInstance = value;
			}
		}
		private static bool TryGetInstance_xBind_2(global::TestRepro.MyModel ___tctx, out object o)
		{
			o = null;
			o = ___tctx.MyString;
			return true;
		}
	}
	[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
	 class _MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_MyResourceDictionaryRDSC2 
	{
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_prefix_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57 = "ms-appx:///TestProject/";
		global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();
		public _View Build(object __ResourceOwner_1)
		{
			_View __rootInstance = null;
			var __that = this;
			__rootInstance = 
			new global::Microsoft.UI.Xaml.Controls.TextBlock
			{
				IsParsing = true,
				Name = "tb",
				// Source 0\MyResourceDictionary.xaml (Line 12:5)
			}
			.MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_XamlApply((MyResourceDictionary_92716e07ff456818f6d4125e055d4d57XamlApplyExtensions.XamlApplyHandler0)(c2 => 
			{
				/* _isTopLevelDictionary:True */
				__that._component_0 = c2;
				__nameScope.RegisterName("tb", c2);
				__that.tb = c2;
				c2.SetBinding(
					global::Microsoft.UI.Xaml.Controls.TextBlock.TextProperty,
					new Microsoft.UI.Xaml.Data.Binding()
					{
						Mode = BindingMode.OneWay,
					}
						.BindingApply(___b => /*defaultBindModeOneWay*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, null, ___ctx => ___ctx is TestRepro.MyModel ___tctx ? (TryGetInstance_xBind_3(___tctx, out var bindResult3) ? (true, bindResult3) : (false, default)) : (false, default), null , new [] {"MyString"}))
				);
				global::Uno.UI.FrameworkElementHelper.SetBaseUri(c2, __baseUri_MyResourceDictionary_92716e07ff456818f6d4125e055d4d57);
				c2.CreationComplete();
			}
			))
			;
			if (__rootInstance is FrameworkElement __fe) 
			{
				var owner = this;
				__fe.Loading += delegate
				{
					_component_0.UpdateResourceBindings();
				}
				;
			}
			if (__rootInstance is DependencyObject d)
			{
				if (global::Microsoft.UI.Xaml.NameScope.GetNameScope(d) == null)
				{
					global::Microsoft.UI.Xaml.NameScope.SetNameScope(d, __nameScope);
					__nameScope.Owner = d;
				}
				global::Uno.UI.FrameworkElementHelper.AddObjectReference(d, this);
			}
			return __rootInstance;
		}
		private global::Microsoft.UI.Xaml.Markup.ComponentHolder _component_0_Holder  = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: true);
		private global::Microsoft.UI.Xaml.Controls.TextBlock _component_0
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.TextBlock)_component_0_Holder.Instance;
			}
			set
			{
				_component_0_Holder.Instance = value;
			}
		}
		private global::Microsoft.UI.Xaml.Data.ElementNameSubject _tbSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
		private global::Microsoft.UI.Xaml.Controls.TextBlock tb
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.TextBlock)_tbSubject.ElementInstance;
			}
			set
			{
				_tbSubject.ElementInstance = value;
			}
		}
		private static bool TryGetInstance_xBind_3(global::TestRepro.MyModel ___tctx, out object o)
		{
			o = null;
			o = ___tctx.MyString;
			return true;
		}
	}
}
namespace MyProject
{
	static class MyResourceDictionary_92716e07ff456818f6d4125e055d4d57XamlApplyExtensions
	{
		public delegate void XamlApplyHandler0(global::Microsoft.UI.Xaml.Controls.TextBlock instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.TextBlock MyResourceDictionary_92716e07ff456818f6d4125e055d4d57_XamlApply(this global::Microsoft.UI.Xaml.Controls.TextBlock instance, XamlApplyHandler0 handler)
		{
			handler(instance);
			return instance;
		}
	}
}
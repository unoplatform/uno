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

#if HAS_UNO_SKIA
using _View = Microsoft.UI.Xaml.UIElement;
#elif __ANDROID__
using _View = Android.Views.View;
#elif __APPLE_UIKIT__ || __IOS__ || __TVOS__
using _View = UIKit.UIView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace TestRepro
{
	partial class MainPage : global::Microsoft.UI.Xaml.Controls.Page
	{
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_prefix_MainPage_d6cd66944958ced0c513e0a04797b51d = "ms-appx:///TestProject/";
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d = "ms-appx:///TestProject/";
		private global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();
		[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Generated code")]
		[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Generated code")]
		private void InitializeComponent()
		{
			NameScope.SetNameScope(this, __nameScope);
			var __that = this;
			base.IsParsing = true;
			// Source 0\MainPage.xaml (Line 1:2)
			base.Content = 
			new global::Microsoft.UI.Xaml.Controls.StackPanel
			{
				IsParsing = true,
				// Source 0\MainPage.xaml (Line 6:3)
				Children = 
				{
					new global::Microsoft.UI.Xaml.Controls.TextBlock
					{
						IsParsing = true,
						// Source 0\MainPage.xaml (Line 7:4)
					}
					.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler0)(__p1 => 
					{
					/* _isTopLevelDictionary:False */
					__that._component_0 = __p1;
					__p1.SetBinding(
						global::Microsoft.UI.Xaml.Controls.TextBlock.TextProperty,
						new Microsoft.UI.Xaml.Data.Binding()
						{
							Mode = BindingMode.OneTime,
						}
							.BindingApply(__that, (___b, ___t) =>  /*defaultBindModeOneTime ViewModel.Name.ToUpper()*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, ___t, ___ctx => ___ctx is global::TestRepro.MainPage ___tctx ? ((true, ___tctx.ViewModel.Name.ToUpper())) : (false, default), null ))
					);
					global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
					__p1.CreationComplete();
					}
					))
					,
					new global::Microsoft.UI.Xaml.Controls.TextBlock
					{
						IsParsing = true,
						// Source 0\MainPage.xaml (Line 8:4)
					}
					.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler0)(__p1 => 
					{
					/* _isTopLevelDictionary:False */
					__that._component_1 = __p1;
					__p1.SetBinding(
						global::Microsoft.UI.Xaml.Controls.TextBlock.TextProperty,
						new Microsoft.UI.Xaml.Data.Binding()
						{
							Mode = global::Microsoft.UI.Xaml.Data.BindingMode.OneWay,
						}
							.BindingApply(__that, (___b, ___t) =>  /*defaultBindModeOneTime ViewModel.Name.ToUpper()*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, ___t, ___ctx => ___ctx is global::TestRepro.MainPage ___tctx ? ((true, ___tctx.ViewModel.Name.ToUpper())) : (false, default), null , new [] {"ViewModel.Name"}))
					);
					global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
					__p1.CreationComplete();
					}
					))
					,
					new global::Microsoft.UI.Xaml.Controls.TextBlock
					{
						IsParsing = true,
						// Source 0\MainPage.xaml (Line 9:4)
					}
					.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler0)(__p1 => 
					{
					/* _isTopLevelDictionary:False */
					__that._component_2 = __p1;
					__p1.SetBinding(
						global::Microsoft.UI.Xaml.Controls.TextBlock.TextProperty,
						new Microsoft.UI.Xaml.Data.Binding()
						{
							Mode = global::Microsoft.UI.Xaml.Data.BindingMode.TwoWay,
						}
							.BindingApply(__that, (___b, ___t) =>  /*defaultBindModeOneTime ViewModel.Name.ToUpper()*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, ___t, ___ctx => ___ctx is global::TestRepro.MainPage ___tctx ? ((true, ___tctx.ViewModel.Name.ToUpper())) : (false, default), (___tctx, __value) => ViewModel.MyBindBack((string)__value) , new [] {"ViewModel.Name"}))
					);
					global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
					__p1.CreationComplete();
					}
					))
					,
				}
			}
			.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler1)(__p1 => 
			{
			global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
			__p1.CreationComplete();
			}
			))
			;
			
			this
			.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler2)(__p1 => 
			{
			// Source 0\MainPage.xaml (Line 1:2)
			
			// WARNING Property __p1.base does not exist on {http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page, the namespace is http://www.w3.org/XML/1998/namespace. This error was considered irrelevant by the XamlFileGenerator
			}
			))
			.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler2)(__p1 => 
			{
			// Class TestRepro.MainPage
			global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
			__p1.CreationComplete();
			}
			))
			;
			OnInitializeCompleted();

			Bindings = new MainPage_Bindings(this);
			((global::Microsoft.UI.Xaml.FrameworkElement)this).Loading += __UpdateBindingsAndResources;
			((global::Microsoft.UI.Xaml.FrameworkElement)this).Unloaded += __StopTracking;
		}
		partial void OnInitializeCompleted();
		private void __UpdateBindingsAndResources(global::Microsoft.UI.Xaml.FrameworkElement s, object e)
		{
			this.Bindings.Update();
			this.Bindings.UpdateResources();
		}

		private void __StopTracking(object s, global::Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			this.Bindings.StopTracking();
		}

		private global::Microsoft.UI.Xaml.Markup.ComponentHolder _component_0_Holder = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: true);
		private global::Microsoft.UI.Xaml.Controls.TextBlock _component_0
		{
			get => (global::Microsoft.UI.Xaml.Controls.TextBlock)_component_0_Holder.Instance;
			set => _component_0_Holder.Instance = value;
		}
		private global::Microsoft.UI.Xaml.Markup.ComponentHolder _component_1_Holder = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: true);
		private global::Microsoft.UI.Xaml.Controls.TextBlock _component_1
		{
			get => (global::Microsoft.UI.Xaml.Controls.TextBlock)_component_1_Holder.Instance;
			set => _component_1_Holder.Instance = value;
		}
		private global::Microsoft.UI.Xaml.Markup.ComponentHolder _component_2_Holder = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: true);
		private global::Microsoft.UI.Xaml.Controls.TextBlock _component_2
		{
			get => (global::Microsoft.UI.Xaml.Controls.TextBlock)_component_2_Holder.Instance;
			set => _component_2_Holder.Instance = value;
		}
		private interface IMainPage_Bindings
		{
			void Initialize();
			void Update();
			void UpdateResources();
			void StopTracking();
			void NotifyXLoad(string name);
		}
		#pragma warning disable 0169 //  Suppress unused field warning in case Bindings is not used.
		private IMainPage_Bindings Bindings;
		#pragma warning restore 0169
		[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Generated code")]
		[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Generated code")]
		[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
		private class MainPage_Bindings : IMainPage_Bindings
		{
			#if UNO_HAS_UIELEMENT_IMPLICIT_PINNING
			private global::System.WeakReference _ownerReference;
			private global::TestRepro.MainPage Owner { get => (global::TestRepro.MainPage)_ownerReference?.Target; set => _ownerReference = new global::System.WeakReference(value); }
			#else
			private global::TestRepro.MainPage Owner { get; set; }
			#endif
			public MainPage_Bindings(global::TestRepro.MainPage owner)
			{
				Owner = owner;
			}
			void IMainPage_Bindings.NotifyXLoad(string name)
			{
			}
			void IMainPage_Bindings.Initialize()
			{
			}
			void IMainPage_Bindings.Update()
			{
				var owner = Owner;
				owner._component_0.ApplyXBind();
				owner._component_1.ApplyXBind();
				owner._component_2.ApplyXBind();
			}
			void IMainPage_Bindings.UpdateResources()
			{
				var owner = Owner;
				owner._component_0.UpdateResourceBindings();
				owner._component_1.UpdateResourceBindings();
				owner._component_2.UpdateResourceBindings();
			}
			void IMainPage_Bindings.StopTracking()
			{
				var owner = Owner;
				owner._component_0.SuspendXBind();
				owner._component_1.SuspendXBind();
				owner._component_2.SuspendXBind();
			}
		}
	}
}
namespace MyProject
{
	static class MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions
	{
		public delegate void XamlApplyHandler0(global::Microsoft.UI.Xaml.Controls.TextBlock instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.TextBlock MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply(this global::Microsoft.UI.Xaml.Controls.TextBlock instance, XamlApplyHandler0 handler)
		{
			handler(instance);
			return instance;
		}
		public delegate void XamlApplyHandler1(global::Microsoft.UI.Xaml.Controls.StackPanel instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.StackPanel MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply(this global::Microsoft.UI.Xaml.Controls.StackPanel instance, XamlApplyHandler1 handler)
		{
			handler(instance);
			return instance;
		}
		public delegate void XamlApplyHandler2(global::Microsoft.UI.Xaml.Controls.Page instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.Page MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply(this global::Microsoft.UI.Xaml.Controls.Page instance, XamlApplyHandler2 handler)
		{
			handler(instance);
			return instance;
		}
	}
}

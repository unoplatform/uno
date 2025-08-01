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
				// Source 0\MainPage.xaml (Line 10:3)
				Children = 
				{
					new Microsoft.UI.Xaml.ElementStub( () => 
					new global::Microsoft.UI.Xaml.Controls.Grid
					{
						IsParsing = true,
						Name = "outerGrid",
						// Source 0\MainPage.xaml (Line 11:4)
						Children = 
						{
							new global::Microsoft.UI.Xaml.Controls.StackPanel
							{
								IsParsing = true,
								Name = "inner1",
								// Source 0\MainPage.xaml (Line 12:5)
								Children = 
								{
									new global::Microsoft.UI.Xaml.Controls.Button
									{
										IsParsing = true,
										Name = "inner1Button",
										// Source 0\MainPage.xaml (Line 13:6)
									}
									.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler0)(__p1 => 
									{
									__nameScope.RegisterName("inner1Button", __p1);
									__that.inner1Button = __p1;
									global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
									__p1.CreationComplete();
									}
									))
									,
								}
							}
							.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler1)(__p1 => 
							{
							__nameScope.RegisterName("inner1", __p1);
							__that.inner1 = __p1;
							global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
							__p1.CreationComplete();
							}
							))
							,
							new global::Microsoft.UI.Xaml.Controls.Button
							{
								IsParsing = true,
								Name = "inner2",
								Template = 								new global::Microsoft.UI.Xaml.Controls.ControlTemplate(this, Build_PagΞ0_StaPanΞ0_GriΞ1_But_TemΞ0_ConTem)
								,
								// Source 0\MainPage.xaml (Line 15:5)
							}
							.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler0)(__p1 => 
							{
							__nameScope.RegisterName("inner2", __p1);
							__that.inner2 = __p1;
							global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
							__p1.CreationComplete();
							}
							))
							,
							new global::Microsoft.UI.Xaml.Controls.StackPanel
							{
								IsParsing = true,
								Name = "inner3",
								// Source 0\MainPage.xaml (Line 24:5)
								Children = 
								{
									new global::Microsoft.UI.Xaml.Controls.Button
									{
										IsParsing = true,
										Name = "inner3Button",
										// Source 0\MainPage.xaml (Line 25:6)
									}
									.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler0)(__p1 => 
									{
									__nameScope.RegisterName("inner3Button", __p1);
									__that.inner3Button = __p1;
									global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
									__p1.CreationComplete();
									}
									))
									,
								}
							}
							.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler1)(__p1 => 
							{
							__nameScope.RegisterName("inner3", __p1);
							__that.inner3 = __p1;
							global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
							__p1.CreationComplete();
							}
							))
							,
						}
					}
					.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler2)(__p1 => 
					{
					/* _isTopLevelDictionary:False */
					__that._component_0 = __p1;
					__nameScope.RegisterName("outerGrid", __p1);
					__that.outerGrid = __p1;
					/* Skipping x:Load attribute already applied to ElementStub */
					global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
					__p1.CreationComplete();
					}
					))
					)					.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler3)(__p1 => 
					{
					__p1.Name = "outerGrid";
					_outerGridSubject.ElementInstance = __p1;
					__p1.SetBinding(
						global::Microsoft.UI.Xaml.ElementStub.LoadProperty,
						new Microsoft.UI.Xaml.Data.Binding()
						{
							Mode = BindingMode.OneTime,
						}
							.BindingApply(__that, (___b, ___t) =>  /*defaultBindModeOneTime IsLoaded*/ global::Uno.UI.Xaml.BindingHelper.SetBindingXBindProvider(___b, ___t, ___ctx => ___ctx is global::TestRepro.MainPage ___tctx ? (TryGetInstance_xBind_1(___tctx, out var bindResult1) ? (true, bindResult1) : (false, default)) : (false, default), null ))
					);
					__that._component_1 = __p1;
					var _component_1_update_That = (this as global::Uno.UI.DataBinding.IWeakReferenceProvider).WeakReference;
					void _component_1_update(global::Microsoft.UI.Xaml.ElementStub sender)
					{
						if (_component_1_update_That.Target is global::TestRepro.MainPage that)
						{
							if (sender.IsMaterialized)
							{
								that.Bindings.UpdateResources();
								that.Bindings.NotifyXLoad("outerGrid");
							}
							else
							{
								that._outerGridSubject.ElementInstance = null;
								that._inner1Subject.ElementInstance = null;
								that._inner1Subject.ElementInstance = null;
								that._inner1ButtonSubject.ElementInstance = null;
								that._inner1ButtonSubject.ElementInstance = null;
								that._inner2Subject.ElementInstance = null;
								that._inner2Subject.ElementInstance = null;
								that._inner3Subject.ElementInstance = null;
								that._inner3Subject.ElementInstance = null;
								that._inner3ButtonSubject.ElementInstance = null;
								that._inner3ButtonSubject.ElementInstance = null;
							}
						}
					}
					__p1.MaterializationChanged += _component_1_update;
					var owner = this;
					void _component_1_materializing(object sender)
					{
						if (_component_1_update_That.Target is global::TestRepro.MainPage that)
						{
							that._component_0.ApplyXBind();
							that._component_0.UpdateResourceBindings();
						}
					}
					__p1.Materializing += _component_1_materializing;
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
			.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler4)(__p1 => 
			{
			// Source 0\MainPage.xaml (Line 1:2)
			
			// WARNING Property __p1.base does not exist on {http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page, the namespace is http://www.w3.org/XML/1998/namespace. This error was considered irrelevant by the XamlFileGenerator
			}
			))
			.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler4)(__p1 => 
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
		private static _View Build_PagΞ0_StaPanΞ0_GriΞ1_But_TemΞ0_ConTem(object __owner, global::Microsoft.UI.Xaml.TemplateMaterializationSettings __settings)
		{
			
			return new __MainPage_d6cd66944958ced0c513e0a04797b51d.__PagΞ0_StaPanΞ0_GriΞ1_But_TemΞ0_ConTem().Build(__owner, __settings);
		}

		private void __UpdateBindingsAndResources(global::Microsoft.UI.Xaml.FrameworkElement s, object e)
		{
			this.Bindings.Update();
			this.Bindings.UpdateResources();
		}

		private void __StopTracking(object s, global::Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			this.Bindings.StopTracking();
		}

		private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _inner1Subject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
		private global::Microsoft.UI.Xaml.Controls.StackPanel inner1
		{
			get => (global::Microsoft.UI.Xaml.Controls.StackPanel)_inner1Subject.ElementInstance;
			set => _inner1Subject.ElementInstance = value;
		}
		private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _inner1ButtonSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
		private global::Microsoft.UI.Xaml.Controls.Button inner1Button
		{
			get => (global::Microsoft.UI.Xaml.Controls.Button)_inner1ButtonSubject.ElementInstance;
			set => _inner1ButtonSubject.ElementInstance = value;
		}
		private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _inner2Subject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
		private global::Microsoft.UI.Xaml.Controls.Button inner2
		{
			get => (global::Microsoft.UI.Xaml.Controls.Button)_inner2Subject.ElementInstance;
			set => _inner2Subject.ElementInstance = value;
		}
		private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _inner3Subject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
		private global::Microsoft.UI.Xaml.Controls.StackPanel inner3
		{
			get => (global::Microsoft.UI.Xaml.Controls.StackPanel)_inner3Subject.ElementInstance;
			set => _inner3Subject.ElementInstance = value;
		}
		private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _inner3ButtonSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
		private global::Microsoft.UI.Xaml.Controls.Button inner3Button
		{
			get => (global::Microsoft.UI.Xaml.Controls.Button)_inner3ButtonSubject.ElementInstance;
			set => _inner3ButtonSubject.ElementInstance = value;
		}
		private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _outerGridSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
		private global::Microsoft.UI.Xaml.Controls.Grid outerGrid
		{
			get => (global::Microsoft.UI.Xaml.Controls.Grid)_outerGridSubject.ElementInstance;
			set => _outerGridSubject.ElementInstance = value;
		}
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		[global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdate]
		private class __MainPage_d6cd66944958ced0c513e0a04797b51d
		{
			[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Generated code")]
			[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2111", Justification = "Generated code")]
			[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
			[global::System.Runtime.CompilerServices.CreateNewOnMetadataUpdate]
			public class __PagΞ0_StaPanΞ0_GriΞ1_But_TemΞ0_ConTem
			{
				[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
				private const string __baseUri_prefix_MainPage_d6cd66944958ced0c513e0a04797b51d = "ms-appx:///TestProject/";
				[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
				private const string __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d = "ms-appx:///TestProject/";
				global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();
				global::System.Object __ResourceOwner_1;
				_View __rootInstance = null;
				public _View Build(object __ResourceOwner_1, global::Microsoft.UI.Xaml.TemplateMaterializationSettings __settings)
				{
					var __that = this;
					this.__ResourceOwner_1 = __ResourceOwner_1;
					this.__rootInstance = 
					new global::Microsoft.UI.Xaml.Controls.Grid
					{
						IsParsing = true,
						Name = "gridInsideTemplate",
						// Source 0\MainPage.xaml (Line 18:8)
						Children = 
						{
							new global::Microsoft.UI.Xaml.Controls.Grid
							{
								IsParsing = true,
								Name = "gridInsideGridInsideTemplate",
								// Source 0\MainPage.xaml (Line 19:9)
							}
							.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler2)(__p1 => 
							{
							__nameScope.RegisterName("gridInsideGridInsideTemplate", __p1);
							__that.gridInsideGridInsideTemplate = __p1;
							global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
							__p1.CreationComplete();
							}
							))
							,
						}
					}
					.MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply((MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions.XamlApplyHandler2)(__p1 => 
					{
					__nameScope.RegisterName("gridInsideTemplate", __p1);
					__that.gridInsideTemplate = __p1;
					global::Uno.UI.FrameworkElementHelper.SetBaseUri(__p1, __baseUri_MainPage_d6cd66944958ced0c513e0a04797b51d);
					__p1.CreationComplete();
					}
					))
					;
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
				private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _gridInsideGridInsideTemplateSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
				private global::Microsoft.UI.Xaml.Controls.Grid gridInsideGridInsideTemplate
				{
					get => (global::Microsoft.UI.Xaml.Controls.Grid)_gridInsideGridInsideTemplateSubject.ElementInstance;
					set => _gridInsideGridInsideTemplateSubject.ElementInstance = value;
				}
				private readonly global::Microsoft.UI.Xaml.Data.ElementNameSubject _gridInsideTemplateSubject = new global::Microsoft.UI.Xaml.Data.ElementNameSubject();
				private global::Microsoft.UI.Xaml.Controls.Grid gridInsideTemplate
				{
					get => (global::Microsoft.UI.Xaml.Controls.Grid)_gridInsideTemplateSubject.ElementInstance;
					set => _gridInsideTemplateSubject.ElementInstance = value;
				}
			}
		}
		private global::Microsoft.UI.Xaml.Markup.ComponentHolder _component_0_Holder = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: true);
		private global::Microsoft.UI.Xaml.Controls.Grid _component_0
		{
			get => (global::Microsoft.UI.Xaml.Controls.Grid)_component_0_Holder.Instance;
			set => _component_0_Holder.Instance = value;
		}
		private global::Microsoft.UI.Xaml.Markup.ComponentHolder _component_1_Holder = new global::Microsoft.UI.Xaml.Markup.ComponentHolder(isWeak: false);
		private global::Microsoft.UI.Xaml.ElementStub _component_1
		{
			get => (global::Microsoft.UI.Xaml.ElementStub)_component_1_Holder.Instance;
			set => _component_1_Holder.Instance = value;
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
				if (name == "outerGrid")
				{
				}
			}
			void IMainPage_Bindings.Initialize()
			{
			}
			void IMainPage_Bindings.Update()
			{
				var owner = Owner;
				owner._component_0.ApplyXBind();
				owner._component_1.ApplyXBind();
			}
			void IMainPage_Bindings.UpdateResources()
			{
				var owner = Owner;
				owner._component_0.UpdateResourceBindings();
				owner._component_1.UpdateResourceBindings();
			}
			void IMainPage_Bindings.StopTracking()
			{
				var owner = Owner;
				owner._component_0.SuspendXBind();
				owner._component_1.SuspendXBind();
			}
		}
		private static bool TryGetInstance_xBind_1(global::TestRepro.MainPage ___tctx, out object o)
		{
			o = null;
			o = ___tctx.IsLoaded;
			return true;
		}
	}
}
namespace MyProject
{
	static class MainPage_d6cd66944958ced0c513e0a04797b51dXamlApplyExtensions
	{
		public delegate void XamlApplyHandler0(global::Microsoft.UI.Xaml.Controls.Button instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.Button MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply(this global::Microsoft.UI.Xaml.Controls.Button instance, XamlApplyHandler0 handler)
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
		public delegate void XamlApplyHandler2(global::Microsoft.UI.Xaml.Controls.Grid instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.Grid MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply(this global::Microsoft.UI.Xaml.Controls.Grid instance, XamlApplyHandler2 handler)
		{
			handler(instance);
			return instance;
		}
		public delegate void XamlApplyHandler3(global::Microsoft.UI.Xaml.ElementStub instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.ElementStub MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply(this global::Microsoft.UI.Xaml.ElementStub instance, XamlApplyHandler3 handler)
		{
			handler(instance);
			return instance;
		}
		public delegate void XamlApplyHandler4(global::Microsoft.UI.Xaml.Controls.Page instance);
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		public static global::Microsoft.UI.Xaml.Controls.Page MainPage_d6cd66944958ced0c513e0a04797b51d_XamlApply(this global::Microsoft.UI.Xaml.Controls.Page instance, XamlApplyHandler4 handler)
		{
			handler(instance);
			return instance;
		}
	}
}

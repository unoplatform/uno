#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Windows.Foundation.Metadata;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Resources;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Helpers.Xaml;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Markup;
using Uno.Xaml;

using Color = Windows.UI.Color;

#if __ANDROID__
using _View = Android.Views.View;
#elif __APPLE_UIKIT__
using _View = UIKit.UIView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Markup.Reader
{
	[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "Normal flow of operations")]
	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "Normal flow of operations")]
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Normal flow of operations")]
	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Normal flow of operations")]
	internal partial class XamlObjectBuilder
	{
		private XamlFileDefinition _fileDefinition;
		private readonly string? _fileUri;
		private XamlTypeResolver TypeResolver { get; }
		private readonly List<(string elementName, ElementNameSubject bindingSubject)> _elementNames = new List<(string, ElementNameSubject)>();
		private readonly Stack<Type> _styleTargetTypeStack = new Stack<Type>();
		private Queue<Action> _postActions = new Queue<Action>();
		private List<XamlParseException>? _parseExceptions;

		private static Type[] _genericConvertibles = new[]
		{
			typeof(Media.Brush),
			typeof(Media.SolidColorBrush),
			typeof(Color),
			typeof(Thickness),
			typeof(CornerRadius),
			typeof(Media.FontFamily),
			typeof(GridLength),
			typeof(Media.Animation.KeyTime),
			typeof(Duration),
			typeof(Media.Matrix),
			typeof(FontWeight),
		};
		private static readonly char[] _parenthesesArray = new[] { '(', ')' };

		public XamlObjectBuilder(XamlFileDefinition xamlFileDefinition)
		{
			_fileDefinition = xamlFileDefinition;
			TypeResolver = new XamlTypeResolver(_fileDefinition);
		}

		internal XamlObjectBuilder(XamlFileDefinition xamlFileDefinition, string fileUri)
		{
			_fileDefinition = xamlFileDefinition;
			_fileUri = fileUri;
			TypeResolver = new XamlTypeResolver(_fileDefinition);
		}

		internal object? Build(object? component = null, bool createInstanceFromXClass = false)
		{
			bool topLevelExceptionSet = false;
			try
			{
				var topLevelControl = _fileDefinition.Objects.First();

				var instance = LoadObject(topLevelControl, rootInstance: null, component: component, createInstanceFromXClass: createInstanceFromXClass);

				if (_parseExceptions?.Count > 0)
				{
					topLevelExceptionSet = true;

					if (_parseExceptions.Count == 1)
					{
						throw _parseExceptions[0];
					}
					else
					{
						throw new AggregateException("Multiple exceptions were thrown during XAML parsing", _parseExceptions);
					}
				}

				ApplyPostActions(instance);

				return instance;
			}
			catch (Exception e)
			{
				if (!topLevelExceptionSet)
				{
					if (e is XamlParseException xpe)
					{
						AddParseException(xpe);
					}
					else
					{
						AddParseException(new XamlParseException("Failed to build the XAML tree", e));
					}
				}
			}

			if (_parseExceptions?.Count == 1)
			{
				throw _parseExceptions[0];
			}
			else
			{
				throw new XamlParseException(
					"Multiple exceptions were thrown during XAML parsing",
					new AggregateException(null, _parseExceptions ?? []));
			}
		}

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
		// Regardless the setup, XamlReader still uses the new templated-parent impl, including the new framework-template.ctor.
		// But because they are not referenced anywhere, they could be trimmed and leads to:
		// > MissingMethodException: MissingConstructor_Name, Microsoft.UI.Xaml.DataTemplate
		// note: This is only needed while we are still supporting legacy codegen. It can be safely deleted once we moved to the new setup.
		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ControlTemplate))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(DataTemplate))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(ItemsPanelTemplate))]
#endif
		private object? LoadObject(
			XamlObjectDefinition? control,
			object? rootInstance,
			object? component = null,
			TemplateMaterializationSettings? settings = null,
			MemberInitializationContext? memberContext = null,
			bool createInstanceFromXClass = false)
		{
			if (control == null)
			{
				return null;
			}

			if (control.Type.PreferredXamlNamespace == XamlConstants.Xmlnses.X &&
				control.Type.Name == "NullExtension")
			{
				return null;
			}

			void TrySetContextualProperties(object? instance, XamlObjectDefinition control)
			{
				if (instance is FrameworkElement fe)
				{
					if (_fileUri is { })
					{
						fe.SetBaseUri(fe.BaseUri.OriginalString, _fileUri, control.LineNumber, control.LinePosition);
					}
					if (settings?.TemplatedParent is { } tp)
					{
						fe.SetTemplatedParent(tp);
						settings.TemplateMemberCreatedCallback?.Invoke(fe);
					}
				}
			}

			if (createInstanceFromXClass && TypeResolver.FindTypeByXClass(control) is { } classType)
			{
				var created = Activator.CreateInstance(classType);
				TrySetContextualProperties(created, control);

				return created;
			}

			var type = TypeResolver.FindType(control.Type);
#if false
			// This region is a failed attempt at "-Extension" resolution priority. It is disabled because we still don't understand the exact behavior.

			// The order of lookup is to look for the Extension-suffixed class name first and then look for the class name without the Extension suffix.
			// -- https://learn.microsoft.com/en-us/dotnet/desktop/xaml-services/markup-extensions-overview#defining-the-support-type-for-a-custom-markup-extension
			// This rule is quite complicated..:
			// ScenarioA. Given (Sanity,SanityExtension) defined:
			//		{Sanity}->SanityExtension, {SanityExtension}->SanityExtension
			// ScenarioB. Given (Sanity,SanityExtension,SanityExtensionExtension) defined:
			//		{Sanity}->SanityExtension, {SanityExtension}->SanityExtension, {SanityExtensionExtension}->SanityExtensionExtension
			// ScenarioC. Given (SanityExtensionExtension) defined:
			//		{SanityExtension}->SanityExtensionExtension
			// ScenarioD. Given (SanityExtension,SanityExtensionExtension) defined:
			//		{SanityExtension}->SanityExtensionExtension, {SanityExtensionExtension}->SanityExtensionExtension
			// ^ And, it doesnt explain why {SanityExtension} resolves to different things between scenario B and D...
			if (type?.Is<MarkupExtension>() == true && !control.Type.Name.EndsWith("Extension", StringComparison.Ordinal))
			{
				// prefer $"{name}Extension" match if we dont already have this suffix
				if (TypeResolver.FindType(control.Type.PreferredXamlNamespace, control.Type.Name + "Extension") is { } extensionType &&
					extensionType.Is<MarkupExtension>())
				{
					type = extensionType;
				}
			}
#endif
			if (type == null)
			{
				// If we can match a $"{name}Extension" class and it extends from MarkupExtension, that is also a valid match.
				// This shortcut syntax is not limited for {Markup} markup declaration syntax, but also works on <Markup> xaml-node declaration. (verified on WinAppSdk)
				// note: On windows, the custom markup type MUST BE ALREADY used/referenced in the xaml once, in order for XamlReader to work with it, otherwise it will throws:
				//		Microsoft.UI.Xaml.Markup.XamlParseException: 'The text associated with this error code could not be found.
				//		The type 'Sanity' was not found. [Line: 1 Position: 9]'
				if (TypeResolver.FindType(control.Type.PreferredXamlNamespace, control.Type.Name + "Extension") is { } extensionType &&
					extensionType.Is<MarkupExtension>())
				{
					type = extensionType;
				}
				else
				{
					throw new InvalidOperationException($"Unable to find type {control.Type}. If the linker is enabled, more info at https://aka.platform.uno/linker-configuration");
				}
			}

			var unknownContent = control.Members.Where(m => m.Member.Name == XamlConstants.UnknownContent).FirstOrDefault();
			var initializationMember = control.Members.Where(m => m.Member.Name == "_Initialization").FirstOrDefault();

			if (type.Is<FrameworkTemplate>())
			{
				NewFrameworkTemplateBuilder builder = (o, s) =>
				{
					var contentOwner = unknownContent;

					return LoadObject(contentOwner?.Objects.FirstOrDefault(), rootInstance: rootInstance, settings: s) as _View;
				};

				// We're loading the object ahead of time in order to get parse errors in that block
				// if any. We're not using the result of that load.
				_ = LoadObject(unknownContent?.Objects.FirstOrDefault(), rootInstance: rootInstance);

				var created = Activator.CreateInstance(type, /* owner: */null, /* factory: */builder);
				TrySetContextualProperties(created, control);

				return created;
			}
			else if (type.Is<MarkupExtension>())
			{
				var instance = Activator.CreateInstance(type) as MarkupExtension ??
					throw new InvalidCastException($"Can not cast from '{type.FullName}' to '{nameof(MarkupExtension)}'.");

				foreach (var member in control.Members)
				{
					ProcessNamedMember(control, instance, member, rootInstance ?? instance, settings);
				}

				var provider = new XamlServiceProviderContext
				{
					TargetObject = memberContext?.Target,
					TargetProperty = memberContext?.Property is { } pi
						? new ProvideValueTargetProperty
						{
							DeclaringType = pi.DeclaringType,
							Name = pi.Name,
							Type = pi.PropertyType,
						}
						: null,
					RootObject = rootInstance,
				};

				var value = ((IMarkupExtensionOverrides)instance).ProvideValue(provider);

				// It seems WinUI will generally throw: "Failed to assign to property '{fullname of dp}'"
				// like for returning true(bool) to a string dp. So there is no conversion needed here.
				// We can just let the caller to throw when invalid assignment eventually occurs.

				return value;
			}
			else if (type.Is<ResourceDictionary>())
			{
				var instance = Activator.CreateInstance(type) as ResourceDictionary ??
					throw new InvalidCastException($"Can not cast from '{type.FullName}' to '{nameof(ResourceDictionary)}'.");

				foreach (var member in control.Members.Where(m => m != unknownContent))
				{
					ProcessNamedMember(control, instance, member, instance, settings: null);
				}
				if (unknownContent is { })
				{
					ProcessResourceDictionaryContent(instance, unknownContent, rootInstance);
				}

				return instance;
			}
			else if (type.IsPrimitive && initializationMember?.Value is string primitiveValue)
			{
				return Convert.ChangeType(primitiveValue, type, CultureInfo.InvariantCulture);
			}
			else if (type == typeof(string) && initializationMember?.Value is string stringValue)
			{
				return stringValue;
			}
			else if (type == typeof(Media.Geometry) && unknownContent?.Value is string geometryStringValue)
			{
				var generated = Uno.Media.Parsers.ParseGeometry(geometryStringValue);

				return (Media.Geometry)generated;
			}
			else if (_genericConvertibles.Contains(type) && unknownContent?.Value is string otherContentValue)
			{
				return XamlBindingHelper.ConvertValue(type, otherContentValue);
			}
			else
			{
				var instance = component ?? Activator.CreateInstance(type)!;
				rootInstance ??= instance;

				var instanceAsFE = instance as FrameworkElement;
				if (instanceAsFE is { })
				{
					instanceAsFE.IsParsing = true;
					TrySetContextualProperties(instanceAsFE, control);
				}

				IDisposable? TryProcessStyle()
				{
					if (instance is Style style)
					{
						if (control.Members.FirstOrDefault(m => m.Member.Name == "TargetType") is { } targetTypeDefinition)
						{
							if (BuildLiteralValue(targetTypeDefinition) is Type targetType)
							{
								_styleTargetTypeStack.Push(targetType);

								return Uno.Disposables.Disposable.Create(() =>
								{
									if (_styleTargetTypeStack.Pop() != targetType)
									{
										throw new InvalidOperationException("StyleTargetType is out of synchronization");
									}
								});
							}
							else
							{
								throw new InvalidOperationException($"The type {targetTypeDefinition.Member.Type} is unknown");
							}
						}
					}

					return null;
				}
				using (TryProcessStyle())
				{
					foreach (var member in control.Members)
					{
						try
						{
							ProcessNamedMember(control, instance, member, rootInstance, settings);
						}
						catch (Exception ex)
						{
							AddParseException(new XamlParseException($"Failed to parse member ({ex.Message})", ex, member.LineNumber, member.LinePosition));
						}
					}
				}

				instanceAsFE?.CreationComplete();

				return instance;
			}
		}

		private void AddParseException(XamlParseException xamlParseException)
		{
			_parseExceptions ??= new List<XamlParseException>();
			_parseExceptions.Add(xamlParseException);
		}

		private string RewriteAttachedPropertyPath(string? value)
		{
			value ??= "";

			if (value.Contains('('))
			{
				foreach (var ns in _fileDefinition.Namespaces)
				{
					if (ns != null)
					{
						var clrNamespace = ns.Namespace.TrimStart("using:");

						value = value.Replace("(" + ns.Prefix + ":", "(" + clrNamespace + ":");
					}
				}

				var match = AttachedPropertyMatching().Match(value);

				if (match.Success)
				{
					do
					{
						if (!match.Value.Contains(':'))
						{
							// if there is no ":" this means that the type is using the default
							// namespace, so try to resolve the best way we can.

							var parts = match.Value.Trim(_parenthesesArray).Split('.');

							if (parts.Length == 2)
							{
								var targetType = TypeResolver.FindType(parts[0]);

								if (targetType != null)
								{
									var newPath = targetType.Namespace + ":" + targetType.Name + "." + parts[1];

									value = value.Replace(match.Value, '(' + newPath + ')');
								}
							}
						}
					}
					while ((match = match.NextMatch()).Success);
				}
			}

			return value;
		}

		private void ProcessNamedMember(
			XamlObjectDefinition control,
			object instance,
			XamlMemberDefinition member,
			object rootInstance,
			TemplateMaterializationSettings? settings)
		{
			var isAttached = TypeResolver.IsAttachedProperty(member);
			var isNestedChildNode = IsNestedChildNode(member);

			// Exclude attached properties, must be set in the extended apply section.
			// If there is no type attached, this can be a binding.
			if (TypeResolver.IsType(control.Type, member.Member.DeclaringType)
				&& !isAttached
				|| isNestedChildNode
			// && FindEventType(member.Member) == null
			)
			{
				if (instance is TextBlock textBlock)
				{
					ProcessTextBlock(control, textBlock, member, rootInstance, settings);
				}
				else if (instance is Documents.Span span && isNestedChildNode)
				{
					ProcessSpan(control, span, member, rootInstance);
				}
				// WinUI assigned ContentProperty syntax
				else if (
					instance is ColumnDefinition columnDefinition &&
					isNestedChildNode &&
					member.Value is string columnDefinitionContent &&
					!string.IsNullOrWhiteSpace(columnDefinitionContent))
				{
					columnDefinition.Width = GridLength.ParseGridLength(columnDefinitionContent.Trim()).FirstOrDefault();
				}
				else if (
					instance is RowDefinition rowDefinition &&
					isNestedChildNode &&
					member.Value is string rowDefinitionContent &&
					!string.IsNullOrWhiteSpace(rowDefinitionContent))
				{
					rowDefinition.Height = GridLength.ParseGridLength(rowDefinitionContent.Trim()).FirstOrDefault();
				}
				else if (isNestedChildNode
					&& TypeResolver.FindContentProperty(TypeResolver.FindType(control.Type)) == null
					&& TypeResolver.IsCollectionOrListType(TypeResolver.FindType(control.Type)))
				{
					AddCollectionItems(instance, member.Objects, rootInstance, settings, null);
				}
				else if (GetMemberProperty(control, member) is PropertyInfo propertyInfo)
				{
					if (member.Objects.None())
					{
						if (TypeResolver.IsInitializedCollection(propertyInfo)
							// The Resources property has a public setter, but should be treated as an empty collection
							|| IsResourcesProperty(propertyInfo)
						)
						{
							// WinUI Grid succinct syntax
							if (instance is Grid grid &&
								member.Member.PreferredXamlNamespace == XamlConstants.PresentationXamlXmlNamespace &&
								member.Member.Name is "ColumnDefinitions" or "RowDefinitions" &&
								(member.Member.Name is "ColumnDefinitions") is var isColumnDefinition &&
								member.Value is string definitions)
							{
								var lengths = GridLength.ParseGridLength(definitions);
								foreach (var length in lengths)
								{
									if (isColumnDefinition)
									{
										grid.ColumnDefinitions.Add(new ColumnDefinition { Width = length });
									}
									else
									{
										grid.RowDefinitions.Add(new RowDefinition { Height = length });
									}
								}
							}
							else
							{
								// Empty collection
							}
						}
						else
						{
							if (propertyInfo.PropertyType == typeof(TargetPropertyPath))
							{
								ProcessTargetPropertyPath(instance, member, propertyInfo);
							}
							else
							{
								GetPropertySetter(propertyInfo).Invoke(instance, new[] { BuildLiteralValue(member, propertyInfo.PropertyType) });
							}
						}
					}
					else
					{
						if (IsMarkupExtension(member))
						{
							ProcessMemberMarkupExtension(instance, rootInstance, member, propertyInfo);
						}
						else
						{
							ProcessMemberElements(instance, member, propertyInfo, rootInstance, settings);
						}
					}
				}
				else if (GetMemberEvent(control, member) is EventInfo eventInfo)
				{
					if (member.Value is string eventHandlerName)
					{
						SubscribeToEvent(instance, rootInstance, eventInfo, eventHandlerName, false);
					}
					else if (member.Objects.FirstOrDefault() is { } memberObject && memberObject.Type.Name == "Bind")
					{
						if (memberObject.Members.FirstOrDefault() is { } bindMember && bindMember.Value is string xBindEventHandlerName)
						{
							SubscribeToEvent(instance, rootInstance, eventInfo, xBindEventHandlerName, true);
						}
					}
				}
				else
				{
					// throw new InvalidOperationException($"The Property {member.Member.Name} does not exist on {member.Member.DeclaringType}");
				}
			}
			else if (isAttached)
			{
				var dependencyProperty = TypeResolver.FindDependencyProperty(member);

				if (dependencyProperty != null)
				{
					if (member.Objects.None())
					{
						(instance as DependencyObject)?.SetValue(dependencyProperty, BuildLiteralValue(member, dependencyProperty.Type));
					}
					else
					{
						if (IsMarkupExtension(member))
						{
							ProcessMemberMarkupExtension(instance, rootInstance, member, null);
						}
						else if (instance is DependencyObject dependencyObject)
						{
							ProcessMemberElements(dependencyObject, member, dependencyProperty, rootInstance, settings);
						}
						else
						{
							throw new InvalidOperationException($"{instance} is not a DependencyObject");
						}
					}
				}
			}
			else if (member.Member.DeclaringType == null && member.Member.Name == "Name")
			{
				// This is a special case, where the declaring type is from the x: namespace,
				// but is considered of an unknown type. This can happen when providing the
				// name of a control using x:Name instead of Name.
				if (TypeResolver.GetPropertyByName(control.Type, "Name") is PropertyInfo nameInfo)
				{
					GetPropertySetter(nameInfo).Invoke(instance, new[] { member.Value });
				}

				// Update x:Name generated fields, if any
				if (rootInstance != null && member.Value is string nameValue)
				{
					var allMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
					var rootInstanceType = rootInstance.GetType();

					if (TypeResolver.GetPropertyByName(rootInstanceType, nameValue, allMembers) is { } xNameProperty)
					{
						GetPropertySetter(xNameProperty).Invoke(rootInstance, new[] { instance });
					}
					else if (TypeResolver.GetFieldByName(rootInstanceType, nameValue, allMembers) is { } xNameField)
					{
						xNameField.SetValue(rootInstance, instance);
					}
				}
			}
		}

		private static void SubscribeToEvent(object instance, object rootInstance, EventInfo eventInfo, string eventHandlerName, bool supportsParameterless)
		{
			var eventParamCount = eventInfo.EventHandlerType?.GetMethod("Invoke")?.GetParameters().Length ?? throw new InvalidOperationException();
			var rootType = rootInstance.GetType();
			var targetMethod = GetMethod(eventHandlerName, eventParamCount, rootType);
			if (targetMethod != null)
			{
				var handler = targetMethod.CreateDelegate(eventInfo.EventHandlerType, rootInstance);
				eventInfo.AddEventHandler(instance, handler);
			}
			else if (supportsParameterless && GetMethod(eventHandlerName, 0, rootType) is { } parameterlessMethod && eventParamCount <= 2)
			{
				var wrapper = new EventHandlerWrapper(rootInstance, parameterlessMethod);
				var wrappedHandler = (typeof(EventHandlerWrapper).GetMethod(eventParamCount == 2 ? nameof(EventHandlerWrapper.Handler2) : nameof(EventHandlerWrapper.Handler1)))
					?? throw new InvalidOperationException();
				var handler = Delegate.CreateDelegate(
					eventInfo.EventHandlerType,
					wrapper,
					wrappedHandler
				); ;
				eventInfo.AddEventHandler(instance, handler);
			}
		}

		private static MethodInfo? GetMethod(string methodName, int paramCount, Type type)
			=> type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.Where(m => m.Name == methodName && m.GetParameters().Length == paramCount)
			.FirstOrDefault();

		private void ProcessSpan(XamlObjectDefinition control, Span span, XamlMemberDefinition member, object rootInstance)
		{
			if (member.Objects.Count != 0)
			{
				foreach (var node in member.Objects)
				{
					if (LoadObject(node, rootInstance) is Inline inline)
					{
						span.Inlines.Add(inline);
					}
				}
			}

			if (member.Value != null)
			{
				span.Inlines.Add(
					new Run
					{
						Text = member.Value.ToString()
					}
				);
			}
		}

		private void ProcessTargetPropertyPath(object instance, XamlMemberDefinition member, PropertyInfo propertyInfo)
		{
			if (member.Value is string targetPath)
			{
				// This builds property setters for specified member setter.
				var separatorIndex = targetPath.IndexOf('.');
				var elementName = targetPath.Substring(0, separatorIndex);
				var propertyName = targetPath.Substring(separatorIndex + 1);

				if (instance is Setter setter)
				{
					setter.Target = new TargetPropertyPath(elementName, new PropertyPath(RewriteAttachedPropertyPath(propertyName)));
				}
			}
			else
			{
				throw new NotSupportedException($"The property {propertyInfo} must be provided a value");
			}
		}

		private void ProcessTextBlock(XamlObjectDefinition control, TextBlock instance, XamlMemberDefinition member, object rootInstance, TemplateMaterializationSettings? settings)
		{
			if (IsBlankBaseMember(member))
			{
			}
			else if (IsNestedChildNode(member) || member.Member.Name == nameof(TextBlock.Inlines))
			{
				foreach (var node in member.Objects)
				{
					var value = LoadObject(node, rootInstance);

					if ((TypeResolver.FindType(node.Type) ?? TypeResolver.FindType(node.Type.PreferredXamlNamespace, node.Type.Name + "Extension"))?.Is<MarkupExtension>() == true)
					{
						// WinUI will actually throw an exception for markup that returns `string` or `Run` here:
						// > XamlParseException: The text associated with this error code could not be found.
						// > Run -> Failed to assign to property 'Microsoft.UI.Xaml.Controls.TextBlock.Inlines' because the type 'Microsoft.UI.Xaml.Documents.Run' cannot be assigned to the type 'Microsoft.UI.Xaml.Documents.InlineCollection'. [Line: 22 Position: 8]'
						// > string -> Failed to assign to property 'Microsoft.UI.Xaml.Controls.TextBlock.Inlines'. [Line: 20 Position: 7]'
						throw new XamlParseException(value is Inline
							? $"Failed to assign to property '{typeof(TextBlock).FullName}.{nameof(TextBlock.Inlines)}' because the type '{value.GetType().FullName}' cannot be assigned to the type '{typeof(InlineCollection).FullName}'."
							: $"Failed to assign to property '{typeof(TextBlock).FullName}.{nameof(TextBlock.Inlines)}'."
						);
					}
					if (value is Inline inline)
					{
						instance.Inlines.Add(inline);
					}
				}
			}
			else if (GetMemberProperty(control, member) is PropertyInfo propertyInfo)
			{
				if (member.Objects.Count == 0)
				{
					GetPropertySetter(propertyInfo).Invoke(instance, new[] { BuildLiteralValue(member, propertyInfo.PropertyType) });
				}
				else
				{
					if (IsMarkupExtension(member))
					{
						ProcessMemberMarkupExtension(instance, rootInstance, member, propertyInfo);
					}
					else
					{
						ProcessMemberElements(instance, member, propertyInfo, rootInstance, settings);
					}
				}
			}
		}

		private void ProcessResourceDictionaryContent(ResourceDictionary rd, XamlMemberDefinition unknownContent, object? rootInstance)
		{
			// note: In order for static resolution to work, the referenced resources must be already parsed & added, which means:
			// - MergedDictionaries should be processed before this method call.
			// - Member resources should be all processed prior the resolution can began.
			var delayedResolutionList = new List<IDependencyObjectStoreProvider>();

			foreach (var child in unknownContent.Objects)
			{
				var childInstance = LoadObject(child, rootInstance);
				var resourceKey = GetResourceKey(child);
				var styleTargetType = childInstance is Style
					? GetResourceTargetType(child) ?? throw new InvalidOperationException($"No target type was specified (Line {child.LineNumber}:{child.LinePosition}")
					: default;

				if ((resourceKey ?? styleTargetType) is { } key)
				{
					rd.Add(key, childInstance);
				}

				if (HasAnyResourceMarkup(child) && childInstance is IDependencyObjectStoreProvider provider)
				{
					delayedResolutionList.Add(provider);
				}
			}

			// Delay resolve static resources
			foreach (var provider in delayedResolutionList)
			{
				provider.Store.UpdateResourceBindings(ResourceUpdateReason.StaticResourceLoading, containingDictionary: rd);
			}
		}

		private void ProcessMemberElements(
			DependencyObject instance,
			XamlMemberDefinition member,
			DependencyProperty property,
			object rootInstance,
			TemplateMaterializationSettings? settings)
		{
			if (TypeResolver.IsCollectionOrListType(property.Type))
			{
				object BuildInstance()
				{
					if (property.Type.GetGenericTypeDefinition() == typeof(IList<>))
					{
						return Activator.CreateInstance(typeof(List<>)!.MakeGenericType(property.Type.GenericTypeArguments[0]))!;
					}
					else
					{
						return Activator.CreateInstance(property.Type)!;
					}
				}

				var collection = BuildInstance();

				AddCollectionItems(collection, member.Objects, rootInstance, settings, null);

				instance.SetValue(property, collection);
			}
			else
			{
				instance.SetValue(property, LoadObject(member.Objects.First(), rootInstance: rootInstance, settings: settings));
			}
		}

		private void ProcessMemberElements(
			object instance,
			XamlMemberDefinition member,
			PropertyInfo propertyInfo,
			object rootInstance,
			TemplateMaterializationSettings? settings)
		{
			if (TypeResolver.IsCollectionOrListType(propertyInfo.PropertyType))
			{
				if (propertyInfo.PropertyType.IsAssignableTo(typeof(ResourceDictionary)))
				{
					// A resource-dictionary property (typically FE.Resources) can only have two types of nested contents:
					// 1. a single res-dict
					// 2. zero-to-many non-res-dict resources
					// Nesting multiple res-dict or a mix of (res-dict(s) + resource(s)) will throw:
					// > Xaml Internal Error error WMC9999: This Member 'Resources' has more than one item, use the Items property
					// It is also illegal to nested Page.Resources\ResourceDictionary.MergedDictionary without a ResourceDictionary node in between.
					// > Xaml Xml Parsing Error error WMC9997: 'Unexpected 'PROPERTYELEMENT' in parse rule 'NonemptyPropertyElement ::= . PROPERTYELEMENT Content? ENDTAG.'.'

					// Case 1: a single res-dict
					if (member.Objects is [var singleChild] &&
						TypeResolver.FindType(singleChild.Type)?.IsAssignableTo(typeof(ResourceDictionary)) == true)
					{
						if (propertyInfo.SetMethod == null)
						{
							throw new InvalidOperationException($"The property {propertyInfo} does not provide a setter (Line {member.LineNumber}:{member.LinePosition}");
						}

						var rd = (ResourceDictionary)LoadObject(singleChild, rootInstance)!;
						if (IsFrameworkElementResources(propertyInfo))
						{
							((FrameworkElement)instance).Resources = rd;
						}
						else
						{
							propertyInfo.SetMethod.Invoke(instance, new[] { rd });
						}
					}
					// Case 2: zero-to-many non-res-dict resources
					else if (member.Objects.All(x => TypeResolver.FindType(x.Type)?.IsAssignableTo(typeof(ResourceDictionary)) != true))
					{
						if (propertyInfo.GetMethod == null)
						{
							throw new InvalidOperationException($"The property {propertyInfo} does not provide a getter (Line {member.LineNumber}:{member.LinePosition}");
						}

						if (propertyInfo.GetMethod.Invoke(instance, null) is ResourceDictionary rd)
						{
							ProcessResourceDictionaryContent(rd, member, rootInstance);
						}
						else
						{
							throw new ArgumentNullException(
								$"The property {propertyInfo} is not initialized (Line {member.LineNumber}:{member.LinePosition}). " +
								$"Make sure the property is instanced from the constructor, or nest the resources under '{propertyInfo.PropertyType}'.");
						}
					}
					else
					{
						throw new XamlParseException($"This Member '{propertyInfo.DeclaringType}.{propertyInfo.Name}' has more than one item, use the Items property");
					}

					static bool IsFrameworkElementResources(PropertyInfo propertyInfo) =>
						propertyInfo.DeclaringType == typeof(FrameworkElement) &&
						propertyInfo.Name == nameof(FrameworkElement.Resources);
				}
				else if (propertyInfo.DeclaringType?.IsAssignableTo(typeof(ResourceDictionary)) == true &&
					propertyInfo.Name is nameof(ResourceDictionary.ThemeDictionaries) or nameof(ResourceDictionary.MergedDictionaries))
				{
					foreach (var child in member.Objects)
					{
						var rd = (ResourceDictionary)LoadObject(child, rootInstance)!;
						if (propertyInfo.Name is nameof(ResourceDictionary.ThemeDictionaries))
						{
							((ResourceDictionary)instance).ThemeDictionaries.Add(GetResourceKey(child), rd);
						}
						else
						{
							((ResourceDictionary)instance).MergedDictionaries.Add(rd);
						}
					}
				}
				else if (propertyInfo.SetMethod?.IsPublic == true &&
					member.Objects.Select(x => x.Type).Distinct().All(x => TypeResolver.FindType(x).Is(propertyInfo.PropertyType)))
				{
					// It is actually valid to have multiple nested collection containers under a property, however only the last will be kept.
					foreach (var child in member.Objects)
					{
						var collection = LoadObject(child, rootInstance: rootInstance);

						GetPropertySetter(propertyInfo).Invoke(instance, new[] { collection });
					}
				}
				else if (propertyInfo.SetMethod?.IsPublic == true
					&& TypeResolver.IsNewableType(propertyInfo.PropertyType))
				{
					var collection = Activator.CreateInstance(propertyInfo.PropertyType);
					AddCollectionItems(collection!, member.Objects, rootInstance, settings, new(instance, propertyInfo));

					GetPropertySetter(propertyInfo).Invoke(instance, new[] { collection });
				}
				else if (TypeResolver.IsInitializedCollection(propertyInfo))
				{
					if (propertyInfo.GetMethod == null)
					{
						throw new InvalidOperationException($"The property {propertyInfo} does not provide a getter (Line {member.LineNumber}:{member.LinePosition}");
					}

					var propertyInstance = propertyInfo.GetMethod.Invoke(instance, null);

					if (propertyInstance != null)
					{
						if (propertyInstance is IDictionary<object, object> propertyInstanceAsDictionary)
						{
							AddGenericDictionaryItems(propertyInstanceAsDictionary, member.Objects, rootInstance);
						}
						else
						{
							AddCollectionItems(propertyInstance, member.Objects, rootInstance, settings, new(instance, propertyInfo));
						}
					}
					else
					{
						throw new InvalidOperationException($"The property {propertyInfo} getter did not provide a value (Line {member.LineNumber}:{member.LinePosition}");
					}
				}
				else
				{
					throw new InvalidOperationException($"Unsupported collection type {propertyInfo.PropertyType} on {propertyInfo}");
				}
			}
			else
			{
				GetPropertySetter(propertyInfo).Invoke(instance, [LoadObject(
					member.Objects.First(),
					rootInstance: rootInstance,
					settings: settings,
					memberContext: new(instance, propertyInfo))
				]);
			}
		}

		private static MethodInfo GetPropertySetter(PropertyInfo propertyInfo)
			=> propertyInfo?.SetMethod ?? throw new InvalidOperationException($"Unable to find setter for property [{propertyInfo}]");

		private void ProcessMemberMarkupExtension(object instance, object? rootInstance, XamlMemberDefinition member, PropertyInfo? propertyInfo)
		{
			if (IsBindingMarkupNode(member))
			{
				ProcessBindingMarkupNode(instance, rootInstance, member);
			}
			else if (IsStaticResourceMarkupNode(member) || IsThemeResourceMarkupNode(member) || IsCustomResourceMarkupNode(member))
			{
				ProcessStaticResourceMarkupNode(instance, member, propertyInfo);
			}
		}

		private void ProcessStaticResourceMarkupNode(object instance, XamlMemberDefinition member, PropertyInfo? propertyInfo)
		{
			var resourceNode = member.Objects.FirstOrDefault();

			if (resourceNode != null)
			{
				var keyName = resourceNode.Members.FirstOrDefault()?.Value?.ToString();
				var dependencyProperty = TypeResolver.FindDependencyProperty(member);

				if (keyName != null
					&& dependencyProperty != null
					&& instance is DependencyObject dependencyObject)
				{
					if (IsCustomResourceMarkupNode(member))
					{
						var objectType = dependencyObject.GetType().FullName;
						var propertyName = dependencyProperty.Name;
						var propertyType = dependencyProperty.Type.FullName;
						var resource = CustomXamlResourceLoader.Current?.GetResourceInternal(keyName, objectType, propertyName, propertyType);
						if (resource != null && resource.GetType() == dependencyProperty.Type)
						{
							dependencyObject.SetValue(dependencyProperty, resource);
						}
					}
					else
					{
						ResourceResolver.ApplyResource(
							dependencyObject,
							dependencyProperty,
							keyName,
							isThemeResourceExtension: IsThemeResourceMarkupNode(member),
							isHotReloadSupported: true);

						if (instance is FrameworkElement fe)
						{
							fe.Loading += delegate
							{
								fe.UpdateResourceBindings();
							};
						}
					}

				}
				else if (propertyInfo != null)
				{
					GetPropertySetter(propertyInfo).Invoke(
								instance,
								new[] { ResourceResolver.ResolveResourceStatic(keyName, propertyInfo.PropertyType) }
							);

					if (instance is Setter setter && propertyInfo.Name == "Value")
					{
						// Register StaticResource/ThemeResource assignations to Value for resource updates
						setter.ApplyThemeResourceUpdateValues(
							keyName,
							null,
							IsThemeResourceMarkupNode(member),
							true
						);
					}
				}
			}
		}

		private static object? ResolveStaticResource(object? instance, string? keyName)
		{
			var staticResource = (instance as FrameworkElement)
					.Flatten(i => (i?.Parent as FrameworkElement))
					.Select(fe =>
					{
						if (fe != null
							&& fe.Resources.TryGetValue(keyName, out var resource, shouldCheckSystem: false))
						{
							return resource;
						}
						return null;
					})
					.Concat(ResourceResolver.ResolveTopLevelResource(keyName))
					.Trim()
					.FirstOrDefault();

			return staticResource;
		}

		private bool IsStaticResourceMarkupNode(XamlMemberDefinition member)
			=> member.Objects.Any(o => o.Type.Name == "StaticResource");

		private bool IsThemeResourceMarkupNode(XamlMemberDefinition member)
			=> member.Objects.Any(o => o.Type.Name == "ThemeResource");

		private bool IsCustomResourceMarkupNode(XamlMemberDefinition member)
			=> member.Objects.Any(o => o.Type.Name == "CustomResource");

		private bool IsResourcesProperty(PropertyInfo propertyInfo)
			=> propertyInfo.Name == "Resources" && propertyInfo.PropertyType == typeof(ResourceDictionary);

		private void ProcessBindingMarkupNode(object instance, object? rootInstance, XamlMemberDefinition member)
		{
			var binding = BuildBindingExpression(instance, rootInstance, member);

			if (instance is IDependencyObjectStoreProvider provider)
			{
				var dependencyProperty = TypeResolver.FindDependencyProperty(member);

				if (dependencyProperty != null)
				{
					provider.Store.SetBinding(dependencyProperty, binding);
				}
				else if (TypeResolver.GetPropertyByName(member.Owner.Type, member.Member.Name) is PropertyInfo propertyInfo)
				{
					if (member.Objects.Empty())
					{
						GetPropertySetter(propertyInfo).Invoke(instance, new[] { BuildLiteralValue(member, propertyInfo.PropertyType) });
					}
					else
					{
						GetPropertySetter(propertyInfo).Invoke(instance, new[] { BuildBindingExpression(null, rootInstance, member) });
					}
				}
				else
				{
					throw new NotSupportedException($"Unknown dependency property {member.Member}");
				}
			}
			else
			{
				throw new NotSupportedException($"Binding is not supported on {member.Member}");
			}
		}

		private Binding BuildBindingExpression(object? instance, object? rootInstance, XamlMemberDefinition member)
		{
			var bindingNode = member.Objects.FirstOrDefault(o => o.Type.Name == "Binding");
			var templateBindingNode = member.Objects.FirstOrDefault(o => o.Type.Name == "TemplateBinding");
			var xBindNode = member.Objects.FirstOrDefault(o => o.Type.Name == "Bind");

			var binding = new Data.Binding();

			if (templateBindingNode != null)
			{
				binding.RelativeSource = RelativeSource.TemplatedParent;
			}

			if (xBindNode != null)
			{
				binding.Source = rootInstance;
				// TODO: here we should be setting Mode to OneTime by default, and we should also respect x:DefaultBindMode values set
				// further up in the tree.
			}

			if (bindingNode == null && templateBindingNode == null && xBindNode == null)
			{
				throw new InvalidOperationException("Unable to find Binding or TemplateBinding or x:Bind node");
			}

			foreach (var bindingProperty in (bindingNode ?? templateBindingNode ?? xBindNode)!.Members)
			{
				switch (bindingProperty.Member.Name)
				{
					case "_PositionalParameters":
					case nameof(Binding.Path):
						var path = bindingProperty.Value?.ToString();
						if (templateBindingNode is not null && TypeResolver.IsAttachedProperty(member))
						{
							path = $"({path})";
						}

						binding.Path = RewriteAttachedPropertyPath(path);
						break;

					case nameof(Binding.ElementName):
						var subject = new ElementNameSubject();
						binding.ElementName = subject;

						if (bindingProperty.Value?.ToString() is { } value)
						{
							AddElementName(value, subject);
						}
						break;

					case nameof(Binding.TargetNullValue):
						binding.TargetNullValue = bindingProperty.Value?.ToString();
						break;

					case nameof(Binding.FallbackValue):
						binding.FallbackValue = bindingProperty.Value?.ToString();
						break;

					case nameof(Binding.UpdateSourceTrigger):
						if (Enum.TryParse<UpdateSourceTrigger>(bindingProperty.Value?.ToString(), out var trigger))
						{
							binding.UpdateSourceTrigger = trigger;
						}
						else
						{
							throw new NotSupportedException($"Invalid binding mode {bindingProperty.Value}");
						}
						break;

					case nameof(Binding.RelativeSource):
						if (bindingProperty.Objects.First() is XamlObjectDefinition relativeSource && relativeSource.Type.Name == "RelativeSource")
						{
							var relativeSourceValue = relativeSource.Members.FirstOrDefault()?.Value?.ToString()?.ToLowerInvariant();
							switch (relativeSourceValue)
							{
								case "templatedparent":
									binding.RelativeSource = RelativeSource.TemplatedParent;
									break;

								default:
									throw new NotSupportedException($"RelativeSource {relativeSourceValue} is not supported");
							}
						}
						break;

					case nameof(Binding.Mode):
						if (Enum.TryParse<Data.BindingMode>(bindingProperty.Value?.ToString(), out var mode))
						{
							binding.Mode = mode;
						}
						else
						{
							throw new NotSupportedException($"Invalid binding mode {bindingProperty.Value}");
						}
						break;

					case nameof(Binding.ConverterParameter):
						binding.ConverterParameter = bindingProperty.Value?.ToString();
						break;

					case nameof(Binding.Converter):
						if (
							bindingProperty.Objects.First() is XamlObjectDefinition converterResource
						)
						{
							if (converterResource.Type.Name == "StaticResource")
							{
								var staticResourceName = converterResource.Members.FirstOrDefault()?.Value?.ToString();

								void ResolveResource()
								{
									var staticResource = ResolveStaticResource(instance, staticResourceName);

									if (staticResource != null)
									{
										binding.Converter = staticResource as IValueConverter;
									}
								}

								_postActions.Enqueue(ResolveResource);
							}
							else
							{
								throw new NotSupportedException($"Markup extension {converterResource.Type.Name} is not supported for Bindiner.Converter");
							}
						}
						break;

					default:
						throw new NotSupportedException($"Binding option {bindingProperty.Member} is not supported");
				}
			}

			return binding;
		}

		private void AddElementName(string elementName, ElementNameSubject subject)
		{
			_elementNames.Add((elementName, subject));
		}

		private bool IsBindingMarkupNode(XamlMemberDefinition member)
			=> member.Objects.Any(o => o.Type.Name == "Binding" || o.Type.Name == "TemplateBinding" || o.Type.Name == "Bind");

		private bool HasAnyResourceMarkup(XamlObjectDefinition member)
		{
			foreach (var childMember in member.Members)
			{
				if (IsStaticResourceMarkupNode(childMember) || IsThemeResourceMarkupNode(childMember))
				{
					return true;
				}

				foreach (var childObject in member.Objects)
				{
					if (HasAnyResourceMarkup(childObject))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static bool IsMarkupExtension(XamlMemberDefinition member)
			=> member
				.Objects
				.Where(m =>
					m.Type.Name == "Binding"
					|| m.Type.Name == "Bind"
					|| m.Type.Name == "StaticResource"
					|| m.Type.Name == "ThemeResource"
					|| m.Type.Name == "CustomResource"
					|| m.Type.Name == "TemplateBinding"
				)
				.Any();

		private void AddGenericDictionaryItems(
			IDictionary<object, object> dictionary,
			IEnumerable<XamlObjectDefinition> nonBindingObjects,
			object rootInstance)
		{
			foreach (var child in nonBindingObjects)
			{
				var item = LoadObject(child, rootInstance: rootInstance);

				var resourceKey = GetResourceKey(child);

				if (resourceKey != null && item != null)
				{
					dictionary[resourceKey] = item;
				}
			}
		}

		private void AddDictionaryItems(object collectionInstance, IEnumerable<XamlObjectDefinition> nonBindingObjects, object rootInstance)
		{
			MethodInfo? addMethodInfo = null;

			foreach (var child in nonBindingObjects)
			{
				var item = LoadObject(child, rootInstance: rootInstance);

				if (addMethodInfo == null)
				{
					addMethodInfo = collectionInstance
						.GetType()
						.GetMethods(BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.Public)
						.Where(m => m.Name == "set_Item")
						.FirstOrDefault(m => m.GetParameters() is { Length: 1 } p
							&& (item?.GetType() ?? typeof(object)).Is(p[0].ParameterType))
						?? throw new InvalidOperationException($"The type {collectionInstance.GetType()} does not contains an Add({item?.GetType()}) method");
				}

				addMethodInfo.Invoke(collectionInstance, new[] { item });
			}
		}

		private void AddCollectionItems(
			object collectionInstance,
			IEnumerable<XamlObjectDefinition> nonBindingObjects,
			object rootInstance,
			TemplateMaterializationSettings? settings,
			MemberInitializationContext? memberContext)
		{
			var collectionType = collectionInstance.GetType();

			foreach (var child in nonBindingObjects)
			{
				var item = LoadObject(child, rootInstance: rootInstance, settings: settings, memberContext: memberContext);
				var itemType = item?.GetType() ?? typeof(object);

				var addMethodInfo =
					TypeResolver.FindCollectionAddItemMethod(collectionType, itemType) ??
					throw new InvalidOperationException($"The type {collectionType} does not contains an Add({itemType}) method");

				addMethodInfo.Invoke(collectionInstance, new[] { item });
			}
		}

		private object? GetResourceKey(XamlObjectDefinition child) =>
			child.Members.FirstOrDefault(m =>
				string.Equals(m.Member.Name, "Name", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(m.Member.Name, "Key", StringComparison.OrdinalIgnoreCase)
			)
			?.Value
			?.ToString();

		private Type? GetResourceTargetType(XamlObjectDefinition child) => TypeResolver.FindType(
			child.Members
				.FirstOrDefault(m => string.Equals(m.Member.Name, "TargetType", StringComparison.OrdinalIgnoreCase))
				?.Value
				?.ToString() ??
			""
		);

		private PropertyInfo? GetMemberProperty(XamlObjectDefinition control, XamlMemberDefinition member)
		{
			if (member.Member.Name == "_UnknownContent")
			{
				var property = TypeResolver.FindContentProperty(TypeResolver.FindType(control.Type));
				if (property == null)
				{
					throw new InvalidOperationException($"Implicit content is not supported on {control.Type}");
				}

				return property;
			}
			else
			{
				return TypeResolver.GetPropertyByName(control.Type, member.Member.Name);
			}
		}

		private EventInfo? GetMemberEvent(XamlObjectDefinition control, XamlMemberDefinition member)
		{
			return TypeResolver.GetEventByName(control.Type, member.Member.Name);
		}

		private object? BuildLiteralValue(XamlMemberDefinition member, Type? propertyType = null)
		{
			if (member.Objects.None())
			{
				var memberValue = member.Value?.ToString();

				propertyType = propertyType ?? TypeResolver.FindPropertyType(member.Member);

				if (propertyType != null)
				{
					if (propertyType == typeof(Type))
					{
						return TypeResolver.FindType(memberValue);
					}
					else if (propertyType == typeof(DependencyProperty) && member.Owner.Type.Name == "Setter")
					{
						if (memberValue is { Length: > 0 } &&
							PropertyPathPattern().Match(memberValue) is { Success: true, Groups: var g })
						{
							Type? declaringType;
							if (g["type"].Success)
							{
								if (g["xmlns"].Success && g["xmlns"].Value is { } xmlns && xmlns.Length > 0)
								{
									declaringType = TypeResolver.FindType($"{xmlns}:{g["type"].Value}");
								}
								else
								{
									declaringType = TypeResolver.FindType(g["type"].Value);
								}
							}
							else
							{
								declaringType = _styleTargetTypeStack.Peek();
							}

							var propertyName = g["property"].Value;

							if (TypeResolver.FindDependencyProperty(declaringType, propertyName) is DependencyProperty property)
							{
								return property;
							}
							else
							{
								throw new Exception($"The property {declaringType?.ToString() ?? g["type"].Name}.{propertyName} does not exist");
							}
						}
						else
						{
							throw new Exception($"Invalid property path: {memberValue}");
						}
					}
					else
					{
						return BuildLiteralValue(propertyType, memberValue);
					}
				}
				else
				{
					throw new Exception($"The property {member.Owner?.Type?.Name}.{member.Member?.Name} is unknown");
				}
			}
			else
			{
				var expression = member.Objects.First();

				throw new NotSupportedException("MarkupExtension {0} is not supported.".InvariantCultureFormat(expression.Type.Name));
			}
		}

		private object? BuildLiteralValue(Type propertyType, string? memberValue)
		{
			if (propertyType.GetCustomAttribute<CreateFromStringAttribute>() is { } createFromString)
			{
				var sourceType = propertyType;
				var methodName = createFromString.MethodName;
				if (createFromString.MethodName.Contains('.'))
				{
					var splitIndex = createFromString.MethodName.LastIndexOf('.');
					var typeName = createFromString.MethodName.Substring(0, splitIndex);
					sourceType = TypeResolver.FindType(typeName);
					methodName = createFromString.MethodName.Substring(splitIndex + 1);
				}

				if (sourceType?.GetMethod(methodName) is { } conversionMethod && conversionMethod.IsStatic && !conversionMethod.IsPrivate)
				{
					try
					{
						return conversionMethod.Invoke(null, new object?[] { memberValue });
					}
					catch (Exception ex)
					{
						throw new XamlParseException("Executing [CreateFromString] method for type " + propertyType + " failed.", ex);
					}
				}
				else
				{
					throw new XamlParseException("Method referenced by [CreateFromString] cannot be found for " + propertyType);
				}
			}

			return Uno.UI.DataBinding.BindingPropertyHelper.Convert(propertyType, memberValue);
		}

		private void ApplyPostActions(object? instance)
		{
			while (_postActions.Count != 0)
			{
				_postActions.Dequeue()();
			}

			if (instance is FrameworkElement fe)
			{
				ResolveElementNames(fe);
			}
		}

		private void ResolveElementNames(FrameworkElement root)
		{
			foreach (var (elementName, bindingSubject) in _elementNames)
			{
				if (root.FindName(elementName) is DependencyObject element)
				{
					bindingSubject.ElementInstance = element;
				}
			}
		}

		private static bool IsBlankBaseMember(XamlMemberDefinition member) =>
			member.Member.Name == "base" &&
			member.Objects.Count == 0 &&
			member.Value as string == string.Empty;

		/// <summary>
		/// Check if the member is a nested child node (implicit [ContentProperty] value(s)).
		/// </summary>
		/// <remarks>
		/// This will return false for member child node (member property).
		/// </remarks>
		private static bool IsNestedChildNode(XamlMemberDefinition member) => member.Member.Name == XamlConstants.UnknownContent;

		private class EventHandlerWrapper
		{
			private readonly object _instance;
			private readonly MethodInfo _method;

			public EventHandlerWrapper(object instance, MethodInfo method)
			{
				_instance = instance;
				_method = method;
			}

			public void Handler2(object sender, object args)
			{
				_method.Invoke(_instance, Array.Empty<object>());
			}

			public void Handler1(object sender)
			{
				_method.Invoke(_instance, Array.Empty<object>());
			}
		}

		private record MemberInitializationContext(object Target, PropertyInfo Property);

		[GeneratedRegex(@"(\(.*?\))")]
		private static partial Regex AttachedPropertyMatching();

		/// <summary>
		/// Matches non-nested path like string: (xmlns:type.property)
		/// where 'xmlns', 'type' and the parentheses are optional.
		/// </summary>
		/// <remarks>
		/// The presence of both 'type' and 'property', doesnt automatically imply an attached property:
		/// "The XAML parser also accepts dependency property names that include a qualifying class.
		/// For example the parser interprets either "Button.Background" or "Control.Background"
		/// as being a reference to the Background property in a style for a Button."
		/// </remarks>
		[GeneratedRegex(@"^\(?((?<xmlns>\w+):)?((?<type>\w+)\.)?(?<property>\w+)\)?$", RegexOptions.ExplicitCapture)]
		private static partial Regex PropertyPathPattern();
	}
}

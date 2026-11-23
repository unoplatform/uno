// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference binaryformatobjectwriter.cpp, commit fee1e8acc

#nullable enable

using System;
using System.Collections;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.Helpers;
using Uno.UI.Helpers.Xaml;
using Windows.UI;

namespace Uno.UI.Xaml.Xbf;

// Executes the nodes of an XBF node stream to create the object graph they describe.
//
// TODO Uno: WinUI runs the nodes against its ObjectWriterStack/ObjectWriterFrame and the
// XamlQualifiedObject/XamlType/XamlProperty abstractions of the core. Uno has no such runtime
// type system, so the frames hold the instances directly and the types and properties are
// resolved by the generated XbfMetadataProvider.
internal ref struct BinaryFormatObjectWriter
{
	private readonly XbfLoadContext m_context;
	private readonly XbfMetadataProvider m_provider;
	private object?[] m_stack;
	private int m_depth;
	private object? m_qoLastInstance;

	public BinaryFormatObjectWriter(XbfLoadContext context)
	{
		m_context = context;
		m_provider = context.Provider;
		m_stack = new object?[8];
		m_depth = 0;
		m_qoLastInstance = null;
	}

	/// <summary>
	/// Runs the nodes found between <paramref name="startOffset"/> and <paramref name="endOffset"/>.
	/// </summary>
	/// <param name="activeObject">The instance the nodes apply to, if they are not creating their own.</param>
	/// <returns>The last instance the nodes created.</returns>
	public object? WriteNodes(int startOffset, int endOffset, object? activeObject = null)
	{
		var reader = new XamlBinaryFormatSubReader2(m_provider.NodeStream, startOffset);

		if (activeObject is not null)
		{
			PushScope(activeObject);
		}

		while (reader.TryReadNodeType(endOffset, out var nodeType))
		{
			switch (nodeType)
			{
				case ObjectWriterNodeType.PushScopeCreateTypeBeginInit:
				case ObjectWriterNodeType.CreateTypeBeginInit:
					{
						var instance = m_provider.Create(reader.ReadUInt());
						PushScope(instance);
						BeginInitOnCurrentInstance(instance);
					}
					break;

				case ObjectWriterNodeType.EndInitPopScope:
					{
						var instance = CurrentInstance;
						EndInitOnCurrentInstance(instance);
						PopScope();
					}
					break;

				case ObjectWriterNodeType.PushScopeGetValue:
					{
						var value = m_provider.GetProperty(reader.ReadUInt(), CurrentInstance!);
						PushScope(value);
					}
					break;

				case ObjectWriterNodeType.PushScope:
					PushScope(m_qoLastInstance);
					break;

				case ObjectWriterNodeType.PopScope:
					PopScope();
					break;

				case ObjectWriterNodeType.PushConstant:
					m_qoLastInstance = ReadConstant(ref reader);
					break;

				case ObjectWriterNodeType.SetValue:
					SetValueOnCurrentInstance(reader.ReadUInt(), m_qoLastInstance);
					break;

				case ObjectWriterNodeType.SetValueConstant:
					{
						var propertyId = reader.ReadUInt();
						SetValueOnCurrentInstance(propertyId, ReadConstant(ref reader));
					}
					break;

				case ObjectWriterNodeType.SetValueTypeConvertedConstant:
					{
						var propertyId = reader.ReadUInt();
						var value = ReadConstant(ref reader);
						SetValueOnCurrentInstance(propertyId, TypeConvertValue(propertyId, value));
					}
					break;

				case ObjectWriterNodeType.SetValueFromStaticResource:
				case ObjectWriterNodeType.SetValueFromThemeResource:
					{
						var property = m_provider.GetDependencyProperty(reader.ReadUInt() >> 1);
						var key = m_provider.GetString(reader.ReadUInt());

						ResourceResolver.ApplyResource(
							(DependencyObject)CurrentInstance!,
							property,
							key,
							isThemeResourceExtension: nodeType == ObjectWriterNodeType.SetValueFromThemeResource,
							isHotReloadSupported: false,
							fromXamlParser: true,
							context: m_context.ParseContext);
					}
					break;

				case ObjectWriterNodeType.UnoSetValueFromResourceStatic:
					{
						var propertyId = reader.ReadUInt();
						var key = m_provider.GetString(reader.ReadUInt());
						var type = m_provider.GetXamlType(reader.ReadUInt());

						SetValueOnCurrentInstance(propertyId, ResourceResolver.ResolveResourceStatic(key, type, m_context.ParseContext));
					}
					break;

				case ObjectWriterNodeType.ProvideStaticResourceValue:
				case ObjectWriterNodeType.ProvideThemeResourceValue:
					{
						var key = m_provider.GetString(reader.ReadUInt());
						var type = m_provider.GetXamlType(reader.ReadUInt());

						m_qoLastInstance = ResourceResolver.ResolveResourceStatic(key, type, m_context.ParseContext);
					}
					break;

				case ObjectWriterNodeType.UnoSetterThemeResource:
					{
						var key = m_provider.GetString(reader.ReadUInt());
						var isThemeResource = reader.ReadByte() != 0;

						((Setter)CurrentInstance!).ApplyThemeResourceUpdateValues(key, m_context.ParseContext!, isThemeResource, false);
					}
					break;

				case ObjectWriterNodeType.SetValueFromTemplateBinding:
					{
						var property = m_provider.GetDependencyProperty(reader.ReadUInt() >> 1);
						var path = m_provider.GetString(reader.ReadUInt());

						BindingOperations.SetBinding(
							(DependencyObject)CurrentInstance!,
							property,
							new Binding
							{
								Path = path,
								RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
							});
					}
					break;

				case ObjectWriterNodeType.SetValueFromMarkupExtension:
					{
						var propertyId = reader.ReadUInt();

						if ((propertyId & 1) != 0 && m_qoLastInstance is BindingBase binding)
						{
							BindingOperations.SetBinding(
								(DependencyObject)CurrentInstance!,
								m_provider.GetDependencyProperty(propertyId >> 1),
								binding);
						}
						else
						{
							SetValueOnCurrentInstance(propertyId, m_qoLastInstance);
						}
					}
					break;

				case ObjectWriterNodeType.UnoSetBindingElementName:
					{
						var name = m_provider.GetString(reader.ReadUInt());
						var isRuntimeBound = reader.ReadByte() != 0;

						((Binding)m_qoLastInstance!).ElementNameSubject = m_context.GetElementNameSubject(name, isRuntimeBound);
					}
					break;

				case ObjectWriterNodeType.UnoSetTargetPropertyPath:
					{
						var name = m_provider.GetString(reader.ReadUInt());
						var path = m_provider.GetString(reader.ReadUInt());

						((Setter)CurrentInstance!).Target = new TargetPropertyPath(m_context.GetElementNameSubject(name, isRuntimeBound: false), path);
					}
					break;

				case ObjectWriterNodeType.UnoSetStoryboardTargetName:
					{
						var targetName = m_provider.GetString(reader.ReadUInt());
						var name = m_provider.GetString(reader.ReadUInt());
						var isRuntimeBound = reader.ReadByte() != 0;
						var timeline = (Timeline)CurrentInstance!;

						Storyboard.SetTargetName(timeline, targetName);
						Storyboard.SetTarget(timeline, m_context.GetElementNameSubject(name, isRuntimeBound));
					}
					break;

				case ObjectWriterNodeType.UnoSetValueElementNameSubject:
					{
						var propertyId = reader.ReadUInt();
						var name = m_provider.GetString(reader.ReadUInt());
						var isRuntimeBound = reader.ReadByte() != 0;

						SetValueOnCurrentInstance(propertyId, m_context.GetElementNameSubject(name, isRuntimeBound));
					}
					break;

				case ObjectWriterNodeType.AddToCollection:
					AddToCollectionOnCurrentInstance();
					break;

				case ObjectWriterNodeType.UnoAddToCollectionTyped:
					m_provider.Add(reader.ReadUInt(), CurrentInstance!, m_qoLastInstance);
					break;

				case ObjectWriterNodeType.AddToDictionaryWithKey:
					{
						var key = ReadConstant(ref reader);
						((ResourceDictionary)CurrentInstance!)[key!] = m_qoLastInstance!;
					}
					break;

				case ObjectWriterNodeType.SetConnectionId:
					{
						var connectionId = reader.ReadUInt();

						// The owner is the instance of the x:Class, including for the content of its templates.
						(m_context.Owner as IXbfComponentConnector)?.Connect(connectionId, CurrentInstance!);
					}
					break;

				case ObjectWriterNodeType.SetName:
					SetNameOnCurrentInstance(m_provider.GetString(reader.ReadUInt()));
					break;

				case ObjectWriterNodeType.UnoSetUid:
					MarkupHelper.SetXUid(CurrentInstance!, m_provider.GetString(reader.ReadUInt()));
					break;

				case ObjectWriterNodeType.UnoTrackResourceBindings:
					TrackResourceBindingsOnCurrentInstance(setNameScope: reader.ReadByte() != 0);
					break;

				case ObjectWriterNodeType.UnoSetNameScope:
					NameScope.SetNameScope((DependencyObject)CurrentInstance!, m_context.NameScope);
					break;

				case ObjectWriterNodeType.UnoAddDeferredToDictionary:
					{
						var key = ReadConstant(ref reader);
						var contentLength = reader.ReadUInt();
						var contentStart = reader.NextIndex;
						reader.NextIndex = contentStart + contentLength;

						var resource = new XbfDeferredBuilder(m_context, contentStart, contentLength, instance: null);
						((ResourceDictionary)CurrentInstance!)[key!] = new ResourceDictionary.ResourceInitializer(resource.Create);
					}
					break;

				case ObjectWriterNodeType.UnoPushScopeCreateTemplateBeginInit:
					{
						var templateKind = reader.ReadByte();
						var contentLength = reader.ReadUInt();
						var contentStart = reader.NextIndex;
						reader.NextIndex = contentStart + contentLength;

						var builder = contentLength == 0
							? null
							: m_provider.GetTemplateContent(contentStart, contentLength, m_context.ParseContext, m_context.BaseUri).Builder;

						DependencyObject template = templateKind switch
						{
							XbfConstants.TemplateKindControl => new ControlTemplate(m_context.Owner, builder),
							XbfConstants.TemplateKindData => new DataTemplate(m_context.Owner, builder),
							XbfConstants.TemplateKindItemsPanel => new ItemsPanelTemplate(m_context.Owner, builder),
							_ => throw new InvalidOperationException($"The XBF template kind {templateKind} is unknown."),
						};

						PushScope(template);
					}
					break;

				case ObjectWriterNodeType.UnoPushScopeCreateElementStub:
					{
						var contentLength = reader.ReadUInt();
						var contentStart = reader.NextIndex;
						reader.NextIndex = contentStart + contentLength;

						var element = new XbfDeferredBuilder(m_context, contentStart, contentLength, instance: null);
						PushScope(new ElementStub(element.CreateElement));
					}
					break;

				case ObjectWriterNodeType.UnoSetElementNameSubjectInstance:
					m_context.GetElementNameSubject(m_provider.GetString(reader.ReadUInt()), isRuntimeBound: false).ElementInstance = CurrentInstance;
					break;

				case ObjectWriterNodeType.UnoSetDeferredBuilder:
					{
						var contentLength = reader.ReadUInt();
						var contentStart = reader.NextIndex;
						reader.NextIndex = contentStart + contentLength;

						SetDeferredBuilderOnCurrentInstance(contentStart, contentLength);
					}
					break;

				case ObjectWriterNodeType.EndOfStream:
					return m_qoLastInstance;

				default:
					throw new InvalidOperationException($"The XBF node {nodeType} is not supported.");
			}
		}

		return m_qoLastInstance;
	}

	/// <summary>
	/// Runs the nodes of the content of an x:Class against its instance.
	/// </summary>
	public void WriteComponentNodes(int startOffset, object component)
	{
		BeginInitOnCurrentInstance(component);

		WriteNodes(startOffset, int.MaxValue, component);

		EndInitOnCurrentInstance(component);
	}

	private readonly object? CurrentInstance => m_stack[m_depth - 1];

	private void PushScope(object? instance)
	{
		if (m_depth == m_stack.Length)
		{
			Array.Resize(ref m_stack, m_depth * 2);
		}

		m_stack[m_depth++] = instance;
	}

	private void PopScope()
	{
		m_qoLastInstance = m_stack[--m_depth];
		m_stack[m_depth] = null;
	}

	private readonly void BeginInitOnCurrentInstance(object instance)
	{
		if (instance is IDependencyObjectParse parse)
		{
			parse.IsParsing = true;
		}

		if (m_context.Settings is { } settings && instance is DependencyObject dependencyObject)
		{
			settings.OnMemberCreated(dependencyObject);
		}
	}

	private readonly void EndInitOnCurrentInstance(object? instance)
	{
		if (instance is FrameworkElement frameworkElement)
		{
			if (m_context.BaseUri is { } baseUri)
			{
				frameworkElement.SetBaseUri(baseUri, null!, -1, -1);
			}

			frameworkElement.CreationComplete();
		}
		else if (instance is IDependencyObjectParse parse)
		{
			parse.CreationComplete();
		}
	}

	private readonly void SetValueOnCurrentInstance(int propertyId, object? value)
	{
		var instance = CurrentInstance!;

		if ((propertyId & 1) != 0)
		{
			((DependencyObject)instance).SetValue(m_provider.GetDependencyProperty(propertyId >> 1), value);
		}
		else
		{
			m_provider.SetProperty(propertyId >> 1, instance, value);
		}
	}

	private readonly object? TypeConvertValue(int propertyId, object? value)
	{
		if ((propertyId & 1) != 0 && value is string text)
		{
			return Uno.UI.DataBinding.BindingPropertyHelper.Convert(m_provider.GetDependencyProperty(propertyId >> 1).Type, text);
		}

		return value;
	}

	private readonly void AddToCollectionOnCurrentInstance()
	{
		if (CurrentInstance is IList list)
		{
			list.Add(m_qoLastInstance);
		}
		else
		{
			throw new InvalidOperationException($"The collection {CurrentInstance?.GetType()} cannot be added to.");
		}
	}

	private readonly void SetNameOnCurrentInstance(string name)
	{
		var instance = CurrentInstance!;

		m_context.NameScope?.RegisterName(name, instance);
		m_context.GetElementNameSubject(name, isRuntimeBound: false).ElementInstance = instance;

		// WinUI sets x:Name on types like VisualState whose Name is get-only.
		MarkupHelper.SetXName(instance, name);
	}

	private readonly void TrackResourceBindingsOnCurrentInstance(bool setNameScope)
	{
		if (CurrentInstance is not DependencyObject instance)
		{
			return;
		}

		if (setNameScope && m_context.NameScope is { } nameScope)
		{
			NameScope.SetNameScope(instance, nameScope);
		}

		if (m_context.NameScope is null)
		{
			// Register directly for binding updates on the resource referencer itself, since we're inside a top-level ResourceDictionary
			if (instance is FrameworkElement frameworkElement)
			{
				frameworkElement.Loading += static (sender, _) => ((DependencyObject)sender).UpdateResourceBindings();
			}

			return;
		}

		FrameworkElement? resourceContext = null;
		if (instance is not FrameworkElement)
		{
			for (var i = m_depth - 2; i >= 0; i--)
			{
				if (m_stack[i] is FrameworkElement ancestor)
				{
					resourceContext = ancestor;
					break;
				}
			}
		}

		m_context.TrackResourceBindings(instance, resourceContext);
	}

	private readonly void SetDeferredBuilderOnCurrentInstance(int contentStart, int contentLength)
	{
		var instance = CurrentInstance!;
		var builder = new XbfDeferredBuilder(m_context, contentStart, contentLength, instance);

		switch (instance)
		{
			case VisualState visualState:
				visualState.LazyBuilder = builder.Build;
				break;

			case VisualTransition visualTransition:
				visualTransition.LazyBuilder = builder.Build;
				break;

			default:
				// Nothing to defer to: the nodes are applied right away.
				builder.Build();
				break;
		}
	}

	private readonly object? ReadConstant(ref XamlBinaryFormatSubReader2 reader)
	{
		var constantType = reader.ReadConstantNodeType();

		switch (constantType)
		{
			case PersistedConstantType.IsBoolFalse:
				return XbfBoxes.False;

			case PersistedConstantType.IsBoolTrue:
				return XbfBoxes.True;

			case PersistedConstantType.IsFloat:
				return (double)reader.ReadSingle();

			case PersistedConstantType.UnoIsDouble:
				return reader.ReadDouble();

			case PersistedConstantType.UnoIsSingle:
				return reader.ReadSingle();

			case PersistedConstantType.IsSigned:
				return reader.ReadInt32();

			case PersistedConstantType.UnoIsInt64:
				return reader.ReadInt64();

			case PersistedConstantType.IsSharedString:
				return m_provider.GetString(reader.ReadUInt());

			case PersistedConstantType.IsUniqueString:
				return Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadUInt()));

			case PersistedConstantType.IsNullString:
			case PersistedConstantType.UnoIsNull:
				return null;

			case PersistedConstantType.IsThickness:
				{
					var left = reader.ReadSingle();
					var top = reader.ReadSingle();
					var right = reader.ReadSingle();
					var bottom = reader.ReadSingle();
					return new Thickness(left, top, right, bottom);
				}

			case PersistedConstantType.UnoIsCornerRadius:
				{
					var topLeft = reader.ReadSingle();
					var topRight = reader.ReadSingle();
					var bottomRight = reader.ReadSingle();
					var bottomLeft = reader.ReadSingle();
					return new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
				}

			case PersistedConstantType.IsGridLength:
				{
					var type = (GridUnitType)reader.ReadInt32();
					var value = reader.ReadSingle();
					return new GridLength(value, type);
				}

			case PersistedConstantType.IsColor:
				{
					var argb = (uint)reader.ReadInt32();
					return Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
				}

			case PersistedConstantType.IsEnum:
				{
					var type = m_provider.GetXamlType(reader.ReadUInt());
					return Enum.ToObject(type, reader.ReadInt32());
				}

			case PersistedConstantType.UnoIsType:
				return m_provider.GetXamlType(reader.ReadUInt());

			case PersistedConstantType.UnoIsDependencyProperty:
				return m_provider.GetDependencyProperty(reader.ReadUInt());

			case PersistedConstantType.UnoIsGenerated:
				return m_provider.Constant(reader.ReadUInt());

			default:
				throw new InvalidOperationException($"The XBF constant {constantType} is not supported.");
		}
	}
}

internal static class XbfBoxes
{
	internal static readonly object True = true;
	internal static readonly object False = false;
}

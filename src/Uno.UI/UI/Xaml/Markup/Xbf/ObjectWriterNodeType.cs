// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ObjectWriterNodeType.h, commit fee1e8acc

#nullable enable

namespace Uno.UI.Xaml.Xbf;

// Node type descriptions for the
// the operations of the object writer.
// These values are written into the XBF so
// MUST REMAIN STATIC.
internal enum ObjectWriterNodeType : byte
{
	None = 0,

	// stack manipulation
	PushScope = 1,
	PopScope = 2,

	// tree creation
	AddNamespace = 3,
	PushConstant = 4,
	PushResolvedType = 5,
	PushResolvedProperty = 6,
	SetValue = 7,
	AddToCollection = 8,
	AddToDictionary = 9,
	AddToDictionaryWithKey = 10,

	// type specific operations
	CheckPeerType = 11,
	SetConnectionId = 12,
	SetName = 13,
	GetResourcePropertyBag = 14,

	// custom data blob
	SetCustomRuntimeData = 15,
	SetResourceDictionaryItems = 16,
	SetDeferredProperty = 17,

	// aggregate operations
	PushScopeAddNamespace = 18,
	PushScopeGetValue = 19,
	PushScopeCreateTypeBeginInit = 20,
	PushScopeCreateTypeWithConstantBeginInit = 21,
	PushScopeCreateTypeWithTypeConvertedConstantBeginInit = 22,
	CreateTypeBeginInit = 23,
	CreateTypeWithConstantBeginInit = 24,
	CreateTypeWithTypeConvertedConstantBeginInit = 25,
	SetValueConstant = 26,
	SetValueTypeConvertedConstant = 27,
	SetValueTypeConvertedResolvedProperty = 28,
	SetValueTypeConvertedResolvedType = 29,
	SetValueFromStaticResource = 30,
	SetValueFromTemplateBinding = 31,
	SetValueFromMarkupExtension = 32,
	EndInitPopScope = 33,
	ProvideStaticResourceValue = 34,
	ProvideThemeResourceValue = 35,
	SetValueFromThemeResource = 36,

	// sentinel
	EndOfStream = 37,

	// Conditional XAML nodes
	// TODO Uno: conditional namespaces are resolved by the XAML generator. The node stands for a linker hint instead:
	// [ConstantId][ContentLength][Content nodes...], the content being skipped when the constant is false.
	BeginConditionalScope = 38,
	EndConditionalScope = 39,

	// more aggregate operations
	EndInitProvideValuePopScope = 40,

	// Uno specific: these nodes carry what WinUI keeps in CustomWriterRuntimeData
	// sub-streams (template content, deferred visual states) or resolves through
	// the core name scope tables. They are inlined in the node stream, prefixed by
	// their byte length so that the reader can skip them.

	// [TemplateKind][ContentLength][Content nodes...]
	UnoPushScopeCreateTemplateBeginInit = 64,
	// [ContentLength][nodes applied to the current instance when it materializes]
	UnoSetDeferredBuilder = 65,
	// [NameStringId][IsRuntimeBound] : the last instance is a Binding
	UnoSetBindingElementName = 66,
	// [NameStringId][PathStringId] : Setter.Target in a VisualState
	UnoSetTargetPropertyPath = 67,
	// [TargetNameStringId][NameStringId][IsRuntimeBound] : Storyboard.TargetName on a Timeline
	UnoSetStoryboardTargetName = 68,
	// [AdderId] : typed counterpart of AddToCollection
	UnoAddToCollectionTyped = 69,
	// [PropertyId][NameStringId][IsRuntimeBound] : sets an ElementNameSubject on an attached property (RelativePanel)
	UnoSetValueElementNameSubject = 70,
	// [StringId] : x:Uid
	UnoSetUid = 71,
	// [PropertyId][StringId][TypeId] : sets the property from the resource key, without a resource binding
	UnoSetValueFromResourceStatic = 72,
	// [PropertyId] : PushScopeGetValue for an attached property
	UnoPushScopeGetAttachedValue = 73,
	// [SetNameScope] : registers the current instance for resource binding updates
	UnoTrackResourceBindings = 74,
	// [StringId][IsThemeResource] : Setter.ApplyThemeResourceUpdateValues
	UnoSetterThemeResource = 75,
	// Gives the name scope of the template to the current instance
	UnoSetNameScope = 76,
	// [Key constant][ContentLength][Content nodes...] : a resource created when it is first requested
	UnoAddDeferredToDictionary = 77,
	// [ContentLength][Content nodes...] : x:Load, the content being the element the stub stands for
	UnoPushScopeCreateElementStub = 78,
	// [NameStringId] : the current instance stands for the named element until it gets created
	UnoSetElementNameSubjectInstance = 79,
	// [Key constant][ResourceKeyStringId] : <StaticResource x:Key ResourceKey /> in a dictionary
	UnoAddStaticResourceAliasToDictionary = 80,
	// [PropertyNameStringId][IsOwnerDependencyObject] : the last instance is a Binding, set on a property that is not a DependencyProperty
	UnoSetBindingByName = 81,
	// [ObjectId] : an object the class of the file creates in C#, a template for instance
	UnoPushConnectedObject = 82,

	///////////////////////// temporary states during optimization
	CreateType = 128,
	CreateTypeWithInitialValue = 129,
	BeginInit = 130,
	EndInit = 131,
	GetValue = 132,
	TypeConvertValue = 133,
	PushScopeCreateType = 134,
	PushScopeCreateTypeWithConstant = 135,
	PushScopeCreateTypeWithTypeConvertedConstant = 136,
	CreateTypeWithConstant = 137,
	CreateTypeWithTypeConvertedConstant = 138,
	ProvideValue = 139,
	ProvideTemplateBindingValue = 140,
	SetDirectiveProperty = 141,
	StreamOffsetMarker = 142,
}

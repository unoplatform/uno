// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference AutomationPeerAnnotation_Partial.cpp, tag winui3/release/1.4.2

namespace Windows.UI.Xaml.Automation.Peers;

/// <summary>
/// Represents a single UI automation annotation.
/// </summary>
public partial class AutomationPeerAnnotation : DependencyObject
{
	/// <summary>
	/// Gets or sets the type of the annotation.
	/// </summary>
	public AnnotationType Type
	{
		get => (AnnotationType)GetValue(TypeProperty);
		set => SetValue(TypeProperty, value);
	}

	/// <summary>
	/// Gets or sets the automation peer of the element that implements the annotation.
	/// </summary>
	public AutomationPeer Peer
	{
		get => (AutomationPeer)GetValue(PeerProperty);
		set => SetValue(PeerProperty, value);
	}

	/// <summary>
	/// Identifies the AutomationPeerAnnotation.Peer property.
	/// </summary>
	public static DependencyProperty PeerProperty { get; } =
	DependencyProperty.Register(
		nameof(Peer), typeof(AutomationPeer),
		typeof(AutomationPeerAnnotation),
		new FrameworkPropertyMetadata(default(AutomationPeer)));

	/// <summary>
	/// Identifies the AutomationPeerAnnotation.Type property.
	/// </summary>
	public static DependencyProperty TypeProperty { get; } =
	DependencyProperty.Register(
		nameof(Type), typeof(AnnotationType),
		typeof(AutomationPeerAnnotation),
		new FrameworkPropertyMetadata(default(AnnotationType)));

	public AutomationPeerAnnotation(AnnotationType type) : base()
	{
		Type = type;
	}

	public AutomationPeerAnnotation(AnnotationType type, AutomationPeer peer) : base()
	{
		Type = type;
		Peer = peer;
	}

	public AutomationPeerAnnotation() : base() { }
}

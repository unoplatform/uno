// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AccessKeyStringBuilder_Partial.cpp, tag winui3/release/1.5-stable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Documents;

namespace Uno.UI.DirectUI;

internal class AccessKeyStringBuilder
{
	static void InterleaveStringWithDelimiters(string toInterleave, int length, out string interleavedString)
	{
		if (length > 3)
		{
			throw new ArgumentOutOfRangeException(nameof(length), "Length must be at most 3");
		}

		// We take advantage of the fact that access keys can be at most 3 characters long.
		// So, after interleaving ',' and ' ', the string can be at most 9 characters long.
		var accessKeyInterleaved = new char[9];

		for (var i = 0; i < length; i++)
		{
			accessKeyInterleaved[3 * i] = ',';
			accessKeyInterleaved[(3 * i) + 1] = ' ';
			accessKeyInterleaved[(3 * i) + 2] = toInterleave[i];
		}

		interleavedString = new string(accessKeyInterleaved, 0, length * 3);
	}

	static void GetAccessKeyFromOwner(DependencyObject spOwner, out string accessKey, out DependencyObject scopeOwner)
	{
		if (spOwner is FrameworkElement ownerAsFrameworkElement)
		{
			accessKey = (string)ownerAsFrameworkElement.GetValue(UIElement.AccessKeyProperty);
			ownerAsFrameworkElement.GetAccessKeyScopeOwner(out scopeOwner);
		}
		else if (spOwner is TextElement ownerAsTextElement)
		{
			accessKey = (string)ownerAsTextElement.GetValue(TextElement.AccessKeyProperty);
			ownerAsTextElement.GetAccessKeyScopeOwner(out scopeOwner);
		}
		else
		{
			throw new ArgumentException("Invalid argument", nameof(spOwner));
		}
	}

	public static string GetAccessKeyMessageFromElement(DependencyObject spOwner)
	{
		string prefix;
		string accessKey;
		string messageWithoutPrefix;

		DependencyObject akScopeOwner;

		GetAccessKeyFromOwner(spOwner, out accessKey, out akScopeOwner);

		// If no AccessKey was set, return an empty String
		if (string.IsNullOrEmpty(accessKey))
		{
			return "";
		}

		// If the scope owner exists, grab its sequence and add access keys.
		AutomationPeer automationPeer = null;
		if (akScopeOwner != null)
		{
			if (akScopeOwner is FrameworkElement akScopeOwnerAsFrameworkElement)
			{
				automationPeer = akScopeOwnerAsFrameworkElement.GetOrCreateAutomationPeer();
			}
			else if (akScopeOwner is TextElement akScopeOwnerAsTextElement)
			{
				automationPeer = akScopeOwnerAsTextElement.GetOrCreateAutomationPeer();
			}
			else
			{
				throw new ArgumentException("Invalid argument", nameof(akScopeOwner));
			}
		}

		prefix = automationPeer?.GetAccessKey() ?? "Alt";

		InterleaveStringWithDelimiters(accessKey, accessKey.Length, out messageWithoutPrefix);

		return prefix + messageWithoutPrefix;
	}
}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;


namespace Uno.UI.Tasks.BatchMerge
{
	public class MergedDictionary
	{
		public static MergedDictionary CreateMergedDicionary()
		{
			var document = new XmlDocument();
			return new MergedDictionary(document);
		}

		public override string ToString()
		{
			var xmlDoc = (XmlDocument)GetXaml();

			StringWriter sw = new StringWriter();
			XmlWriterSettings settings = new XmlWriterSettings { Indent = true, OmitXmlDeclaration = true, Encoding = Encoding.UTF8 };
			XmlWriter writer = XmlWriter.Create(sw, settings);
			xmlDoc.WriteTo(writer);
			writer.Flush();
			return Utils.UnEscapeAmpersand(sw.ToString());
		}

		public void MergeContent(String content)
		{
			content = Utils.EscapeAmpersand(content);

			var document = new XmlDocument();
			document.LoadXml(content);

			Dictionary<string, string> standardNamespaceDictionary = new Dictionary<string, string>();
			Dictionary<string, string> xmlnsReplacementDictionary = new Dictionary<string, string>();

			GenerateStandardNameSpaces(document, standardNamespaceDictionary, xmlnsReplacementDictionary);
			foreach (KeyValuePair<string, string> entry in standardNamespaceDictionary)
			{
				AddNamespace(entry.Key, entry.Value);
			}
			foreach (XmlNode node in document.ChildNodes)
			{
				if (node.Name == "ResourceDictionary")
				{
					foreach (XmlNode xmlNode in node.ChildNodes)
					{
						AddNode(xmlNode, xmlnsReplacementDictionary);
					}
				}
			}
		}

		public void FinalizeXaml()
		{
			if (mergedThemeDictionaryByKeyDictionary.Keys.Count > 0)
			{
				XmlElement themeDictionariesElement = owningDocument.CreateElement("ResourceDictionary.ThemeDictionaries", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
				xmlElement.AppendChild(themeDictionariesElement);

				foreach (string themeDictionaryKey in mergedThemeDictionaryByKeyDictionary.Keys)
				{
					mergedThemeDictionaryByKeyDictionary[themeDictionaryKey].FinalizeXaml();
					XmlElement themeResourceDictionaryElement = mergedThemeDictionaryByKeyDictionary[themeDictionaryKey].GetXaml() as XmlElement;
					themeDictionariesElement.AppendChild(themeResourceDictionaryElement);
					themeResourceDictionaryElement.SetAttribute("Key", "http://schemas.microsoft.com/winfx/2006/xaml", themeDictionaryKey);
				}
			}

			for (int i = 0; i < nodeList.Count; i++)
			{
				if (!nodeListNodesToIgnore.Contains(i))
				{
					// We may have null placeholders left over that were never found.  If there are,
					// that's not necessarily an indication that something's wrong - for example,
					// we might have nodes that are part of the built-in list of theme resources.
					// We'll just ignore null nodes.
					XmlNode node = nodeList[i];

					if (node != null)
					{
						xmlElement.AppendChild(node);
					}
				}
			}
		}

		private MergedDictionary(XmlDocument document) : this(document, null) { }

		private MergedDictionary(XmlDocument document, MergedDictionary parentDictionary)
		{
			owningDocument = document;
			xmlElement = owningDocument.CreateElement("ResourceDictionary", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");

			if (parentDictionary == null)
			{
				xmlElement = owningDocument.AppendChild(xmlElement) as XmlElement;
				xmlElement.SetAttribute("xmlns:x", "http://schemas.microsoft.com/winfx/2006/xaml");
			}

			nodeList = new List<XmlNode>();
			nodeListNodesToIgnore = new List<int>();
			nodeKeyToNodeListIndexDictionary = new Dictionary<string, int>();
			mergedThemeDictionaryByKeyDictionary = new Dictionary<string, MergedDictionary>();
			namespaceList = new List<string>();
			this.parentDictionary = parentDictionary;

			AddNamespace("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006");
			xmlElement.SetAttribute("Ignorable", "http://schemas.openxmlformats.org/markup-compatibility/2006", "ios android wasm skia");
		}

		private void AddNamespace(string xmlnsString, string namespaceString)
		{
			if (!namespaceList.Contains(xmlnsString))
			{
				xmlElement.SetAttribute("xmlns:" + xmlnsString, namespaceString);
				namespaceList.Add(xmlnsString);
			}
		}

		private void AddNode(XmlNode node, Dictionary<string, string> xmlnsReplacementDictionary)
		{
			// Remove Comment
			if (node is XmlComment)
			{
				return;
			}

			node = owningDocument.ImportNode(node, true);
			ReplaceNamespacePrefix(node, xmlnsReplacementDictionary);

			if (node.Name == "ResourceDictionary.ThemeDictionaries")
			{
				// This will be a list of either ResourceDictionaries or comments.
				// We'll figure out what the ResourceDictionaries' keys are,
				// then either add their contents to the existing theme dictionaries we have,
				// or create new ones if we've found new keys.
				foreach (XmlNode childNode in node.ChildNodes)
				{
					string nodeKey = GetKey(childNode);

					if (nodeKey == null || nodeKey.Length == 0)
					{
						continue;
					}
					else if (nodeKey == "Dark")
					{
						// If we don't specify the dictionary "Dark", then "Default" will be used instead.
						// Having both of those, however, will result in "Default" never being used, even
						// if it contains something that "Dark" does not.
						// Since we're merging everything into a single dictionary, we need to standardize
						// to only one of the two. Since most dictionaries use "Default", we'll go with that.
						nodeKey = "Default";
					}

					MergedDictionary mergedThemeDictionary = null;

					if (mergedThemeDictionaryByKeyDictionary.TryGetValue(nodeKey, out mergedThemeDictionary) == false)
					{
						mergedThemeDictionary = new MergedDictionary(owningDocument, this);
						mergedThemeDictionaryByKeyDictionary.Add(nodeKey, mergedThemeDictionary);
					}

					foreach (XmlNode resourceDictionaryChild in childNode.ChildNodes)
					{
						mergedThemeDictionary.AddNode(resourceDictionaryChild, xmlnsReplacementDictionary);
					}
				}
			}
			else
			{
				// First, we need to check if this is a node with a key.  If it is, then we'll replace
				// the previous node we saw with this key, in order to have only one entry per key.
				// if it's not, or if we haven't seen a previous node with this key, then we'll just
				// add it to our list.
				string nodeKey = GetKey(node);

				if (nodeKey.Length == 0 || !nodeKeyToNodeListIndexDictionary.ContainsKey(nodeKey))
				{
					if (nodeKey.Length != 0)
					{
						nodeKeyToNodeListIndexDictionary.Add(nodeKey, nodeList.Count);
					}

					nodeList.Add(node);
				}
				else
				{
					int previousNodeIndex = nodeKeyToNodeListIndexDictionary[nodeKey];
					nodeList[previousNodeIndex] = node;
				}

				if (nodeKey.Length > 0 && parentDictionary != null)
				{
					parentDictionary.RemoveAncestorNodesWithKey(nodeKey);
				}

				if (nodeKey.Length > 0 && parentDictionary != null)
				{
					parentDictionary.RemoveAncestorNodesWithKey(nodeKey);
				}
			}
		}

		private XmlNode GetXaml()
		{
			if (parentDictionary == null)
			{
				return owningDocument;
			}
			else
			{
				return xmlElement;
			}
		}

		private static void ReplaceNamespacePrefix(XmlNode currentNode, Dictionary<string, string> xmlnsReplacementDictionary)
		{
			if (currentNode.Prefix != null && currentNode.Prefix.Length > 0)
			{
				if (xmlnsReplacementDictionary.ContainsKey(currentNode.Prefix))
				{
					currentNode.Prefix = xmlnsReplacementDictionary[currentNode.Prefix];
				}
			}

			// We also want to check the node's attributes for any of the prefixes -
			// e.g. Style.TargetType may contain an instance of "local:..."
			if (currentNode.Attributes != null)
			{
				foreach (XmlAttribute attribute in currentNode.Attributes)
				{
					if (attribute.Value != null)
					{
						foreach (string xmlnsToReplace in xmlnsReplacementDictionary.Keys)
						{
							attribute.Value = attribute.Value.Replace(xmlnsToReplace + ":", xmlnsReplacementDictionary[xmlnsToReplace] + ":");
						}
					}
				}
			}

			foreach (XmlNode node in currentNode.ChildNodes)
			{
				ReplaceNamespacePrefix(node, xmlnsReplacementDictionary);
			}
		}

		private static string GetKey(XmlNode node)
		{
			if (node.Attributes == null)
			{
				return string.Empty;
			}

			string key = string.Empty;

			foreach (XmlAttribute attribute in node.Attributes)
			{
				if (attribute.Name == "x:Key" || attribute.Name == "x:Name")
				{
					key = attribute.Value;
					break;
				}
			}

			// If we didn't find an "x:Key" or "x:Name" attribute, then try looking for a "TargetType" attribute
			// if this is a "Style" tag - that functions in the same way.
			if (key.Length == 0 && node.Name == "Style" && node.Attributes != null)
			{
				foreach (XmlAttribute attribute in node.Attributes)
				{
					if (attribute.Name == "TargetType")
					{
						key = attribute.Value;
						break;
					}
				}
			}

			if (key.Length > 0)
			{
				// If this node has a key and a conditional-inclusion namespace, we'll attach a prefix
				// to the key corresponding to the condition we checked in order to allow multiple such nodes
				// with the same key to exist.
				int indexOfContractPresent = node.NamespaceURI.IndexOf("IsApiContract", StringComparison.Ordinal);

				if (indexOfContractPresent >= 0)
				{
					key = node.NamespaceURI.Substring(indexOfContractPresent) + ":" + key;
				}
			}

			return key;
		}

		private static Dictionary<string, string> standardNameDictionary = new Dictionary<string, string> {
			{ "using:Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls", "controls" },
			{ "using:Microsoft" + /* UWP don't rename */ ".UI.Xaml.Controls.Primitives", "primitives" },
			{ "using:Microsoft" + /* UWP don't rename */ ".UI.Xaml.Media", "media" },
			{ "http://schemas.microsoft.com/winfx/2006/xaml", "x"},
		};

		private static string _conditionalXamlPattern = @"http://schemas.microsoft.com/winfx/2006/xaml/presentation?IsApiContract(Not|)Present\(Windows.Foundation.UniversalApiContract,([0-9]+)\)";
		private static string GetStandardNamespace(string name, string value)
		{
			var match = Regex.Match(value, _conditionalXamlPattern);
			if (match.Success)
			{
				return "contract" + match.Groups[2].Captures[0].Value + match.Groups[1].Captures[0].Value + "Present";
			}
			if (standardNameDictionary.ContainsKey(value))
			{
				return standardNameDictionary[value];
			}
			return name;
		}

		private static void GenerateStandardNameSpaces(XmlDocument document, Dictionary<string, string> standardNamespaceDictionary, Dictionary<string, string> toBeReplacedDictionary)
		{
			foreach (XmlNode node in document.ChildNodes)
			{
				if (node.Name == "ResourceDictionary")
				{
					foreach (XmlAttribute att in node.Attributes)
					{
						if (att.Name.StartsWith("xmlns:", StringComparison.Ordinal))
						{
							string name = att.Name.Substring(6); // exclude "xmlns:"
							string stardardName = GetStandardNamespace(name, att.Value);
							standardNamespaceDictionary.Add(stardardName, att.Value);

							if (!name.Equals(stardardName))
							{
								toBeReplacedDictionary.Add(name, stardardName);
							}
						}
					}
				}
			}
		}

		private void RemoveAncestorNodesWithKey(string key)
		{
			if (nodeKeyToNodeListIndexDictionary.ContainsKey(key))
			{
				nodeListNodesToIgnore.Add(nodeKeyToNodeListIndexDictionary[key]);
			}

			if (parentDictionary != null)
			{
				parentDictionary.RemoveAncestorNodesWithKey(key);
			}
		}

		private XmlElement xmlElement;
		private XmlDocument owningDocument;
		private List<XmlNode> nodeList;

		// We'll want to remove nodes from the node list if a child dictionary has the same node,
		// but if we just removed the nodes then the values in nodeKeyToNodeListIndexDictionary would be wrong.
		// To make things easier, we'll instead just mark those nodes as ones to ignore rather than actually
		// removing them from the list.
		private List<int> nodeListNodesToIgnore;

		private Dictionary<string, int> nodeKeyToNodeListIndexDictionary;
		private Dictionary<string, MergedDictionary> mergedThemeDictionaryByKeyDictionary;
		private List<string> namespaceList;
		private MergedDictionary parentDictionary;
	}
}

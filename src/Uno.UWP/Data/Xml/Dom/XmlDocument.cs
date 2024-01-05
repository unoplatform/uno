using System.Collections.Generic;
using Windows.Foundation;
using Windows.Storage;
using System.Threading.Tasks;
using System;
using SystemXmlEntity = System.Xml.XmlEntity;
using SystemXmlNotation = System.Xml.XmlNotation;
using SystemXmlNode = System.Xml.XmlNode;
using SystemXmlAttribute = System.Xml.XmlAttribute;
using SystemXmlCDataSection = System.Xml.XmlCDataSection;
using SystemXmlComment = System.Xml.XmlComment;
using SystemXmlDocument = System.Xml.XmlDocument;
using SystemXmlDocumentFragment = System.Xml.XmlDocumentFragment;
using SystemXmlDocumentType = System.Xml.XmlDocumentType;
using SystemXmlDomImplementation = System.Xml.XmlImplementation;
using SystemXmlElement = System.Xml.XmlElement;
using SystemXmlEntityReference = System.Xml.XmlEntityReference;
using SystemXmlNamedNodeMap = System.Xml.XmlNamedNodeMap;
using SystemXmlNodeList = System.Xml.XmlNodeList;
using SystemXmlProcessingInstruction = System.Xml.XmlProcessingInstruction;
using SystemXmlText = System.Xml.XmlText;
using System.Diagnostics.CodeAnalysis;

namespace Windows.Data.Xml.Dom
{
	public partial class XmlDocument : IXmlNode, IXmlNodeSerializer, IXmlNodeSelector
	{
		private readonly Dictionary<object, object> _systemXmlToUwp = new Dictionary<object, object>();
		internal readonly SystemXmlDocument _backingDocument = new SystemXmlDocument();

		public XmlDocumentType? Doctype => (XmlDocumentType?)Wrap(_backingDocument.DocumentType);

		public XmlElement? DocumentElement => (XmlElement?)Wrap(_backingDocument.DocumentElement);

		public XmlDomImplementation Implementation => (XmlDomImplementation)Wrap(_backingDocument.Implementation);

		public object Prefix
		{
			get => _backingDocument.Prefix;
			set => _backingDocument.Prefix = value.ToString()!;
		}

		public object? NodeValue
		{
			get => _backingDocument.Value;
			set => _backingDocument.Value = value?.ToString();
		}

		public IXmlNode? FirstChild => (IXmlNode?)Wrap(_backingDocument.FirstChild);

		public IXmlNode? LastChild => (IXmlNode?)Wrap(_backingDocument.LastChild);

		public object LocalName => _backingDocument.LocalName;

		public IXmlNode? NextSibling => (IXmlNode?)Wrap(_backingDocument.NextSibling);

		public object NamespaceUri => _backingDocument.NamespaceURI;

		public Dom.NodeType NodeType => Dom.NodeType.ElementNode;

		public string NodeName => _backingDocument.Name;

		public XmlNamedNodeMap? Attributes => (XmlNamedNodeMap?)Wrap(_backingDocument.Attributes);

		public XmlDocument OwnerDocument => (XmlDocument)Wrap(_backingDocument.OwnerDocument!);

		public IXmlNode? ParentNode => (IXmlNode?)Wrap(_backingDocument.ParentNode);

		public XmlNodeList ChildNodes => (XmlNodeList)Wrap(_backingDocument.ChildNodes);

		public IXmlNode? PreviousSibling => (IXmlNode?)Wrap(_backingDocument.PreviousSibling);

		public string InnerText
		{
			get => _backingDocument.InnerText;
			set => _backingDocument.InnerText = value;
		}

		public XmlDocument()
		{
			_backingDocument = new SystemXmlDocument();
		}

		public XmlElement CreateElement(string tagName) =>
			(XmlElement)Wrap(_backingDocument.CreateElement(tagName));

		public XmlDocumentFragment CreateDocumentFragment() =>
			(XmlDocumentFragment)Wrap(_backingDocument.CreateDocumentFragment());

		public XmlText CreateTextNode(string data) =>
			(XmlText)Wrap(_backingDocument.CreateTextNode(data));

		public XmlComment CreateComment(string data) =>
			(XmlComment)Wrap(_backingDocument.CreateComment(data));

		public XmlProcessingInstruction CreateProcessingInstruction(string target, string data) =>
			(XmlProcessingInstruction)Wrap(_backingDocument.CreateProcessingInstruction(target, data));

		public XmlAttribute CreateAttribute(string name) =>
			(XmlAttribute)Wrap(_backingDocument.CreateAttribute(name));

		public XmlEntityReference CreateEntityReference(string name) =>
			(XmlEntityReference)Wrap(_backingDocument.CreateEntityReference(name));

		public XmlNodeList GetElementsByTagName(string tagName) =>
			(XmlNodeList)Wrap(_backingDocument.GetElementsByTagName(tagName));

		public XmlCDataSection CreateCDataSection(string data) =>
			(XmlCDataSection)Wrap(_backingDocument.CreateCDataSection(data));

		public XmlAttribute CreateAttributeNS(object namespaceUri, string qualifiedName) =>
			(XmlAttribute)Wrap(_backingDocument.CreateAttribute(qualifiedName, namespaceUri?.ToString()));

		public XmlElement CreateElementNS(object namespaceUri, string qualifiedName) =>
			(XmlElement)Wrap(_backingDocument.CreateElement(qualifiedName, namespaceUri?.ToString()));

		public XmlElement? GetElementById(string elementId) =>
			(XmlElement?)Wrap(_backingDocument.GetElementById(elementId));

		public IXmlNode ImportNode(IXmlNode node, bool deep) =>
			(IXmlNode)Wrap(_backingDocument.ImportNode((SystemXmlNode)Unwrap(node), deep));

		public bool HasChildNodes() => _backingDocument.HasChildNodes;

		public IXmlNode? InsertBefore(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode?)Wrap(
				_backingDocument.InsertBefore(
					(SystemXmlNode)Unwrap(newChild),
					(SystemXmlNode)Unwrap(referenceChild)));

		public IXmlNode ReplaceChild(IXmlNode newChild, IXmlNode referenceChild) =>
			(IXmlNode)Wrap(
				_backingDocument.ReplaceChild(
					(SystemXmlNode)Unwrap(newChild),
					(SystemXmlNode)Unwrap(referenceChild)));

		public IXmlNode RemoveChild(IXmlNode childNode) =>
			(IXmlNode)Wrap(
				_backingDocument.RemoveChild(
					(SystemXmlNode)Unwrap(childNode)));

		public IXmlNode? AppendChild(IXmlNode newChild) =>
			(IXmlNode?)Wrap(
				_backingDocument.AppendChild(
					(SystemXmlNode)Unwrap(newChild)));

		public IXmlNode CloneNode(bool deep) => (IXmlNode)Wrap(_backingDocument.CloneNode(deep));

		public void Normalize() => _backingDocument.Normalize();

		public IXmlNode? SelectSingleNode(string xpath) => (IXmlNode?)Wrap(_backingDocument.SelectSingleNode(xpath));

		public XmlNodeList? SelectNodes(string xpath) => (XmlNodeList?)Wrap(_backingDocument.SelectNodes(xpath));

		public string GetXml() => _backingDocument.OuterXml;

		public void LoadXml(string xml) => _backingDocument.LoadXml(xml);

		public IAsyncAction SaveToFileAsync(IStorageFile file) =>
			SaveToFileInternalAsync(file).AsAsyncAction();

		private async Task SaveToFileInternalAsync(IStorageFile file)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			await Task.Run(() => _backingDocument.Save(file.Path));
		}

		public static IAsyncOperation<XmlDocument> LoadFromFileAsync(IStorageFile file) =>
			LoadFromFileInternalAsync(file).AsAsyncOperation();

		private static async Task<XmlDocument> LoadFromFileInternalAsync(IStorageFile file)
		{
			if (file is null)
			{
				throw new ArgumentNullException(nameof(file));
			}

			var document = new XmlDocument();
			await Task.Run(() => document._backingDocument.Load(file.Path));
			return document;
		}

		/// <summary>
		/// Wraps System.Xml node to UWP XML node and caches the
		/// instance for repeated retrieval.
		/// </summary>
		/// <param name="node">System.Xml node.</param>
		/// <returns>UWP XML node.</returns>
		[return: NotNullIfNotNull(nameof(node))]
		internal object? Wrap(object? node)
		{
			if (node == null)
			{
				return null;
			}

			if (!_systemXmlToUwp.TryGetValue(node, out var wrapped))
			{
				wrapped = CreateWrapper(node);
				_systemXmlToUwp.Add(node, wrapped);
			}
			return wrapped;
		}

		/// <summary>
		/// Wraps UWP XML node to System.Xml node.
		/// </summary>
		/// <param name="node">UWP XML node.</param>
		/// <returns>System.Xml node.</returns>
		[return: NotNullIfNotNull(nameof(node))]
		internal object? Unwrap(object? node) =>
			node switch
			{
				DtdEntity dtdEntity => dtdEntity._backingEntity,
				DtdNotation dtdNotation => dtdNotation._backingNotation,
				XmlAttribute attribute => attribute._backingAttribute,
				XmlCDataSection dataSection => dataSection._backingDataSection,
				XmlDocument document => document._backingDocument,
				XmlDocumentFragment documentFragment => documentFragment._backingDocumentFragment,
				XmlDocumentType documentType => documentType._backingDocumentType,
				XmlDomImplementation domImplementation => domImplementation._backingImplementation,
				XmlElement element => element._backingElement,
				XmlEntityReference entityReference => entityReference._backingEntityReference,
				XmlNamedNodeMap namedNodeMap => namedNodeMap._backingNamedNodeMap,
				XmlNodeList nodeList => nodeList._backingList,
				XmlProcessingInstruction processingInstruction => processingInstruction._backingProcessingInstruction,
				XmlComment comment => comment._backingComment,
				null => null,
				_ => throw new InvalidOperationException("Trying to unwrap an unknown XML type."),
			};

		private object CreateWrapper(object node) =>
			node switch
			{
				SystemXmlEntity entity => new DtdEntity(this, entity),
				SystemXmlNotation notation => new DtdNotation(this, notation),
				SystemXmlAttribute attribute => new XmlAttribute(this, attribute),
				SystemXmlCDataSection dataSection => new XmlCDataSection(this, dataSection),
				SystemXmlComment comment => new XmlComment(this, comment),
				SystemXmlDocumentFragment documentFragment => new XmlDocumentFragment(this, documentFragment),
				SystemXmlDocumentType documentType => new XmlDocumentType(this, documentType),
				SystemXmlDomImplementation implementation => new XmlDomImplementation(implementation),
				SystemXmlElement element => new XmlElement(this, element),
				SystemXmlEntityReference entityReference => new XmlEntityReference(this, entityReference),
				SystemXmlNamedNodeMap namedNodeMap => new XmlNamedNodeMap(this, namedNodeMap),
				SystemXmlNodeList nodeList => new XmlNodeList(this, nodeList),
				SystemXmlProcessingInstruction processingInstruction => new XmlProcessingInstruction(this, processingInstruction),
				SystemXmlText text => new XmlText(this, text),
				_ => throw new InvalidOperationException("Trying to wrap an unknown XML type."),
			};
	}
}

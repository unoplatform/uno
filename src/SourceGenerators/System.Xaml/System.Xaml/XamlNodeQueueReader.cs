//
// Copyright (C) 2011 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Uno.Xaml
{
	internal class XamlNodeQueueReader : XamlReader, IXamlLineInfo
	{
		private readonly XamlNodeQueue _source;
		private XamlNodeLineInfo _node;

		public XamlNodeQueueReader (XamlNodeQueue source)
		{
			_source = source;
			_node = default (XamlNodeLineInfo);
		}

		public override bool IsEof {
			get { return _node.Node.NodeType == XamlNodeType.None; }
		}
		
		public override XamlMember Member {
			get { return NodeType != XamlNodeType.StartMember ? null : _node.Node.Member.Member; }
		}

		public override NamespaceDeclaration Namespace {
			get { return NodeType != XamlNodeType.NamespaceDeclaration ? null : (NamespaceDeclaration) _node.Node.Value; }
		}

		public override XamlNodeType NodeType {
			get { return _node.Node.NodeType; }
		}

		public override XamlSchemaContext SchemaContext {
			get { return _source.SchemaContext; }
		}

		public override XamlType Type {
			get { return NodeType != XamlNodeType.StartObject ? null : _node.Node.Object.Type; }
		}

		public override object Value {
			get { return NodeType != XamlNodeType.Value ? null : _node.Node.Value; }
		}

		public override bool Read ()
		{
			if (_source.IsEmpty) {
				_node = default (XamlNodeLineInfo);
				return false;
			}
			_node = _source.Dequeue ();
			return true;
		}

		public bool HasLineInfo {
			get { return _node.LineNumber > 0; }
		}
		
		public int LineNumber {
			get { return _node.LineNumber; }
		}

		public int LinePosition {
			get { return _node.LinePosition; }
		}
	}
}

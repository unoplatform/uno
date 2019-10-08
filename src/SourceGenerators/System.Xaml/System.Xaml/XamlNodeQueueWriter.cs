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
	internal class XamlNodeQueueWriter : XamlWriter
	{
		private readonly XamlNodeQueue _source;

		public XamlNodeQueueWriter (XamlNodeQueue source)
		{
			_source = source;
		}

		public override XamlSchemaContext SchemaContext {
			get { return _source.SchemaContext; }
		}

		public override void WriteEndMember ()
		{
			_source.Enqueue (new XamlNodeInfo (XamlNodeType.EndMember, default (XamlNodeMember)));
		}

		public override void WriteEndObject ()
		{
			_source.Enqueue (new XamlNodeInfo (XamlNodeType.EndObject, default (XamlObject)));
		}

		public override void WriteGetObject ()
		{
			_source.Enqueue (new XamlNodeInfo (XamlNodeType.GetObject, default (XamlObject)));
		}

		public override void WriteNamespace (NamespaceDeclaration ns)
		{
			_source.Enqueue (new XamlNodeInfo (ns));
		}

		public override void WriteStartMember (XamlMember xamlMember)
		{
			_source.Enqueue (new XamlNodeInfo (XamlNodeType.StartMember, new XamlNodeMember (default (XamlObject), xamlMember)));
		}

		public override void WriteStartObject (XamlType type)
		{
			_source.Enqueue (new XamlNodeInfo (XamlNodeType.StartObject, new XamlObject (type, null)));
		}

		public override void WriteValue (object value)
		{
			_source.Enqueue (new XamlNodeInfo (value));
		}
	}
}

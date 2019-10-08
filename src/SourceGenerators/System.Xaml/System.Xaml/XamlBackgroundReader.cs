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
using System;
using System.Threading;

namespace Uno.Xaml
{
	public class XamlBackgroundReader : XamlReader, IXamlLineInfo
	{
		public XamlBackgroundReader (XamlReader wrappedReader)
		{
			_r = wrappedReader ?? throw new ArgumentNullException (nameof(wrappedReader));
			_q = new XamlNodeQueue (_r.SchemaContext) { LineInfoProvider = _r as IXamlLineInfo };
		}

		private Thread _thread;
		private readonly XamlReader _r;
		private readonly XamlNodeQueue _q;
		private bool _readAllDone, _doWork = true;
		private readonly ManualResetEvent _wait = new ManualResetEvent (true);

		public bool HasLineInfo {
			get { return ((IXamlLineInfo) _q.Reader).HasLineInfo; }
		}
		
		public override bool IsEof {
			get { return _readAllDone && _q.IsEmpty; }
		}
		
		public int LineNumber {
			get { return ((IXamlLineInfo) _q.Reader).LineNumber; }
		}
		
		// [MonoTODO ("always returns 0")]
		public int LinePosition {
			get { return ((IXamlLineInfo) _q.Reader).LinePosition; }
		}
		
		public override XamlMember Member {
			get { return _q.Reader.Member; }
		}
		
		public override NamespaceDeclaration Namespace {
			get { return _q.Reader.Namespace; }
		}
		
		public override XamlNodeType NodeType {
			get { return _q.Reader.NodeType; }
		}
		
		public override XamlSchemaContext SchemaContext {
			get { return _q.Reader.SchemaContext; }
		}
		
		public override XamlType Type {
			get { return _q.Reader.Type; }
		}
		
		public override object Value {
			get { return _q.Reader.Value; }
		}

		protected override void Dispose (bool disposing)
		{
			_doWork = false;
		}
		
		public override bool Read ()
		{
			if (_q.IsEmpty)
			{
				_wait.WaitOne ();
			}

			return _q.Reader.Read ();
		}
		
		public void StartThread ()
		{
			StartThread ("XAML reader thread"); // documented name
		}
		
		public void StartThread (string threadName)
		{
			if (_thread != null)
			{
				throw new InvalidOperationException ("Thread has already started");
			}

			_thread = new Thread (new ParameterizedThreadStart (delegate {
				while (_doWork && _r.Read ()) {
					_q.Writer.WriteNode (_r);
					_wait.Set ();
				}
				_readAllDone = true;
			})) { Name = threadName };
			_thread.Start ();
		}
	}
}

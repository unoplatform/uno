#nullable enable

using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using Uno.UI.Runtime.Wayland;
using Uno.UI.Runtime.Wayland.Protocols;
using Uno.UI.Runtime.Wayland.Native;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed partial class DisplayTests
{
	[TestInitialize]
	public void RequireLibwayland()
	{
		if (!OperatingSystem.IsLinux() || !NativeLibrary.TryLoad("libwayland-client.so.0", out var library))
		{
			Assert.Inconclusive("libwayland-client is required for the in-process protocol peer.");
			return;
		}
		NativeLibrary.Free(library);
	}

	[TestMethod]
	public void DispatchDeliversSyncCallbacksAndStopsWhileIdle()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		Assert.AreEqual(new WaylandGlobal(7, "wl_compositor", 6), display.GetGlobal(WlCompositor.ProtocolName));
		using var compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, WlCompositor.MaximumVersion);
		Assert.AreEqual(6u, compositor.Version);
		using var completed = new ManualResetEventSlim();
		using var callback = display.Sync();
		callback.Done += (_, evt) =>
		{
			Assert.AreEqual(42u, evt.CallbackData);
			completed.Set();
		};
		display.StartDispatch();
		Assert.IsTrue(completed.Wait(TimeSpan.FromSeconds(5)), "Sync callback was not dispatched.");
		display.StopDispatch();
		Assert.IsNull(display.DispatchError);
		display.StartDispatch();
		display.StopDispatch();
		Assert.IsNull(display.DispatchError);
	}

	[TestMethod]
	public void PostedCommandsRunOnTheDispatchThreadUnderSyncRoot()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		using var ran = new ManualResetEventSlim();
		var onDispatchThread = false;
		var heldLock = false;
		display.Post(() =>
		{
			onDispatchThread = display.IsDispatchThread;
			heldLock = Monitor.IsEntered(display.SyncRoot);
			ran.Set();
		});
		display.StartDispatch();
		Assert.IsTrue(ran.Wait(TimeSpan.FromSeconds(5)), "Posted command did not run.");
		display.StopDispatch();
		Assert.IsTrue(onDispatchThread);
		Assert.IsTrue(heldLock);
		Assert.IsNull(display.DispatchError);
	}

	[TestMethod]
	public void FailingPostedCommandStopsDispatchWithError()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		display.Post(() => throw new InvalidOperationException("test command failure"));
		display.StartDispatch();
		Assert.IsTrue(SpinWait.SpinUntil(() =>
		{
			lock (display.SyncRoot)
			{
				return display.DispatchError is not null;
			}
		}, TimeSpan.FromSeconds(5)));
		display.StopDispatch();
		Assert.AreEqual("test command failure", display.DispatchError?.InnerException?.Message);
	}

	[TestMethod]
	public void StoppedDisplayRejectsPostedOwnership()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		display.StartDispatch();
		display.StopDispatch();
		var rejected = 0;
		Assert.IsFalse(display.TryPost(() => Assert.Fail("Stopped dispatch executed a command."), () => rejected++));
		Assert.AreEqual(1, rejected);
	}

	[TestMethod]
	public void ShutdownReleasesQueuedOwnership()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		var rejected = 0;
		Assert.IsTrue(display.TryPost(() => Assert.Fail("Queued command should be rejected."), () => rejected++));
		display.StopDispatch();
		Assert.AreEqual(1, rejected);
	}

	[TestMethod]
	public void DisposedDisplayRejectsPostedOwnership()
	{
		using var peer = new ProtocolPeer();
		var display = peer.Connect();
		display.Dispose();
		var rejected = 0;
		Assert.IsFalse(display.TryPost(() => Assert.Fail(), () => rejected++));
		Assert.AreEqual(1, rejected);
	}

	[TestMethod]
	public void DispatchFailureRejectsQueuedAndNewOwnership()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		var rejected = 0;
		display.Post(() => throw new InvalidOperationException("dispatch failure"));
		Assert.IsTrue(display.TryPost(() => Assert.Fail(), () => rejected++));
		display.StartDispatch();
		Assert.IsTrue(SpinWait.SpinUntil(() =>
		{
			lock (display.SyncRoot)
			{
				return display.DispatchError is not null;
			}
		}, TimeSpan.FromSeconds(5)));
		display.StopDispatch();
		Assert.IsFalse(display.TryPost(() => Assert.Fail(), () => rejected++));
		Assert.AreEqual(2, rejected);
	}

	[TestMethod]
	public void BindClampsToGeneratedVersion()
	{
		using var peer = new ProtocolPeer(compositorVersion: WlCompositor.MaximumVersion + 1);
		using var display = peer.Connect();
		using var compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, uint.MaxValue);
		Assert.AreEqual(WlCompositor.MaximumVersion, compositor.Version);
	}

	[TestMethod]
	public void CallbackExceptionsAreContainedAndReported()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		using var callback = display.Sync();
		callback.Done += (_, _) => throw new InvalidOperationException("test callback failure");
		display.StartDispatch();
		Assert.IsTrue(SpinWait.SpinUntil(() =>
		{
			lock (display.SyncRoot)
			{
				return display.DispatchError is not null;
			}
		}, TimeSpan.FromSeconds(5)));
		display.StopDispatch();
		Assert.IsInstanceOfType<InvalidOperationException>(display.DispatchError);
		Assert.AreEqual("test callback failure", display.DispatchError.InnerException?.Message);
	}

	[TestMethod]
	public void UnsupportedRequestVersionFailsBeforeMarshalling()
	{
		using var peer = new ProtocolPeer();
		using var display = peer.Connect();
		using var compositor = (WlCompositor)display.Bind(WlCompositor.ProtocolName, 1);
		using var surface = compositor.CreateSurface();
		Assert.ThrowsExactly<NotSupportedException>(() => surface.SetBufferScale(2));
		surface.Dispose();
		Assert.ThrowsExactly<ObjectDisposedException>(() => surface.Commit());
	}

	[LibraryImport("libc", SetLastError = true)]
	private static partial int socketpair(int domain, int type, int protocol, out SocketPair fds);
	[StructLayout(LayoutKind.Sequential)]
	private struct SocketPair
	{
		internal int Client;
		internal int Server;
	}

	internal sealed class ProtocolPeer : IDisposable
	{
		private readonly Socket _server;
		private readonly Thread _thread;
		private int _client;
		private uint _registry;
		private readonly uint _compositorVersion;
		private readonly bool _answerSync;
		private readonly bool _dataDeviceManager;
		private readonly object _sendGate = new();
		private readonly ManualResetEventSlim _sourceCreated = new();
		private uint _manager, _source;
		private uint _compositor;
		private readonly HashSet<uint> _surfaces = new();
		private readonly Dictionary<uint, uint> _pendingFrames = new();
		private readonly BlockingCollection<uint> _frameCallbacks = new();

		internal ProtocolPeer(uint compositorVersion = 6, bool answerSync = true, bool dataDeviceManager = false)
		{
			_compositorVersion = compositorVersion;
			_answerSync = answerSync;
			_dataDeviceManager = dataDeviceManager;
			if (socketpair(1, 1 | 0x80000, 0, out var pair) != 0)
			{
				throw new IOException($"socketpair failed: {Marshal.GetLastPInvokeError()}");
			}
			_client = pair.Client;
			_server = new Socket(new SafeSocketHandle(pair.Server, ownsHandle: true));
			_thread = new Thread(Serve) { IsBackground = true };
			_thread.Start();
		}
		internal WaylandDisplay Connect()
		{
			var fd = _client;
			_client = -1;
			return WaylandDisplay.ConnectToFileDescriptor(fd);
		}
		internal void CancelSource()
		{
			Assert.IsTrue(_sourceCreated.Wait(TimeSpan.FromSeconds(5)), "Data source was not created.");
			Send(_source, 2, []); // wl_data_source.cancelled
		}

		internal void CompleteFrame(uint timestamp)
		{
			Assert.IsTrue(_frameCallbacks.TryTake(out var callback, TimeSpan.FromSeconds(5)), "No frame callback was committed.");
			Send(callback, 0, BitConverter.GetBytes(timestamp));
			Send(1, 1, BitConverter.GetBytes(callback));
		}

		private void AdvertiseGlobal(uint id, string interfaceName, uint version)
		{
			var name = Encoding.UTF8.GetBytes(interfaceName + "\0");
			var global = new byte[4 + 4 + ((name.Length + 3) & ~3) + 4];
			BinaryPrimitives.WriteUInt32LittleEndian(global, id);
			BinaryPrimitives.WriteUInt32LittleEndian(global.AsSpan(4), (uint)name.Length);
			name.CopyTo(global, 8);
			BinaryPrimitives.WriteUInt32LittleEndian(global.AsSpan(global.Length - 4), version);
			Send(_registry, 0, global);
		}

		private void Serve()
		{
			try
			{
				while (true)
				{
					var header = new byte[8];
					Receive(header);
					var id = BinaryPrimitives.ReadUInt32LittleEndian(header);
					var word = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4));
					var opcode = word & 0xffff;
					var body = new byte[(word >> 16) - 8];
					Receive(body);
					if (id == 1 && opcode == 1)
					{
						_registry = BinaryPrimitives.ReadUInt32LittleEndian(body);
						AdvertiseGlobal(7, WlCompositor.ProtocolName, _compositorVersion);
						if (_dataDeviceManager)
						{
							AdvertiseGlobal(8, WlDataDeviceManager.ProtocolName, 3);
						}
					}
					else if (id == _registry && opcode == 0 && BinaryPrimitives.ReadUInt32LittleEndian(body) == 7)
					{
						_compositor = BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(body.Length - 4));
					}
					else if (id == _compositor && opcode == 0)
					{
						_surfaces.Add(BinaryPrimitives.ReadUInt32LittleEndian(body));
					}
					else if (_surfaces.Contains(id) && opcode == 3)
					{
						_pendingFrames[id] = BinaryPrimitives.ReadUInt32LittleEndian(body);
					}
					else if (_surfaces.Contains(id) && opcode == 6 && _pendingFrames.Remove(id, out var frame))
					{
						_frameCallbacks.Add(frame);
					}
					else if (_dataDeviceManager && id == _registry && opcode == 0 && BinaryPrimitives.ReadUInt32LittleEndian(body) == 8)
					{
						_manager = BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(body.Length - 4));
					}
					else if (_dataDeviceManager && id == _manager && opcode == 0)
					{
						_source = BinaryPrimitives.ReadUInt32LittleEndian(body);
						_sourceCreated.Set();
					}
					else if (id == 1 && opcode == 0 && _answerSync)
					{
						var callback = BinaryPrimitives.ReadUInt32LittleEndian(body);
						Send(callback, 0, BitConverter.GetBytes(42u));
						Send(1, 1, BitConverter.GetBytes(callback));
					}
				}
			}
			catch (Exception error) when (error is SocketException or ObjectDisposedException or EndOfStreamException)
			{
				try
				{
					_server.Shutdown(SocketShutdown.Both);
				}
				catch (Exception shutdownError) when (shutdownError is SocketException or ObjectDisposedException)
				{
				}
			}
		}
		private void Receive(byte[] bytes)
		{
			var offset = 0;
			while (offset < bytes.Length)
			{
				var read = _server.Receive(bytes.AsSpan(offset));
				if (read == 0)
				{
					throw new EndOfStreamException();
				}
				offset += read;
			}
		}
		private void Send(uint id, uint opcode, byte[] body)
		{
			lock (_sendGate)
			{
				SendCore(id, opcode, body);
			}
		}

		private unsafe void SendCore(uint id, uint opcode, byte[] body)
		{
			var bytes = new byte[body.Length + 8];
			BinaryPrimitives.WriteUInt32LittleEndian(bytes, id);
			BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), ((uint)bytes.Length << 16) | opcode);
			body.CopyTo(bytes, 8);
			var offset = 0;
			while (offset < bytes.Length)
			{
				fixed (byte* pointer = bytes)
				{
					var written = Posix.write((int)_server.SafeHandle.DangerousGetHandle(), pointer + offset, (nuint)(bytes.Length - offset));
					if (written <= 0)
					{
						throw new SocketException(Marshal.GetLastPInvokeError());
					}
					offset += checked((int)written);
				}
			}
		}
		public void Dispose()
		{
			try
			{
				_server.Shutdown(SocketShutdown.Both);
			}
			catch (SocketException)
			{
			}
			_server.Dispose();
			Assert.IsTrue(_thread.Join(TimeSpan.FromSeconds(5)), "Protocol peer did not stop.");
			_sourceCreated.Dispose();
			_frameCallbacks.Dispose();
			if (_client >= 0)
			{
				new SafeSocketHandle(_client, ownsHandle: true).Dispose();
			}
		}
	}
}

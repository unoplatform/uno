using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Graphics;
using static Uno.UI.Runtime.Skia.LibDrm;

namespace Uno.UI.Runtime.Skia
{
	[DebuggerDisplay("{Name}")]
	internal unsafe class DrmConnector
	{
		private static string[] KnownConnectorTypes =
		{
			"None", "VGA", "DVI-I", "DVI-D", "DVI-A", "Composite", "S-Video", "LVDS", "Component", "DIN",
			"DisplayPort", "HDMI-A", "HDMI-B", "TV", "eDP", "Virtual", "DSI"
		};

		/// <summary>
		/// specific the type of connector is HDMI-A, DVI, DisplayPort, etc.
		/// </summary>
		public enum DrmConnectorType : uint
		{
			None,
			VGA,
			DVI_I,
			DVI_D,
			DVI_A,
			Composite,
			S_Video,
			LVDS,
			Component,
			DIN,
			DisplayPort,
			HDMI_A,
			HDMI_B,
			TV,
			eDP,
			Virtual,
			DSI,
		}

		public DrmModeConnection Connection { get; }
		public uint Id { get; }
		public string Name { get; }
		public Size SizeMm { get; }
		public DrmModeSubPixel SubPixel { get; }
		internal uint EncoderId { get; }
		internal List<uint> EncoderIds { get; } = new List<uint>();
		public List<DrmModeInfo> Modes { get; } = new List<DrmModeInfo>();
		public DrmConnectorType ConnectorType { get; }
		public uint ConnectorTypeId { get; }
		internal DrmConnector(drmModeConnector* conn)
		{
			Connection = conn->connection;
			Id = conn->connector_id;
			SizeMm = new Size(conn->mmWidth, conn->mmHeight);
			SubPixel = conn->subpixel;
			for (var c = 0; c < conn->count_encoders; c++)
			{
				EncoderIds.Add(conn->encoders[c]);
			}

			EncoderId = conn->encoder_id;
			for (var c = 0; c < conn->count_modes; c++)
			{
				Modes.Add(new DrmModeInfo(ref conn->modes[c]));
			}

			ConnectorType = (DrmConnectorType)conn->connector_type;
			ConnectorTypeId = conn->connector_type_id;
			if (conn->connector_type > KnownConnectorTypes.Length - 1)
			{
				Name = $"Unknown({conn->connector_type})-{conn->connector_type_id}";
			}
			else
			{
				Name = KnownConnectorTypes[conn->connector_type] + "-" + conn->connector_type_id;
			}
		}
	}

	internal unsafe class DrmModeInfo
	{
		internal drmModeModeInfo Mode;

		internal DrmModeInfo(ref drmModeModeInfo info)
		{
			Mode = info;
			fixed (void* pName = info.name)
				Name = Marshal.PtrToStringAnsi(new IntPtr(pName));
		}

		public SizeInt32 Resolution => new SizeInt32(Mode.hdisplay, Mode.vdisplay);
		public bool IsPreferred => (Mode.type & DrmModeType.DRM_MODE_TYPE_PREFERRED) != 0;

		public string? Name { get; }
	}

	internal unsafe class DrmEncoder
	{
		public drmModeEncoder Encoder { get; }
		public List<drmModeCrtc> PossibleCrtcs { get; } = new List<drmModeCrtc>();

		public DrmEncoder(drmModeEncoder encoder, drmModeCrtc[] crtcs)
		{
			Encoder = encoder;
			for (var c = 0; c < crtcs.Length; c++)
			{
				var bit = 1 << c;
				if ((encoder.possible_crtcs & bit) != 0)
					PossibleCrtcs.Add(crtcs[c]);
			}
		}
	}

	internal unsafe class DrmResources
	{
		public List<DrmConnector> Connectors { get; } = new List<DrmConnector>();
		internal Dictionary<uint, DrmEncoder> Encoders { get; } = new Dictionary<uint, DrmEncoder>();
		public DrmResources(int fd, bool connectorsForceProbe = false)
		{
			var res = drmModeGetResources(fd);
			if (res == null)
				throw new Win32Exception("drmModeGetResources failed");

			var crtcs = new drmModeCrtc[res->count_crtcs];
			for (var c = 0; c < res->count_crtcs; c++)
			{
				var crtc = drmModeGetCrtc(fd, res->crtcs[c]);
				crtcs[c] = *crtc;
				drmModeFreeCrtc(crtc);
			}

			for (var c = 0; c < res->count_encoders; c++)
			{
				var enc = drmModeGetEncoder(fd, res->encoders[c]);
				Encoders[res->encoders[c]] = new DrmEncoder(*enc, crtcs);
				drmModeFreeEncoder(enc);
			}

			for (var c = 0; c < res->count_connectors; c++)
			{
				var conn = connectorsForceProbe ? drmModeGetConnector(fd, res->connectors[c]) : drmModeGetConnectorCurrent(fd, res->connectors[c]);
				Connectors.Add(new DrmConnector(conn));
				drmModeFreeConnector(conn);
			}
		}

		internal string Dump()
		{
			var sb = new StringBuilder();
			void Print(int off, string s)
			{
				for (var c = 0; c < off; c++)
					sb.Append("    ");
				sb.AppendLine(s);
			}
			Print(0, "Connectors");
			foreach (var conn in Connectors)
			{
				Print(1, $"{conn.Name}:");
				Print(2, $"Id: {conn.Id}");
				Print(2, $"Size: {conn.SizeMm} mm");
				Print(2, $"Encoder id: {conn.EncoderId}");
				Print(2, "Modes");
				foreach (var m in conn.Modes)
					Print(3, $"{m.Name} {(m.IsPreferred ? "PREFERRED" : "")}");
			}

			return sb.ToString();
		}
	}
}

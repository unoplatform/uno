#nullable enable

using System;
using System.Numerics;

namespace UITests.Shared.Windows_UI_Composition.SculpturesInWords;

/// <summary>
/// An indexed triangle mesh decimated offline from a public-domain scan: uint32 vertCount and
/// triCount, then x, y, z, nx, ny, nz per vertex and three uint32 indices per triangle.
/// </summary>
internal sealed class MeshData
{
	private MeshData(Vector3[] positions, Vector3[] normals, int[] indices)
	{
		Positions = positions;
		Normals = normals;
		Indices = indices;
	}

	internal Vector3[] Positions { get; }

	internal Vector3[] Normals { get; }

	internal int[] Indices { get; }

	internal static MeshData FromBytes(byte[] bytes)
	{
		var vertCount = (int)BitConverter.ToUInt32(bytes, 0);
		var triCount = (int)BitConverter.ToUInt32(bytes, 4);

		var positions = new Vector3[vertCount];
		var normals = new Vector3[vertCount];
		var o = 8;
		for (var i = 0; i < vertCount; i++, o += 24)
		{
			positions[i] = new Vector3(
				BitConverter.ToSingle(bytes, o),
				BitConverter.ToSingle(bytes, o + 4),
				BitConverter.ToSingle(bytes, o + 8));
			normals[i] = new Vector3(
				BitConverter.ToSingle(bytes, o + 12),
				BitConverter.ToSingle(bytes, o + 16),
				BitConverter.ToSingle(bytes, o + 20));
		}

		var indices = new int[triCount * 3];
		for (var i = 0; i < indices.Length; i++, o += 4)
		{
			indices[i] = (int)BitConverter.ToUInt32(bytes, o);
		}

		return new MeshData(positions, normals, indices);
	}
}

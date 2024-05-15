#nullable enable

using System;

namespace Uno.UI.Composition;

internal static class TempAndTintHelpers
{
	// Reference: Computation of Correlated Color Temperature and Distribution Temperature (A. R. Robertson, 1968), https://opg.optica.org/josa/abstract.cfm?uri=josa-58-11-1528
	// Reference: XYZ to Correlated Color Temperature (ANSI 'C' implementation of Robertson's method), http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_T.html
	// Reference: https://github.com/Manderby/CML/blob/master/code/CML/src/Internal/CMLIllumination.c

	internal struct UVTR
	{
		public float U;
		public float V;
		public float T;
		public float R;
	}

	internal static readonly UVTR[] _uvtr = new UVTR[31]
	{
		new() { U = 0.18006f, V = 0.26352f, T = -0.24341001f, R = 1.0e-10f },
		new() { U = 0.18065999f, V = 0.26589f, T = -0.25479001f, R = 0.0000099999997f },
		new() { U = 0.18133f, V = 0.26846001f, T = -0.26876f, R = 0.000019999999f },
		new() { U = 0.18208f, V = 0.27118999f , T = -0.28538999f , R = 0.000029999999f },
		new() { U = 0.18292999f, V = 0.27406999f , T = -0.30469999f , R = 0.000039999999f },
		new() { U = 0.18388f, V = 0.27709001f , T = -0.32675001f , R = 0.000049999999f },
		new() { U = 0.18494f, V = 0.28020999f , T = -0.35156f , R = 0.000059999998f },
		new() { U = 0.18611f, V = 0.28342f , T = -0.37915f , R = 0.000070000002f },
		new() { U = 0.1874f, V = 0.28668001f , T = -0.40955001f , R = 0.000079999998f },
		new() { U = 0.18880001f, V = 0.28997001f , T = -0.44277999f , R = 0.000090000001f },
		new() { U = 0.19032f, V = 0.29326001f , T = -0.47887999f , R = 0.000099999997f },
		new() { U = 0.19462f, V = 0.30140999f , T = -0.58204001f , R = 0.00012500001f },
		new() { U = 0.19961999f, V = 0.30921f , T = -0.70471001f , R = 0.00015000001f },
		new() { U = 0.20524999f, V = 0.31647f , T = -0.84900999f , R = 0.00017499999f },
		new() { U = 0.21142f, V = 0.32312f , T = -1.0182f , R = 0.00019999999f },
		new() { U = 0.21807f, V = 0.32909f , T = -1.2168f , R = 0.000225f },
		new() { U = 0.22510999f, V = 0.33439001f , T = -1.4512f , R = 0.00025000001f },
		new() { U = 0.23247001f, V = 0.33904001f , T = -1.7298f , R = 0.000275f },
		new() { U = 0.2401f, V = 0.34308001f , T = -2.0637f , R = 0.00030000001f },
		new() { U = 0.24792001f, V = 0.34654999f , T = -2.4681001f , R = 0.000325f },
		new() { U = 0.25591001f, V = 0.34951001f , T = -2.9640999f , R = 0.00034999999f },
		new() { U = 0.264f, V = 0.352f , T = -3.5813999f , R = 0.000375f },
		new() { U = 0.27217999f, V = 0.35407001f , T = -4.3632998f , R = 0.00039999999f },
		new() { U = 0.28038999f, V = 0.35576999f , T = -5.3762002f , R = 0.00042500001f },
		new() { U = 0.28863001f, V = 0.35714f , T = -6.7262001f , R = 0.00044999999f },
		new() { U = 0.29685f, V = 0.35822999f , T = -8.5955f , R = 0.00047500001f },
		new() { U = 0.30504999f, V = 0.35907f , T = -11.324f , R = 0.00050000002f },
		new() { U = 0.3132f, V = 0.35968f , T = -15.628f , R = 0.00052499998f },
		new() { U = 0.32128999f, V = 0.36011001f , T = -23.325001f , R = 0.00055f },
		new() { U = 0.32931f, V = 0.36037999f , T = -40.77f , R = 0.00057500001f },
		new() { U = 0.33724001f, V = 0.36050999f , T = -116.45f , R = 0.00060000003f }
	};

	public static (float RedGain, float BlueGain) TempTintToGains(float normalizedTemp, float normalizedTint)
	{
		float srcTemp = 6502.0781f;
		float dstB = normalizedTemp * 100.0f;
		float dstY = 6502.0781f;

		if (dstB <= 0.0)
		{
			if (dstB < 0.0)
			{
				srcTemp = dstB * 0.01249999925494194f;
				var currentTemp = srcTemp;
				srcTemp = 0.0001537969801574945f - -0.00005379698268370703f * srcTemp;
				dstY = 1.0f / srcTemp;
				srcTemp = 0.0001537969801574945f - currentTemp * 0.00008429825538769364f;
				srcTemp = 1.0f / srcTemp;
			}
		}
		else
		{
			dstB = dstB * 0.01249999925494194f;
			dstB = dstB * 0.00006842524453531951f + 0.0001537969801574945f;
			dstY = 1.0f / dstB;
		}

		TempTintToXYZ(out float srcG, out float srcY, srcTemp, 0.0032560001f, out dstB);
		XYZtoRGB(out srcY, out srcG, srcG, srcY, dstB, out srcTemp);

		srcY = srcY / srcG;
		srcTemp = srcTemp / srcG;
		srcG = normalizedTint * 1.25f * 0.009999999776482582f + 0.003256000112742186f;

		TempTintToXYZ(out float dstX, out dstY, dstY, srcG, out srcG);
		XYZtoRGB(out dstY, out dstX, dstX, dstY, srcG, out dstB);

		dstY = dstY / dstX;
		dstB = dstB / dstX;

		return (dstY / srcY, dstB / srcTemp);
	}

	private static void XYZtoRGB(out float r, out float g, float x, float y, float z, out float b)
	{
		r = x * 3.240454196929932f - y * 1.53713846206665f - z * 0.4985314011573792f;
		g = y * 1.876010775566101f - x * 0.9692659974098206f + z * 0.04155600070953369f;
		b = x * 0.05564339831471443f - y * 0.2040258944034576f + z * 1.057225227355957f;
	}

	private static void TempTintToXYZ(out float x, out float y, float temp, float tint, out float z)
	{
		if (temp < 1666.666625976562f)
		{
			temp = 1666.6666f;
		}

		float max = Math.Clamp(tint, -0.1f, 0.1f);
		TempTintToYUV(out float flY, out float flU, temp, max, out float flV);
		Yuv1960toXYZ(out x, out y, flY, flU, flV, out z);
	}

	private static void Yuv1960toXYZ(out float x, out float y, float yi, float u, float v, out float z)
	{
		if (v >= 0.0000001000000011686097f)
		{
			x = u * 6.0f * yi / (v * 4.0f);
			z = yi * ((4.0f - u) * 6.0f - v * 60.0f) / (v * 12.0f);
		}
		else
		{
			z = 0.0f;
			x = 0.0f;
		}

		y = yi;
	}

	private static void TempTintToYUV(out float y, out float u, float temp, float tint, out float v)
	{
		if (temp < 1666.666625976562f)
		{
			temp = 1666.6666f;
		}

		float clampedTint = Math.Clamp(tint, -0.1f, 0.1f);
		float reciprocalTemperature = 1.0f / temp;

		int index = 1;
		for (int i = 0; i < 31; i++)
		{
			if (_uvtr[i].R <= reciprocalTemperature && _uvtr[i + 1].R >= reciprocalTemperature)
			{
				break;
			}

			index++;
		}

		if (index >= 0x1F)
		{
			index = 30;
		}

		float reciprocalTemperatureDelta = InvertLerp(_uvtr[index - 1].R, reciprocalTemperature, _uvtr[index].R);
		float temperatureSquared = _uvtr[index - 1].T * _uvtr[index - 1].T + 1.0f;
		float temperatureRoot = MathF.Sqrt(temperatureSquared);
		float temperatureC = 1.0f / temperatureRoot;
		float tc = _uvtr[index - 1].T * temperatureC;
		float tt = _uvtr[index].T * _uvtr[index].T + 1.0f;
		float ttRoot = MathF.Sqrt(tt);
		float reciprocalTt = 1.0f / ttRoot;
		float ttC = _uvtr[index].T * reciprocalTt;
		float deltaT = tc + (ttC - tc) * reciprocalTemperatureDelta;
		float deltaC = (reciprocalTt - temperatureC) * reciprocalTemperatureDelta + temperatureC;
		float reciprocalTempDelta = deltaT / deltaC;

		float ru = (_uvtr[index].U - _uvtr[index - 1].U) * reciprocalTemperatureDelta + _uvtr[index - 1].U;
		float rv = reciprocalTemperatureDelta * (_uvtr[index].V - _uvtr[index - 1].V) + _uvtr[index - 1].V;
		float deltaUc = reciprocalTempDelta * reciprocalTempDelta + 1.0f;
		float deltaUd = MathF.Sqrt(deltaUc);
		float deltaUe = -clampedTint / deltaUd;

		y = 1.0f;
		u = deltaUe + ru;
		float deltaUf = deltaUe * reciprocalTempDelta;
		v = deltaUf + rv;

		MapUV(ref u, ref v, ru, rv);
	}

	private static float InvertLerp(float a, float value, float b)
	{
		if (a == b)
		{
			return 0.5f;
		}
		else
		{
			return (value - a) / (b - a);
		}
	}

	private static void MapUV(ref float u, ref float v, float ui, float vi)
	{
		float value = 0.4000000059604645f - u * 0.1000000014901161f;

		if (v > value)
		{
			float val = (value - v) / (vi - 0.1000000014901161f * (u - ui) - v);
			u = u + (ui - u) * val;
			v = val * (vi - v) + v;
		}
	}
}

#nullable enable

using System;

namespace Uno.UI.Composition
{
	internal static class TempAndTintUtils
	{
		internal struct Robertson
		{
			public float U;
			public float V;
			public float S;
			public float R;
		}

		internal static readonly Robertson[] _robertson = new Robertson[31]
		{
			new() { U = 0.18006f, V = 0.26352f, S = -0.24341001f, R = 1.0e-10f },
			new() { U = 0.18065999f, V = 0.26589f, S = -0.25479001f, R = 0.0000099999997f },
			new() { U = 0.18133f, V = 0.26846001f, S = -0.26876f, R = 0.000019999999f },
			new() { U = 0.18208f, V = 0.27118999f , S = -0.28538999f , R = 0.000029999999f },
			new() { U = 0.18292999f, V = 0.27406999f , S = -0.30469999f , R = 0.000039999999f },
			new() { U = 0.18388f, V = 0.27709001f , S = -0.32675001f , R = 0.000049999999f },
			new() { U = 0.18494f, V = 0.28020999f , S = -0.35156f , R = 0.000059999998f },
			new() { U = 0.18611f, V = 0.28342f , S = -0.37915f , R = 0.000070000002f },
			new() { U = 0.1874f, V = 0.28668001f , S = -0.40955001f , R = 0.000079999998f },
			new() { U = 0.18880001f, V = 0.28997001f , S = -0.44277999f , R = 0.000090000001f },
			new() { U = 0.19032f, V = 0.29326001f , S = -0.47887999f , R = 0.000099999997f },
			new() { U = 0.19462f, V = 0.30140999f , S = -0.58204001f , R = 0.00012500001f },
			new() { U = 0.19961999f, V = 0.30921f , S = -0.70471001f , R = 0.00015000001f },
			new() { U = 0.20524999f, V = 0.31647f , S = -0.84900999f , R = 0.00017499999f },
			new() { U = 0.21142f, V = 0.32312f , S = -1.0182f , R = 0.00019999999f },
			new() { U = 0.21807f, V = 0.32909f , S = -1.2168f , R = 0.000225f },
			new() { U = 0.22510999f, V = 0.33439001f , S = -1.4512f , R = 0.00025000001f },
			new() { U = 0.23247001f, V = 0.33904001f , S = -1.7298f , R = 0.000275f },
			new() { U = 0.2401f, V = 0.34308001f , S = -2.0637f , R = 0.00030000001f },
			new() { U = 0.24792001f, V = 0.34654999f , S = -2.4681001f , R = 0.000325f },
			new() { U = 0.25591001f, V = 0.34951001f , S = -2.9640999f , R = 0.00034999999f },
			new() { U = 0.264f, V = 0.352f , S = -3.5813999f , R = 0.000375f },
			new() { U = 0.27217999f, V = 0.35407001f , S = -4.3632998f , R = 0.00039999999f },
			new() { U = 0.28038999f, V = 0.35576999f , S = -5.3762002f , R = 0.00042500001f },
			new() { U = 0.28863001f, V = 0.35714f , S = -6.7262001f , R = 0.00044999999f },
			new() { U = 0.29685f, V = 0.35822999f , S = -8.5955f , R = 0.00047500001f },
			new() { U = 0.30504999f, V = 0.35907f , S = -11.324f , R = 0.00050000002f },
			new() { U = 0.3132f, V = 0.35968f , S = -15.628f , R = 0.00052499998f },
			new() { U = 0.32128999f, V = 0.36011001f , S = -23.325001f , R = 0.00055f },
			new() { U = 0.32931f, V = 0.36037999f , S = -40.77f , R = 0.00057500001f },
			new() { U = 0.33724001f, V = 0.36050999f , S = -116.45f , R = 0.00060000003f }
		};

		public static (float RedGain, float BlueGain) NormalizedTempTintToGains(float normTemp, float normTint)
		{
			float flDstX;
			float srcG;
			float srcTemp = 6502.0781f;
			float flDstB = normTemp * 100.0f;
			float srcY;
			float flDstY = 6502.0781f;

			if (flDstB <= 0.0)
			{
				if (flDstB < 0.0)
				{
					srcTemp = flDstB * 0.01249999925494194f;
					var currentTemp = srcTemp;
					srcTemp = 0.0001537969801574945f - -0.00005379698268370703f * srcTemp;
					flDstY = 1.0f / srcTemp;
					srcTemp = 0.0001537969801574945f - currentTemp * 0.00008429825538769364f;
					srcTemp = 1.0f / srcTemp;
				}
			}
			else
			{
				flDstB = flDstB * 0.01249999925494194f;
				flDstB = flDstB * 0.00006842524453531951f + 0.0001537969801574945f;
				flDstY = 1.0f / flDstB;
			}

			TempTintToXYZ(out srcG, out srcY, srcTemp, 0.0032560001f, out flDstB);
			XYZtoRGB(out srcY, out srcG, srcG, srcY, flDstB, out srcTemp);

			srcY = srcY / srcG;
			srcTemp = srcTemp / srcG;
			srcG = normTint * 1.25f * 0.009999999776482582f + 0.003256000112742186f;

			TempTintToXYZ(out flDstX, out flDstY, flDstY, srcG, out srcG);
			XYZtoRGB(out flDstY, out flDstX, flDstX, flDstY, srcG, out flDstB);

			flDstY = flDstY / flDstX;
			flDstB = flDstB / flDstX;

			return (flDstY / srcY, flDstB / srcTemp);
		}

		private static void XYZtoRGB(out float r, out float g, float xVal, float yVal, float zVal, out float b)
		{
			r = xVal * 3.240454196929932f - yVal * 1.53713846206665f - zVal * 0.4985314011573792f;
			g = yVal * 1.876010775566101f - xVal * 0.9692659974098206f + zVal * 0.04155600070953369f;
			b = xVal * 0.05564339831471443f - yVal * 0.2040258944034576f + zVal * 1.057225227355957f;
		}

		private static void TempTintToXYZ(out float pflX, out float pflY, float flTemp, float flTint, out float pflZ)
		{
			float flMax;
			float flY;
			float flU;
			float flV;

			if (flTemp < 1666.666625976562f)
			{
				flTemp = 1666.6666f;
			}

			flMax = Math.Clamp(flTint, -0.1f, 0.1f);
			TempTintToYUV(out flY, out flU, flTemp, flMax, out flV);
			Yuv1960toXYZ(out pflX, out pflY, flY, flU, flV, out pflZ);
		}

		private static void Yuv1960toXYZ(out float pflX, out float pflY, float flY, float flU, float flV, out float pflZ)
		{
			if (flV >= 0.0000001000000011686097f)
			{
				pflX = flU * 6.0f * flY / (flV * 4.0f);
				pflZ = flY * ((4.0f - flU) * 6.0f - flV * 60.0f) / (flV * 12.0f);
			}
			else
			{
				pflZ = 0.0f;
				pflX = 0.0f;
			}

			pflY = flY;
		}

		private static void TempTintToYUV(out float pflY, out float pflU, float flTemp, float flTint, out float pflV)
		{
			float flDelU;
			float flDelUa;
			float flDelUb;
			float flDelUc;
			float flDelUd;
			float flDelUe;
			float flDelUf;
			float flJPrev;
			float flJPreva;
			float flVbb;
			float flVbba;
			float flVbbb;
			float flVbbc;
			float flVbbd;
			float flRecipTemp;
			float flRecipTempa;
			float flRecipTempb;
			float flRecipTempc;
			float flRecipTempd;
			float flTinta;

			if (flTemp < 1666.666625976562f)
			{
				flTemp = 1666.6666f;
			}

			flTinta = Math.Clamp(flTint, -0.1f, 0.1f);
			flRecipTemp = 1.0f / flTemp;

			var idx = 1;
			for (int i = 0; i < 31; i++)
			{
				if (_robertson[i].R <= flRecipTemp && _robertson[i + 1].R >= flRecipTemp)
				{
					break;
				}

				idx++;
			}

			if (idx >= 0x1F)
			{
				idx = 30;
			}

			flDelU = InvLerp(_robertson[idx - 1].R, flRecipTemp, _robertson[idx].R);
			flRecipTempa = _robertson[idx - 1].S * _robertson[idx - 1].S + 1.0f;
			flRecipTempb = MathF.Sqrt(flRecipTempa);
			flRecipTempc = 1.0f / flRecipTempb;
			flJPrev = _robertson[idx - 1].S * flRecipTempc;
			flVbb = _robertson[idx].S * _robertson[idx].S + 1.0f;
			flVbba = MathF.Sqrt(flVbb);
			flVbbb = 1.0f / flVbba;
			flVbbc = _robertson[idx].S * flVbbb;
			flDelUa = flJPrev + (flVbbc - flJPrev) * flDelU;
			flDelUb = (flVbbb - flRecipTempc) * flDelU + flRecipTempc;
			flRecipTempd = flDelUa / flDelUb;

			flJPreva = (_robertson[idx].U - _robertson[idx - 1].U) * flDelU
					 + _robertson[idx - 1].U;

			flVbbd = flDelU * (_robertson[idx].V - _robertson[idx - 1].V)
				   + _robertson[idx - 1].V;

			flDelUc = flRecipTempd * flRecipTempd + 1.0f;
			flDelUd = MathF.Sqrt(flDelUc);
			flDelUe = -flTinta / flDelUd;
			pflY = 1.0f;
			pflU = flDelUe + flJPreva;
			flDelUf = flDelUe * flRecipTempd;
			pflV = flDelUf + flVbbd;

			MapUV(ref pflU, ref pflV, flJPreva, flVbbd);
		}

		private static float InvLerp(float flV0, float flVt, float flV1)
		{
			if (flV0 == flV1)
			{
				return (float)0.5f;
			}
			else
			{
				return (float)((flVt - flV0) / (flV1 - flV0));
			}
		}

		private static void MapUV(ref float pflU, ref float pflV, float flUbb, float flVbb)
		{
			float flT;
			float uVal = 0.4000000059604645f - pflU * 0.1000000014901161f;

			if (pflV > uVal)
			{
				flT = (uVal - pflV) / (flVbb - 0.1000000014901161f * (pflU - flUbb) - pflV);
				pflU = pflU + (flUbb - pflU) * flT;
				pflV = flT * (flVbb - pflV) + pflV;
			}
		}
	}
}

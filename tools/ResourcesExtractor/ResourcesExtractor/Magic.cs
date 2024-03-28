using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ResourcesExtractor;

public static class Magic
{
	public enum Languages
	{
		af_ZA = 0x0436,
		am_et = 0x045e,
		ar_SA = 0x401,
		as_IN = 0x044d,
		az_Latn_AZ = 0x42c,
		bg_BG = 0x0402,
		bn_IN = 0x445,
		bs_Latn_BA = 0x141a,
		ca_ES = 0x0403,
		ca_Es_VALENCIA = 0x0803,
		cs_CZ = 0x0405,
		cy_GB = 0x0452,
		da_DK = 0x0406,
		de_DE = 0x407,
		el_GR = 0x408,
		en_GB = 0x809,
		en_us = 0x409,
		//es_ES = 0x40a,
		es_MX = 0x80a,
		et_EE = 0x0425,
		eu_ES = 0x042d,
		fa_IR = 0x0429,
		fi_FI = 0x040b,
		fil_ph = 0x0464,
		fr_CA = 0xc0c,
		fr_FR = 0x40c,
		ga_IE = 0x83c,
		gd_gb = 0x0491,
		gl_ES = 0x0456,
		gu_IN = 0x0447,
		he_IL = 0x040d,
		hi_IN = 0x0439,
		hr_HR = 0x041a,
		hu_HU = 0x040e,
		hy_AM = 0x042b,
		id_ID = 0x0421,
		is_IS = 0x040f,
		it_IT = 0x410,
		ja_JP = 0x0411,
		ka_GE = 0x0437,
		kk_KZ = 0x043f,
		km_kh = 0x0453,
		kn_in = 0x044b,
		ko_KR = 0x412,
		kok_IN = 0x0457,
		lb_LU = 0x046e,
		lo_la = 0x0454,
		lt_LT = 0x427,
		lv_LV = 0x0426,
		mi_NZ = 0x0481,
		mk_mk = 0x042f,
		ml_in = 0x044c,
		mr_IN = 0x044e,
		ms_MY = 0x43e,
		mt_MT = 0x043a,
		nb_NO = 0x414,
		ne_NP = 0x0461,
		nl_NL = 0x413,
		nn_NO = 0x814,
		or_IN = 0x0448,
		pa_IN = 0x0446,
		pl_PL = 0x0415,
		pt_BR = 0x416,
		pt_PT = 0x816,
		quz_PE = 0xc6b,
		ro_RO = 0x0418,
		ru_RU = 0x0419,
		sk_SK = 0x041b,
		sl_SI = 0x0424,
		sq_AL = 0x041c,
		sr_Cyrl_BA = 0x1c1a,
		sr_Cyrl_RS = 0x281a,
		sr_Latn_RS = 0x241a,
		sv_SE = 0x41d,
		ta_in = 0x449,
		te_in = 0x044a,
		th_TH = 0x041e,
		tr_TR = 0x041f,
		tt_RU = 0x0444,
		ug_CN = 0x0480,
		uk_UA = 0x0422,
		ur_PK = 0x420,
		uz_latn_uz = 0x443,
		vi_VN = 0x042a,
		zh_CN = 0x0804,
		zh_TW = 0x0404,
	}

	public static unsafe string GetLocalizedResource(int resourceId, int langid)
	{
		try
		{
			IntPtr block = MAKEINTRESOURCEW(resourceId / 16 + 1);
			int offset = resourceId % 16;
			IntPtr hModule = NativeMethods.GetModuleHandle("Windows.UI.Xaml.dll");


			// Find and load the string resource
			//var langid = MAKELANGID(0x09 /* LANG_ENGLISH */, 0x01 /* SUBLANG_ENGLISH_US */);
			IntPtr hResource = NativeMethods.FindResourceEx(hModule, (IntPtr)NativeMethods.RT_STRING, block, langid);
			IntPtr hStr = NativeMethods.LoadResource(hModule, hResource);

			ushort* curr = (ushort*)hStr;
			for (int i = 0; i < offset; i++)
			{
				// Skip to next string
				curr += (*curr + 1);
			}

			int len = *curr;
			curr++;

			return new string((char*)curr, 0, len).Replace("\u200f", "");
		}
		catch (Exception)
		{
			Debug.WriteLine("Localization for " + resourceId + " is not available");
			return null;
		}
	}

	private static IntPtr MAKEINTRESOURCEW(int i)
	{
		ushort w = unchecked((ushort)i);
		IntPtr ip = (IntPtr)w;
		return ip;
	}

	//private static int MAKELANGID(int primaryLang, int subLang)
	//{
	//    return ((((ushort)(subLang)) << 10) | (ushort)(primaryLang));
	//}

	private static class NativeMethods
	{
		public const int RT_STRING = 6;

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, int wLanguage);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);
	}
}

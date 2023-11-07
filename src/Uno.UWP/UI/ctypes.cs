// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ctypes.h & ctypes.cpp, tag winui3/release/1.4.2

using System;
namespace Windows.UI;

internal class Ctypes
{
	public const Int32 XCLASS_UPPER = 0x0001;
	public const Int32 XCLASS_LOWER = 0x0002;
	public const Int32 XCLASS_DIGIT = 0x0004;
	public const Int32 XCLASS_SPACE = 0x0008;
	public const Int32 XCLASS_PUNCT = 0x0010;
	public const Int32 XCLASS_CONTROL = 0x0020;
	public const Int32 XCLASS_XDIGIT = 0x0080;
	public const Int32 XCLASS_FLEADING = 0x0200;
	public const Int32 XCLASS_NAME = 0x0400;
	public const Int32 XCLASS_JSNAME = 0x0800;

	private static readonly Int32[] s_types =
	{
		0, // 0x00
		0, // 0x01
		0, // 0x02
		0, // 0x03
		0, // 0x04
		0, // 0x05
		0, // 0x06
		0, // 0x07
		0, // 0x08
		XCLASS_SPACE, // 0x09
		XCLASS_SPACE, // 0x0a
		XCLASS_SPACE, // 0x0b
		XCLASS_SPACE, // 0x0c
		XCLASS_SPACE, // 0x0d
		0, // 0x0e
		0, // 0x0f

		0, // 0x10
		0, // 0x11
		0, // 0x12
		0, // 0x13
		0, // 0x14
		0, // 0x15
		0, // 0x16
		0, // 0x17
		0, // 0x18
		0, // 0x19
		0, // 0x1a
		0, // 0x1b
		0, // 0x1c
		0, // 0x1d
		0, // 0x1e
		0, // 0x1f

		XCLASS_SPACE, // 0x20
		0, // 0x21 !
		0, // 0x22 "
		0, // 0x23 #
		XCLASS_JSNAME, // 0x24 $
		0, // 0x25 %
		0, // 0x26 &
		0, // 0x27 '
		0, // 0x28 (
		0, // 0x29 )
		0, // 0x2a *
		XCLASS_FLEADING, // 0x2b +
		0, // 0x2c ,
		XCLASS_FLEADING, // 0x2d -
		XCLASS_FLEADING, // 0x2e .
		0, // 0x2f /

		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x30 0
		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x31 1
		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x32 2
		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x33 3
		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x34 4
		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x35 5
		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x36 6
		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x37 7
		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x38 8
		XCLASS_DIGIT | XCLASS_XDIGIT, // 0x39 9
		0, // 0x3a :
		0, // 0x3b ;
		0, // 0x3c <
		0, // 0x3d =
		0, // 0x3e >
		0, // 0x3f ?

		0, // 0x40 @
		XCLASS_UPPER | XCLASS_XDIGIT, // 0x41 A
		XCLASS_UPPER | XCLASS_XDIGIT, // 0x42 B
		XCLASS_UPPER | XCLASS_XDIGIT, // 0x43 C
		XCLASS_UPPER | XCLASS_XDIGIT, // 0x44 D
		XCLASS_UPPER | XCLASS_XDIGIT, // 0x45 E
		XCLASS_UPPER | XCLASS_XDIGIT, // 0x46 F
		XCLASS_UPPER, // 0x47 G
		XCLASS_UPPER, // 0x48 H
		XCLASS_UPPER, // 0x49 I
		XCLASS_UPPER, // 0x4a J
		XCLASS_UPPER, // 0x4b K
		XCLASS_UPPER, // 0x4c L
		XCLASS_UPPER, // 0x4d M
		XCLASS_UPPER, // 0x4e N
		XCLASS_UPPER, // 0x4f O

		XCLASS_UPPER, // 0x50 P
		XCLASS_UPPER, // 0x51 Q
		XCLASS_UPPER, // 0x52 R
		XCLASS_UPPER, // 0x53 S
		XCLASS_UPPER, // 0x54 T
		XCLASS_UPPER, // 0x55 U
		XCLASS_UPPER, // 0x56 V
		XCLASS_UPPER, // 0x57 W
		XCLASS_UPPER, // 0x58 X
		XCLASS_UPPER, // 0x59 Y
		XCLASS_UPPER, // 0x5a Z
		0, // 0x5b [
		0, // 0x5c
		0, // 0x5d ]
		0, // 0x5e ^
		XCLASS_NAME, // 0x5f _

		0, // 0x60 `
		XCLASS_LOWER | XCLASS_XDIGIT, // 0x61 a
		XCLASS_LOWER | XCLASS_XDIGIT, // 0x62 b
		XCLASS_LOWER | XCLASS_XDIGIT, // 0x63 c
		XCLASS_LOWER | XCLASS_XDIGIT, // 0x64 d
		XCLASS_LOWER | XCLASS_XDIGIT, // 0x65 e
		XCLASS_LOWER | XCLASS_XDIGIT, // 0x66 f
		XCLASS_LOWER, // 0x67 g
		XCLASS_LOWER, // 0x68 h
		XCLASS_LOWER, // 0x69 i
		XCLASS_LOWER, // 0x6a j
		XCLASS_LOWER, // 0x6b k
		XCLASS_LOWER, // 0x6c l
		XCLASS_LOWER, // 0x6d m
		XCLASS_LOWER, // 0x6e n
		XCLASS_LOWER, // 0x6f o

		XCLASS_LOWER, // 0x70 p
		XCLASS_LOWER, // 0x71 q
		XCLASS_LOWER, // 0x72 r
		XCLASS_LOWER, // 0x73 s
		XCLASS_LOWER, // 0x74 t
		XCLASS_LOWER, // 0x75 u
		XCLASS_LOWER, // 0x76 v
		XCLASS_LOWER, // 0x77 w
		XCLASS_LOWER, // 0x78 x
		XCLASS_LOWER, // 0x79 y
		XCLASS_LOWER, // 0x7a z
		0, // 0x7b {
		0, // 0x7c |
		0, // 0x7d }
		0, // 0x7e ~
		0, // 0x7f
	};

	public static int xisspace(char ch) { return (ch < 128) ? s_types[ch] & XCLASS_SPACE : 0; }
	public static int xisdigit(char ch) { return (ch < 128) ? s_types[ch] & XCLASS_DIGIT : 0; }
	public static int xisxdigit(char ch) { return (ch < 128) ? s_types[ch] & XCLASS_XDIGIT : 0; }
	public static int xisfleading(char ch) { return (ch < 128) ? s_types[ch] & (XCLASS_FLEADING | XCLASS_DIGIT) : 0; }
	public static int xisupper(char ch) { return (ch < 128) ? s_types[ch] & XCLASS_UPPER : 0; }
	public static int xislower(char ch) { return (ch < 128) ? s_types[ch] & XCLASS_LOWER : 0; }
	public static int xisalpha(char ch) { return (ch < 128) ? s_types[ch] & (XCLASS_UPPER | XCLASS_LOWER) : 0; }
	public static int xisalnum(char ch) { return (ch < 128) ? s_types[ch] & (XCLASS_UPPER | XCLASS_LOWER | XCLASS_DIGIT) : 0; }
	public static int xisname(char ch) { return (ch < 128) ? s_types[ch] & (XCLASS_NAME | XCLASS_UPPER | XCLASS_LOWER | XCLASS_DIGIT) : 0; }
	public static int xisname0(char ch) { return (ch < 128) ? s_types[ch] & (XCLASS_NAME | XCLASS_UPPER | XCLASS_LOWER) : 0; }
}

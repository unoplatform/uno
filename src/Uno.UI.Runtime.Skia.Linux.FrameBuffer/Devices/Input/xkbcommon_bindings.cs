/*
 * Copyright 1985, 1987, 1990, 1998  The Open Group
 * Copyright 2008  Dan Nicholson
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN
 * ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 *
 * Except as contained in this notice, the names of the authors or their
 * institutions shall not be used in advertising or otherwise to promote the
 * sale, use or other dealings in this Software without prior written
 * authorization from the authors.
 */

/************************************************************
 * Copyright (c) 1993 by Silicon Graphics Computer Systems, Inc.
 *
 * Permission to use, copy, modify, and distribute this
 * software and its documentation for any purpose and without
 * fee is hereby granted, provided that the above copyright
 * notice appear in all copies and that both that copyright
 * notice and this permission notice appear in supporting
 * documentation, and that the name of Silicon Graphics not be
 * used in advertising or publicity pertaining to distribution
 * of the software without specific prior written permission.
 * Silicon Graphics makes no representation about the suitability
 * of this software for any purpose. It is provided "as is"
 * without any express or implied warranty.
 *
 * SILICON GRAPHICS DISCLAIMS ALL WARRANTIES WITH REGARD TO THIS
 * SOFTWARE, INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
 * AND FITNESS FOR A PARTICULAR PURPOSE. IN NO EVENT SHALL SILICON
 * GRAPHICS BE LIABLE FOR ANY SPECIAL, INDIRECT OR CONSEQUENTIAL
 * DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE,
 * DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE
 * OR OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION  WITH
 * THE USE OR PERFORMANCE OF THIS SOFTWARE.
 *
 ********************************************************/

/*
 * Copyright © 2009-2012 Daniel Stone
 * Copyright © 2012 Intel Corporation
 * Copyright © 2012 Ran Benita
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice (including the next
 * paragraph) shall be included in all copies or substantial portions of the
 * Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 *
 * Author: Daniel Stone <daniel@fooishbar.org>
 */

using System;
using System.Runtime.InteropServices;
using Uno.UI.Runtime.Skia;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.Devices.Input;

internal class LibXKBCommon
{
	private const string LibXkbCommon = "libxkbcommon.so";

	[DllImport(LibXkbCommon)]
	public static extern IntPtr xkb_context_new(xkb_context_flags flags);

	[DllImport(LibXkbCommon)]
	public static extern unsafe IntPtr xkb_keymap_new_from_names(IntPtr context, xkb_rule_names* names, xkb_keymap_compile_flags flags);

	[DllImport(LibXkbCommon)]
	public static extern IntPtr xkb_state_new(IntPtr keymap);

	[DllImport(LibXkbCommon)]
	public static extern uint xkb_state_key_get_one_sym(IntPtr state, uint key);

	[DllImport(LibXkbCommon)]
	public static extern unsafe int xkb_keysym_get_name(uint keysym, byte* buffer, IntPtr size);

	[DllImport(LibXkbCommon)]
	public static extern unsafe int xkb_state_key_get_utf8(IntPtr state, uint key, byte* buffer, IntPtr size);

	[DllImport(LibXkbCommon)]
	public static extern xkb_state_component xkb_state_update_key(IntPtr state, uint key, xkb_key_direction direction);

	[DllImport(LibXkbCommon, CharSet = CharSet.Ansi)]
	public static extern string xkb_keymap_layout_get_name(IntPtr keymap, uint idx);

	/** Flags for context creation. */
	public enum xkb_context_flags
	{
		/** Do not apply any context flags. */
		XKB_CONTEXT_NO_FLAGS = 0,
		/** Create this context with an empty include path. */
		XKB_CONTEXT_NO_DEFAULT_INCLUDES = (1 << 0),
		/**
		 * Don't take RMLVO names from the environment.
		 *
		 * @since 0.3.0
		 */
		XKB_CONTEXT_NO_ENVIRONMENT_NAMES = (1 << 1),
		/**
		 * Disable the use of secure_getenv for this context, so that privileged
		 * processes can use environment variables. Client uses at their own risk.
		 *
		 * @since 1.5.0
		 */
		XKB_CONTEXT_NO_SECURE_GETENV = (1 << 2)
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct xkb_rule_names : IDisposable
	{
		public xkb_rule_names(FramebufferHostBuilder.XKBKeymapParams keymapParams) : this(keymapParams.model, keymapParams.rules, keymapParams.layout, keymapParams.variant, keymapParams.options) { }

		public xkb_rule_names(string? model = null, string? rules = null, string? layout = null, string? variant = null, string? options = null)
		{
			this.model = (byte*)Marshal.StringToHGlobalAnsi(model);
			this.rules = (byte*)Marshal.StringToHGlobalAnsi(rules);
			this.layout = (byte*)Marshal.StringToHGlobalAnsi(layout);
			this.variant = (byte*)Marshal.StringToHGlobalAnsi(variant);
			this.options = (byte*)Marshal.StringToHGlobalAnsi(options);
		}

		public void Dispose()
		{
			Marshal.FreeHGlobal((IntPtr)model);
			Marshal.FreeHGlobal((IntPtr)rules);
			Marshal.FreeHGlobal((IntPtr)layout);
			Marshal.FreeHGlobal((IntPtr)variant);
			Marshal.FreeHGlobal((IntPtr)options);
		}

		/**
		 * The rules file to use. The rules file describes how to interpret
		 * the values of the model, layout, variant and options fields.
		 *
		 * If NULL or the empty string "", a default value is used.
		 * If the XKB_DEFAULT_RULES environment variable is set, it is used
		 * as the default.  Otherwise the system default is used.
		 */
		byte* rules;
		/**
		 * The keyboard model by which to interpret keycodes and LEDs.
		 *
		 * If NULL or the empty string "", a default value is used.
		 * If the XKB_DEFAULT_MODEL environment variable is set, it is used
		 * as the default.  Otherwise the system default is used.
		 */
		byte* model;
		/**
		 * A comma separated list of layouts (languages) to include in the
		 * keymap.
		 *
		 * If NULL or the empty string "", a default value is used.
		 * If the XKB_DEFAULT_LAYOUT environment variable is set, it is used
		 * as the default.  Otherwise the system default is used.
		 */
		byte* layout;
		/**
		 * A comma separated list of variants, one per layout, which may
		 * modify or augment the respective layout in various ways.
		 *
		 * Generally, should either be empty or have the same number of values
		 * as the number of layouts. You may use empty values as in "intl,,neo".
		 *
		 * If NULL or the empty string "", and a default value is also used
		 * for the layout, a default value is used.  Otherwise no variant is
		 * used.
		 * If the XKB_DEFAULT_VARIANT environment variable is set, it is used
		 * as the default.  Otherwise the system default is used.
		 */
		byte* variant;
		/**
		 * A comma separated list of options, through which the user specifies
		 * non-layout related preferences, like which key combinations are used
		 * for switching layouts, or which key is the Compose key.
		 *
		 * If NULL, a default value is used.  If the empty string "", no
		 * options are used.
		 * If the XKB_DEFAULT_OPTIONS environment variable is set, it is used
		 * as the default.  Otherwise the system default is used.
		 */
		byte* options;
	}

	public enum xkb_keymap_compile_flags
	{
		/** Do not apply any flags. */
		XKB_KEYMAP_COMPILE_NO_FLAGS = 0
	}

	/** Specifies the direction of the key (press / release). */
	public enum xkb_key_direction
	{
		XKB_KEY_UP,   /**< The key was released. */
		XKB_KEY_DOWN  /**< The key was pressed. */
	}

	/**
	 * Modifier and layout types for state objects.  This enum is bitmaskable,
	 * e.g. (XKB_STATE_MODS_DEPRESSED | XKB_STATE_MODS_LATCHED) is valid to
	 * exclude locked modifiers.
	 *
	 * In XKB, the DEPRESSED components are also known as 'base'.
	 */
	public enum xkb_state_component
	{
		/** Depressed modifiers, i.e. a key is physically holding them. */
		XKB_STATE_MODS_DEPRESSED = (1 << 0),
		/** Latched modifiers, i.e. will be unset after the next non-modifier
		 *  key press. */
		XKB_STATE_MODS_LATCHED = (1 << 1),
		/** Locked modifiers, i.e. will be unset after the key provoking the
		 *  lock has been pressed again. */
		XKB_STATE_MODS_LOCKED = (1 << 2),
		/** Effective modifiers, i.e. currently active and affect key
		 *  processing (derived from the other state components).
		 *  Use this unless you explicitly care how the state came about. */
		XKB_STATE_MODS_EFFECTIVE = (1 << 3),
		/** Depressed layout, i.e. a key is physically holding it. */
		XKB_STATE_LAYOUT_DEPRESSED = (1 << 4),
		/** Latched layout, i.e. will be unset after the next non-modifier
		 *  key press. */
		XKB_STATE_LAYOUT_LATCHED = (1 << 5),
		/** Locked layout, i.e. will be unset after the key provoking the lock
		 *  has been pressed again. */
		XKB_STATE_LAYOUT_LOCKED = (1 << 6),
		/** Effective layout, i.e. currently active and affects key processing
		 *  (derived from the other state components).
		 *  Use this unless you explicitly care how the state came about. */
		XKB_STATE_LAYOUT_EFFECTIVE = (1 << 7),
		/** LEDs (derived from the other state components). */
		XKB_STATE_LEDS = (1 << 8)
	};
}

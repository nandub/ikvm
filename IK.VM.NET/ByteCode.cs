/*
  Copyright (C) 2002 Jeroen Frijters

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jeroen Frijters
  jeroen@frijters.net
  
*/
using System;

enum ByteCode
{
	__nop = 0,
	__aconst_null = 1,
	__iconst_m1 = 2,
	__iconst_0 = 3,
	__iconst_1 = 4,
	__iconst_2 = 5,
	__iconst_3 = 6,
	__iconst_4 = 7,
	__iconst_5 = 8,
	__lconst_0 = 9,
	__lconst_1 = 10,
	__fconst_0 = 11,
	__fconst_1 = 12,
	__fconst_2 = 13,
	__dconst_0 = 14,
	__dconst_1 = 15,
	__bipush = 16,
	__sipush = 17,
	__ldc = 18,
	__ldc_w = 19,
	__ldc2_w = 20,
	__iload = 21,
	__lload = 22,
	__fload = 23,
	__dload = 24,
	__aload = 25,
	__iload_0 = 26,
	__iload_1 = 27,
	__iload_2 = 28,
	__iload_3 = 29,
	__lload_0 = 30,
	__lload_1 = 31,
	__lload_2 = 32,
	__lload_3 = 33,
	__fload_0 = 34,
	__fload_1 = 35,
	__fload_2 = 36,
	__fload_3 = 37,
	__dload_0 = 38,
	__dload_1 = 39,
	__dload_2 = 40,
	__dload_3 = 41,
	__aload_0 = 42,
	__aload_1 = 43,
	__aload_2 = 44,
	__aload_3 = 45,
	__iaload = 46,
	__laload = 47,
	__faload = 48,
	__daload = 49,
	__aaload = 50,
	__baload = 51,
	__caload = 52,
	__saload = 53,
	__istore = 54,
	__lstore = 55,
	__fstore = 56,
	__dstore = 57,
	__astore = 58,
	__istore_0 = 59,
	__istore_1 = 60,
	__istore_2 = 61,
	__istore_3 = 62,
	__lstore_0 = 63,
	__lstore_1 = 64,
	__lstore_2 = 65,
	__lstore_3 = 66,
	__fstore_0 = 67,
	__fstore_1 = 68,
	__fstore_2 = 69,
	__fstore_3 = 70,
	__dstore_0 = 71,
	__dstore_1 = 72,
	__dstore_2 = 73,
	__dstore_3 = 74,
	__astore_0 = 75,
	__astore_1 = 76,
	__astore_2 = 77,
	__astore_3 = 78,
	__iastore = 79,
	__lastore = 80,
	__fastore = 81,
	__dastore = 82,
	__aastore = 83,
	__bastore = 84,
	__castore = 85,
	__sastore = 86,
	__pop = 87,
	__pop2 = 88,
	__dup = 89,
	__dup_x1 = 90,
	__dup_x2 = 91,
	__dup2 = 92,
	__dup2_x1 = 93,
	__dup2_x2 = 94,
	__swap = 95,
	__iadd = 96,
	__ladd = 97,
	__fadd = 98,
	__dadd = 99,
	__isub = 100,
	__lsub = 101,
	__fsub = 102,
	__dsub = 103,
	__imul = 104,
	__lmul = 105,
	__fmul = 106,
	__dmul = 107,
	__idiv = 108,
	__ldiv = 109,
	__fdiv = 110,
	__ddiv = 111,
	__irem = 112,
	__lrem = 113,
	__frem = 114,
	__drem = 115,
	__ineg = 116,
	__lneg = 117,
	__fneg = 118,
	__dneg = 119,
	__ishl = 120,
	__lshl = 121,
	__ishr = 122,
	__lshr = 123,
	__iushr = 124,
	__lushr = 125,
	__iand = 126,
	__land = 127,
	__ior = 128,
	__lor = 129,
	__ixor = 130,
	__lxor = 131,
	__iinc = 132,
	__i2l = 133,
	__i2f = 134,
	__i2d = 135,
	__l2i = 136,
	__l2f = 137,
	__l2d = 138,
	__f2i = 139,
	__f2l = 140,
	__f2d = 141,
	__d2i = 142,
	__d2l = 143,
	__d2f = 144,
	__i2b = 145,
	__i2c = 146,
	__i2s = 147,
	__lcmp = 148,
	__fcmpl = 149,
	__fcmpg = 150,
	__dcmpl = 151,
	__dcmpg = 152,
	__ifeq = 153,
	__ifne = 154,
	__iflt = 155,
	__ifge = 156,
	__ifgt = 157,
	__ifle = 158,
	__if_icmpeq = 159,
	__if_icmpne = 160,
	__if_icmplt = 161,
	__if_icmpge = 162,
	__if_icmpgt = 163,
	__if_icmple = 164,
	__if_acmpeq = 165,
	__if_acmpne = 166,
	__goto = 167,
	__jsr = 168,
	__ret = 169,
	__tableswitch = 170,
	__lookupswitch = 171,
	__ireturn = 172,
	__lreturn = 173,
	__freturn = 174,
	__dreturn = 175,
	__areturn = 176,
	__return = 177,
	__getstatic = 178,
	__putstatic = 179,
	__getfield = 180,
	__putfield = 181,
	__invokevirtual = 182,
	__invokespecial = 183,
	__invokestatic = 184,
	__invokeinterface = 185,
	__xxxunusedxxx = 186,
	__new = 187,
	__newarray = 188,
	__anewarray = 189,
	__arraylength = 190,
	__athrow = 191,
	__checkcast = 192,
	__instanceof = 193,
	__monitorenter = 194,
	__monitorexit = 195,
	__wide = 196,
	__multianewarray = 197,
	__ifnull = 198,
	__ifnonnull = 199,
	__goto_w = 200,
	__jsr_w = 201
}

enum NormalizedByteCode
{
	__nop = 0,
	__aconst_null = 1,
	__iconst = -1,
	__lconst_0 = 9,
	__lconst_1 = 10,
	__fconst_0 = 11,
	__fconst_1 = 12,
	__fconst_2 = 13,
	__dconst_0 = 14,
	__dconst_1 = 15,
	__ldc = 18,
	__iload = 21,
	__lload = 22,
	__fload = 23,
	__dload = 24,
	__aload = 25,
	__iaload = 46,
	__laload = 47,
	__faload = 48,
	__daload = 49,
	__aaload = 50,
	__baload = 51,
	__caload = 52,
	__saload = 53,
	__istore = 54,
	__lstore = 55,
	__fstore = 56,
	__dstore = 57,
	__astore = 58,
	__iastore = 79,
	__lastore = 80,
	__fastore = 81,
	__dastore = 82,
	__aastore = 83,
	__bastore = 84,
	__castore = 85,
	__sastore = 86,
	__pop = 87,
	__pop2 = 88,
	__dup = 89,
	__dup_x1 = 90,
	__dup_x2 = 91,
	__dup2 = 92,
	__dup2_x1 = 93,
	__dup2_x2 = 94,
	__swap = 95,
	__iadd = 96,
	__ladd = 97,
	__fadd = 98,
	__dadd = 99,
	__isub = 100,
	__lsub = 101,
	__fsub = 102,
	__dsub = 103,
	__imul = 104,
	__lmul = 105,
	__fmul = 106,
	__dmul = 107,
	__idiv = 108,
	__ldiv = 109,
	__fdiv = 110,
	__ddiv = 111,
	__irem = 112,
	__lrem = 113,
	__frem = 114,
	__drem = 115,
	__ineg = 116,
	__lneg = 117,
	__fneg = 118,
	__dneg = 119,
	__ishl = 120,
	__lshl = 121,
	__ishr = 122,
	__lshr = 123,
	__iushr = 124,
	__lushr = 125,
	__iand = 126,
	__land = 127,
	__ior = 128,
	__lor = 129,
	__ixor = 130,
	__lxor = 131,
	__iinc = 132,
	__i2l = 133,
	__i2f = 134,
	__i2d = 135,
	__l2i = 136,
	__l2f = 137,
	__l2d = 138,
	__f2i = 139,
	__f2l = 140,
	__f2d = 141,
	__d2i = 142,
	__d2l = 143,
	__d2f = 144,
	__i2b = 145,
	__i2c = 146,
	__i2s = 147,
	__lcmp = 148,
	__fcmpl = 149,
	__fcmpg = 150,
	__dcmpl = 151,
	__dcmpg = 152,
	__ifeq = 153,
	__ifne = 154,
	__iflt = 155,
	__ifge = 156,
	__ifgt = 157,
	__ifle = 158,
	__if_icmpeq = 159,
	__if_icmpne = 160,
	__if_icmplt = 161,
	__if_icmpge = 162,
	__if_icmpgt = 163,
	__if_icmple = 164,
	__if_acmpeq = 165,
	__if_acmpne = 166,
	__goto = 167,
	__jsr = 168,
	__ret = 169,
	__lookupswitch = 171,
	__ireturn = 172,
	__lreturn = 173,
	__freturn = 174,
	__dreturn = 175,
	__areturn = 176,
	__return = 177,
	__getstatic = 178,
	__putstatic = 179,
	__getfield = 180,
	__putfield = 181,
	__invokevirtual = 182,
	__invokespecial = 183,
	__invokestatic = 184,
	__invokeinterface = 185,
	__new = 187,
	__newarray = 188,
	__anewarray = 189,
	__arraylength = 190,
	__athrow = 191,
	__checkcast = 192,
	__instanceof = 193,
	__monitorenter = 194,
	__monitorexit = 195,
	__multianewarray = 197,
	__ifnull = 198,
	__ifnonnull = 199,
}

enum ByteCodeMode
{
	Unused,
	Simple,
	Constant_1,
	Constant_2,
	Branch_2,
	Local_1,
	Local_2,
	Constant_2_1_1,
	Immediate_1,
	Immediate_2,
	Local_1_Immediate_1,
	Local_2_Immediate_2,
	Tableswitch,
	Lookupswitch,
	Constant_2_Immediate_1,
	Branch_4
}

struct ByteCodeMetaData
{
	private static ByteCodeMetaData[] data = new ByteCodeMetaData[202];
	private ByteCodeMode reg;
	private ByteCodeMode wide;
	private NormalizedByteCode normbc;
	private int arg;
	private bool hasFixedArg;

	private ByteCodeMetaData(ByteCode bc, ByteCodeMode reg, ByteCodeMode wide)
	{
		this.reg = reg;
		this.wide = wide;
		this.normbc = (NormalizedByteCode)Enum.Parse(typeof(NormalizedByteCode), bc.ToString());
		this.arg = 0;
		this.hasFixedArg = false;
		data[(int)bc] = this;
	}

	private ByteCodeMetaData(ByteCode bc, NormalizedByteCode normbc, ByteCodeMode reg, ByteCodeMode wide)
	{
		this.reg = reg;
		this.wide = wide;
		this.normbc = normbc;
		this.arg = 0;
		this.hasFixedArg = false;
		data[(int)bc] = this;
	}

	private ByteCodeMetaData(ByteCode bc, NormalizedByteCode normbc, int arg, ByteCodeMode reg, ByteCodeMode wide)
	{
		this.reg = reg;
		this.wide = wide;
		this.normbc = normbc;
		this.arg = arg;
		this.hasFixedArg = true;
		data[(int)bc] = this;
	}

	internal static NormalizedByteCode GetNormalizedByteCode(ByteCode bc)
	{
		return data[(int)bc].normbc;
	}

	internal static int GetArg(ByteCode bc, int arg)
	{
		if(data[(int)bc].hasFixedArg)
		{
			return data[(int)bc].arg;
		}
		return arg;
	}

	internal static ByteCodeMode GetMode(ByteCode bc)
	{
		return data[(int)bc].reg;
	}

	internal static ByteCodeMode GetWideMode(ByteCode bc)
	{
		ByteCodeMode m = data[(int)bc].wide;
		if(m == ByteCodeMode.Unused)
		{
			throw new ArgumentException("Wide version of " + bc + " is not a valid instruction", "bc");
		}
		return m;
	}

	static ByteCodeMetaData()
	{
		new ByteCodeMetaData(ByteCode.__nop, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__aconst_null, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iconst_m1, NormalizedByteCode.__iconst, -1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iconst_0, NormalizedByteCode.__iconst, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iconst_1, NormalizedByteCode.__iconst, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iconst_2, NormalizedByteCode.__iconst, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iconst_3, NormalizedByteCode.__iconst, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iconst_4, NormalizedByteCode.__iconst, 4, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iconst_5, NormalizedByteCode.__iconst, 5, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lconst_0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lconst_1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fconst_0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fconst_1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fconst_2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dconst_0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dconst_1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__bipush, NormalizedByteCode.__iconst, ByteCodeMode.Immediate_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__sipush, NormalizedByteCode.__iconst, ByteCodeMode.Immediate_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ldc, ByteCodeMode.Constant_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ldc_w, NormalizedByteCode.__ldc, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ldc2_w, NormalizedByteCode.__ldc, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iload, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lload, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fload, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dload, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__aload, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iload_0, NormalizedByteCode.__iload, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iload_1, NormalizedByteCode.__iload, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iload_2, NormalizedByteCode.__iload, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iload_3, NormalizedByteCode.__iload, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lload_0, NormalizedByteCode.__lload, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lload_1, NormalizedByteCode.__lload, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lload_2, NormalizedByteCode.__lload, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lload_3, NormalizedByteCode.__lload, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fload_0, NormalizedByteCode.__fload, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fload_1, NormalizedByteCode.__fload, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fload_2, NormalizedByteCode.__fload, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fload_3, NormalizedByteCode.__fload, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dload_0, NormalizedByteCode.__dload, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dload_1, NormalizedByteCode.__dload, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dload_2, NormalizedByteCode.__dload, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dload_3, NormalizedByteCode.__dload, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__aload_0, NormalizedByteCode.__aload, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__aload_1, NormalizedByteCode.__aload, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__aload_2, NormalizedByteCode.__aload, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__aload_3, NormalizedByteCode.__aload, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iaload, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__laload, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__faload, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__daload, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__aaload, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__baload, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__caload, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__saload, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__istore, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lstore, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fstore, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dstore, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__astore, ByteCodeMode.Local_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__istore_0, NormalizedByteCode.__istore, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__istore_1, NormalizedByteCode.__istore, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__istore_2, NormalizedByteCode.__istore, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__istore_3, NormalizedByteCode.__istore, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lstore_0, NormalizedByteCode.__lstore, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lstore_1, NormalizedByteCode.__lstore, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lstore_2, NormalizedByteCode.__lstore, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lstore_3, NormalizedByteCode.__lstore, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fstore_0, NormalizedByteCode.__fstore, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fstore_1, NormalizedByteCode.__fstore, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fstore_2, NormalizedByteCode.__fstore, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fstore_3, NormalizedByteCode.__fstore, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dstore_0, NormalizedByteCode.__dstore, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dstore_1, NormalizedByteCode.__dstore, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dstore_2, NormalizedByteCode.__dstore, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dstore_3, NormalizedByteCode.__dstore, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__astore_0, NormalizedByteCode.__astore, 0, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__astore_1, NormalizedByteCode.__astore, 1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__astore_2, NormalizedByteCode.__astore, 2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__astore_3, NormalizedByteCode.__astore, 3, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iastore, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lastore, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fastore, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dastore, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__aastore, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__bastore, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__castore, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__sastore, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__pop, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__pop2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dup, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dup_x1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dup_x2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dup2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dup2_x1, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dup2_x2, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__swap, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iadd, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ladd, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fadd, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dadd, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__isub, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lsub, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fsub, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dsub, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__imul, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lmul, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fmul, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dmul, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__idiv, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ldiv, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fdiv, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ddiv, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__irem, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lrem, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__frem, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__drem, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ineg, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lneg, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fneg, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dneg, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ishl, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lshl, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ishr, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lshr, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iushr, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lushr, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iand, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__land, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ior, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lor, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ixor, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lxor, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iinc, ByteCodeMode.Local_1_Immediate_1, ByteCodeMode.Local_2_Immediate_2);
		new ByteCodeMetaData(ByteCode.__i2l, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__i2f, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__i2d, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__l2i, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__l2f, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__l2d, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__f2i, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__f2l, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__f2d, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__d2i, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__d2l, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__d2f, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__i2b, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__i2c, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__i2s, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lcmp, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fcmpl, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__fcmpg, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dcmpl, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dcmpg, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ifeq, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ifne, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__iflt, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ifge, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ifgt, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ifle, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__if_icmpeq, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__if_icmpne, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__if_icmplt, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__if_icmpge, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__if_icmpgt, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__if_icmple, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__if_acmpeq, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__if_acmpne, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__goto, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__jsr, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ret, ByteCodeMode.Local_1, ByteCodeMode.Local_2);
		new ByteCodeMetaData(ByteCode.__tableswitch, NormalizedByteCode.__lookupswitch, ByteCodeMode.Tableswitch, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lookupswitch, ByteCodeMode.Lookupswitch, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ireturn, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__lreturn, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__freturn, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__dreturn, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__areturn, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__return, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__getstatic, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__putstatic, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__getfield, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__putfield, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__invokevirtual, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__invokespecial, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__invokestatic, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__invokeinterface, ByteCodeMode.Constant_2_1_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__xxxunusedxxx, NormalizedByteCode.__nop, ByteCodeMode.Unused, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__new, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__newarray, ByteCodeMode.Immediate_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__anewarray, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__arraylength, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__athrow, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__checkcast, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__instanceof, ByteCodeMode.Constant_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__monitorenter, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__monitorexit, ByteCodeMode.Simple, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__wide, NormalizedByteCode.__nop, ByteCodeMode.Unused, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__multianewarray, ByteCodeMode.Constant_2_Immediate_1, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ifnull, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__ifnonnull, ByteCodeMode.Branch_2, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__goto_w, NormalizedByteCode.__goto, ByteCodeMode.Branch_4, ByteCodeMode.Unused);
		new ByteCodeMetaData(ByteCode.__jsr_w, NormalizedByteCode.__jsr, ByteCodeMode.Branch_4, ByteCodeMode.Unused);
	}
}

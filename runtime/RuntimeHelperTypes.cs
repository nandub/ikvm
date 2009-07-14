/*
  Copyright (C) 2009 Jeroen Frijters

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
using System.Reflection;
#if IKVM_REF_EMIT
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif
using System.Diagnostics;

namespace IKVM.Internal
{
	static class RuntimeHelperTypes
	{
		private static Type classLiteralType;
		private static FieldInfo classLiteralField;

		internal static FieldInfo GetClassLiteralField(Type type)
		{
			Debug.Assert(type != typeof(void));
			if (classLiteralType == null)
			{
#if STATIC_COMPILER
				classLiteralType = JVM.CoreAssembly.GetType("ikvm.internal.ClassLiteral`1");
#elif !FIRST_PASS
				classLiteralType = typeof(ikvm.@internal.ClassLiteral<>);
#endif
			}
#if !IKVM_REF_EMIT
			if (!IsTypeBuilder(type))
			{
				return classLiteralType.MakeGenericType(type).GetField("Value", BindingFlags.Public | BindingFlags.Static);
			}
#endif
			if (classLiteralField == null)
			{
				classLiteralField = classLiteralType.GetField("Value", BindingFlags.Public | BindingFlags.Static);
			}
#if IKVM_REF_EMIT
			// MONOBUG https://bugzilla.novell.com/show_bug.cgi?id=486307
			return TypeBuilder.GetField(MonoHackGenericType.Make(classLiteralType, type), classLiteralField);
#else
			return TypeBuilder.GetField(classLiteralType.MakeGenericType(type), classLiteralField);
#endif
		}

		private static bool IsTypeBuilder(Type type)
		{
			return type is TypeBuilder || (type.HasElementType && IsTypeBuilder(type.GetElementType()));
		}

#if STATIC_COMPILER
		internal static void Create(CompilerClassLoader ccl)
		{
			EmitClassLiteral(ccl);
		}

		private static void EmitClassLiteral(CompilerClassLoader ccl)
		{
			TypeBuilder tb = ccl.GetTypeWrapperFactory().ModuleBuilder.DefineType("ikvm.internal.ClassLiteral`1", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.Class | TypeAttributes.BeforeFieldInit);
			GenericTypeParameterBuilder typeParam = tb.DefineGenericParameters("T")[0];
			Type classType = CoreClasses.java.lang.Class.Wrapper.TypeAsSignatureType;
			classLiteralField = tb.DefineField("Value", classType, FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly);
			ConstructorBuilder cctor = tb.DefineTypeInitializer();
			CodeEmitter ilgen = CodeEmitter.Create(cctor);
			ilgen.Emit(OpCodes.Ldtoken, typeParam);
			ilgen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) }));
			MethodWrapper mw = CoreClasses.java.lang.Class.Wrapper.GetMethodWrapper("<init>", "(Lcli.System.Type;)V", false);
			mw.Link();
			mw.EmitNewobj(ilgen);
			ilgen.Emit(OpCodes.Stsfld, classLiteralField);
			ilgen.Emit(OpCodes.Ret);
			classLiteralType = tb.CreateType();
		}
#endif
	}
}

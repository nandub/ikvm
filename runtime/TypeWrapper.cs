/*
  Copyright (C) 2002-2009 Jeroen Frijters

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
using System.Collections.Generic;
using System.Reflection;
#if IKVM_REF_EMIT
using IKVM.Reflection.Emit;
#else
using System.Reflection.Emit;
#endif
using System.Diagnostics;
using System.Security;
using System.Security.Permissions;
using IKVM.Attributes;

namespace IKVM.Internal
{
	struct ExModifiers
	{
		internal readonly Modifiers Modifiers;
		internal readonly bool IsInternal;

		internal ExModifiers(Modifiers modifiers, bool isInternal)
		{
			this.Modifiers = modifiers;
			this.IsInternal = isInternal;
		}
	}

	static class AttributeHelper
	{
		private static CustomAttributeBuilder hideFromJavaAttribute;
#if STATIC_COMPILER
		private static CustomAttributeBuilder ghostInterfaceAttribute;
		private static CustomAttributeBuilder deprecatedAttribute;
		private static CustomAttributeBuilder editorBrowsableNever;
		private static ConstructorInfo implementsAttribute;
		private static ConstructorInfo throwsAttribute;
		private static ConstructorInfo sourceFileAttribute;
		private static ConstructorInfo lineNumberTableAttribute1;
		private static ConstructorInfo lineNumberTableAttribute2;
		private static ConstructorInfo enclosingMethodAttribute;
		private static ConstructorInfo signatureAttribute;
		private static CustomAttributeBuilder paramArrayAttribute;
		private static ConstructorInfo nonNestedInnerClassAttribute;
		private static ConstructorInfo nonNestedOuterClassAttribute;
		private static Type typeofModifiers = JVM.LoadType(typeof(Modifiers));
		private static Type typeofSourceFileAttribute = JVM.LoadType(typeof(SourceFileAttribute));
		private static Type typeofLineNumberTableAttribute = JVM.LoadType(typeof(LineNumberTableAttribute));
#endif // STATIC_COMPILER
		private static Type typeofRemappedClassAttribute = JVM.LoadType(typeof(RemappedClassAttribute));
		private static Type typeofRemappedTypeAttribute = JVM.LoadType(typeof(RemappedTypeAttribute));
		private static Type typeofModifiersAttribute = JVM.LoadType(typeof(ModifiersAttribute));
		private static Type typeofRemappedInterfaceMethodAttribute = JVM.LoadType(typeof(RemappedInterfaceMethodAttribute));
		private static Type typeofNameSigAttribute = JVM.LoadType(typeof(NameSigAttribute));
		private static Type typeofJavaModuleAttribute = JVM.LoadType(typeof(JavaModuleAttribute));
		private static Type typeofSignatureAttribute = JVM.LoadType(typeof(SignatureAttribute));
		private static Type typeofInnerClassAttribute = JVM.LoadType(typeof(InnerClassAttribute));
		private static Type typeofImplementsAttribute = JVM.LoadType(typeof(ImplementsAttribute));
		private static Type typeofGhostInterfaceAttribute = JVM.LoadType(typeof(GhostInterfaceAttribute));
		private static Type typeofExceptionIsUnsafeForMappingAttribute = JVM.LoadType(typeof(ExceptionIsUnsafeForMappingAttribute));
		private static Type typeofThrowsAttribute = JVM.LoadType(typeof(ThrowsAttribute));
		private static Type typeofHideFromReflectionAttribute = JVM.LoadType(typeof(HideFromReflectionAttribute));
		private static Type typeofHideFromJavaAttribute = JVM.LoadType(typeof(HideFromJavaAttribute));
		private static Type typeofNoPackagePrefixAttribute = JVM.LoadType(typeof(NoPackagePrefixAttribute));
		private static Type typeofConstantValueAttribute = JVM.LoadType(typeof(ConstantValueAttribute));
		private static Type typeofAnnotationAttributeAttribute = JVM.LoadType(typeof(AnnotationAttributeAttribute));
		private static Type typeofNonNestedInnerClassAttribute = JVM.LoadType(typeof(NonNestedInnerClassAttribute));
		private static Type typeofNonNestedOuterClassAttribute = JVM.LoadType(typeof(NonNestedOuterClassAttribute));
		private static Type typeofEnclosingMethodAttribute = JVM.LoadType(typeof(EnclosingMethodAttribute));

#if STATIC_COMPILER
		private static object ParseValue(ClassLoaderWrapper loader, TypeWrapper tw, string val)
		{
			if(tw == CoreClasses.java.lang.String.Wrapper)
			{
				return val;
			}
			else if(tw.TypeAsTBD.IsEnum)
			{
				if(tw.TypeAsTBD.Assembly.ReflectionOnly)
				{
					// TODO implement full parsing semantics
					FieldInfo field = tw.TypeAsTBD.GetField(val);
					if(field == null)
					{
						throw new NotImplementedException("Parsing enum value: " + val);
					}
					return field.GetRawConstantValue();
				}
				return Enum.Parse(tw.TypeAsTBD, val);
			}
			else if(tw.TypeAsTBD == typeof(Type))
			{
				TypeWrapper valtw = loader.LoadClassByDottedNameFast(val);
				if(valtw != null)
				{
					return valtw.TypeAsBaseType;
				}
				return Type.GetType(val, true);
			}
			else if(tw == PrimitiveTypeWrapper.BOOLEAN)
			{
				return bool.Parse(val);
			}
			else if(tw == PrimitiveTypeWrapper.BYTE)
			{
				return (byte)sbyte.Parse(val);
			}
			else if(tw == PrimitiveTypeWrapper.CHAR)
			{
				return char.Parse(val);
			}
			else if(tw == PrimitiveTypeWrapper.SHORT)
			{
				return short.Parse(val);
			}
			else if(tw == PrimitiveTypeWrapper.INT)
			{
				return int.Parse(val);
			}
			else if(tw == PrimitiveTypeWrapper.FLOAT)
			{
				return float.Parse(val);
			}
			else if(tw == PrimitiveTypeWrapper.LONG)
			{
				return long.Parse(val);
			}
			else if(tw == PrimitiveTypeWrapper.DOUBLE)
			{
				return double.Parse(val);
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private static void SetPropertiesAndFields(ClassLoaderWrapper loader, Attribute attrib, IKVM.Internal.MapXml.Attribute attr)
		{
			Type t = attrib.GetType();
			if(attr.Properties != null)
			{
				foreach(IKVM.Internal.MapXml.Param prop in attr.Properties)
				{
					PropertyInfo pi = t.GetProperty(prop.Name);
					pi.SetValue(attrib, ParseValue(loader, ClassFile.FieldTypeWrapperFromSig(loader, prop.Sig), prop.Value), null);
				}
			}
			if(attr.Fields != null)
			{
				foreach(IKVM.Internal.MapXml.Param field in attr.Fields)
				{
					FieldInfo fi = t.GetField(field.Name);
					fi.SetValue(attrib, ParseValue(loader, ClassFile.FieldTypeWrapperFromSig(loader, field.Sig), field.Value));
				}
			}
		}

		internal static Attribute InstantiatePseudoCustomAttribute(ClassLoaderWrapper loader, IKVM.Internal.MapXml.Attribute attr)
		{
			Type t = StaticCompiler.GetType(attr.Type);
			Type[] argTypes;
			object[] args;
			GetAttributeArgsAndTypes(loader, attr, out argTypes, out args);
			ConstructorInfo ci = t.GetConstructor(argTypes);
			Attribute attrib = ci.Invoke(args) as Attribute;
			SetPropertiesAndFields(loader, attrib, attr);
			return attrib;
		}

		private static bool IsDeclarativeSecurityAttribute(ClassLoaderWrapper loader, IKVM.Internal.MapXml.Attribute attr, out SecurityAction action, out PermissionSet pset)
		{
			action = SecurityAction.Demand;
			pset = null;
			if(attr.Type != null)
			{
				Type t = StaticCompiler.GetType(attr.Type);
				if(typeof(SecurityAttribute).IsAssignableFrom(t))
				{
					Type[] argTypes;
					object[] args;
					GetAttributeArgsAndTypes(loader, attr, out argTypes, out args);
					ConstructorInfo ci = t.GetConstructor(argTypes);
					SecurityAttribute attrib = ci.Invoke(args) as SecurityAttribute;
					SetPropertiesAndFields(loader, attrib, attr);
					action = attrib.Action;
					pset = new PermissionSet(PermissionState.None);
					pset.AddPermission(attrib.CreatePermission());
					return true;
				}
			}
			return false;
		}

		internal static void SetCustomAttribute(ClassLoaderWrapper loader, TypeBuilder tb, IKVM.Internal.MapXml.Attribute attr)
		{
			SecurityAction action;
			PermissionSet pset;
			if(IsDeclarativeSecurityAttribute(loader, attr, out action, out pset))
			{
				tb.AddDeclarativeSecurity(action, pset);
			}
			else
			{
				tb.SetCustomAttribute(CreateCustomAttribute(loader, attr));
			}
		}

		internal static void SetCustomAttribute(ClassLoaderWrapper loader, FieldBuilder fb, IKVM.Internal.MapXml.Attribute attr)
		{
			fb.SetCustomAttribute(CreateCustomAttribute(loader, attr));
		}

		internal static void SetCustomAttribute(ClassLoaderWrapper loader, ParameterBuilder pb, IKVM.Internal.MapXml.Attribute attr)
		{
			pb.SetCustomAttribute(CreateCustomAttribute(loader, attr));
		}

		internal static void SetCustomAttribute(ClassLoaderWrapper loader, MethodBuilder mb, IKVM.Internal.MapXml.Attribute attr)
		{
			SecurityAction action;
			PermissionSet pset;
			if(IsDeclarativeSecurityAttribute(loader, attr, out action, out pset))
			{
				mb.AddDeclarativeSecurity(action, pset);
			}
			else
			{
				mb.SetCustomAttribute(CreateCustomAttribute(loader, attr));
			}
		}

		internal static void SetCustomAttribute(ClassLoaderWrapper loader, ConstructorBuilder cb, IKVM.Internal.MapXml.Attribute attr)
		{
			SecurityAction action;
			PermissionSet pset;
			if(IsDeclarativeSecurityAttribute(loader, attr, out action, out pset))
			{
				cb.AddDeclarativeSecurity(action, pset);
			}
			else
			{
				cb.SetCustomAttribute(CreateCustomAttribute(loader, attr));
			}
		}

		internal static void SetCustomAttribute(ClassLoaderWrapper loader, PropertyBuilder pb, IKVM.Internal.MapXml.Attribute attr)
		{
			pb.SetCustomAttribute(CreateCustomAttribute(loader, attr));
		}

		internal static void SetCustomAttribute(ClassLoaderWrapper loader, AssemblyBuilder ab, IKVM.Internal.MapXml.Attribute attr)
		{
			ab.SetCustomAttribute(CreateCustomAttribute(loader, attr));
		}

		private static void GetAttributeArgsAndTypes(ClassLoaderWrapper loader, IKVM.Internal.MapXml.Attribute attr, out Type[] argTypes, out object[] args)
		{
			// TODO add error handling
			TypeWrapper[] twargs = ClassFile.ArgTypeWrapperListFromSig(loader, attr.Sig);
			argTypes = new Type[twargs.Length];
			args = new object[argTypes.Length];
			for(int i = 0; i < twargs.Length; i++)
			{
				argTypes[i] = twargs[i].TypeAsSignatureType;
				TypeWrapper tw = twargs[i];
				if(tw == CoreClasses.java.lang.Object.Wrapper)
				{
					tw = ClassFile.FieldTypeWrapperFromSig(loader, attr.Params[i].Sig);
				}
				if(tw.IsArray)
				{
					Array arr = Array.CreateInstance(tw.ElementTypeWrapper.TypeAsArrayType, attr.Params[i].Elements.Length);
					for(int j = 0; j < arr.Length; j++)
					{
						arr.SetValue(ParseValue(loader, tw.ElementTypeWrapper, attr.Params[i].Elements[j].Value), j);
					}
					args[i] = arr;
				}
				else
				{
					args[i] = ParseValue(loader, tw, attr.Params[i].Value);
				}
			}
		}

		private static CustomAttributeBuilder CreateCustomAttribute(ClassLoaderWrapper loader, IKVM.Internal.MapXml.Attribute attr)
		{
			// TODO add error handling
			Type[] argTypes;
			object[] args;
			GetAttributeArgsAndTypes(loader, attr, out argTypes, out args);
			if(attr.Type != null)
			{
				Type t = StaticCompiler.GetType(attr.Type);
				if(typeof(SecurityAttribute).IsAssignableFrom(t))
				{
					throw new NotImplementedException("Declarative SecurityAttribute support not implemented");
				}
				ConstructorInfo ci = t.GetConstructor(argTypes);
				if(ci == null)
				{
					throw new InvalidOperationException(string.Format("Constructor missing: {0}::<init>{1}", attr.Class, attr.Sig));
				}
				PropertyInfo[] namedProperties;
				object[] propertyValues;
				if(attr.Properties != null)
				{
					namedProperties = new PropertyInfo[attr.Properties.Length];
					propertyValues = new object[attr.Properties.Length];
					for(int i = 0; i < namedProperties.Length; i++)
					{
						namedProperties[i] = t.GetProperty(attr.Properties[i].Name);
						propertyValues[i] = ParseValue(loader, ClassFile.FieldTypeWrapperFromSig(loader, attr.Properties[i].Sig), attr.Properties[i].Value);
					}
				}
				else
				{
					namedProperties = new PropertyInfo[0];
					propertyValues = new object[0];
				}
				FieldInfo[] namedFields;
				object[] fieldValues;
				if(attr.Fields != null)
				{
					namedFields = new FieldInfo[attr.Fields.Length];
					fieldValues = new object[attr.Fields.Length];
					for(int i = 0; i < namedFields.Length; i++)
					{
						namedFields[i] = t.GetField(attr.Fields[i].Name);
						fieldValues[i] = ParseValue(loader, ClassFile.FieldTypeWrapperFromSig(loader, attr.Fields[i].Sig), attr.Fields[i].Value);
					}
				}
				else
				{
					namedFields = new FieldInfo[0];
					fieldValues = new object[0];
				}
				return new CustomAttributeBuilder(ci, args, namedProperties, propertyValues, namedFields, fieldValues);
			}
			else
			{
				if(attr.Properties != null)
				{
					throw new NotImplementedException("Setting property values on Java attributes is not implemented");
				}
				TypeWrapper t = loader.LoadClassByDottedName(attr.Class);
				MethodWrapper mw = t.GetMethodWrapper("<init>", attr.Sig, false);
				mw.Link();
				ConstructorInfo ci = (ConstructorInfo)mw.GetMethod();
				if(ci == null)
				{
					throw new InvalidOperationException(string.Format("Constructor missing: {0}::<init>{1}", attr.Class, attr.Sig));
				}
				FieldInfo[] namedFields;
				object[] fieldValues;
				if(attr.Fields != null)
				{
					namedFields = new FieldInfo[attr.Fields.Length];
					fieldValues = new object[attr.Fields.Length];
					for(int i = 0; i < namedFields.Length; i++)
					{
						FieldWrapper fw = t.GetFieldWrapper(attr.Fields[i].Name, attr.Fields[i].Sig);
						fw.Link();
						namedFields[i] = fw.GetField();
						fieldValues[i] = ParseValue(loader, ClassFile.FieldTypeWrapperFromSig(loader, attr.Fields[i].Sig), attr.Fields[i].Value);
					}
				}
				else
				{
					namedFields = new FieldInfo[0];
					fieldValues = new object[0];
				}
				return new CustomAttributeBuilder(ci, args, namedFields, fieldValues);
			}
		}
#endif

#if STATIC_COMPILER
		internal static void SetEditorBrowsableNever(TypeBuilder tb)
		{
			if(editorBrowsableNever == null)
			{
				editorBrowsableNever = new CustomAttributeBuilder(StaticCompiler.GetType("System.ComponentModel.EditorBrowsableAttribute").GetConstructor(new Type[] { StaticCompiler.GetType("System.ComponentModel.EditorBrowsableState") }), new object[] { (int)System.ComponentModel.EditorBrowsableState.Never });
			}
			tb.SetCustomAttribute(editorBrowsableNever);
		}

		internal static void SetEditorBrowsableNever(MethodBuilder mb)
		{
			if(editorBrowsableNever == null)
			{
				editorBrowsableNever = new CustomAttributeBuilder(StaticCompiler.GetType("System.ComponentModel.EditorBrowsableAttribute").GetConstructor(new Type[] { StaticCompiler.GetType("System.ComponentModel.EditorBrowsableState") }), new object[] { (int)System.ComponentModel.EditorBrowsableState.Never });
			}
			mb.SetCustomAttribute(editorBrowsableNever);
		}

		internal static void SetEditorBrowsableNever(ConstructorBuilder cb)
		{
			if(editorBrowsableNever == null)
			{
				editorBrowsableNever = new CustomAttributeBuilder(StaticCompiler.GetType("System.ComponentModel.EditorBrowsableAttribute").GetConstructor(new Type[] { StaticCompiler.GetType("System.ComponentModel.EditorBrowsableState") }), new object[] { (int)System.ComponentModel.EditorBrowsableState.Never });
			}
			cb.SetCustomAttribute(editorBrowsableNever);
		}

		internal static void SetEditorBrowsableNever(PropertyBuilder pb)
		{
			if(editorBrowsableNever == null)
			{
				editorBrowsableNever = new CustomAttributeBuilder(StaticCompiler.GetType("System.ComponentModel.EditorBrowsableAttribute").GetConstructor(new Type[] { StaticCompiler.GetType("System.ComponentModel.EditorBrowsableState") }), new object[] { (int)System.ComponentModel.EditorBrowsableState.Never });
			}
			pb.SetCustomAttribute(editorBrowsableNever);
		}

		internal static void SetDeprecatedAttribute(MethodBase mb)
		{
			if(deprecatedAttribute == null)
			{
				deprecatedAttribute = new CustomAttributeBuilder(typeof(ObsoleteAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
			}
			MethodBuilder method = mb as MethodBuilder;
			if(method != null)
			{
				method.SetCustomAttribute(deprecatedAttribute);
			}
			else
			{
				((ConstructorBuilder)mb).SetCustomAttribute(deprecatedAttribute);
			}
		}

		internal static void SetDeprecatedAttribute(TypeBuilder tb)
		{
			if(deprecatedAttribute == null)
			{
				deprecatedAttribute = new CustomAttributeBuilder(typeof(ObsoleteAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
			}
			tb.SetCustomAttribute(deprecatedAttribute);
		}

		internal static void SetDeprecatedAttribute(FieldBuilder fb)
		{
			if(deprecatedAttribute == null)
			{
				deprecatedAttribute = new CustomAttributeBuilder(typeof(ObsoleteAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
			}
			fb.SetCustomAttribute(deprecatedAttribute);
		}

		internal static void SetDeprecatedAttribute(PropertyBuilder pb)
		{
			if(deprecatedAttribute == null)
			{
				deprecatedAttribute = new CustomAttributeBuilder(typeof(ObsoleteAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
			}
			pb.SetCustomAttribute(deprecatedAttribute);
		}

		internal static void SetThrowsAttribute(MethodBase mb, string[] exceptions)
		{
			if(exceptions != null && exceptions.Length != 0)
			{
				if(throwsAttribute == null)
				{
					throwsAttribute = typeofThrowsAttribute.GetConstructor(new Type[] { typeof(string[]) });
				}
				if(mb is MethodBuilder)
				{
					MethodBuilder method = (MethodBuilder)mb;
					method.SetCustomAttribute(new CustomAttributeBuilder(throwsAttribute, new object[] { exceptions }));
				}
				else
				{
					ConstructorBuilder constructor = (ConstructorBuilder)mb;
					constructor.SetCustomAttribute(new CustomAttributeBuilder(throwsAttribute, new object[] { exceptions }));
				}
			}
		}

		internal static void SetGhostInterface(TypeBuilder typeBuilder)
		{
			if(ghostInterfaceAttribute == null)
			{
				ghostInterfaceAttribute = new CustomAttributeBuilder(typeofGhostInterfaceAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			}
			typeBuilder.SetCustomAttribute(ghostInterfaceAttribute);
		}

		internal static void SetNonNestedInnerClass(TypeBuilder typeBuilder, string className)
		{
			if(nonNestedInnerClassAttribute == null)
			{
				nonNestedInnerClassAttribute = typeofNonNestedInnerClassAttribute.GetConstructor(new Type[] { typeof(string) });
			}
			typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(nonNestedInnerClassAttribute, new object[] { className }));
		}

		internal static void SetNonNestedOuterClass(TypeBuilder typeBuilder, string className)
		{
			if(nonNestedOuterClassAttribute == null)
			{
				nonNestedOuterClassAttribute = typeofNonNestedOuterClassAttribute.GetConstructor(new Type[] { typeof(string) });
			}
			typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(nonNestedOuterClassAttribute, new object[] { className }));
		}
#endif // STATIC_COMPILER

		internal static void HideFromReflection(MethodBuilder mb)
		{
			CustomAttributeBuilder cab = new CustomAttributeBuilder(typeofHideFromReflectionAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			mb.SetCustomAttribute(cab);
		}

		internal static void HideFromReflection(FieldBuilder fb)
		{
			CustomAttributeBuilder cab = new CustomAttributeBuilder(typeofHideFromReflectionAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			fb.SetCustomAttribute(cab);
		}

		internal static void HideFromReflection(PropertyBuilder pb)
		{
			CustomAttributeBuilder cab = new CustomAttributeBuilder(typeofHideFromReflectionAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			pb.SetCustomAttribute(cab);
		}

		internal static bool IsHideFromReflection(MethodInfo mi)
		{
			return IsDefined(mi, typeofHideFromReflectionAttribute);
		}

		internal static bool IsHideFromReflection(FieldInfo fi)
		{
			return IsDefined(fi, typeofHideFromReflectionAttribute);
		}

		internal static bool IsHideFromReflection(PropertyInfo pi)
		{
			return IsDefined(pi, typeofHideFromReflectionAttribute);
		}

		internal static void HideFromJava(TypeBuilder typeBuilder)
		{
			if(hideFromJavaAttribute == null)
			{
				hideFromJavaAttribute = new CustomAttributeBuilder(typeofHideFromJavaAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			}
			typeBuilder.SetCustomAttribute(hideFromJavaAttribute);
		}

		internal static void HideFromJava(ConstructorBuilder cb)
		{
			if(hideFromJavaAttribute == null)
			{
				hideFromJavaAttribute = new CustomAttributeBuilder(typeofHideFromJavaAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			}
			cb.SetCustomAttribute(hideFromJavaAttribute);
		}

		internal static void HideFromJava(MethodBuilder mb)
		{
			if(hideFromJavaAttribute == null)
			{
				hideFromJavaAttribute = new CustomAttributeBuilder(typeofHideFromJavaAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			}
			mb.SetCustomAttribute(hideFromJavaAttribute);
		}

		internal static void HideFromJava(FieldBuilder fb)
		{
			if(hideFromJavaAttribute == null)
			{
				hideFromJavaAttribute = new CustomAttributeBuilder(typeofHideFromJavaAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			}
			fb.SetCustomAttribute(hideFromJavaAttribute);
		}

#if STATIC_COMPILER
		internal static void HideFromJava(PropertyBuilder pb)
		{
			if(hideFromJavaAttribute == null)
			{
				hideFromJavaAttribute = new CustomAttributeBuilder(typeofHideFromJavaAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			}
			pb.SetCustomAttribute(hideFromJavaAttribute);
		}
#endif // STATIC_COMPILER

		internal static bool IsHideFromJava(Type type)
		{
			return IsDefined(type, typeofHideFromJavaAttribute);
		}

		internal static bool IsHideFromJava(MemberInfo mi)
		{
			// NOTE all privatescope fields and methods are "hideFromJava"
			// because Java cannot deal with the potential name clashes
			FieldInfo fi = mi as FieldInfo;
			if(fi != null && (fi.Attributes & FieldAttributes.FieldAccessMask) == FieldAttributes.PrivateScope)
			{
				return true;
			}
			MethodBase mb = mi as MethodBase;
			if(mb != null && (mb.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope)
			{
				return true;
			}
			return IsDefined(mi, typeofHideFromJavaAttribute);
		}

#if STATIC_COMPILER
		internal static void SetImplementsAttribute(TypeBuilder typeBuilder, TypeWrapper[] ifaceWrappers)
		{
			if(ifaceWrappers != null && ifaceWrappers.Length != 0)
			{
				string[] interfaces = new string[ifaceWrappers.Length];
				for(int i = 0; i < interfaces.Length; i++)
				{
					interfaces[i] = ifaceWrappers[i].Name;
				}
				if(implementsAttribute == null)
				{
					implementsAttribute = typeofImplementsAttribute.GetConstructor(new Type[] { typeof(string[]) });
				}
				typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(implementsAttribute, new object[] { interfaces }));
			}
		}
#endif

		internal static bool IsGhostInterface(Type type)
		{
			return IsDefined(type, typeofGhostInterfaceAttribute);
		}

		internal static bool IsRemappedType(Type type)
		{
			return IsDefined(type, typeofRemappedTypeAttribute);
		}

		internal static bool IsExceptionIsUnsafeForMapping(Type type)
		{
			return IsDefined(type, typeofExceptionIsUnsafeForMappingAttribute);
		}

		// this method compares t1 and t2 by name
		// if the type name and assembly name (ignoring the version and strong name) match
		// the type are considered the same
		private static bool MatchTypes(Type t1, Type t2)
		{
			return t1.FullName == t2.FullName
				&& t1.Assembly.GetName().Name == t2.Assembly.GetName().Name;
		}

		internal static object GetConstantValue(FieldInfo field)
		{
#if !STATIC_COMPILER
			if(!field.DeclaringType.Assembly.ReflectionOnly)
			{
				// In Java, instance fields can also have a ConstantValue attribute so we emulate that
				// with ConstantValueAttribute (for consumption by ikvmstub only)
				object[] attrib = field.GetCustomAttributes(typeof(ConstantValueAttribute), false);
				if(attrib.Length == 1)
				{
					return ((ConstantValueAttribute)attrib[0]).GetConstantValue();
				}
				return null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(field))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofConstantValueAttribute))
					{
						return cad.ConstructorArguments[0].Value;
					}
				}
				return null;
			}
		}

		internal static ModifiersAttribute GetModifiersAttribute(Type type)
		{
#if !STATIC_COMPILER
			if(!type.Assembly.ReflectionOnly)
			{
				object[] attr = type.GetCustomAttributes(typeof(ModifiersAttribute), false);
				return attr.Length == 1 ? (ModifiersAttribute)attr[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(type))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofModifiersAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						if(args.Count == 2)
						{
							return new ModifiersAttribute((Modifiers)args[0].Value, (bool)args[1].Value);
						}
						return new ModifiersAttribute((Modifiers)args[0].Value);
					}
				}
				return null;
			}
		}

		internal static ModifiersAttribute GetModifiersAttribute(PropertyInfo property)
		{
#if !STATIC_COMPILER
			if (!property.DeclaringType.Assembly.ReflectionOnly)
			{
				object[] attr = property.GetCustomAttributes(typeof(ModifiersAttribute), false);
				return attr.Length == 1 ? (ModifiersAttribute)attr[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(property))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofModifiersAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						if(args.Count == 2)
						{
							return new ModifiersAttribute((Modifiers)args[0].Value, (bool)args[1].Value);
						}
						return new ModifiersAttribute((Modifiers)args[0].Value);
					}
				}
				return null;
			}
		}

		internal static ExModifiers GetModifiers(MethodBase mb, bool assemblyIsPrivate)
		{
#if !STATIC_COMPILER
			if(!mb.DeclaringType.Assembly.ReflectionOnly)
			{
				object[] customAttribute = mb.GetCustomAttributes(typeof(ModifiersAttribute), false);
				if(customAttribute.Length == 1)
				{
					ModifiersAttribute mod = (ModifiersAttribute)customAttribute[0];
					return new ExModifiers(mod.Modifiers, mod.IsInternal);
				}
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(mb))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofModifiersAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						if(args.Count == 2)
						{
							return new ExModifiers((Modifiers)args[0].Value, (bool)args[1].Value);
						}
						return new ExModifiers((Modifiers)args[0].Value, false);
					}
				}
			}
			Modifiers modifiers = 0;
			if(mb.IsPublic)
			{
				modifiers |= Modifiers.Public;
			}
			else if(mb.IsPrivate)
			{
				modifiers |= Modifiers.Private;
			}
			else if(mb.IsFamily || mb.IsFamilyOrAssembly)
			{
				modifiers |= Modifiers.Protected;
			}
			else if(assemblyIsPrivate)
			{
				modifiers |= Modifiers.Private;
			}
			// NOTE Java doesn't support non-virtual methods, but we set the Final modifier for
			// non-virtual methods to approximate the semantics
			if((mb.IsFinal || (!mb.IsVirtual && ((modifiers & Modifiers.Private) == 0))) && !mb.IsStatic && !mb.IsConstructor)
			{
				modifiers |= Modifiers.Final;
			}
			if(mb.IsAbstract)
			{
				modifiers |= Modifiers.Abstract;
			}
			else
			{
				// Some .NET interfaces (like System._AppDomain) have synchronized methods,
				// Java doesn't allow synchronized on an abstract methods, so we ignore it for
				// abstract methods.
				if((mb.GetMethodImplementationFlags() & MethodImplAttributes.Synchronized) != 0)
				{
					modifiers |= Modifiers.Synchronized;
				}
			}
			if(mb.IsStatic)
			{
				modifiers |= Modifiers.Static;
			}
			if((mb.Attributes & MethodAttributes.PinvokeImpl) != 0)
			{
				modifiers |= Modifiers.Native;
			}
			ParameterInfo[] parameters = mb.GetParameters();
			if(parameters.Length > 0 && IsDefined(parameters[parameters.Length - 1], typeof(ParamArrayAttribute)))
			{
				modifiers |= Modifiers.VarArgs;
			}
			return new ExModifiers(modifiers, false);
		}

		internal static ExModifiers GetModifiers(FieldInfo fi, bool assemblyIsPrivate)
		{
#if !STATIC_COMPILER
			if(!fi.DeclaringType.Assembly.ReflectionOnly)
			{
				object[] customAttribute = fi.GetCustomAttributes(typeof(ModifiersAttribute), false);
				if(customAttribute.Length == 1)
				{
					ModifiersAttribute mod = (ModifiersAttribute)customAttribute[0];
					return new ExModifiers(mod.Modifiers, mod.IsInternal);
				}
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(fi))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofModifiersAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						if(args.Count == 2)
						{
							return new ExModifiers((Modifiers)args[0].Value, (bool)args[1].Value);
						}
						return new ExModifiers((Modifiers)args[0].Value, false);
					}
				}
			}
			Modifiers modifiers = 0;
			if(fi.IsPublic)
			{
				modifiers |= Modifiers.Public;
			}
			else if(fi.IsPrivate)
			{
				modifiers |= Modifiers.Private;
			}
			else if(fi.IsFamily || fi.IsFamilyOrAssembly)
			{
				modifiers |= Modifiers.Protected;
			}
			else if(assemblyIsPrivate)
			{
				modifiers |= Modifiers.Private;
			}
			if(fi.IsInitOnly || fi.IsLiteral)
			{
				modifiers |= Modifiers.Final;
			}
			if(fi.IsNotSerialized)
			{
				modifiers |= Modifiers.Transient;
			}
			if(fi.IsStatic)
			{
				modifiers |= Modifiers.Static;
			}
			if(Array.IndexOf(fi.GetRequiredCustomModifiers(), typeof(System.Runtime.CompilerServices.IsVolatile)) != -1)
			{
				modifiers |= Modifiers.Volatile;
			}
			return new ExModifiers(modifiers, false);
		}

#if STATIC_COMPILER
		internal static void SetModifiers(MethodBuilder mb, Modifiers modifiers, bool isInternal)
		{
			CustomAttributeBuilder customAttributeBuilder;
			if (isInternal)
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers, typeof(bool) }), new object[] { modifiers, isInternal });
			}
			else
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers }), new object[] { modifiers });
			}
			mb.SetCustomAttribute(customAttributeBuilder);
		}

		internal static void SetModifiers(ConstructorBuilder cb, Modifiers modifiers, bool isInternal)
		{
			CustomAttributeBuilder customAttributeBuilder;
			if (isInternal)
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers, typeof(bool) }), new object[] { modifiers, isInternal });
			}
			else
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers }), new object[] { modifiers });
			}
			cb.SetCustomAttribute(customAttributeBuilder);
		}

		internal static void SetModifiers(FieldBuilder fb, Modifiers modifiers, bool isInternal)
		{
			CustomAttributeBuilder customAttributeBuilder;
			if (isInternal)
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers, typeof(bool) }), new object[] { modifiers, isInternal });
			}
			else
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers }), new object[] { modifiers });
			}
			fb.SetCustomAttribute(customAttributeBuilder);
		}

		internal static void SetModifiers(PropertyBuilder pb, Modifiers modifiers, bool isInternal)
		{
			CustomAttributeBuilder customAttributeBuilder;
			if (isInternal)
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers, typeof(bool) }), new object[] { modifiers, isInternal });
			}
			else
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers }), new object[] { modifiers });
			}
			pb.SetCustomAttribute(customAttributeBuilder);
		}

		internal static void SetModifiers(TypeBuilder tb, Modifiers modifiers, bool isInternal)
		{
			CustomAttributeBuilder customAttributeBuilder;
			if (isInternal)
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers, typeof(bool) }), new object[] { modifiers, isInternal });
			}
			else
			{
				customAttributeBuilder = new CustomAttributeBuilder(typeofModifiersAttribute.GetConstructor(new Type[] { typeofModifiers }), new object[] { modifiers });
			}
			tb.SetCustomAttribute(customAttributeBuilder);
		}

		internal static void SetNameSig(MethodBase mb, string name, string sig)
		{
			CustomAttributeBuilder customAttributeBuilder = new CustomAttributeBuilder(typeofNameSigAttribute.GetConstructor(new Type[] { typeof(string), typeof(string) }), new object[] { name, sig });
			MethodBuilder method = mb as MethodBuilder;
			if(method != null)
			{
				method.SetCustomAttribute(customAttributeBuilder);
			}
			else
			{
				((ConstructorBuilder)mb).SetCustomAttribute(customAttributeBuilder);
			}
		}

		internal static void SetNameSig(FieldBuilder fb, string name, string sig)
		{
			CustomAttributeBuilder customAttributeBuilder = new CustomAttributeBuilder(typeofNameSigAttribute.GetConstructor(new Type[] { typeof(string), typeof(string) }), new object[] { name, sig });
			fb.SetCustomAttribute(customAttributeBuilder);
		}

		internal static byte[] FreezeDryType(Type type)
		{
			System.IO.MemoryStream mem = new System.IO.MemoryStream();
			System.IO.BinaryWriter bw = new System.IO.BinaryWriter(mem, System.Text.UTF8Encoding.UTF8);
			bw.Write((short)1);
			bw.Write(type.FullName);
			bw.Write((short)0);
			return mem.ToArray();
		}

		internal static void SetInnerClass(TypeBuilder typeBuilder, string innerClass, Modifiers modifiers)
		{
			Type[] argTypes = new Type[] { typeof(string), typeofModifiers };
			object[] args = new object[] { innerClass, modifiers };
			ConstructorInfo ci = typeofInnerClassAttribute.GetConstructor(argTypes);
			CustomAttributeBuilder customAttributeBuilder = new CustomAttributeBuilder(ci, args);
			typeBuilder.SetCustomAttribute(customAttributeBuilder);
		}

		internal static void SetSourceFile(TypeBuilder typeBuilder, string filename)
		{
			if(sourceFileAttribute == null)
			{
				sourceFileAttribute = typeofSourceFileAttribute.GetConstructor(new Type[] { typeof(string) });
			}
			typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(sourceFileAttribute, new object[] { filename }));
		}

		internal static void SetSourceFile(ModuleBuilder moduleBuilder, string filename)
		{
			if(sourceFileAttribute == null)
			{
				sourceFileAttribute = typeofSourceFileAttribute.GetConstructor(new Type[] { typeof(string) });
			}
			moduleBuilder.SetCustomAttribute(new CustomAttributeBuilder(sourceFileAttribute, new object[] { filename }));
		}

		internal static void SetLineNumberTable(MethodBase mb, IKVM.Attributes.LineNumberTableAttribute.LineNumberWriter writer)
		{
			object arg;
			ConstructorInfo con;
			if(writer.Count == 1)
			{
				if(lineNumberTableAttribute2 == null)
				{
					lineNumberTableAttribute2 = typeofLineNumberTableAttribute.GetConstructor(new Type[] { typeof(ushort) });
				}
				con = lineNumberTableAttribute2;
				arg = (ushort)writer.LineNo;
			}
			else
			{
				if(lineNumberTableAttribute1 == null)
				{
					lineNumberTableAttribute1 = typeofLineNumberTableAttribute.GetConstructor(new Type[] { typeof(byte[]) });
				}
				con = lineNumberTableAttribute1;
				arg = writer.ToArray();
			}
			if(mb is ConstructorBuilder)
			{
				((ConstructorBuilder)mb).SetCustomAttribute(new CustomAttributeBuilder(con, new object[] { arg }));
			}
			else
			{
				((MethodBuilder)mb).SetCustomAttribute(new CustomAttributeBuilder(con, new object[] { arg }));
			}
		}

		internal static void SetEnclosingMethodAttribute(TypeBuilder tb, string className, string methodName, string methodSig)
		{
			if(enclosingMethodAttribute == null)
			{
				enclosingMethodAttribute = typeofEnclosingMethodAttribute.GetConstructor(new Type[] { typeof(string), typeof(string), typeof(string) });
			}
			tb.SetCustomAttribute(new CustomAttributeBuilder(enclosingMethodAttribute, new object[] { className, methodName, methodSig }));
		}

		internal static void SetSignatureAttribute(TypeBuilder tb, string signature)
		{
			if(signatureAttribute == null)
			{
				signatureAttribute = typeofSignatureAttribute.GetConstructor(new Type[] { typeof(string) });
			}
			tb.SetCustomAttribute(new CustomAttributeBuilder(signatureAttribute, new object[] { signature }));
		}

		internal static void SetSignatureAttribute(FieldBuilder fb, string signature)
		{
			if(signatureAttribute == null)
			{
				signatureAttribute = typeofSignatureAttribute.GetConstructor(new Type[] { typeof(string) });
			}
			fb.SetCustomAttribute(new CustomAttributeBuilder(signatureAttribute, new object[] { signature }));
		}

		internal static void SetSignatureAttribute(MethodBase mb, string signature)
		{
			if(signatureAttribute == null)
			{
				signatureAttribute = typeofSignatureAttribute.GetConstructor(new Type[] { typeof(string) });
			}
			if(mb is ConstructorBuilder)
			{
				((ConstructorBuilder)mb).SetCustomAttribute(new CustomAttributeBuilder(signatureAttribute, new object[] { signature }));
			}
			else
			{
				((MethodBuilder)mb).SetCustomAttribute(new CustomAttributeBuilder(signatureAttribute, new object[] { signature }));
			}
		}

		internal static void SetParamArrayAttribute(ParameterBuilder pb)
		{
			if(paramArrayAttribute == null)
			{
				paramArrayAttribute = new CustomAttributeBuilder(typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
			}
			pb.SetCustomAttribute(paramArrayAttribute);
		}
#endif  // STATIC_COMPILER

		internal static NameSigAttribute GetNameSig(FieldInfo field)
		{
#if !STATIC_COMPILER
			if(!field.DeclaringType.Assembly.ReflectionOnly)
			{
				object[] attr = field.GetCustomAttributes(typeof(NameSigAttribute), false);
				return attr.Length == 1 ? (NameSigAttribute)attr[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(field))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofNameSigAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return new NameSigAttribute((string)args[0].Value, (string)args[1].Value);
					}
				}
				return null;
			}
		}

		internal static NameSigAttribute GetNameSig(MethodBase method)
		{
#if !STATIC_COMPILER
			if(!method.DeclaringType.Assembly.ReflectionOnly)
			{
				object[] attr = method.GetCustomAttributes(typeof(NameSigAttribute), false);
				return attr.Length == 1 ? (NameSigAttribute)attr[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(method))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofNameSigAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return new NameSigAttribute((string)args[0].Value, (string)args[1].Value);
					}
				}
				return null;
			}
		}

		internal static T[] DecodeArray<T>(CustomAttributeTypedArgument arg)
		{
			IList<CustomAttributeTypedArgument> elems = (IList<CustomAttributeTypedArgument>)arg.Value;
			T[] arr = new T[elems.Count];
			for(int i = 0; i < arr.Length; i++)
			{
				arr[i] = (T)elems[i].Value;
			}
			return arr;
		}

		internal static ImplementsAttribute GetImplements(Type type)
		{
#if !STATIC_COMPILER
			if(!type.Assembly.ReflectionOnly)
			{
				object[] attribs = type.GetCustomAttributes(typeof(ImplementsAttribute), false);
				return attribs.Length == 1 ? (ImplementsAttribute)attribs[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(type))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofImplementsAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return new ImplementsAttribute(DecodeArray<string>(args[0]));
					}
				}
				return null;
			}
		}

		internal static ThrowsAttribute GetThrows(MethodBase mb)
		{
#if !STATIC_COMPILER
			if(!mb.DeclaringType.Assembly.ReflectionOnly)
			{
				object[] attribs = mb.GetCustomAttributes(typeof(ThrowsAttribute), false);
				return attribs.Length == 1 ? (ThrowsAttribute)attribs[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(mb))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofThrowsAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return new ThrowsAttribute(DecodeArray<string>(args[0]));
					}
				}
				return null;
			}
		}

		internal static string[] GetNonNestedInnerClasses(Type t)
		{
#if !STATIC_COMPILER
			if(!t.Assembly.ReflectionOnly)
			{
				object[] attribs = t.GetCustomAttributes(typeof(NonNestedInnerClassAttribute), false);
				string[] classes = new string[attribs.Length];
				for (int i = 0; i < attribs.Length; i++)
				{
					classes[i] = ((NonNestedInnerClassAttribute)attribs[i]).InnerClassName;
				}
				return classes;
			}
			else
#endif
			{
				List<string> list = new List<string>();
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(t))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofNonNestedInnerClassAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						list.Add((string)args[0].Value);
					}
				}
				return list.ToArray();
			}
		}

		internal static string GetNonNestedOuterClasses(Type t)
		{
#if !STATIC_COMPILER
			if(!t.Assembly.ReflectionOnly)
			{
				object[] attribs = t.GetCustomAttributes(typeof(NonNestedOuterClassAttribute), false);
				return attribs.Length == 1 ? ((NonNestedOuterClassAttribute)attribs[0]).OuterClassName : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(t))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofNonNestedOuterClassAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return (string)args[0].Value;
					}
				}
				return null;
			}
		}

		internal static SignatureAttribute GetSignature(MethodBase mb)
		{
#if !STATIC_COMPILER
			if(!mb.DeclaringType.Assembly.ReflectionOnly)
			{
				object[] attribs = mb.GetCustomAttributes(typeof(SignatureAttribute), false);
				return attribs.Length == 1 ? (SignatureAttribute)attribs[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(mb))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofSignatureAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return new SignatureAttribute((string)args[0].Value);
					}
				}
				return null;
			}
		}

		internal static SignatureAttribute GetSignature(Type type)
		{
#if !STATIC_COMPILER
			if(!type.Assembly.ReflectionOnly)
			{
				object[] attribs = type.GetCustomAttributes(typeof(SignatureAttribute), false);
				return attribs.Length == 1 ? (SignatureAttribute)attribs[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(type))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofSignatureAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return new SignatureAttribute((string)args[0].Value);
					}
				}
				return null;
			}
		}

		internal static SignatureAttribute GetSignature(FieldInfo fi)
		{
#if !STATIC_COMPILER
			if(!fi.DeclaringType.Assembly.ReflectionOnly)
			{
				object[] attribs = fi.GetCustomAttributes(typeof(SignatureAttribute), false);
				return attribs.Length == 1 ? (SignatureAttribute)attribs[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(fi))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofSignatureAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return new SignatureAttribute((string)args[0].Value);
					}
				}
				return null;
			}
		}

		internal static InnerClassAttribute GetInnerClass(Type type)
		{
#if !STATIC_COMPILER
			if(!type.Assembly.ReflectionOnly)
			{
				object[] attribs = type.GetCustomAttributes(typeof(InnerClassAttribute), false);
				return attribs.Length == 1 ? (InnerClassAttribute)attribs[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(type))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofInnerClassAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return new InnerClassAttribute((string)args[0].Value, (Modifiers)args[1].Value);
					}
				}
				return null;
			}
		}

		internal static RemappedInterfaceMethodAttribute[] GetRemappedInterfaceMethods(Type type)
		{
#if !STATIC_COMPILER
			if(!type.Assembly.ReflectionOnly)
			{
				object[] attr = type.GetCustomAttributes(typeof(RemappedInterfaceMethodAttribute), false);
				RemappedInterfaceMethodAttribute[] attr1 = new RemappedInterfaceMethodAttribute[attr.Length];
				Array.Copy(attr, attr1, attr.Length);
				return attr1;
			}
			else
#endif
			{
				List<RemappedInterfaceMethodAttribute> attrs = new List<RemappedInterfaceMethodAttribute>();
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(type))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofRemappedInterfaceMethodAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						attrs.Add(new RemappedInterfaceMethodAttribute((string)args[0].Value, (string)args[1].Value));
					}
				}
				return attrs.ToArray();
			}
		}

		internal static RemappedTypeAttribute GetRemappedType(Type type)
		{
#if !STATIC_COMPILER
			if(!type.Assembly.ReflectionOnly)
			{
				object[] attribs = type.GetCustomAttributes(typeof(RemappedTypeAttribute), false);
				return attribs.Length == 1 ? (RemappedTypeAttribute)attribs[0] : null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(type))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofRemappedTypeAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						return new RemappedTypeAttribute((Type)args[0].Value);
					}
				}
				return null;
			}
		}

		internal static RemappedClassAttribute[] GetRemappedClasses(Assembly coreAssembly)
		{
#if !STATIC_COMPILER
			if(!coreAssembly.ReflectionOnly)
			{
				object[] attr = coreAssembly.GetCustomAttributes(typeof(RemappedClassAttribute), false);
				RemappedClassAttribute[] attr1 = new RemappedClassAttribute[attr.Length];
				Array.Copy(attr, attr1, attr.Length);
				return attr1;
			}
			else
#endif
			{
				List<RemappedClassAttribute> attrs = new List<RemappedClassAttribute>();
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(coreAssembly))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofRemappedClassAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						attrs.Add(new RemappedClassAttribute((string)args[0].Value, (Type)args[1].Value));
					}
				}
				return attrs.ToArray();
			}
		}

		internal static string GetAnnotationAttributeType(Type type)
		{
#if !STATIC_COMPILER
			if(!type.Assembly.ReflectionOnly)
			{
				object[] attr = type.GetCustomAttributes(typeof(AnnotationAttributeAttribute), false);
				if(attr.Length == 1)
				{
					return ((AnnotationAttributeAttribute)attr[0]).AttributeType;
				}
				return null;
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(type))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofAnnotationAttributeAttribute))
					{
						return (string)cad.ConstructorArguments[0].Value;
					}
				}
				return null;
			}
		}

		internal static AssemblyName[] GetInternalsVisibleToAttributes(Assembly assembly)
		{
			List<AssemblyName> list = new List<AssemblyName>();
			foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(assembly))
			{
				if(cad.Constructor.DeclaringType == typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute))
				{
					try
					{
						list.Add(new AssemblyName((string)cad.ConstructorArguments[0].Value));
					}
					catch
					{
						// HACK since there is no list of exception that the AssemblyName constructor can throw, we simply catch all
					}
				}
			}
			return list.ToArray();
		}

		internal static bool IsDefined(Module mod, Type attribute)
		{
#if !STATIC_COMPILER
			if(!mod.Assembly.ReflectionOnly)
			{
				return mod.IsDefined(attribute, false);
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(mod))
				{
					// NOTE we don't support subtyping relations!
					if(MatchTypes(cad.Constructor.DeclaringType, attribute))
					{
						return true;
					}
				}
				return false;
			}
		}

		internal static bool IsDefined(Assembly asm, Type attribute)
		{
#if !STATIC_COMPILER
			if(!asm.ReflectionOnly)
			{
				return asm.IsDefined(attribute, false);
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(asm))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, attribute))
					{
						return true;
					}
				}
				return false;
			}
		}

		internal static bool IsDefined(Type type, Type attribute)
		{
#if !STATIC_COMPILER
			if(!type.Assembly.ReflectionOnly)
			{
				return type.IsDefined(attribute, false);
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(type))
				{
					// NOTE we don't support subtyping relations!
					if(MatchTypes(cad.Constructor.DeclaringType, attribute))
					{
						return true;
					}
				}
				return false;
			}
		}

		internal static bool IsDefined(ParameterInfo pi, Type attribute)
		{
#if !STATIC_COMPILER
			if(!pi.Member.DeclaringType.Assembly.ReflectionOnly)
			{
				return pi.IsDefined(attribute, false);
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(pi))
				{
					// NOTE we don't support subtyping relations!
					if(MatchTypes(cad.Constructor.DeclaringType, attribute))
					{
						return true;
					}
				}
				return false;
			}
		}

		internal static bool IsDefined(MemberInfo member, Type attribute)
		{
#if !STATIC_COMPILER
			if(!member.DeclaringType.Assembly.ReflectionOnly)
			{
				return member.IsDefined(attribute, false);
			}
			else
#endif
			{
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(member))
				{
					// NOTE we don't support subtyping relations!
					if(MatchTypes(cad.Constructor.DeclaringType, attribute))
					{
						return true;
					}
				}
				return false;
			}
		}

		internal static bool IsJavaModule(Module mod)
		{
			return IsDefined(mod, typeofJavaModuleAttribute);
		}

		internal static object[] GetJavaModuleAttributes(Module mod)
		{
#if !STATIC_COMPILER
			if(!mod.Assembly.ReflectionOnly)
			{
				return mod.GetCustomAttributes(typeofJavaModuleAttribute, false);
			}
			else
#endif
			{
				List<JavaModuleAttribute> attrs = new List<JavaModuleAttribute>();
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(mod))
				{
					if(MatchTypes(cad.Constructor.DeclaringType, typeofJavaModuleAttribute))
					{
						IList<CustomAttributeTypedArgument> args = cad.ConstructorArguments;
						if(args.Count == 0)
						{
							attrs.Add(new JavaModuleAttribute());
						}
						else
						{
							attrs.Add(new JavaModuleAttribute(DecodeArray<string>(args[0])));
						}
					}
				}
				return attrs.ToArray();
			}
		}

		internal static bool IsNoPackagePrefix(Type type)
		{
			return IsDefined(type, typeofNoPackagePrefixAttribute) || IsDefined(type.Assembly, typeofNoPackagePrefixAttribute);
		}

		internal static EnclosingMethodAttribute GetEnclosingMethodAttribute(Type type)
		{
			if (type.Assembly.ReflectionOnly)
			{
				foreach (CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(type))
				{
					if (MatchTypes(cad.Constructor.DeclaringType, typeofEnclosingMethodAttribute))
					{
						return new EnclosingMethodAttribute((string)cad.ConstructorArguments[0].Value, (string)cad.ConstructorArguments[1].Value, (string)cad.ConstructorArguments[2].Value);
					}
				}
			}
			else
			{
				object[] attr = type.GetCustomAttributes(typeof(EnclosingMethodAttribute), false);
				if (attr.Length == 1)
				{
					return (EnclosingMethodAttribute)attr[0];
				}
			}
			return null;
		}

#if STATIC_COMPILER
		internal static void SetRemappedClass(AssemblyBuilder assemblyBuilder, string name, Type shadowType)
		{
			ConstructorInfo remappedClassAttribute = typeofRemappedClassAttribute.GetConstructor(new Type[] { typeof(string), typeof(Type) });
			assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(remappedClassAttribute, new object[] { name, shadowType }));
		}

		internal static void SetRemappedType(TypeBuilder typeBuilder, Type shadowType)
		{
			ConstructorInfo remappedTypeAttribute = typeofRemappedTypeAttribute.GetConstructor(new Type[] { typeof(Type) });
			typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(remappedTypeAttribute, new object[] { shadowType }));
		}

		internal static void SetRemappedInterfaceMethod(TypeBuilder typeBuilder, string name, string mappedTo)
		{
			CustomAttributeBuilder cab = new CustomAttributeBuilder(typeofRemappedInterfaceMethodAttribute.GetConstructor(new Type[] { typeof(string), typeof(string) }), new object[] { name, mappedTo } );
			typeBuilder.SetCustomAttribute(cab);
		}

		internal static void SetExceptionIsUnsafeForMapping(TypeBuilder typeBuilder)
		{
			CustomAttributeBuilder cab = new CustomAttributeBuilder(typeofExceptionIsUnsafeForMappingAttribute.GetConstructor(Type.EmptyTypes), new object[0]);
			typeBuilder.SetCustomAttribute(cab);
		}

		internal static void SetConstantValue(FieldBuilder field, object constantValue)
		{
			CustomAttributeBuilder constantValueAttrib;
			try
			{
				constantValueAttrib = new CustomAttributeBuilder(typeofConstantValueAttribute.GetConstructor(new Type[] { constantValue.GetType() }), new object[] { constantValue });
			}
			catch (OverflowException)
			{
				// FXBUG for char values > 32K .NET (1.1 and 2.0) throws an exception (because it tries to convert to Int16)
				if (constantValue is char)
				{
					// we use the int constant value instead, the stub generator can handle that
					constantValueAttrib = new CustomAttributeBuilder(typeofConstantValueAttribute.GetConstructor(new Type[] { typeof(int) }), new object[] { (int)(char)constantValue });
				}
				else
				{
					throw;
				}
			}
			field.SetCustomAttribute(constantValueAttrib);
		}
#endif // STATIC_COMPILER

		internal static void SetRuntimeCompatibilityAttribute(AssemblyBuilder assemblyBuilder)
		{
			Type runtimeCompatibilityAttribute = typeof(System.Runtime.CompilerServices.RuntimeCompatibilityAttribute);
			assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(
				runtimeCompatibilityAttribute.GetConstructor(Type.EmptyTypes), new object[0],
				new PropertyInfo[] { runtimeCompatibilityAttribute.GetProperty("WrapNonExceptionThrows") }, new object[] { true },
				new FieldInfo[0], new object[0]));
		}
	}

	abstract class Annotation
	{
		// NOTE this method returns null if the type could not be found
		// or if the type is not a Custom Attribute and we're not in the static compiler
		internal static Annotation Load(ClassLoaderWrapper loader, object[] def)
		{
			Debug.Assert(def[0].Equals(AnnotationDefaultAttribute.TAG_ANNOTATION));
			string annotationClass = (string)def[1];
#if !STATIC_COMPILER
			if(!annotationClass.EndsWith("$Annotation;")
				&& !annotationClass.EndsWith("$Annotation$__ReturnValue;")
				&& !annotationClass.EndsWith("$Annotation$__Multiple;"))
			{
				// we don't want to try to load an annotation in dynamic mode,
				// unless it is a .NET custom attribute (which can affect runtime behavior)
				return null;
			}
#endif
			try
			{
				TypeWrapper annot = loader.RetTypeWrapperFromSig(annotationClass.Replace('/', '.'));
				return annot.Annotation;
			}
#if STATIC_COMPILER
			catch(ClassNotFoundException x)
			{
				StaticCompiler.IssueMessage(Message.ClassNotFound, x.Message);
				return null;
			}
#endif
			catch (RetargetableJavaException)
			{
				Tracer.Warning(Tracer.Compiler, "Unable to load annotation class {0}", annotationClass);
				return null;
			}
		}

		private static object LookupEnumValue(Type enumType, string value)
		{
			FieldInfo field = enumType.GetField(value, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if(field != null)
			{
				return field.GetRawConstantValue();
			}
			// both __unspecified and missing values end up here
			return Activator.CreateInstance(Enum.GetUnderlyingType(enumType));
		}

		// note that we only support the integer types that C# supports
		// (the CLI also supports bool, char, IntPtr & UIntPtr)
		private static object OrBoxedIntegrals(object v1, object v2)
		{
			Debug.Assert(v1.GetType() == v2.GetType());
			if(v1 is ulong)
			{
				ulong l1 = (ulong)v1;
				ulong l2 = (ulong)v2;
				return l1 | l2;
			}
			else
			{
				long v = ((IConvertible)v1).ToInt64(null) | ((IConvertible)v2).ToInt64(null);
				switch(Type.GetTypeCode(v1.GetType()))
				{
					case TypeCode.SByte:
						return (sbyte)v;
					case TypeCode.Byte:
						return (byte)v;
					case TypeCode.Int16:
						return (short)v;
					case TypeCode.UInt16:
						return (ushort)v;
					case TypeCode.Int32:
						return (int)v;
					case TypeCode.UInt32:
						return (uint)v;
					case TypeCode.Int64:
						return (long)v;
					default:
						throw new InvalidOperationException();
				}
			}
		}

		protected static object ConvertValue(ClassLoaderWrapper loader, Type targetType, object obj)
		{
			if(targetType.IsEnum)
			{
				// TODO check the obj descriptor matches the type we expect
				if(((object[])obj)[0].Equals(AnnotationDefaultAttribute.TAG_ARRAY))
				{
					object[] arr = (object[])obj;
					object value = null;
					for(int i = 1; i < arr.Length; i++)
					{
						// TODO check the obj descriptor matches the type we expect
						string s = ((object[])arr[i])[2].ToString();
						object newval = LookupEnumValue(targetType, s);
						if (value == null)
						{
							value = newval;
						}
						else
						{
							value = OrBoxedIntegrals(value, newval);
						}
					}
					return value;
				}
				else
				{
					string s = ((object[])obj)[2].ToString();
					if(s == "__unspecified")
					{
						// TODO we should probably return null and handle that
					}
					return LookupEnumValue(targetType, s);
				}
			}
			else if(targetType == typeof(Type))
			{
				// TODO check the obj descriptor matches the type we expect
				return loader.FieldTypeWrapperFromSig(((string)((object[])obj)[1]).Replace('/', '.')).TypeAsTBD;
			}
			else if(targetType.IsArray)
			{
				// TODO check the obj descriptor matches the type we expect
				object[] arr = (object[])obj;
				Type elementType = targetType.GetElementType();
				object[] targetArray = new object[arr.Length - 1];
				for(int i = 1; i < arr.Length; i++)
				{
					targetArray[i - 1] = ConvertValue(loader, elementType, arr[i]);
				}
				return targetArray;
			}
			else
			{
				return obj;
			}
		}

		internal static bool MakeDeclSecurity(Type type, object annotation, out SecurityAction action, out PermissionSet permSet)
		{
			ConstructorInfo ci = type.GetConstructor(new Type[] { typeof(SecurityAction) });
			if (ci == null)
			{
				// TODO issue message?
				action = 0;
				permSet = null;
				return false;
			}
			SecurityAttribute attr = null;
			object[] arr = (object[])annotation;
			for (int i = 2; i < arr.Length; i += 2)
			{
				string name = (string)arr[i];
				if (name == "value")
				{
					attr = (SecurityAttribute)ci.Invoke(new object[] { ConvertValue(null, typeof(SecurityAction), arr[i + 1]) });
				}
			}
			if (attr == null)
			{
				// TODO issue message?
				action = 0;
				permSet = null;
				return false;
			}
			for (int i = 2; i < arr.Length; i += 2)
			{
				string name = (string)arr[i];
				if (name != "value")
				{
					PropertyInfo pi = type.GetProperty(name);
					pi.SetValue(attr, ConvertValue(null, pi.PropertyType, arr[i + 1]), null);
				}
			}
			action = attr.Action;
			permSet = new PermissionSet(PermissionState.None);
			permSet.AddPermission(attr.CreatePermission());
			return true;
		}

		internal static bool HasRetentionPolicyRuntime(object[] annotations)
		{
			if(annotations != null)
			{
				foreach(object[] def in annotations)
				{
					if(def[1].Equals("Ljava/lang/annotation/Retention;"))
					{
						for(int i = 2; i < def.Length; i += 2)
						{
							if(def[i].Equals("value"))
							{
								object[] val = def[i + 1] as object[];
								if(val != null
									&& val.Length == 3
									&& val[0].Equals(AnnotationDefaultAttribute.TAG_ENUM)
									&& val[1].Equals("Ljava/lang/annotation/RetentionPolicy;")
									&& val[2].Equals("RUNTIME"))
								{
									return true;
								}
							}
						}
					}
				}
			}
			return false;
		}

		protected static object QualifyClassNames(ClassLoaderWrapper loader, object annotation)
		{
			bool copy = false;
			object[] def = (object[])annotation;
			for(int i = 3; i < def.Length; i += 2)
			{
				object[] val = def[i] as object[];
				if(val != null)
				{
					object[] newval = ValueQualifyClassNames(loader, val);
					if(newval != val)
					{
						if(!copy)
						{
							copy = true;
							object[] newdef = new object[def.Length];
							Array.Copy(def, newdef, def.Length);
							def = newdef;
						}
						def[i] = newval;
					}
				}
			}
			return def;
		}

		private static object[] ValueQualifyClassNames(ClassLoaderWrapper loader, object[] val)
		{
			if(val[0].Equals(AnnotationDefaultAttribute.TAG_ANNOTATION))
			{
				return (object[])QualifyClassNames(loader, val);
			}
			else if(val[0].Equals(AnnotationDefaultAttribute.TAG_CLASS))
			{
				string sig = (string)val[1];
				if(sig.StartsWith("L"))
				{
					TypeWrapper tw = loader.LoadClassByDottedNameFast(sig.Substring(1, sig.Length - 2).Replace('/', '.'));
					if(tw != null)
					{
						return new object[] { AnnotationDefaultAttribute.TAG_CLASS, "L" + tw.TypeAsBaseType.AssemblyQualifiedName.Replace('.', '/') + ";" };
					}
				}
				return val;
			}
			else if(val[0].Equals(AnnotationDefaultAttribute.TAG_ENUM))
			{
				string sig = (string)val[1];
				TypeWrapper tw = loader.LoadClassByDottedNameFast(sig.Substring(1, sig.Length - 2).Replace('/', '.'));
				if(tw != null)
				{
					return new object[] { AnnotationDefaultAttribute.TAG_ENUM, "L" + tw.TypeAsBaseType.AssemblyQualifiedName.Replace('.', '/') + ";", val[2] };
				}
				return val;
			}
			else if(val[0].Equals(AnnotationDefaultAttribute.TAG_ARRAY))
			{
				bool copy = false;
				for(int i = 1; i < val.Length; i++)
				{
					object[] nval = val[i] as object[];
					if(nval != null)
					{
						object newnval = ValueQualifyClassNames(loader, nval);
						if(newnval != nval)
						{
							if(!copy)
							{
								copy = true;
								object[] newval = new object[val.Length];
								Array.Copy(val, newval, val.Length);
								val = newval;
							}
							val[i] = newnval;
						}
					}
				}
				return val;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		internal abstract void Apply(ClassLoaderWrapper loader, TypeBuilder tb, object annotation);
		internal abstract void Apply(ClassLoaderWrapper loader, MethodBuilder mb, object annotation);
		internal abstract void Apply(ClassLoaderWrapper loader, ConstructorBuilder cb, object annotation);
		internal abstract void Apply(ClassLoaderWrapper loader, FieldBuilder fb, object annotation);
		internal abstract void Apply(ClassLoaderWrapper loader, ParameterBuilder pb, object annotation);
		internal abstract void Apply(ClassLoaderWrapper loader, AssemblyBuilder ab, object annotation);
		internal abstract void Apply(ClassLoaderWrapper loader, PropertyBuilder pb, object annotation);

		internal virtual void ApplyReturnValue(ClassLoaderWrapper loader, MethodBuilder mb, ref ParameterBuilder pb, object annotation)
		{
		}
	}

	[Flags]
	enum TypeFlags : ushort
	{
		HasIncompleteInterfaceImplementation = 1,
		InternalAccess = 2,
		HasStaticInitializer = 4,
		VerifyError = 8,
		ClassFormatError = 16,
		HasUnsupportedAbstractMethods = 32,
	}

	internal abstract class TypeWrapper
	{
		private readonly string name;		// java name (e.g. java.lang.Object)
		private readonly Modifiers modifiers;
		private TypeFlags flags;
		private MethodWrapper[] methods;
		private FieldWrapper[] fields;
		private readonly TypeWrapper baseWrapper;
#if !STATIC_COMPILER
		private object classObject;
#endif
		internal static readonly TypeWrapper[] EmptyArray = new TypeWrapper[0];
		internal const Modifiers UnloadableModifiersHack = Modifiers.Final | Modifiers.Interface | Modifiers.Private;
		internal const Modifiers VerifierTypeModifiersHack = Modifiers.Final | Modifiers.Interface;

		internal TypeWrapper(Modifiers modifiers, string name, TypeWrapper baseWrapper)
		{
			Profiler.Count("TypeWrapper");
			// class name should be dotted or null for primitives
			Debug.Assert(name == null || name.IndexOf('/') < 0);

			this.modifiers = modifiers;
			this.name = name == null ? null : String.Intern(name);
			this.baseWrapper = baseWrapper;
		}

		internal void EmitClassLiteral(CodeEmitter ilgen)
		{
			Debug.Assert(!this.IsPrimitive);

			Type type = GetClassLiteralType();

			// note that this has to be the same check as in LazyInitClass
			if (!this.IsFastClassLiteralSafe || IsForbiddenTypeParameterType(type))
			{
				ilgen.Emit(OpCodes.Ldtoken, type);
				Compiler.getClassFromTypeHandle.EmitCall(ilgen);
			}
			else
			{
				ilgen.Emit(OpCodes.Ldsfld, RuntimeHelperTypes.GetClassLiteralField(type));
			}
		}

		private Type GetClassLiteralType()
		{
			Debug.Assert(!this.IsPrimitive);

			TypeWrapper tw = this;
			if (tw.IsGhostArray)
			{
				int rank = tw.ArrayRank;
				while (tw.IsArray)
				{
					tw = tw.ElementTypeWrapper;
				}
				return ArrayTypeWrapper.MakeArrayType(tw.TypeAsTBD, rank);
			}
			else
			{
				return tw.IsRemapped ? tw.TypeAsBaseType : tw.TypeAsTBD;
			}
		}

		private static bool IsForbiddenTypeParameterType(Type type)
		{
			// these are the types that may not be used as a type argument when instantiating a generic type
			return type == typeof(void)
				|| type == typeof(ArgIterator)
				|| type == typeof(RuntimeArgumentHandle)
				|| type == typeof(TypedReference)
				|| type.ContainsGenericParameters
				|| type.IsByRef;
		}

		internal virtual bool IsFastClassLiteralSafe
		{
			get { return false; }
		}

#if !STATIC_COMPILER
		internal void SetClassObject(object classObject)
		{
			this.classObject = classObject;
		}

		internal object ClassObject
		{
			get
			{
				Debug.Assert(!IsUnloadable && !IsVerifierType);
				if (classObject == null)
				{
					LazyInitClass();
				}
				return classObject;
			}
		}

		private static bool IsReflectionOnly(Type type)
		{
			Assembly asm = type.Assembly;
			if (asm.ReflectionOnly)
			{
				return true;
			}
			if (!type.IsGenericType || type.IsGenericTypeDefinition)
			{
				return false;
			}
			// we have a generic type instantiation, it might have ReflectionOnly type arguments
			foreach (Type arg in type.GetGenericArguments())
			{
				if (IsReflectionOnly(arg))
				{
					return true;
				}
			}
			return false;
		}

#if !FIRST_PASS
		private java.lang.Class GetPrimitiveClass()
		{
			if (this == PrimitiveTypeWrapper.BYTE)
			{
				return java.lang.Byte.TYPE;
			}
			else if (this == PrimitiveTypeWrapper.CHAR)
			{
				return java.lang.Character.TYPE;
			}
			else if (this == PrimitiveTypeWrapper.DOUBLE)
			{
				return java.lang.Double.TYPE;
			}
			else if (this == PrimitiveTypeWrapper.FLOAT)
			{
				return java.lang.Float.TYPE;
			}
			else if (this == PrimitiveTypeWrapper.INT)
			{
				return java.lang.Integer.TYPE;
			}
			else if (this == PrimitiveTypeWrapper.LONG)
			{
				return java.lang.Long.TYPE;
			}
			else if (this == PrimitiveTypeWrapper.SHORT)
			{
				return java.lang.Short.TYPE;
			}
			else if (this == PrimitiveTypeWrapper.BOOLEAN)
			{
				return java.lang.Boolean.TYPE;
			}
			else if (this == PrimitiveTypeWrapper.VOID)
			{
				return java.lang.Void.TYPE;
			}
			else
			{
				throw new InvalidOperationException();
			}
		}
#endif

		private void LazyInitClass()
		{
			lock (this)
			{
				if (classObject == null)
				{
					// DynamicTypeWrapper should haved already had SetClassObject explicitly
					Debug.Assert(!(this is DynamicTypeWrapper));
#if !FIRST_PASS
					java.lang.Class clazz;
					// note that this has to be the same check as in EmitClassLiteral
					if (!this.IsFastClassLiteralSafe)
					{
						if (this.IsPrimitive)
						{
							clazz = GetPrimitiveClass();
						}
						else
						{
							clazz = new java.lang.Class(null);
						}
					}
					else
					{
						Type type = GetClassLiteralType();
						if (IsForbiddenTypeParameterType(type) || IsReflectionOnly(type))
						{
							clazz = new java.lang.Class(type);
						}
						else
						{
							clazz = (java.lang.Class)typeof(ikvm.@internal.ClassLiteral<>).MakeGenericType(type).GetField("Value").GetValue(null);
						}
					}
#if __MonoCS__
					SetTypeWrapperHack(ref clazz.typeWrapper, this);
#else
					clazz.typeWrapper = this;
#endif
					// MONOBUG Interlocked.Exchange is broken on Mono, so we use CompareExchange
					System.Threading.Interlocked.CompareExchange(ref classObject, clazz, null);
#endif
				}
			}
		}

#if __MonoCS__
		// MONOBUG this method is to work around an mcs bug
		internal static void SetTypeWrapperHack<T>(ref T field, TypeWrapper type)
		{
			field = (T)(object)type;
		}
#endif

#if !FIRST_PASS
		private static void ResolvePrimitiveTypeWrapperClasses()
		{
			// note that we're evaluating all ClassObject properties for the side effect
			// (to initialize and associate the ClassObject with the TypeWrapper)
			if (PrimitiveTypeWrapper.BYTE.ClassObject == null
				|| PrimitiveTypeWrapper.CHAR.ClassObject == null
				|| PrimitiveTypeWrapper.DOUBLE.ClassObject == null
				|| PrimitiveTypeWrapper.FLOAT.ClassObject == null
				|| PrimitiveTypeWrapper.INT.ClassObject == null
				|| PrimitiveTypeWrapper.LONG.ClassObject == null
				|| PrimitiveTypeWrapper.SHORT.ClassObject == null
				|| PrimitiveTypeWrapper.BOOLEAN.ClassObject == null
				|| PrimitiveTypeWrapper.VOID.ClassObject == null)
			{
				throw new InvalidOperationException();
			}
		}
#endif

		internal static TypeWrapper FromClass(object classObject)
		{
#if FIRST_PASS
			return null;
#else
			java.lang.Class clazz = (java.lang.Class)classObject;
			// MONOBUG redundant cast to workaround mcs bug
			TypeWrapper tw = (TypeWrapper)(object)clazz.typeWrapper;
			if(tw == null)
			{
				Type type = clazz.type;
				if (type == null)
				{
					ResolvePrimitiveTypeWrapperClasses();
					return FromClass(classObject);
				}
				if (type == typeof(void) || type.IsPrimitive || ClassLoaderWrapper.IsRemappedType(type))
				{
					tw = DotNetTypeWrapper.GetWrapperFromDotNetType(type);
				}
				else
				{
					tw = ClassLoaderWrapper.GetWrapperFromType(type);
				}
#if __MonoCS__
				SetTypeWrapperHack(ref clazz.typeWrapper, tw);
#else
				clazz.typeWrapper = tw;
#endif
			}
			return tw;
#endif
		}
#endif // !STATIC_COMPILER

		public override string ToString()
		{
			return GetType().Name + "[" + name + "]";
		}

		// For UnloadableTypeWrapper it tries to load the type through the specified loader
		// and if that fails it throw a NoClassDefFoundError (not a java.lang.NoClassDefFoundError),
		// for all other types this is a no-op.
		internal virtual TypeWrapper EnsureLoadable(ClassLoaderWrapper loader)
		{
			return this;
		}

		internal bool HasIncompleteInterfaceImplementation
		{
			get
			{
				return (flags & TypeFlags.HasIncompleteInterfaceImplementation) != 0 || (baseWrapper != null && baseWrapper.HasIncompleteInterfaceImplementation);
			}
			set
			{
				// TODO do we need locking here?
				if(value)
				{
					flags |= TypeFlags.HasIncompleteInterfaceImplementation;
				}
				else
				{
					flags &= ~TypeFlags.HasIncompleteInterfaceImplementation;
				}
			}
		}

		internal bool HasUnsupportedAbstractMethods
		{
			get
			{
				foreach(TypeWrapper iface in this.Interfaces)
				{
					if(iface.HasUnsupportedAbstractMethods)
					{
						return true;
					}
				}
				return (flags & TypeFlags.HasUnsupportedAbstractMethods) != 0 || (baseWrapper != null && baseWrapper.HasUnsupportedAbstractMethods);
			}
			set
			{
				// TODO do we need locking here?
				if(value)
				{
					flags |= TypeFlags.HasUnsupportedAbstractMethods;
				}
				else
				{
					flags &= ~TypeFlags.HasUnsupportedAbstractMethods;
				}
			}
		}

		internal virtual bool HasStaticInitializer
		{
			get
			{
				return (flags & TypeFlags.HasStaticInitializer) != 0;
			}
			set
			{
				// TODO do we need locking here?
				if(value)
				{
					flags |= TypeFlags.HasStaticInitializer;
				}
				else
				{
					flags &= ~TypeFlags.HasStaticInitializer;
				}
			}
		}

		internal bool HasVerifyError
		{
			get
			{
				return (flags & TypeFlags.VerifyError) != 0;
			}
			set
			{
				// TODO do we need locking here?
				if(value)
				{
					flags |= TypeFlags.VerifyError;
				}
				else
				{
					flags &= ~TypeFlags.VerifyError;
				}
			}
		}

		internal bool HasClassFormatError
		{
			get
			{
				return (flags & TypeFlags.ClassFormatError) != 0;
			}
			set
			{
				// TODO do we need locking here?
				if(value)
				{
					flags |= TypeFlags.ClassFormatError;
				}
				else
				{
					flags &= ~TypeFlags.ClassFormatError;
				}
			}
		}

		internal virtual bool IsFakeTypeContainer
		{
			get
			{
				return false;
			}
		}

		internal bool IsFakeNestedType
		{
			get
			{
				TypeWrapper outer = this.DeclaringTypeWrapper;
				return outer != null && outer.IsFakeTypeContainer;
			}
		}

		// a ghost is an interface that appears to be implemented by a .NET type
		// (e.g. System.String (aka java.lang.String) appears to implement java.lang.CharSequence,
		// so java.lang.CharSequence is a ghost)
		internal virtual bool IsGhost
		{
			get
			{
				return false;
			}
		}

		// is this an array type of which the ultimate element type is a ghost?
		internal bool IsGhostArray
		{
			get
			{
				return !IsUnloadable && IsArray && (ElementTypeWrapper.IsGhost || ElementTypeWrapper.IsGhostArray);
			}
		}

		internal virtual FieldInfo GhostRefField
		{
			get
			{
				throw new InvalidOperationException();
			}
		}

		internal virtual bool IsRemapped
		{
			get
			{
				return false;
			}
		}

		internal bool IsArray
		{
			get
			{
				return name != null && name[0] == '[';
			}
		}

		// NOTE for non-array types this returns 0
		internal int ArrayRank
		{
			get
			{
				int i = 0;
				if(name != null)
				{
					while(name[i] == '[')
					{
						i++;
					}
				}
				return i;
			}
		}

		internal bool IsNonPrimitiveValueType
		{
			get
			{
				return this != VerifierTypeWrapper.Null && !IsPrimitive && !IsGhost && TypeAsTBD.IsValueType;
			}
		}

		internal bool IsPrimitive
		{
			get
			{
				return name == null;
			}
		}

		internal bool IsWidePrimitive
		{
			get
			{
				return this == PrimitiveTypeWrapper.LONG || this == PrimitiveTypeWrapper.DOUBLE;
			}
		}

		internal bool IsIntOnStackPrimitive
		{
			get
			{
				return name == null &&
					(this == PrimitiveTypeWrapper.BOOLEAN ||
					this == PrimitiveTypeWrapper.BYTE ||
					this == PrimitiveTypeWrapper.CHAR ||
					this == PrimitiveTypeWrapper.SHORT ||
					this == PrimitiveTypeWrapper.INT);
			}
		}

		private static bool IsJavaPrimitive(Type type)
		{
			return type == PrimitiveTypeWrapper.BOOLEAN.TypeAsTBD
				|| type == PrimitiveTypeWrapper.BYTE.TypeAsTBD
				|| type == PrimitiveTypeWrapper.CHAR.TypeAsTBD
				|| type == PrimitiveTypeWrapper.DOUBLE.TypeAsTBD
				|| type == PrimitiveTypeWrapper.FLOAT.TypeAsTBD
				|| type == PrimitiveTypeWrapper.INT.TypeAsTBD
				|| type == PrimitiveTypeWrapper.LONG.TypeAsTBD
				|| type == PrimitiveTypeWrapper.SHORT.TypeAsTBD
				|| type == PrimitiveTypeWrapper.VOID.TypeAsTBD;
		}

		internal bool IsBoxedPrimitive
		{
			get
			{
				return !IsPrimitive && IsJavaPrimitive(TypeAsSignatureType);
			}
		}

		internal bool IsErasedOrBoxedPrimitiveOrRemapped
		{
			get
			{
				bool erased = IsUnloadable || IsGhostArray;
				return erased || IsBoxedPrimitive || (IsRemapped && this is DotNetTypeWrapper);
			}
		}

		internal bool IsUnloadable
		{
			get
			{
				// NOTE we abuse modifiers to note unloadable classes
				return modifiers == UnloadableModifiersHack;
			}
		}

		internal bool IsVerifierType
		{
			get
			{
				// NOTE we abuse modifiers to note verifier types
				return modifiers == VerifierTypeModifiersHack;
			}
		}

		internal virtual bool IsMapUnsafeException
		{
			get
			{
				return false;
			}
		}

		internal Modifiers Modifiers
		{
			get
			{
				return modifiers;
			}
		}

		// since for inner classes, the modifiers returned by Class.getModifiers are different from the actual
		// modifiers (as used by the VM access control mechanism), we have this additional property
		internal virtual Modifiers ReflectiveModifiers
		{
			get
			{
				return modifiers;
			}
		}

		internal bool IsInternal
		{
			get
			{
				return (flags & TypeFlags.InternalAccess) != 0;
			}
			set
			{
				// TODO do we need locking here?
				if(value)
				{
					flags |= TypeFlags.InternalAccess;
				}
				else
				{
					flags &= ~TypeFlags.InternalAccess;
				}
			}
		}

		internal bool IsPublic
		{
			get
			{
				return (modifiers & Modifiers.Public) != 0;
			}
		}

		internal bool IsAbstract
		{
			get
			{
				// interfaces don't need to marked abstract explicitly (and javac 1.1 didn't do it)
				return (modifiers & (Modifiers.Abstract | Modifiers.Interface)) != 0;
			}
		}

		internal bool IsFinal
		{
			get
			{
				return (modifiers & Modifiers.Final) != 0;
			}
		}

		internal bool IsInterface
		{
			get
			{
				Debug.Assert(!IsUnloadable && !IsVerifierType);
				return (modifiers & Modifiers.Interface) != 0;
			}
		}

		// this exists because interfaces and arrays of interfaces are treated specially
		// by the verifier, interfaces don't have a common base (other than java.lang.Object)
		// so any object reference or object array reference can be used where an interface
		// or interface array reference is expected (the compiler will insert the required casts).
		internal bool IsInterfaceOrInterfaceArray
		{
			get
			{
				TypeWrapper tw = this;
				while(tw.IsArray)
				{
					tw = tw.ElementTypeWrapper;
				}
				return tw.IsInterface;
			}
		}

		internal abstract ClassLoaderWrapper GetClassLoader();

		internal FieldWrapper GetFieldWrapper(string fieldName, string fieldSig)
		{
			lock(this)
			{
				if(fields == null)
				{
					LazyPublishMembers();
				}
			}
			foreach(FieldWrapper fw in fields)
			{
				if(fw.Name == fieldName && fw.Signature == fieldSig)
				{
					return fw;
				}	
			}
			foreach(TypeWrapper iface in this.Interfaces)
			{
				FieldWrapper fw = iface.GetFieldWrapper(fieldName, fieldSig);
				if(fw != null)
				{
					return fw;
				}
			}
			if(baseWrapper != null)
			{
				return baseWrapper.GetFieldWrapper(fieldName, fieldSig);
			}
			return null;
		}

		protected virtual void LazyPublishMembers()
		{
			if(methods == null)
			{
				methods = MethodWrapper.EmptyArray;
			}
			if(fields == null)
			{
				fields = FieldWrapper.EmptyArray;
			}
		}

		internal MethodWrapper[] GetMethods()
		{
			lock(this)
			{
				if(methods == null)
				{
					LazyPublishMembers();
				}
			}
			return methods;
		}

		internal FieldWrapper[] GetFields()
		{
			lock(this)
			{
				if(fields == null)
				{
					LazyPublishMembers();
				}
			}
			return fields;
		}

		internal MethodWrapper GetMethodWrapper(string name, string sig, bool inherit)
		{
			lock(this)
			{
				if(methods == null)
				{
					LazyPublishMembers();
				}
			}
			// MemberWrapper interns the name and sig so we can use ref equality
			// profiling has shown this to be more efficient
			string _name = String.IsInterned(name);
			string _sig = String.IsInterned(sig);
			foreach(MethodWrapper mw in methods)
			{
				// NOTE we can use ref equality, because names and signatures are
				// always interned by MemberWrapper
				if(ReferenceEquals(mw.Name, _name) && ReferenceEquals(mw.Signature, _sig))
				{
					return mw;
				}
			}
			if(inherit && baseWrapper != null)
			{
				return baseWrapper.GetMethodWrapper(name, sig, inherit);
			}
			return null;
		}

		internal void SetMethods(MethodWrapper[] methods)
		{
			Debug.Assert(methods != null);
			this.methods = methods;
		}

		internal void SetFields(FieldWrapper[] fields)
		{
			Debug.Assert(fields != null);
			this.fields = fields;
		}

		internal string Name
		{
			get
			{
				return name;
			}
		}

		// the name of the type as it appears in a Java signature string (e.g. "Ljava.lang.Object;" or "I")
		internal virtual string SigName
		{
			get
			{
				return "L" + this.Name + ";";
			}
		}

		// returns true iff wrapper is allowed to access us
		internal bool IsAccessibleFrom(TypeWrapper wrapper)
		{
			return IsPublic
				|| (IsInternal && InternalsVisibleTo(wrapper))
				|| IsPackageAccessibleFrom(wrapper);
		}

		internal bool InternalsVisibleTo(TypeWrapper wrapper)
		{
			return GetClassLoader().InternalsVisibleToImpl(this, wrapper);
		}

		internal bool IsPackageAccessibleFrom(TypeWrapper wrapper)
		{
			return MatchingPackageNames(name, wrapper.name) && InternalsVisibleTo(wrapper);
		}

		private static bool MatchingPackageNames(string name1, string name2)
		{
			int index1 = name1.LastIndexOf('.');
			int index2 = name2.LastIndexOf('.');
			if (index1 == -1 && index2 == -1)
			{
				return true;
			}
			// for array types we need to skip the brackets
			int skip1 = 0;
			int skip2 = 0;
			while (name1[skip1] == '[')
			{
				skip1++;
			}
			while (name2[skip2] == '[')
			{
				skip2++;
			}
			if (skip1 > 0)
			{
				// skip over the L that follows the brackets
				skip1++;
			}
			if (skip2 > 0)
			{
				// skip over the L that follows the brackets
				skip2++;
			}
			if ((index1 - skip1) != (index2 - skip2))
			{
				return false;
			}
			return String.CompareOrdinal(name1, skip1, name2, skip2, index1 - skip1) == 0;
		}

		internal abstract Type TypeAsTBD
		{
			get;
		}

		internal virtual TypeBuilder TypeAsBuilder
		{
			get
			{
				TypeBuilder typeBuilder = TypeAsTBD as TypeBuilder;
				Debug.Assert(typeBuilder != null);
				return typeBuilder;
			}
		}

		internal Type TypeAsSignatureType
		{
			get
			{
				if(IsUnloadable)
				{
					return typeof(object);
				}
				if(IsGhostArray)
				{
					return ArrayTypeWrapper.MakeArrayType(typeof(object), ArrayRank);
				}
				return TypeAsTBD;
			}
		}

		internal virtual Type TypeAsBaseType
		{
			get
			{
				return TypeAsTBD;
			}
		}

		internal Type TypeAsLocalOrStackType
		{
			get
			{
				if(IsUnloadable || IsGhost)
				{
					return typeof(object);
				}
				if(IsNonPrimitiveValueType)
				{
					// return either System.ValueType or System.Enum
					return TypeAsTBD.BaseType;
				}
				if(IsGhostArray)
				{
					return ArrayTypeWrapper.MakeArrayType(typeof(object), ArrayRank);
				}
				return TypeAsTBD;
			}
		}

		/** <summary>Use this if the type is used as an array or array element</summary> */
		internal Type TypeAsArrayType
		{
			get
			{
				if(IsUnloadable || IsGhost)
				{
					return typeof(object);
				}
				if(IsGhostArray)
				{
					return ArrayTypeWrapper.MakeArrayType(typeof(object), ArrayRank);
				}
				return TypeAsTBD;
			}
		}

		internal Type TypeAsExceptionType
		{
			get
			{
				if(IsUnloadable)
				{
					return typeof(Exception);
				}
				return TypeAsTBD;
			}
		}

		internal TypeWrapper BaseTypeWrapper
		{
			get
			{
				return baseWrapper;
			}
		}

		internal TypeWrapper ElementTypeWrapper
		{
			get
			{
				Debug.Assert(!this.IsUnloadable);
				Debug.Assert(this == VerifierTypeWrapper.Null || this.IsArray);

				if(this == VerifierTypeWrapper.Null)
				{
					return VerifierTypeWrapper.Null;
				}

				// TODO consider caching the element type
				switch(name[1])
				{
					case '[':
						// NOTE this call to LoadClassByDottedNameFast can never fail and will not trigger a class load
						// (because the ultimate element type was already loaded when this type was created)
						return GetClassLoader().LoadClassByDottedNameFast(name.Substring(1));
					case 'L':
						// NOTE this call to LoadClassByDottedNameFast can never fail and will not trigger a class load
						// (because the ultimate element type was already loaded when this type was created)
						return GetClassLoader().LoadClassByDottedNameFast(name.Substring(2, name.Length - 3));
					case 'Z':
						return PrimitiveTypeWrapper.BOOLEAN;
					case 'B':
						return PrimitiveTypeWrapper.BYTE;
					case 'S':
						return PrimitiveTypeWrapper.SHORT;
					case 'C':
						return PrimitiveTypeWrapper.CHAR;
					case 'I':
						return PrimitiveTypeWrapper.INT;
					case 'J':
						return PrimitiveTypeWrapper.LONG;
					case 'F':
						return PrimitiveTypeWrapper.FLOAT;
					case 'D':
						return PrimitiveTypeWrapper.DOUBLE;
					default:
						throw new InvalidOperationException(name);
				}
			}
		}

		internal TypeWrapper MakeArrayType(int rank)
		{
			Debug.Assert(rank != 0);
			// NOTE this call to LoadClassByDottedNameFast can never fail and will not trigger a class load
			return GetClassLoader().LoadClassByDottedNameFast(new String('[', rank) + this.SigName);
		}

		internal bool ImplementsInterface(TypeWrapper interfaceWrapper)
		{
			TypeWrapper typeWrapper = this;
			while(typeWrapper != null)
			{
				TypeWrapper[] interfaces = typeWrapper.Interfaces;
				for(int i = 0; i < interfaces.Length; i++)
				{
					if(interfaces[i] == interfaceWrapper)
					{
						return true;
					}
					if(interfaces[i].ImplementsInterface(interfaceWrapper))
					{
						return true;
					}
				}
				typeWrapper = typeWrapper.BaseTypeWrapper;
			}
			return false;
		}

		internal bool IsSubTypeOf(TypeWrapper baseType)
		{
			// make sure IsSubTypeOf isn't used on primitives
			Debug.Assert(!this.IsPrimitive);
			Debug.Assert(!baseType.IsPrimitive);
			// can't be used on Unloadable
			Debug.Assert(!this.IsUnloadable);
			Debug.Assert(!baseType.IsUnloadable);

			if(baseType.IsInterface)
			{
				if(baseType == this)
				{
					return true;
				}
				return ImplementsInterface(baseType);
			}
			// NOTE this isn't just an optimization, it is also required when this is an interface
			if(baseType == CoreClasses.java.lang.Object.Wrapper)
			{
				return true;
			}
			TypeWrapper subType = this;
			while(subType != baseType)
			{
				subType = subType.BaseTypeWrapper;
				if(subType == null)
				{
					return false;
				}
			}
			return true;
		}

		internal bool IsAssignableTo(TypeWrapper wrapper)
		{
			if(this == wrapper)
			{
				return true;
			}
			if(this.IsPrimitive || wrapper.IsPrimitive)
			{
				return false;
			}
			if(this == VerifierTypeWrapper.Null)
			{
				return true;
			}
			if(wrapper.IsInterface)
			{
				return ImplementsInterface(wrapper);
			}
			int rank1 = this.ArrayRank;
			int rank2 = wrapper.ArrayRank;
			if(rank1 > 0 && rank2 > 0)
			{
				rank1--;
				rank2--;
				TypeWrapper elem1 = this.ElementTypeWrapper;
				TypeWrapper elem2 = wrapper.ElementTypeWrapper;
				while(rank1 != 0 && rank2 != 0)
				{
					elem1 = elem1.ElementTypeWrapper;
					elem2 = elem2.ElementTypeWrapper;
					rank1--;
					rank2--;
				}
				return (!elem1.IsNonPrimitiveValueType && elem1.IsSubTypeOf(elem2)) || (rank1 == rank2 && elem2.IsGhost && elem1 == CoreClasses.java.lang.Object.Wrapper);
			}
			return this.IsSubTypeOf(wrapper);
		}

#if !STATIC_COMPILER
		internal bool IsInstance(object obj)
		{
			if(obj != null)
			{
				TypeWrapper thisWrapper = this;
				TypeWrapper objWrapper = IKVM.NativeCode.ikvm.runtime.Util.GetTypeWrapperFromObject(obj);
				return objWrapper.IsAssignableTo(thisWrapper);
			}
			return false;
		}
#endif

		internal abstract TypeWrapper[] Interfaces
		{
			get;
		}

		// NOTE this property can only be called for finished types!
		internal abstract TypeWrapper[] InnerClasses
		{
			get;
		}

		// NOTE this property can only be called for finished types!
		internal abstract TypeWrapper DeclaringTypeWrapper
		{
			get;
		}

		internal abstract void Finish();

		private static void ImplementInterfaceMethodStubImpl(MethodWrapper ifmethod, TypeBuilder typeBuilder, DynamicTypeWrapper wrapper)
		{
			// we're mangling the name to prevent subclasses from accidentally overriding this method and to
			// prevent clashes with overloaded method stubs that are erased to the same signature (e.g. unloadable types and ghost arrays)
			// HACK the signature and name are the wrong way around to work around a C++/CLI bug (apparantely it looks looks at the last n
			// characters of the method name, or something bizarre like that)
			// https://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=234167
			string mangledName = ifmethod.DeclaringType.Name + "/" + ifmethod.Signature + ifmethod.Name;
			MethodWrapper mce = null;
			TypeWrapper lookup = wrapper;
			while(lookup != null)
			{
				mce = lookup.GetMethodWrapper(ifmethod.Name, ifmethod.Signature, true);
				if(mce == null || !mce.IsStatic)
				{
					break;
				}
				lookup = mce.DeclaringType.BaseTypeWrapper;
			}
			if(mce != null)
			{
				Debug.Assert(!mce.HasCallerID);
				if(mce.DeclaringType != wrapper)
				{
					// check the loader constraints
					bool error = false;
					if(mce.ReturnType != ifmethod.ReturnType)
					{
						// TODO handle unloadable
						error = true;
					}
					TypeWrapper[] mceparams = mce.GetParameters();
					TypeWrapper[] ifparams = ifmethod.GetParameters();
					for(int i = 0; i < mceparams.Length; i++)
					{
						if(mceparams[i] != ifparams[i])
						{
							// TODO handle unloadable
							error = true;
							break;
						}
					}
					if(error)
					{
						MethodBuilder mb = typeBuilder.DefineMethod(mangledName, MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final, ifmethod.ReturnTypeForDefineMethod, ifmethod.GetParametersForDefineMethod());
						AttributeHelper.HideFromJava(mb);
						CodeEmitter.Create(mb).EmitThrow("java.lang.LinkageError", wrapper.Name + "." + ifmethod.Name + ifmethod.Signature);
						typeBuilder.DefineMethodOverride(mb, (MethodInfo)ifmethod.GetMethod());
						return;
					}
				}
				if(mce.IsMirandaMethod && mce.DeclaringType == wrapper)
				{
					// Miranda methods already have a methodimpl (if needed) to implement the correct interface method
				}
				else if(!mce.IsPublic)
				{
					// NOTE according to the ECMA spec it isn't legal for a privatescope method to be virtual, but this works and
					// it makes sense, so I hope the spec is wrong
					// UPDATE unfortunately, according to Serge Lidin the spec is correct, and it is not allowed to have virtual privatescope
					// methods. Sigh! So I have to use private methods and mangle the name
					MethodBuilder mb = typeBuilder.DefineMethod(mangledName, MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final, ifmethod.ReturnTypeForDefineMethod, ifmethod.GetParametersForDefineMethod());
					AttributeHelper.HideFromJava(mb);
					CodeEmitter.Create(mb).EmitThrow("java.lang.IllegalAccessError", wrapper.Name + "." + ifmethod.Name + ifmethod.Signature);
					typeBuilder.DefineMethodOverride(mb, (MethodInfo)ifmethod.GetMethod());
					wrapper.HasIncompleteInterfaceImplementation = true;
				}
				else if(mce.GetMethod() == null || mce.RealName != ifmethod.RealName)
				{
					MethodBuilder mb = typeBuilder.DefineMethod(mangledName, MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final, ifmethod.ReturnTypeForDefineMethod, ifmethod.GetParametersForDefineMethod());
					AttributeHelper.HideFromJava(mb);
					CodeEmitter ilGenerator = CodeEmitter.Create(mb);
					ilGenerator.Emit(OpCodes.Ldarg_0);
					int argc = mce.GetParameters().Length;
					for(int n = 0; n < argc; n++)
					{
						ilGenerator.Emit(OpCodes.Ldarg_S, (byte)(n + 1));
					}
					mce.EmitCallvirt(ilGenerator);
					ilGenerator.Emit(OpCodes.Ret);
					typeBuilder.DefineMethodOverride(mb, (MethodInfo)ifmethod.GetMethod());
				}
				else if(!ReflectUtil.IsSameAssembly(mce.DeclaringType.TypeAsTBD, typeBuilder))
				{
					// NOTE methods inherited from base classes in a different assembly do *not* automatically implement
					// interface methods, so we have to generate a stub here that doesn't do anything but call the base
					// implementation
					MethodBuilder mb = typeBuilder.DefineMethod(mangledName, MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final, ifmethod.ReturnTypeForDefineMethod, ifmethod.GetParametersForDefineMethod());
					typeBuilder.DefineMethodOverride(mb, (MethodInfo)ifmethod.GetMethod());
					AttributeHelper.HideFromJava(mb);
					CodeEmitter ilGenerator = CodeEmitter.Create(mb);
					ilGenerator.Emit(OpCodes.Ldarg_0);
					int argc = mce.GetParameters().Length;
					for(int n = 0; n < argc; n++)
					{
						ilGenerator.Emit(OpCodes.Ldarg_S, (byte)(n + 1));
					}
					mce.EmitCallvirt(ilGenerator);
					ilGenerator.Emit(OpCodes.Ret);
				}
			}
			else
			{
				if(!wrapper.IsAbstract)
				{
					// the type doesn't implement the interface method and isn't abstract either. The JVM allows this, but the CLR doesn't,
					// so we have to create a stub method that throws an AbstractMethodError
					MethodBuilder mb = typeBuilder.DefineMethod(mangledName, MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Private | MethodAttributes.Virtual | MethodAttributes.Final, ifmethod.ReturnTypeForDefineMethod, ifmethod.GetParametersForDefineMethod());
					AttributeHelper.HideFromJava(mb);
					CodeEmitter.Create(mb).EmitThrow("java.lang.AbstractMethodError", wrapper.Name + "." + ifmethod.Name + ifmethod.Signature);
					typeBuilder.DefineMethodOverride(mb, (MethodInfo)ifmethod.GetMethod());
					wrapper.HasIncompleteInterfaceImplementation = true;
				}
			}
		}

		internal static void ImplementInterfaceMethodStubs(TypeBuilder typeBuilder, DynamicTypeWrapper wrapper, Dictionary<TypeWrapper, TypeWrapper> doneSet, TypeWrapper interfaceTypeWrapper)
		{
			Debug.Assert(interfaceTypeWrapper.IsInterface);

			// make sure we don't do the same method twice
			if (doneSet.ContainsKey(interfaceTypeWrapper))
			{
				return;
			}
			doneSet.Add(interfaceTypeWrapper, interfaceTypeWrapper);
			foreach (MethodWrapper method in interfaceTypeWrapper.GetMethods())
			{
				if(!method.IsStatic && !method.IsDynamicOnly)
				{
					ImplementInterfaceMethodStubImpl(method, typeBuilder, wrapper);
				}
			}
			TypeWrapper[] interfaces = interfaceTypeWrapper.Interfaces;
			for(int i = 0; i < interfaces.Length; i++)
			{
				ImplementInterfaceMethodStubs(typeBuilder, wrapper, doneSet, interfaces[i]);
			}
		}

		[Conditional("DEBUG")]
		internal static void AssertFinished(Type type)
		{
			if(type != null)
			{
				while(type.HasElementType)
				{
					type = type.GetElementType();
				}
				Debug.Assert(!(type is TypeBuilder));
			}
		}

		internal void RunClassInit()
		{
			Type t = IsRemapped ? TypeAsBaseType : TypeAsTBD;
			if(t != null)
			{
				System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
			}
		}

		internal void EmitUnbox(CodeEmitter ilgen)
		{
			Debug.Assert(this.IsNonPrimitiveValueType);

			ilgen.LazyEmitUnboxSpecial(this.TypeAsTBD);
		}

		internal void EmitBox(CodeEmitter ilgen)
		{
			Debug.Assert(this.IsNonPrimitiveValueType);

			ilgen.LazyEmitBox(this.TypeAsTBD);
		}

		internal void EmitConvSignatureTypeToStackType(CodeEmitter ilgen)
		{
			if(IsUnloadable)
			{
			}
			else if(this == PrimitiveTypeWrapper.BYTE)
			{
				ilgen.Emit(OpCodes.Conv_I1);
			}
			else if(IsNonPrimitiveValueType)
			{
				EmitBox(ilgen);
			}
			else if(IsGhost)
			{
				LocalBuilder local = ilgen.DeclareLocal(TypeAsSignatureType);
				ilgen.Emit(OpCodes.Stloc, local);
				ilgen.Emit(OpCodes.Ldloca, local);
				ilgen.Emit(OpCodes.Ldfld, GhostRefField);
			}
		}

		// NOTE sourceType is optional and only used for interfaces,
		// it is *not* used to automatically downcast
		internal void EmitConvStackTypeToSignatureType(CodeEmitter ilgen, TypeWrapper sourceType)
		{
			if(!IsUnloadable)
			{
				if(IsGhost)
				{
					LocalBuilder local1 = ilgen.DeclareLocal(TypeAsLocalOrStackType);
					ilgen.Emit(OpCodes.Stloc, local1);
					LocalBuilder local2 = ilgen.DeclareLocal(TypeAsSignatureType);
					ilgen.Emit(OpCodes.Ldloca, local2);
					ilgen.Emit(OpCodes.Ldloc, local1);
					ilgen.Emit(OpCodes.Stfld, GhostRefField);
					ilgen.Emit(OpCodes.Ldloca, local2);
					ilgen.Emit(OpCodes.Ldobj, TypeAsSignatureType);
				}
					// because of the way interface merging works, any reference is valid
					// for any interface reference
				else if(IsInterfaceOrInterfaceArray && (sourceType == null || sourceType.IsUnloadable || !sourceType.IsAssignableTo(this)))
				{
					ilgen.EmitAssertType(TypeAsTBD);
					Profiler.Count("InterfaceDownCast");
				}
				else if(IsNonPrimitiveValueType)
				{
					EmitUnbox(ilgen);
				}
				else if(sourceType != null && sourceType.IsUnloadable)
				{
					ilgen.Emit(OpCodes.Castclass, TypeAsSignatureType);
				}
			}
		}

		internal virtual void EmitCheckcast(TypeWrapper context, CodeEmitter ilgen)
		{
			if(IsGhost)
			{
				ilgen.Emit(OpCodes.Dup);
				// TODO make sure we get the right "Cast" method and cache it
				// NOTE for dynamic ghosts we don't end up here because AotTypeWrapper overrides this method,
				// so we're safe to call GetMethod on TypeAsTBD (because it has to be a compiled type, if we're here)
				ilgen.Emit(OpCodes.Call, TypeAsTBD.GetMethod("Cast"));
				ilgen.Emit(OpCodes.Pop);
			}
			else if(IsGhostArray)
			{
				ilgen.Emit(OpCodes.Dup);
				// TODO make sure we get the right "CastArray" method and cache it
				// NOTE for dynamic ghosts we don't end up here because AotTypeWrapper overrides this method,
				// so we're safe to call GetMethod on TypeAsTBD (because it has to be a compiled type, if we're here)
				TypeWrapper tw = this;
				int rank = 0;
				while(tw.IsArray)
				{
					rank++;
					tw = tw.ElementTypeWrapper;
				}
				ilgen.Emit(OpCodes.Ldc_I4, rank);
				ilgen.Emit(OpCodes.Call, tw.TypeAsTBD.GetMethod("CastArray"));
				ilgen.Emit(OpCodes.Castclass, ArrayTypeWrapper.MakeArrayType(typeof(object), rank));
			}
			else
			{
				ilgen.EmitCastclass(TypeAsTBD);
			}
		}

		internal virtual void EmitInstanceOf(TypeWrapper context, CodeEmitter ilgen)
		{
			if(IsGhost)
			{
				// TODO make sure we get the right "IsInstance" method and cache it
				// NOTE for dynamic ghosts we don't end up here because DynamicTypeWrapper overrides this method,
				// so we're safe to call GetMethod on TypeAsTBD (because it has to be a compiled type, if we're here)
				ilgen.Emit(OpCodes.Call, TypeAsTBD.GetMethod("IsInstance"));
			}
			else if(IsGhostArray)
			{
				// TODO make sure we get the right "IsInstanceArray" method and cache it
				// NOTE for dynamic ghosts we don't end up here because DynamicTypeWrapper overrides this method,
				// so we're safe to call GetMethod on TypeAsTBD (because it has to be a compiled type, if we're here)
				TypeWrapper tw = this;
				int rank = 0;
				while(tw.IsArray)
				{
					rank++;
					tw = tw.ElementTypeWrapper;
				}
				ilgen.Emit(OpCodes.Ldc_I4, rank);
				ilgen.Emit(OpCodes.Call, tw.TypeAsTBD.GetMethod("IsInstanceArray"));
			}
			else
			{
				ilgen.LazyEmit_instanceof(TypeAsTBD);
			}
		}

		// NOTE don't call this method, call MethodWrapper.Link instead
		internal virtual MethodBase LinkMethod(MethodWrapper mw)
		{
			return mw.GetMethod();
		}

		// NOTE don't call this method, call FieldWrapper.Link instead
		internal virtual FieldInfo LinkField(FieldWrapper fw)
		{
			return fw.GetField();
		}

		internal virtual void EmitRunClassConstructor(CodeEmitter ilgen)
		{
		}

		internal virtual string GetGenericSignature()
		{
			return null;
		}

		internal virtual string GetGenericMethodSignature(MethodWrapper mw)
		{
			return null;
		}

		internal virtual string GetGenericFieldSignature(FieldWrapper fw)
		{
			return null;
		}

		internal virtual string[] GetEnclosingMethod()
		{
			return null;
		}

		internal virtual object[] GetDeclaredAnnotations()
		{
			return null;
		}

		internal virtual object[] GetMethodAnnotations(MethodWrapper mw)
		{
			return null;
		}

		internal virtual object[][] GetParameterAnnotations(MethodWrapper mw)
		{
			return null;
		}

		internal virtual object[] GetFieldAnnotations(FieldWrapper fw)
		{
			return null;
		}

		internal virtual string GetSourceFileName()
		{
			return null;
		}

		internal virtual int GetSourceLineNumber(MethodBase mb, int ilOffset)
		{
			return -1;
		}

#if !STATIC_COMPILER
		internal virtual object GetAnnotationDefault(MethodWrapper mw)
		{
			MethodBase mb = mw.GetMethod();
			if(mb != null)
			{
				if(mb.DeclaringType.Assembly.ReflectionOnly)
				{
					// TODO
					return null;
				}
				object[] attr = mb.GetCustomAttributes(typeof(AnnotationDefaultAttribute), false);
				if(attr.Length == 1)
				{
					return JVM.NewAnnotationElementValue(mw.DeclaringType.GetClassLoader().GetJavaClassLoader(), mw.ReturnType.ClassObject, ((AnnotationDefaultAttribute)attr[0]).Value);
				}
			}
			return null;
		}
#endif // !STATIC_COMPILER

		internal virtual Annotation Annotation
		{
			get
			{
				return null;
			}
		}

		internal virtual Type EnumType
		{
			get
			{
				return null;
			}
		}
	}

	sealed class UnloadableTypeWrapper : TypeWrapper
	{
		internal UnloadableTypeWrapper(string name)
			: base(TypeWrapper.UnloadableModifiersHack, name, null)
		{
#if STATIC_COMPILER
			if(name != "<verifier>")
			{
				if(name.StartsWith("["))
				{
					int skip = 1;
					while(name[skip++] == '[');
					name = name.Substring(skip, name.Length - skip - 1);
				}
				StaticCompiler.IssueMessage(Message.ClassNotFound, name);
			}
#endif
		}

		internal override ClassLoaderWrapper GetClassLoader()
		{
			return null;
		}

		internal override TypeWrapper EnsureLoadable(ClassLoaderWrapper loader)
		{
			TypeWrapper tw = loader.LoadClassByDottedNameFast(this.Name);
			if(tw == null)
			{
				throw new NoClassDefFoundError(this.Name);
			}
			return tw;
		}

		internal override string SigName
		{
			get
			{
				string name = Name;
				if(name.StartsWith("["))
				{
					return name;
				}
				return "L" + name + ";";
			}
		}

		protected override void LazyPublishMembers()
		{
			throw new InvalidOperationException("LazyPublishMembers called on UnloadableTypeWrapper: " + Name);
		}

		internal override Type TypeAsTBD
		{
			get
			{
				throw new InvalidOperationException("get_Type called on UnloadableTypeWrapper: " + Name);
			}
		}

		internal override TypeWrapper[] Interfaces
		{
			get
			{
				throw new InvalidOperationException("get_Interfaces called on UnloadableTypeWrapper: " + Name);
			}
		}

		internal override TypeWrapper[] InnerClasses
		{
			get
			{
				throw new InvalidOperationException("get_InnerClasses called on UnloadableTypeWrapper: " + Name);
			}
		}

		internal override TypeWrapper DeclaringTypeWrapper
		{
			get
			{
				throw new InvalidOperationException("get_DeclaringTypeWrapper called on UnloadableTypeWrapper: " + Name);
			}
		}

		internal override void Finish()
		{
			throw new InvalidOperationException("Finish called on UnloadableTypeWrapper: " + Name);
		}

		internal override void EmitCheckcast(TypeWrapper context, CodeEmitter ilgen)
		{
			ilgen.Emit(OpCodes.Ldtoken, context.TypeAsTBD);
			ilgen.Emit(OpCodes.Ldstr, Name);
			ilgen.Emit(OpCodes.Call, ByteCodeHelperMethods.DynamicCast);
		}

		internal override void EmitInstanceOf(TypeWrapper context, CodeEmitter ilgen)
		{
			ilgen.Emit(OpCodes.Ldtoken, context.TypeAsTBD);
			ilgen.Emit(OpCodes.Ldstr, Name);
			ilgen.Emit(OpCodes.Call, ByteCodeHelperMethods.DynamicInstanceOf);
		}
	}

	sealed class PrimitiveTypeWrapper : TypeWrapper
	{
		internal static readonly PrimitiveTypeWrapper BYTE = new PrimitiveTypeWrapper(typeof(byte), "B");
		internal static readonly PrimitiveTypeWrapper CHAR = new PrimitiveTypeWrapper(typeof(char), "C");
		internal static readonly PrimitiveTypeWrapper DOUBLE = new PrimitiveTypeWrapper(typeof(double), "D");
		internal static readonly PrimitiveTypeWrapper FLOAT = new PrimitiveTypeWrapper(typeof(float), "F");
		internal static readonly PrimitiveTypeWrapper INT = new PrimitiveTypeWrapper(typeof(int), "I");
		internal static readonly PrimitiveTypeWrapper LONG = new PrimitiveTypeWrapper(typeof(long), "J");
		internal static readonly PrimitiveTypeWrapper SHORT = new PrimitiveTypeWrapper(typeof(short), "S");
		internal static readonly PrimitiveTypeWrapper BOOLEAN = new PrimitiveTypeWrapper(typeof(bool), "Z");
		internal static readonly PrimitiveTypeWrapper VOID = new PrimitiveTypeWrapper(typeof(void), "V");

		private readonly Type type;
		private readonly string sigName;

		private PrimitiveTypeWrapper(Type type, string sigName)
			: base(Modifiers.Public | Modifiers.Abstract | Modifiers.Final, null, null)
		{
			this.type = type;
			this.sigName = sigName;
		}

		internal static bool IsPrimitiveType(Type type)
		{
			return type == BYTE.type
				|| type == CHAR.type
				|| type == DOUBLE.type
				|| type == FLOAT.type
				|| type == INT.type
				|| type == LONG.type
				|| type == SHORT.type
				|| type == BOOLEAN.type
				|| type == VOID.type;
		}

		internal override string SigName
		{
			get
			{
				return sigName;
			}
		}

		internal override ClassLoaderWrapper GetClassLoader()
		{
			return ClassLoaderWrapper.GetBootstrapClassLoader();
		}

		internal override Type TypeAsTBD
		{
			get
			{
				return type;
			}
		}

		internal override TypeWrapper[] Interfaces
		{
			get
			{
				return TypeWrapper.EmptyArray;
			}
		}

		internal override TypeWrapper[] InnerClasses
		{
			get
			{
				return TypeWrapper.EmptyArray;
			}
		}

		internal override TypeWrapper DeclaringTypeWrapper
		{
			get
			{
				return null;
			}
		}

		internal override void Finish()
		{
		}

		public override string ToString()
		{
			return "PrimitiveTypeWrapper[" + sigName + "]";
		}
	}

	static class BakedTypeCleanupHack
	{
#if NET_4_0
		internal static void Process(DynamicTypeWrapper wrapper) { }
#else
		private static readonly FieldInfo m_methodBuilder = typeof(ConstructorBuilder).GetField("m_methodBuilder", BindingFlags.Instance | BindingFlags.NonPublic);
		private static readonly FieldInfo[] methodBuilderFields = GetFieldList(typeof(MethodBuilder), new string[]
			{
				"m_ilGenerator",
				"m_ubBody",
				"m_RVAFixups",
				"m_mdMethodFixups",
				"m_localSignature",
				"m_localSymInfo",
				"m_exceptions",
				"m_parameterTypes",
				"m_retParam",
				"m_returnType",
				"m_signature"
			});
		private static readonly FieldInfo[] fieldBuilderFields = GetFieldList(typeof(FieldBuilder), new string[]
			{
				"m_data",
				"m_fieldType",
		});

		private static bool IsSupportedVersion
		{
			get
			{
				return Environment.Version.Major == 2 && Environment.Version.Minor == 0 && Environment.Version.Build == 50727 && Environment.Version.Revision == 4016;
			}
		}

		[SecurityCritical]
		[SecurityTreatAsSafe]
		private static FieldInfo[] GetFieldList(Type type, string[] list)
		{
			if(JVM.SafeGetEnvironmentVariable("IKVM_DISABLE_TYPEBUILDER_HACK") != null || !IsSupportedVersion)
			{
				return null;
			}
			if(!SecurityManager.IsGranted(new SecurityPermission(SecurityPermissionFlag.Assertion)) ||
				!SecurityManager.IsGranted(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess)))
			{
				return null;
			}
			FieldInfo[] fields = new FieldInfo[list.Length];
			for(int i = 0; i < list.Length; i++)
			{
				fields[i] = type.GetField(list[i], BindingFlags.Instance | BindingFlags.NonPublic);
				if(fields[i] == null)
				{
					return null;
				}
			}
			return fields;
		}

		[SecurityCritical]
		[SecurityTreatAsSafe]
		internal static void Process(DynamicTypeWrapper wrapper)
		{
			if(m_methodBuilder != null && methodBuilderFields != null && fieldBuilderFields != null)
			{
				foreach(MethodWrapper mw in wrapper.GetMethods())
				{
					MethodBuilder mb = mw.GetMethod() as MethodBuilder;
					if(mb == null)
					{
						ConstructorBuilder cb = mw.GetMethod() as ConstructorBuilder;
						if(cb != null)
						{
							new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
							mb = (MethodBuilder)m_methodBuilder.GetValue(cb);
							CodeAccessPermission.RevertAssert();
						}
					}
					if(mb != null)
					{
						new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
						foreach(FieldInfo fi in methodBuilderFields)
						{
							fi.SetValue(mb, null);
						}
						CodeAccessPermission.RevertAssert();
					}
				}
				foreach(FieldWrapper fw in wrapper.GetFields())
				{
					FieldBuilder fb = fw.GetField() as FieldBuilder;
					if(fb != null)
					{
						new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Assert();
						foreach(FieldInfo fi in fieldBuilderFields)
						{
							fi.SetValue(fb, null);
						}
						CodeAccessPermission.RevertAssert();
					}
				}
			}
		}
#endif // NET_4_0
	}

	class CompiledTypeWrapper : TypeWrapper
	{
		private readonly Type type;
		private TypeWrapper[] interfaces;
		private TypeWrapper[] innerclasses;
		private MethodInfo clinitMethod;
		private Modifiers reflectiveModifiers;

		internal static CompiledTypeWrapper newInstance(string name, Type type)
		{
			// TODO since ghost and remapped types can only exist in the core library assembly, we probably
			// should be able to remove the Type.IsDefined() tests in most cases
			if(type.IsValueType && AttributeHelper.IsGhostInterface(type))
			{
				return new CompiledGhostTypeWrapper(name, type);
			}
			else if(AttributeHelper.IsRemappedType(type))
			{
				return new CompiledRemappedTypeWrapper(name, type);
			}
			else
			{
				return new CompiledTypeWrapper(name, type);
			}
		}

		private sealed class CompiledRemappedTypeWrapper : CompiledTypeWrapper
		{
			private readonly Type remappedType;

			internal CompiledRemappedTypeWrapper(string name, Type type)
				: base(name, type)
			{
				RemappedTypeAttribute attr = AttributeHelper.GetRemappedType(type);
				if(attr == null)
				{
					throw new InvalidOperationException();
				}
				remappedType = attr.Type;
			}

			internal override Type TypeAsTBD
			{
				get
				{
					return remappedType;
				}
			}

			internal override bool IsRemapped
			{
				get
				{
					return true;
				}
			}

			protected override void LazyPublishMembers()
			{
				List<MethodWrapper> methods = new List<MethodWrapper>();
				List<FieldWrapper> fields = new List<FieldWrapper>();
				MemberInfo[] members = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
				foreach(MemberInfo m in members)
				{
					if(!AttributeHelper.IsHideFromJava(m))
					{
						MethodBase method = m as MethodBase;
						if(method != null &&
							(remappedType.IsSealed || !m.Name.StartsWith("instancehelper_")) &&
							(!remappedType.IsSealed || method.IsStatic))
						{
							methods.Add(CreateRemappedMethodWrapper(method));
						}
						else
						{
							FieldInfo field = m as FieldInfo;
							if(field != null)
							{
								fields.Add(CreateFieldWrapper(field));
							}
						}
					}
				}
				// if we're a remapped interface, we need to get the methods from the real interface
				if(remappedType.IsInterface)
				{
					Type nestedHelper = type.GetNestedType("__Helper", BindingFlags.Public | BindingFlags.Static);
					foreach(RemappedInterfaceMethodAttribute m in AttributeHelper.GetRemappedInterfaceMethods(type))
					{
						MethodInfo method = remappedType.GetMethod(m.MappedTo);
						MethodInfo mbHelper = method;
						ExModifiers modifiers = AttributeHelper.GetModifiers(method, false);
						string name;
						string sig;
						TypeWrapper retType;
						TypeWrapper[] paramTypes;
						MemberFlags flags = MemberFlags.None;
						GetNameSigFromMethodBase(method, out name, out sig, out retType, out paramTypes, ref flags);
						if(nestedHelper != null)
						{
							mbHelper = nestedHelper.GetMethod(m.Name);
							if(mbHelper == null)
							{
								mbHelper = method;
							}
						}
						methods.Add(new CompiledRemappedMethodWrapper(this, m.Name, sig, method, retType, paramTypes, modifiers, false, mbHelper, null));
					}
				}
				SetMethods(methods.ToArray());
				SetFields(fields.ToArray());
			}

			private MethodWrapper CreateRemappedMethodWrapper(MethodBase mb)
			{
				ExModifiers modifiers = AttributeHelper.GetModifiers(mb, false);
				string name;
				string sig;
				TypeWrapper retType;
				TypeWrapper[] paramTypes;
				MemberFlags flags = MemberFlags.None;
				GetNameSigFromMethodBase(mb, out name, out sig, out retType, out paramTypes, ref flags);
				MethodInfo mbHelper = mb as MethodInfo;
				bool hideFromReflection = mbHelper != null && AttributeHelper.IsHideFromReflection(mbHelper);
				MethodInfo mbNonvirtualHelper = null;
				if(!mb.IsStatic && !mb.IsConstructor)
				{
					ParameterInfo[] parameters = mb.GetParameters();
					Type[] argTypes = new Type[parameters.Length + 1];
					argTypes[0] = remappedType;
					for(int i = 0; i < parameters.Length; i++)
					{
						argTypes[i + 1] = parameters[i].ParameterType;
					}
					MethodInfo helper = type.GetMethod("instancehelper_" + mb.Name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, argTypes, null);
					if(helper != null)
					{
						mbHelper = helper;
					}
					mbNonvirtualHelper = type.GetMethod("nonvirtualhelper/" + mb.Name, BindingFlags.NonPublic | BindingFlags.Static, null, argTypes, null);
				}
				return new CompiledRemappedMethodWrapper(this, name, sig, mb, retType, paramTypes, modifiers, hideFromReflection, mbHelper, mbNonvirtualHelper);
			}
		}

		private sealed class CompiledGhostTypeWrapper : CompiledTypeWrapper
		{
			private FieldInfo ghostRefField;
			private Type typeAsBaseType;

			internal CompiledGhostTypeWrapper(string name, Type type)
				: base(name, type)
			{
			}

			internal override Type TypeAsBaseType
			{
				get
				{
					if(typeAsBaseType == null)
					{
						typeAsBaseType = type.GetNestedType("__Interface");
					}
					return typeAsBaseType;
				}
			}

			internal override FieldInfo GhostRefField
			{
				get
				{
					if(ghostRefField == null)
					{
						ghostRefField = type.GetField("__<ref>");
					}
					return ghostRefField;
				}
			}

			internal override bool IsGhost
			{
				get
				{
					return true;
				}
			}
		}

		internal static string GetName(Type type)
		{
			Debug.Assert(!type.IsGenericType);
			Debug.Assert(AttributeHelper.IsJavaModule(type.Module));

			// look for our custom attribute, that contains the real name of the type (for inner classes)
			InnerClassAttribute attr = AttributeHelper.GetInnerClass(type);
			if(attr != null)
			{
				string name = attr.InnerClassName;
				if(name != null)
				{
					return name;
				}
				if(type.DeclaringType != null)
				{
					return GetName(type.DeclaringType) + "$" + type.Name;
				}
			}
			return type.FullName;
		}

		// TODO consider resolving the baseType lazily
		private static TypeWrapper GetBaseTypeWrapper(Type type)
		{
			if(type.IsInterface || AttributeHelper.IsGhostInterface(type))
			{
				return null;
			}
			else if(type.BaseType == null)
			{
				// System.Object must appear to be derived from java.lang.Object
				return CoreClasses.java.lang.Object.Wrapper;
			}
			else
			{
				RemappedTypeAttribute attr = AttributeHelper.GetRemappedType(type);
				if(attr != null)
				{
					if(attr.Type == typeof(object))
					{
						return null;
					}
					else
					{
						return CoreClasses.java.lang.Object.Wrapper;
					}
				}
				TypeWrapper tw = null;
				while(tw == null)
				{
					type = type.BaseType;
					tw = ClassLoaderWrapper.GetWrapperFromType(type);
				}
				return tw;
			}
		}

		private CompiledTypeWrapper(ExModifiers exmod, string name, TypeWrapper baseTypeWrapper)
			: base(exmod.Modifiers, name, baseTypeWrapper)
		{
			this.IsInternal = exmod.IsInternal;
		}

		private CompiledTypeWrapper(string name, Type type)
			: this(GetModifiers(type), name, GetBaseTypeWrapper(type))
		{
			Debug.Assert(!(type is TypeBuilder));
			Debug.Assert(!type.Name.EndsWith("[]"));

			this.type = type;
		}

		internal override ClassLoaderWrapper GetClassLoader()
		{
			return ClassLoaderWrapper.GetAssemblyClassLoader(type.Assembly);
		}

		private static ExModifiers GetModifiers(Type type)
		{
			ModifiersAttribute attr = AttributeHelper.GetModifiersAttribute(type);
			if(attr != null)
			{
				return new ExModifiers(attr.Modifiers, attr.IsInternal);
			}
			// only returns public, protected, private, final, static, abstract and interface (as per
			// the documentation of Class.getModifiers())
			Modifiers modifiers = 0;
			if(type.IsPublic)
			{
				modifiers |= Modifiers.Public;
			}
				// TODO do we really need to look for nested attributes? I think all inner classes will have the ModifiersAttribute.
			else if(type.IsNestedPublic)
			{
				modifiers |= Modifiers.Public | Modifiers.Static;
			}
			else if(type.IsNestedPrivate)
			{
				modifiers |= Modifiers.Private | Modifiers.Static;
			}
			else if(type.IsNestedFamily || type.IsNestedFamORAssem)
			{
				modifiers |= Modifiers.Protected | Modifiers.Static;
			}
			else if(type.IsNestedAssembly || type.IsNestedFamANDAssem)
			{
				modifiers |= Modifiers.Static;
			}

			if(type.IsSealed)
			{
				modifiers |= Modifiers.Final;
			}
			if(type.IsAbstract)
			{
				modifiers |= Modifiers.Abstract;
			}
			if(type.IsInterface)
			{
				modifiers |= Modifiers.Interface;
			}
			return new ExModifiers(modifiers, false);
		}

		internal override bool HasStaticInitializer
		{
			get
			{
				// trigger LazyPublishMembers
				GetMethods();
				return clinitMethod != null;
			}
		}

		internal override TypeWrapper[] Interfaces
		{
			get
			{
				if(interfaces == null)
				{
					// NOTE instead of getting the interfaces list from Type, we use a custom
					// attribute to list the implemented interfaces, because Java reflection only
					// reports the interfaces *directly* implemented by the type, not the inherited
					// interfaces. This is significant for serialVersionUID calculation (for example).
					ImplementsAttribute attr = AttributeHelper.GetImplements(type);
					if(attr != null)
					{
						string[] interfaceNames = attr.Interfaces;
						TypeWrapper[] interfaceWrappers = new TypeWrapper[interfaceNames.Length];
						for(int i = 0; i < interfaceWrappers.Length; i++)
						{
							interfaceWrappers[i] = GetClassLoader().LoadClassByDottedName(interfaceNames[i]);
						}
						this.interfaces = interfaceWrappers;
					}
					else
					{
						interfaces = TypeWrapper.EmptyArray;
					}
				}
				return interfaces;
			}
		}

		internal override TypeWrapper[] InnerClasses
		{
			get
			{
				// TODO why are we caching this?
				if(innerclasses == null)
				{
					Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
					List<TypeWrapper> wrappers = new List<TypeWrapper>();
					for(int i = 0; i < nestedTypes.Length; i++)
					{
						if(!AttributeHelper.IsHideFromJava(nestedTypes[i]) && !nestedTypes[i].Name.StartsWith("__<"))
						{
							wrappers.Add(ClassLoaderWrapper.GetWrapperFromType(nestedTypes[i]));
						}
					}
					foreach(string s in AttributeHelper.GetNonNestedInnerClasses(type))
					{
						wrappers.Add(GetClassLoader().LoadClassByDottedName(s));
					}
					innerclasses = wrappers.ToArray();
				}
				return innerclasses;
			}
		}

		internal override TypeWrapper DeclaringTypeWrapper
		{
			get
			{
				Type declaringType = type.DeclaringType;
				if(declaringType != null)
				{
					return ClassLoaderWrapper.GetWrapperFromType(declaringType);
				}
				string decl = AttributeHelper.GetNonNestedOuterClasses(type);
				if(decl != null)
				{
					return GetClassLoader().LoadClassByDottedName(decl);
				}
				return null;
			}
		}

		internal override Modifiers ReflectiveModifiers
		{
			get
			{
				if (reflectiveModifiers == 0)
				{
					InnerClassAttribute attr = AttributeHelper.GetInnerClass(type);
					if (attr != null)
					{
						reflectiveModifiers = attr.Modifiers;
					}
					else
					{
						reflectiveModifiers = Modifiers;
					}
				}
				return reflectiveModifiers;
			}
		}

		internal override Type TypeAsBaseType
		{
			get
			{
				return type;
			}
		}

		private void SigTypePatchUp(string sigtype, ref TypeWrapper type)
		{
			if(sigtype != type.SigName)
			{
				// if type is an array, we know that it is a ghost array, because arrays of unloadable are compiled
				// as object (not as arrays of object)
				if(type.IsArray)
				{
					type = GetClassLoader().FieldTypeWrapperFromSig(sigtype);
				}
				else if(type.IsPrimitive)
				{
					type = DotNetTypeWrapper.GetWrapperFromDotNetType(type.TypeAsTBD);
					if(sigtype != type.SigName)
					{
						throw new InvalidOperationException();
					}
				}
				else if(type.IsNonPrimitiveValueType)
				{
					// this can't happen and even if it does happen we cannot return
					// UnloadableTypeWrapper because that would result in incorrect code
					// being generated
					throw new InvalidOperationException();
				}
				else
				{
					if(sigtype[0] == 'L')
					{
						sigtype = sigtype.Substring(1, sigtype.Length - 2);
					}
					try
					{
						TypeWrapper tw = GetClassLoader().LoadClassByDottedNameFast(sigtype);
						if(tw != null && tw.IsRemapped)
						{
							type = tw;
							return;
						}
					}
					catch(RetargetableJavaException)
					{
					}
					type = new UnloadableTypeWrapper(sigtype);
				}
			}
		}

		private static void ParseSig(string sig, out string[] sigparam, out string sigret)
		{
			List<string> list = new List<string>();
			int pos = 1;
			for(;;)
			{
				switch(sig[pos])
				{
					case 'L':
					{
						int end = sig.IndexOf(';', pos) + 1;
						list.Add(sig.Substring(pos, end - pos));
						pos = end;
						break;
					}
					case '[':
					{
						int skip = 1;
						while(sig[pos + skip] == '[') skip++;
						if(sig[pos + skip] == 'L')
						{
							int end = sig.IndexOf(';', pos) + 1;
							list.Add(sig.Substring(pos, end - pos));
							pos = end;
						}
						else
						{
							skip++;
							list.Add(sig.Substring(pos, skip));
							pos += skip;
						}
						break;
					}
					case ')':
						sigparam = list.ToArray();
						sigret = sig.Substring(pos + 1);
						return;
					default:
						list.Add(sig.Substring(pos, 1));
						pos++;
						break;
				}
			}
		}

		private void GetNameSigFromMethodBase(MethodBase method, out string name, out string sig, out TypeWrapper retType, out TypeWrapper[] paramTypes, ref MemberFlags flags)
		{
			retType = method is ConstructorInfo ? PrimitiveTypeWrapper.VOID : ClassLoaderWrapper.GetWrapperFromType(((MethodInfo)method).ReturnType);
			ParameterInfo[] parameters = method.GetParameters();
			int len = parameters.Length;
			if(len > 0
				&& parameters[len - 1].ParameterType == CoreClasses.ikvm.@internal.CallerID.Wrapper.TypeAsSignatureType
				&& !method.DeclaringType.IsInterface
				&& GetClassLoader() == ClassLoaderWrapper.GetBootstrapClassLoader())
			{
				len--;
				flags |= MemberFlags.CallerID;
			}
			paramTypes = new TypeWrapper[len];
			for(int i = 0; i < len; i++)
			{
				paramTypes[i] = ClassLoaderWrapper.GetWrapperFromType(parameters[i].ParameterType);
			}
			NameSigAttribute attr = AttributeHelper.GetNameSig(method);
			if(attr != null)
			{
				name = attr.Name;
				sig = attr.Sig;
				string[] sigparams;
				string sigret;
				ParseSig(sig, out sigparams, out sigret);
				// HACK newhelper methods have a return type, but it should be void
				if(name == "<init>")
				{
					retType = PrimitiveTypeWrapper.VOID;
				}
				SigTypePatchUp(sigret, ref retType);
				// if we have a remapped method, the paramTypes array contains an additional entry for "this" so we have
				// to remove that
				if(paramTypes.Length == sigparams.Length + 1)
				{
					TypeWrapper[] temp = paramTypes;
					paramTypes = new TypeWrapper[sigparams.Length];
					Array.Copy(temp, 1, paramTypes, 0, paramTypes.Length);
				}
				Debug.Assert(sigparams.Length == paramTypes.Length);
				for(int i = 0; i < sigparams.Length; i++)
				{
					SigTypePatchUp(sigparams[i], ref paramTypes[i]);
				}
			}
			else
			{
				if(method is ConstructorInfo)
				{
					name = method.IsStatic ? "<clinit>" : "<init>";
				}
				else
				{
					name = method.Name;
				}
				System.Text.StringBuilder sb = new System.Text.StringBuilder("(");
				foreach(TypeWrapper tw in paramTypes)
				{
					sb.Append(tw.SigName);
				}
				sb.Append(")");
				sb.Append(retType.SigName);
				sig = sb.ToString();
			}
		}

		private sealed class DelegateConstructorMethodWrapper : MethodWrapper
		{
			private readonly ConstructorInfo constructor;
			private MethodInfo invoke;

			private DelegateConstructorMethodWrapper(TypeWrapper tw, TypeWrapper iface, ExModifiers mods)
				: base(tw, StringConstants.INIT, "(" + iface.SigName + ")V", null, PrimitiveTypeWrapper.VOID, new TypeWrapper[] { iface }, mods.Modifiers, mods.IsInternal ? MemberFlags.InternalAccess : MemberFlags.None)
			{
			}

			internal DelegateConstructorMethodWrapper(TypeWrapper tw, MethodBase method)
				: this(tw, tw.GetClassLoader().LoadClassByDottedName(tw.Name + DotNetTypeWrapper.DelegateInterfaceSuffix), AttributeHelper.GetModifiers(method, false))
			{
				constructor = (ConstructorInfo)method;
			}

			protected override void DoLinkMethod()
			{
				MethodWrapper mw = GetParameters()[0].GetMethods()[0];
				mw.Link();
				invoke = (MethodInfo)mw.GetMethod();
			}

			internal override void EmitNewobj(CodeEmitter ilgen, MethodAnalyzer ma, int opcodeIndex)
			{
				ilgen.Emit(OpCodes.Dup);
				ilgen.Emit(OpCodes.Ldvirtftn, invoke);
				ilgen.Emit(OpCodes.Newobj, constructor);
			}
		}

		protected override void LazyPublishMembers()
		{
			bool isDelegate = type.BaseType == typeof(MulticastDelegate);
			clinitMethod = type.GetMethod("__<clinit>", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			List<MethodWrapper> methods = new List<MethodWrapper>();
			List<FieldWrapper> fields = new List<FieldWrapper>();
			MemberInfo[] members = type.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
			foreach(MemberInfo m in members)
			{
				if(!AttributeHelper.IsHideFromJava(m))
				{
					MethodBase method = m as MethodBase;
					if(method != null)
					{
						if(method.IsSpecialName && 
							(method.Name == "op_Implicit" || method.Name.StartsWith("__<")))
						{
							// skip
						}
						else if(isDelegate && method.IsConstructor && !method.IsStatic)
						{
							methods.Add(new DelegateConstructorMethodWrapper(this, method));
						}
						else
						{
							string name;
							string sig;
							TypeWrapper retType;
							TypeWrapper[] paramTypes;
							MethodInfo mi = method as MethodInfo;
							bool hideFromReflection = mi != null ? AttributeHelper.IsHideFromReflection(mi) : false;
							MemberFlags flags = hideFromReflection ? MemberFlags.HideFromReflection : MemberFlags.None;
							GetNameSigFromMethodBase(method, out name, out sig, out retType, out paramTypes, ref flags);
							ExModifiers mods = AttributeHelper.GetModifiers(method, false);
							if(mods.IsInternal)
							{
								flags |= MemberFlags.InternalAccess;
							}
							methods.Add(MethodWrapper.Create(this, name, sig, method, retType, paramTypes, mods.Modifiers, flags));
						}
					}
					else
					{
						FieldInfo field = m as FieldInfo;
						if(field != null)
						{
							if(field.IsSpecialName && field.Name.StartsWith("__<"))
							{
								// skip
							}
							else
							{
								fields.Add(CreateFieldWrapper(field));
							}
						}
						else
						{
							// NOTE explictly defined properties (in map.xml) are decorated with HideFromJava,
							// so we don't need to worry about them here
							PropertyInfo property = m as PropertyInfo;
							if(property != null)
							{
								// Only AccessStub properties (marked by HideFromReflectionAttribute)
								// are considered here
								if(AttributeHelper.IsHideFromReflection(property))
								{
									fields.Add(new CompiledAccessStubFieldWrapper(this, property));
								}
								else
								{
									// If the property has a ModifiersAttribute, we know that it is an explicit property
									// (defined in Java source by an @ikvm.lang.Property annotation)
									ModifiersAttribute mods = AttributeHelper.GetModifiersAttribute(property);
									if(mods != null)
									{
										fields.Add(new CompiledPropertyFieldWrapper(this, property, new ExModifiers(mods.Modifiers, mods.IsInternal)));
									}
									else
									{
										fields.Add(CreateFieldWrapper(property));
									}
								}
							}
						}
					}
				}
			}
			SetMethods(methods.ToArray());
			SetFields(fields.ToArray());
		}

		private class CompiledRemappedMethodWrapper : SmartMethodWrapper
		{
			private MethodInfo mbHelper;
#if !STATIC_COMPILER
			private MethodInfo mbNonvirtualHelper;
#endif

			internal CompiledRemappedMethodWrapper(TypeWrapper declaringType, string name, string sig, MethodBase method, TypeWrapper returnType, TypeWrapper[] parameterTypes, ExModifiers modifiers, bool hideFromReflection, MethodInfo mbHelper, MethodInfo mbNonvirtualHelper)
				: base(declaringType, name, sig, method, returnType, parameterTypes, modifiers.Modifiers,
						(modifiers.IsInternal ? MemberFlags.InternalAccess : MemberFlags.None) | (hideFromReflection ? MemberFlags.HideFromReflection : MemberFlags.None))
			{
				this.mbHelper = mbHelper;
#if !STATIC_COMPILER
				this.mbNonvirtualHelper = mbNonvirtualHelper;
#endif
			}

			protected override void CallImpl(CodeEmitter ilgen)
			{
				MethodBase mb = GetMethod();
				MethodInfo mi = mb as MethodInfo;
				if(mi != null)
				{
					ilgen.Emit(OpCodes.Call, mi);
				}
				else
				{
					ilgen.Emit(OpCodes.Call, (ConstructorInfo)mb);
				}
			}

			protected override void CallvirtImpl(CodeEmitter ilgen)
			{
				Debug.Assert(!mbHelper.IsStatic || mbHelper.Name.StartsWith("instancehelper_") || mbHelper.DeclaringType.Name == "__Helper");
				if(mbHelper.IsPublic)
				{
					ilgen.Emit(mbHelper.IsStatic ? OpCodes.Call : OpCodes.Callvirt, mbHelper);
				}
				else
				{
					// HACK the helper is not public, this means that we're dealing with finalize or clone
					ilgen.Emit(OpCodes.Callvirt, (MethodInfo)GetMethod());
				}
			}

			protected override void NewobjImpl(CodeEmitter ilgen)
			{
				MethodBase mb = GetMethod();
				MethodInfo mi = mb as MethodInfo;
				if(mi != null)
				{
					Debug.Assert(mi.Name == "newhelper");
					ilgen.Emit(OpCodes.Call, mi);
				}
				else
				{
					ilgen.Emit(OpCodes.Newobj, (ConstructorInfo)mb);
				}
			}

#if !STATIC_COMPILER && !FIRST_PASS
			[HideFromJava]
			protected override object InvokeNonvirtualRemapped(object obj, object[] args)
			{
				Type[] p1 = GetParametersForDefineMethod();
				Type[] argTypes = new Type[p1.Length + 1];
				p1.CopyTo(argTypes, 1);
				argTypes[0] = this.DeclaringType.TypeAsSignatureType;
				MethodInfo mi = mbNonvirtualHelper;
				if (mi == null)
				{
					mi = mbHelper;
				}
				object[] args1 = new object[args.Length + 1];
				args1[0] = obj;
				args.CopyTo(args1, 1);
				return mi.Invoke(null, args1);
			}

			internal override void EmitCallvirtReflect(CodeEmitter ilgen)
			{
				MethodBase mb = mbHelper != null ? mbHelper : GetMethod();
				ilgen.Emit(mb.IsStatic ? OpCodes.Call : OpCodes.Callvirt, (MethodInfo)mb);
			}
#endif // !STATIC_COMPILER

			internal string GetGenericSignature()
			{
				SignatureAttribute attr = AttributeHelper.GetSignature(mbHelper != null ? mbHelper : GetMethod());
				if(attr != null)
				{
					return attr.Signature;
				}
				return null;
			}
		}

		private FieldWrapper CreateFieldWrapper(PropertyInfo prop)
		{
			MethodInfo getter = prop.GetGetMethod(true);
			ExModifiers modifiers = AttributeHelper.GetModifiers(getter, false);
			// for static methods AttributeHelper.GetModifiers won't set the Final flag
			modifiers = new ExModifiers(modifiers.Modifiers | Modifiers.Final, modifiers.IsInternal);
			string name = prop.Name;
			TypeWrapper type = ClassLoaderWrapper.GetWrapperFromType(prop.PropertyType);
			NameSigAttribute attr = AttributeHelper.GetNameSig(getter);
			if(attr != null)
			{
				name = attr.Name;
				SigTypePatchUp(attr.Sig, ref type);
			}
			return new GetterFieldWrapper(this, type, null, name, type.SigName, modifiers, getter, prop);
		}

		private FieldWrapper CreateFieldWrapper(FieldInfo field)
		{
			ExModifiers modifiers = AttributeHelper.GetModifiers(field, false);
			string name = field.Name;
			TypeWrapper type = ClassLoaderWrapper.GetWrapperFromType(field.FieldType);
			NameSigAttribute attr = AttributeHelper.GetNameSig(field);
			if(attr != null)
			{
				name = attr.Name;
				SigTypePatchUp(attr.Sig, ref type);
			}

			if(field.IsLiteral)
			{
				MemberFlags flags = MemberFlags.None;
				if(AttributeHelper.IsHideFromReflection(field))
				{
					flags |= MemberFlags.HideFromReflection;
				}
				if(modifiers.IsInternal)
				{
					flags |= MemberFlags.InternalAccess;
				}
				return new ConstantFieldWrapper(this, type, name, type.SigName, modifiers.Modifiers, field, null, flags);
			}
			else
			{
				return FieldWrapper.Create(this, type, field, name, type.SigName, modifiers);
			}
		}

		internal override Type TypeAsTBD
		{
			get
			{
				return type;
			}
		}

		internal override bool IsMapUnsafeException
		{
			get
			{
				return AttributeHelper.IsExceptionIsUnsafeForMapping(type);
			}
		}

		internal override void Finish()
		{
			if(BaseTypeWrapper != null)
			{
				BaseTypeWrapper.Finish();
			}
			foreach(TypeWrapper tw in this.Interfaces)
			{
				tw.Finish();
			}
		}

		internal override void EmitRunClassConstructor(CodeEmitter ilgen)
		{
			// trigger LazyPublishMembers
			GetMethods();
			if(clinitMethod != null)
			{
				ilgen.Emit(OpCodes.Call, clinitMethod);
			}
		}

		internal override string GetGenericSignature()
		{
			SignatureAttribute attr = AttributeHelper.GetSignature(type);
			if(attr != null)
			{
				return attr.Signature;
			}
			return null;
		}

		internal override string GetGenericMethodSignature(MethodWrapper mw)
		{
			if(mw is CompiledRemappedMethodWrapper)
			{
				return ((CompiledRemappedMethodWrapper)mw).GetGenericSignature();
			}
			MethodBase mb = mw.GetMethod();
			if(mb != null)
			{
				SignatureAttribute attr = AttributeHelper.GetSignature(mb);
				if(attr != null)
				{
					return attr.Signature;
				}
			}
			return null;
		}

		internal override string GetGenericFieldSignature(FieldWrapper fw)
		{
			FieldInfo fi = fw.GetField();
			if(fi != null)
			{
				SignatureAttribute attr = AttributeHelper.GetSignature(fi);
				if(attr != null)
				{
					return attr.Signature;
				}
			}
			else
			{
				GetterFieldWrapper getter = fw as GetterFieldWrapper;
				if(getter != null)
				{
					SignatureAttribute attr = AttributeHelper.GetSignature(getter.GetGetter());
					if(attr != null)
					{
						return attr.Signature;
					}
				}
			}
			return null;
		}

		internal override string[] GetEnclosingMethod()
		{
			EnclosingMethodAttribute enc = AttributeHelper.GetEnclosingMethodAttribute(type);
			if (enc != null)
			{
				return new string[] { enc.ClassName, enc.MethodName, enc.MethodSignature };
			}
			return null;
		}

		internal override object[] GetDeclaredAnnotations()
		{
			if(type.Assembly.ReflectionOnly)
			{
				// TODO on Whidbey this must be implemented
				return null;
			}
			return type.GetCustomAttributes(false);
		}

		internal override object[] GetMethodAnnotations(MethodWrapper mw)
		{
			MethodBase mb = mw.GetMethod();
			if(mb == null)
			{
				// delegate constructor
				return null;
			}
			if(mb.DeclaringType.Assembly.ReflectionOnly)
			{
				// TODO on Whidbey this must be implemented
				return null;
			}
			return mb.GetCustomAttributes(false);
		}

		internal override object[][] GetParameterAnnotations(MethodWrapper mw)
		{
			MethodBase mb = mw.GetMethod();
			if(mb == null)
			{
				// delegate constructor
				return null;
			}
			if(mb.DeclaringType.Assembly.ReflectionOnly)
			{
				// TODO on Whidbey this must be implemented
				return null;
			}
			ParameterInfo[] parameters = mb.GetParameters();
			int skip = 0;
			if(mb.IsStatic && !mw.IsStatic && mw.Name != "<init>")
			{
				skip = 1;
			}
			int skipEnd = 0;
			if(mw.HasCallerID)
			{
				skipEnd = 1;
			}
			object[][] attribs = new object[parameters.Length - skip - skipEnd][];
			for(int i = skip; i < parameters.Length - skipEnd; i++)
			{
				attribs[i - skip] = parameters[i].GetCustomAttributes(false);
			}
			return attribs;
		}

		internal override object[] GetFieldAnnotations(FieldWrapper fw)
		{
			FieldInfo field = fw.GetField();
			if(field != null)
			{
				if (field.DeclaringType.Assembly.ReflectionOnly)
				{
					// TODO on Whidbey this must be implemented
					return null;
				}
				return field.GetCustomAttributes(false);
			}
			GetterFieldWrapper getter = fw as GetterFieldWrapper;
			if(getter != null)
			{
				if (getter.GetGetter().DeclaringType.Assembly.ReflectionOnly)
				{
					// TODO on Whidbey this must be implemented
					return null;
				}
				return getter.GetGetter().GetCustomAttributes(false);
			}
			CompiledPropertyFieldWrapper prop = fw as CompiledPropertyFieldWrapper;
			if(prop != null)
			{
				if (prop.GetProperty().DeclaringType.Assembly.ReflectionOnly)
				{
					// TODO on Whidbey this must be implemented
					return null;
				}
				return prop.GetProperty().GetCustomAttributes(false);
			}
			return new object[0];
		}

		private class CompiledAnnotation : Annotation
		{
			private Type type;

			internal CompiledAnnotation(Type type)
			{
				this.type = type;
			}

			private CustomAttributeBuilder MakeCustomAttributeBuilder(object annotation)
			{
				return new CustomAttributeBuilder(type.GetConstructor(new Type[] { typeof(object[]) }), new object[] { annotation });
			}

			internal override void Apply(ClassLoaderWrapper loader, TypeBuilder tb, object annotation)
			{
				annotation = QualifyClassNames(loader, annotation);
				tb.SetCustomAttribute(MakeCustomAttributeBuilder(annotation));
			}

			internal override void Apply(ClassLoaderWrapper loader, ConstructorBuilder cb, object annotation)
			{
				annotation = QualifyClassNames(loader, annotation);
				cb.SetCustomAttribute(MakeCustomAttributeBuilder(annotation));
			}

			internal override void Apply(ClassLoaderWrapper loader, MethodBuilder mb, object annotation)
			{
				annotation = QualifyClassNames(loader, annotation);
				mb.SetCustomAttribute(MakeCustomAttributeBuilder(annotation));
			}

			internal override void Apply(ClassLoaderWrapper loader, FieldBuilder fb, object annotation)
			{
				annotation = QualifyClassNames(loader, annotation);
				fb.SetCustomAttribute(MakeCustomAttributeBuilder(annotation));
			}

			internal override void Apply(ClassLoaderWrapper loader, ParameterBuilder pb, object annotation)
			{
				annotation = QualifyClassNames(loader, annotation);
				pb.SetCustomAttribute(MakeCustomAttributeBuilder(annotation));
			}

			internal override void Apply(ClassLoaderWrapper loader, AssemblyBuilder ab, object annotation)
			{
				annotation = QualifyClassNames(loader, annotation);
				ab.SetCustomAttribute(MakeCustomAttributeBuilder(annotation));
			}

			internal override void Apply(ClassLoaderWrapper loader, PropertyBuilder pb, object annotation)
			{
				annotation = QualifyClassNames(loader, annotation);
				pb.SetCustomAttribute(MakeCustomAttributeBuilder(annotation));
			}
		}

		internal override Annotation Annotation
		{
			get
			{
				string annotationAttribute = AttributeHelper.GetAnnotationAttributeType(type);
				if(annotationAttribute != null)
				{
					return new CompiledAnnotation(type.Assembly.GetType(annotationAttribute, true));
				}
				return null;
			}
		}

		internal override Type EnumType
		{
			get
			{
				if((this.Modifiers & Modifiers.Enum) != 0)
				{
					return type.GetNestedType("__Enum");
				}
				return null;
			}
		}

		internal override string GetSourceFileName()
		{
			object[] attr = type.GetCustomAttributes(typeof(SourceFileAttribute), false);
			if(attr.Length == 1)
			{
				return ((SourceFileAttribute)attr[0]).SourceFile;
			}
			if(type.Module.IsDefined(typeof(SourceFileAttribute), false))
			{
				return type.Name + ".java";
			}
			return null;
		}

		internal override int GetSourceLineNumber(MethodBase mb, int ilOffset)
		{
			object[] attr = mb.GetCustomAttributes(typeof(LineNumberTableAttribute), false);
			if(attr.Length == 1)
			{
				return ((LineNumberTableAttribute)attr[0]).GetLineNumber(ilOffset);
			}
			return -1;
		}

		internal override bool IsFastClassLiteralSafe
		{
			get { return true; }
		}
	}

	sealed class DotNetTypeWrapper : TypeWrapper
	{
		private const string NamePrefix = "cli.";
		internal const string DelegateInterfaceSuffix = "$Method";
		internal const string AttributeAnnotationSuffix = "$Annotation";
		internal const string AttributeAnnotationReturnValueSuffix = "$__ReturnValue";
		internal const string AttributeAnnotationMultipleSuffix = "$__Multiple";
		internal const string EnumEnumSuffix = "$__Enum";
		internal const string GenericEnumEnumTypeName = "ikvm.internal.EnumEnum`1";
		internal const string GenericDelegateInterfaceTypeName = "ikvm.internal.DelegateInterface`1";
		internal const string GenericAttributeAnnotationTypeName = "ikvm.internal.AttributeAnnotation`1";
		internal const string GenericAttributeAnnotationReturnValueTypeName = "ikvm.internal.AttributeAnnotationReturnValue`1";
		internal const string GenericAttributeAnnotationMultipleTypeName = "ikvm.internal.AttributeAnnotationMultiple`1";
		private static readonly Dictionary<Type, TypeWrapper> types = new Dictionary<Type, TypeWrapper>();
		private readonly Type type;
		private TypeWrapper[] innerClasses;
		private TypeWrapper outerClass;
		private TypeWrapper[] interfaces;

		private static Modifiers GetModifiers(Type type)
		{
			Modifiers modifiers = 0;
			if(type.IsPublic)
			{
				modifiers |= Modifiers.Public;
			}
			else if(type.IsNestedPublic)
			{
				modifiers |= Modifiers.Static;
				if(IsVisible(type))
				{
					modifiers |= Modifiers.Public;
				}
			}
			else if(type.IsNestedPrivate)
			{
				modifiers |= Modifiers.Private | Modifiers.Static;
			}
			else if(type.IsNestedFamily || type.IsNestedFamORAssem)
			{
				modifiers |= Modifiers.Protected | Modifiers.Static;
			}
			else if(type.IsNestedAssembly || type.IsNestedFamANDAssem)
			{
				modifiers |= Modifiers.Static;
			}

			if(type.IsSealed)
			{
				modifiers |= Modifiers.Final;
			}
			else if(type.IsAbstract) // we can't be abstract if we're final
			{
				modifiers |= Modifiers.Abstract;
			}
			if(type.IsInterface)
			{
				modifiers |= Modifiers.Interface;
			}
			return modifiers;
		}

		// NOTE when this is called on a remapped type, the "warped" underlying type name is returned.
		// E.g. GetName(typeof(object)) returns "cli.System.Object".
		internal static string GetName(Type type)
		{
			Debug.Assert(!type.Name.EndsWith("[]") && !AttributeHelper.IsJavaModule(type.Module));

			string name = type.FullName;

			if(name == null)
			{
				// generic type parameters don't have a full name
				return null;
			}

			if(type.IsGenericType && !type.ContainsGenericParameters)
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				sb.Append(MangleTypeName(type.GetGenericTypeDefinition().FullName));
				sb.Append("_$$$_");
				string sep = "";
				foreach(Type t1 in type.GetGenericArguments())
				{
					Type t = t1;
					sb.Append(sep);
					// NOTE we can't use ClassLoaderWrapper.GetWrapperFromType() here to get t's name,
					// because we might be resolving a generic type that refers to a type that is in
					// the process of being constructed.
					//
					// For example:
					//   class Base<T> { } 
					//   class Derived : Base<Derived> { }
					//
					while(ClassLoaderWrapper.IsVector(t))
					{
						t = t.GetElementType();
						sb.Append('A');
					}
					if(PrimitiveTypeWrapper.IsPrimitiveType(t))
					{
						sb.Append(ClassLoaderWrapper.GetWrapperFromType(t).SigName);
					}
					else
					{
						string s;
						if(ClassLoaderWrapper.IsRemappedType(t))
						{
							s = ClassLoaderWrapper.GetWrapperFromType(t).Name;
						}
						else if(AttributeHelper.IsJavaModule(t.Module))
						{
							s = CompiledTypeWrapper.GetName(t);
						}
						else
						{
							s = DotNetTypeWrapper.GetName(t);
						}
						// only do the mangling for non-generic types (because we don't want to convert
						// the double underscores in two adjacent _$$$_ or _$$$$_ markers)
						if (s.IndexOf("_$$$_") == -1)
						{
							s = s.Replace("__", "$$005F$$005F");
							s = s.Replace(".", "__");
						}
						sb.Append('L').Append(s);
					}
					sep = "_$$_";
				}
				sb.Append("_$$$$_");
				return sb.ToString();
			}

			if(AttributeHelper.IsNoPackagePrefix(type)
				&& name.IndexOf('$') == -1)
			{
				return name.Replace('+', '$');
			}

			return MangleTypeName(name);
		}

		private static string MangleTypeName(string name)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder(NamePrefix, NamePrefix.Length + name.Length);
			bool escape = false;
			bool nested = false;
			for(int i = 0; i < name.Length; i++)
			{
				char c = name[i];
				if(c == '+' && !escape && (sb.Length == 0 || sb[sb.Length - 1] != '$'))
				{
					nested = true;
					sb.Append('$');
				}
				else if("_0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ".IndexOf(c) != -1
					|| (c == '.' && !escape && !nested))
				{
					sb.Append(c);
				}
				else
				{
					sb.Append("$$");
					sb.Append(string.Format("{0:X4}", (int)c));
				}
				if(c == '\\')
				{
					escape = !escape;
				}
				else
				{
					escape = false;
				}
			}
			return sb.ToString();
		}

		// NOTE if the name is not a valid mangled type name, no demangling is done and the
		// original string is returned
		// NOTE we don't enforce canonical form, this is not required, because we cannot
		// guarantee it for unprefixed names anyway, so the caller is responsible for
		// ensuring that the original name was in fact the canonical name.
		internal static string DemangleTypeName(string name)
		{
			if(!name.StartsWith(NamePrefix))
			{
				return name.Replace('$', '+');
			}
			System.Text.StringBuilder sb = new System.Text.StringBuilder(name.Length - NamePrefix.Length);
			for(int i = NamePrefix.Length; i < name.Length; i++)
			{
				char c = name[i];
				if(c == '$')
				{
					if(i + 1 < name.Length && name[i + 1] != '$')
					{
						sb.Append('+');
					}
					else
					{
						i++;
						if(i + 5 > name.Length)
						{
							return name;
						}
						int digit0 = "0123456789ABCDEF".IndexOf(name[++i]);
						int digit1 = "0123456789ABCDEF".IndexOf(name[++i]);
						int digit2 = "0123456789ABCDEF".IndexOf(name[++i]);
						int digit3 = "0123456789ABCDEF".IndexOf(name[++i]);
						if(digit0 == -1 || digit1 == -1 || digit2 == -1 || digit3 == -1)
						{
							return name;
						}
						sb.Append((char)((digit0 << 12) + (digit1 << 8) + (digit2 << 4) + digit3));
					}
				}
				else
				{
					sb.Append(c);
				}
			}
			return sb.ToString();
		}

		// TODO from a perf pov it may be better to allow creation of TypeWrappers,
		// but to simply make sure they don't have ClassObject
		internal static bool IsAllowedOutside(Type type)
		{
			// SECURITY we never expose types from IKVM.Runtime, because doing so would lead to a security hole,
			// since the reflection implementation lives inside this assembly, all internal members would
			// be accessible through Java reflection.
#if !FIRST_PASS && !STATIC_COMPILER
			if(type.Assembly == typeof(DotNetTypeWrapper).Assembly)
			{
				return false;
			}
			if(type.Assembly == IKVM.NativeCode.java.lang.SecurityManager.jniAssembly)
			{
				return false;
			}
#endif
			return true;
		}

		// We allow open generic types to be visible to Java code as very limited classes (or interfaces).
		// They are always package private and have the abstract and final modifiers set, this makes them
		// inaccessible and invalid from a Java point of view. The intent is to avoid any usage of these
		// classes. They exist solely for the purpose of stack walking, because the .NET runtime will report
		// open generic types when walking the stack (as a performance optimization). We cannot (reliably) map
		// these classes to their instantiations, so we report the open generic type class instead.
		// Note also that these classes can only be used as a "handle" to the type, they expose no members,
		// don't implement any interfaces and the base class is always object.
		private sealed class OpenGenericTypeWrapper : TypeWrapper
		{
			private readonly Type type;

			private static Modifiers GetModifiers(Type type)
			{
				Modifiers modifiers = Modifiers.Abstract | Modifiers.Final;
				if (type.IsInterface)
				{
					modifiers |= Modifiers.Interface;
				}
				return modifiers;
			}

			internal OpenGenericTypeWrapper(Type type, string name)
				: base(GetModifiers(type), name, type.IsInterface ? null : CoreClasses.java.lang.Object.Wrapper)
			{
				this.type = type;
			}

			internal override TypeWrapper DeclaringTypeWrapper
			{
				get { return null; }
			}

			internal override TypeWrapper[] InnerClasses
			{
				get { return TypeWrapper.EmptyArray; }
			}

			internal override TypeWrapper[] Interfaces
			{
				get { return TypeWrapper.EmptyArray; }
			}

			internal override Type TypeAsTBD
			{
				get { return type; }
			}

			internal override void Finish()
			{
			}

			internal override ClassLoaderWrapper GetClassLoader()
			{
				return ClassLoaderWrapper.GetAssemblyClassLoader(type.Assembly);
			}

			protected override void LazyPublishMembers()
			{
				SetFields(FieldWrapper.EmptyArray);
				SetMethods(MethodWrapper.EmptyArray);
			}
		}

		private sealed class DelegateInnerClassTypeWrapper : TypeWrapper
		{
			private readonly Type fakeType;

			internal DelegateInnerClassTypeWrapper(string name, Type delegateType, ClassLoaderWrapper classLoader)
				: base(Modifiers.Public | Modifiers.Interface | Modifiers.Abstract, name, null)
			{
#if STATIC_COMPILER
				this.fakeType = FakeTypes.GetDelegateType(delegateType);
#elif !FIRST_PASS
				this.fakeType = typeof(ikvm.@internal.DelegateInterface<>).MakeGenericType(delegateType);
#endif
				MethodInfo invoke = delegateType.GetMethod("Invoke");
				ParameterInfo[] parameters = invoke.GetParameters();
				TypeWrapper[] argTypeWrappers = new TypeWrapper[parameters.Length];
				System.Text.StringBuilder sb = new System.Text.StringBuilder("(");
				for(int i = 0; i < parameters.Length; i++)
				{
					argTypeWrappers[i] = ClassLoaderWrapper.GetWrapperFromType(parameters[i].ParameterType);
					sb.Append(argTypeWrappers[i].SigName);
				}
				TypeWrapper returnType = ClassLoaderWrapper.GetWrapperFromType(invoke.ReturnType);
				sb.Append(")").Append(returnType.SigName);
				MethodWrapper invokeMethod = new DynamicOnlyMethodWrapper(this, "Invoke", sb.ToString(), returnType, argTypeWrappers);
				SetMethods(new MethodWrapper[] { invokeMethod });
				SetFields(FieldWrapper.EmptyArray);
			}

			internal override TypeWrapper DeclaringTypeWrapper
			{
				get
				{
					return ClassLoaderWrapper.GetWrapperFromType(fakeType.GetGenericArguments()[0]);
				}
			}

			internal override void Finish()
			{
			}

			internal override ClassLoaderWrapper GetClassLoader()
			{
				return DeclaringTypeWrapper.GetClassLoader();
			}

			internal override TypeWrapper[] InnerClasses
			{
				get
				{
					return TypeWrapper.EmptyArray;
				}
			}

			internal override TypeWrapper[] Interfaces
			{
				get
				{
					return TypeWrapper.EmptyArray;
				}
			}

			internal override Type TypeAsTBD
			{
				get
				{
					return fakeType;
				}
			}

			internal override bool IsFastClassLiteralSafe
			{
				get { return true; }
			}
		}

		private class DynamicOnlyMethodWrapper : MethodWrapper, ICustomInvoke
		{
			internal DynamicOnlyMethodWrapper(TypeWrapper declaringType, string name, string sig, TypeWrapper returnType, TypeWrapper[] parameterTypes)
				: base(declaringType, name, sig, null, returnType, parameterTypes, Modifiers.Public | Modifiers.Abstract, MemberFlags.None)
			{
			}

			internal override bool IsDynamicOnly
			{
				get
				{
					return true;
				}
			}

#if !STATIC_COMPILER && !FIRST_PASS
			object ICustomInvoke.Invoke(object obj, object[] args, ikvm.@internal.CallerID callerID)
			{
				// a DynamicOnlyMethodWrapper is an interface method, but now that we've been called on an actual object instance,
				// we can resolve to a real method and call that instead
				TypeWrapper tw = TypeWrapper.FromClass(NativeCode.ikvm.runtime.Util.getClassFromObject(obj));
				MethodWrapper mw = tw.GetMethodWrapper(this.Name, this.Signature, true);
				if(mw == null)
				{
					throw new java.lang.AbstractMethodError(tw.Name + "." + this.Name + this.Signature);
				}
				java.lang.reflect.Method m = (java.lang.reflect.Method)mw.ToMethodOrConstructor(true);
				m.@override = true;
				return m.invoke(obj, args, callerID);
			}
#endif // !STATIC_COMPILER && !FIRST_PASS
		}

		private sealed class EnumEnumTypeWrapper : TypeWrapper
		{
			private readonly Type fakeType;

			internal EnumEnumTypeWrapper(string name, Type enumType)
				: base(Modifiers.Public | Modifiers.Enum | Modifiers.Final, name, ClassLoaderWrapper.LoadClassCritical("java.lang.Enum"))
			{
#if STATIC_COMPILER
				this.fakeType = FakeTypes.GetEnumType(enumType);
#elif !FIRST_PASS
				if(enumType.Assembly.ReflectionOnly)
				{
					TypeWrapper decl = ClassLoaderWrapper.GetWrapperFromType(enumType);
					TypeWrapperFactory factory = ClassLoaderWrapper.GetBootstrapClassLoader().GetTypeWrapperFactory();
					string basename = "<ReflectionOnlyType>" + enumType.FullName;
					name = basename;
					int index = 0;
					while(!factory.ReserveName(name))
					{
						name = basename + (++index);
					}
					enumType = factory.ModuleBuilder.DefineEnum(name, TypeAttributes.Public, typeof(int)).CreateType();
					ClassLoaderWrapper.GetBootstrapClassLoader().SetWrapperForType(enumType, decl);
				}
				this.fakeType = typeof(ikvm.@internal.EnumEnum<>).MakeGenericType(enumType);
#endif
			}

			internal object GetUnspecifiedValue()
			{
				return ((EnumFieldWrapper)GetFieldWrapper("__unspecified", this.SigName)).GetValue();
			}

			private class EnumFieldWrapper : FieldWrapper
			{
				private readonly int ordinal;
				private object val;

				internal EnumFieldWrapper(TypeWrapper tw, string name, int ordinal)
					: base(tw, tw, name, tw.SigName, Modifiers.Public | Modifiers.Static | Modifiers.Final | Modifiers.Enum, null, MemberFlags.None)
				{
					this.ordinal = ordinal;
				}

				internal object GetValue()
				{
					if(val == null)
					{
						System.Threading.Interlocked.CompareExchange(ref val, Activator.CreateInstance(this.DeclaringType.TypeAsTBD, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new object[] { this.Name, ordinal }, null), null);
					}
					return val;
				}

				protected override void EmitGetImpl(CodeEmitter ilgen)
				{
					// TODO we should throw a NoSuchFieldError if at runtime we find out that the "field" doesn't exist
					ilgen.LazyEmitLoadClass(this.DeclaringType);
					ilgen.Emit(OpCodes.Ldstr, this.Name);
					this.DeclaringType.BaseTypeWrapper.GetMethodWrapper("valueOf", "(Ljava.lang.Class;Ljava.lang.String;)Ljava.lang.Enum;", false).EmitCall(ilgen);
					ilgen.Emit(OpCodes.Castclass, this.DeclaringType.TypeAsTBD);
				}

				protected override void EmitSetImpl(CodeEmitter ilgen)
				{
				}
			}

			private class EnumValuesMethodWrapper : MethodWrapper, ICustomInvoke
			{
				internal EnumValuesMethodWrapper(TypeWrapper declaringType)
					: base(declaringType, "values", "()[" + declaringType.SigName, null, declaringType.MakeArrayType(1), TypeWrapper.EmptyArray, Modifiers.Public | Modifiers.Static, MemberFlags.None)
				{
				}

				internal override bool IsDynamicOnly
				{
					get
					{
						return true;
					}
				}

#if !STATIC_COMPILER && !FIRST_PASS
				object ICustomInvoke.Invoke(object obj, object[] args, ikvm.@internal.CallerID callerID)
				{
					FieldWrapper[] values = this.DeclaringType.GetFields();
					object[] array = (object[])Array.CreateInstance(this.DeclaringType.TypeAsArrayType, values.Length);
					for(int i = 0; i < values.Length; i++)
					{
						array[i] = ((EnumFieldWrapper)values[i]).GetValue();
					}
					return array;
				}
#endif // !STATIC_COMPILER && !FIRST_PASS
			}

			private class EnumValueOfMethodWrapper : MethodWrapper, ICustomInvoke
			{
				internal EnumValueOfMethodWrapper(TypeWrapper declaringType)
					: base(declaringType, "valueOf", "(Ljava.lang.String;)" + declaringType.SigName, null, declaringType, new TypeWrapper[] { CoreClasses.java.lang.String.Wrapper }, Modifiers.Public | Modifiers.Static, MemberFlags.None)
				{
				}

				internal override bool IsDynamicOnly
				{
					get
					{
						return true;
					}
				}

#if !STATIC_COMPILER && !FIRST_PASS
				object ICustomInvoke.Invoke(object obj, object[] args, ikvm.@internal.CallerID callerID)
				{
					FieldWrapper[] values = this.DeclaringType.GetFields();
					for(int i = 0; i < values.Length; i++)
					{
						if(values[i].Name.Equals(args[0]))
						{
							return ((EnumFieldWrapper)values[i]).GetValue();
						}
					}
					throw new java.lang.IllegalArgumentException("" + args[0]);
				}
#endif // !STATIC_COMPILER && !FIRST_PASS
			}

			protected override void LazyPublishMembers()
			{
				List<FieldWrapper> fields = new List<FieldWrapper>();
				int ordinal = 0;
				foreach(FieldInfo field in this.DeclaringTypeWrapper.TypeAsTBD.GetFields(BindingFlags.Static | BindingFlags.Public))
				{
					if(field.IsLiteral)
					{
						fields.Add(new EnumFieldWrapper(this, field.Name, ordinal++));
					}
				}
				// TODO if the enum already has an __unspecified value, rename this one
				fields.Add(new EnumFieldWrapper(this, "__unspecified", ordinal++));
				SetFields(fields.ToArray());
				SetMethods(new MethodWrapper[] { new EnumValuesMethodWrapper(this), new EnumValueOfMethodWrapper(this) });
				base.LazyPublishMembers();
			}

			internal override TypeWrapper DeclaringTypeWrapper
			{
				get
				{
					return ClassLoaderWrapper.GetWrapperFromType(fakeType.GetGenericArguments()[0]);
				}
			}

			internal override void Finish()
			{
			}

			internal override ClassLoaderWrapper GetClassLoader()
			{
				return DeclaringTypeWrapper.GetClassLoader();
			}

			internal override TypeWrapper[] InnerClasses
			{
				get
				{
					return TypeWrapper.EmptyArray;
				}
			}

			internal override TypeWrapper[] Interfaces
			{
				get
				{
					return TypeWrapper.EmptyArray;
				}
			}

			internal override Type TypeAsTBD
			{
				get
				{
					return fakeType;
				}
			}

			internal override bool IsFastClassLiteralSafe
			{
				get { return true; }
			}
		}

		private abstract class AttributeAnnotationTypeWrapperBase : TypeWrapper
		{
			internal AttributeAnnotationTypeWrapperBase(string name)
				: base(Modifiers.Public | Modifiers.Interface | Modifiers.Abstract | Modifiers.Annotation, name, null)
			{
			}

			internal sealed override void Finish()
			{
			}

			internal sealed override ClassLoaderWrapper GetClassLoader()
			{
				return DeclaringTypeWrapper.GetClassLoader();
			}

			internal sealed override TypeWrapper[] Interfaces
			{
				get
				{
					return new TypeWrapper[] { ClassLoaderWrapper.GetBootstrapClassLoader().LoadClassByDottedName("java.lang.annotation.Annotation") };
				}
			}

			internal sealed override bool IsFastClassLiteralSafe
			{
				get { return true; }
			}
		}

		private sealed class AttributeAnnotationTypeWrapper : AttributeAnnotationTypeWrapperBase
		{
			private readonly Type fakeType;
			private readonly Type attributeType;
			private TypeWrapper[] innerClasses;

			internal AttributeAnnotationTypeWrapper(string name, Type attributeType)
				: base(name)
			{
#if STATIC_COMPILER
				this.fakeType = FakeTypes.GetAttributeType(attributeType);
#elif !FIRST_PASS
				this.fakeType = typeof(ikvm.@internal.AttributeAnnotation<>).MakeGenericType(attributeType);
#endif
				this.attributeType = attributeType;
			}

			private static bool IsSupportedType(Type type)
			{
				// Java annotations only support one-dimensional arrays
				if(type.IsArray)
				{
					type = type.GetElementType();
				}
				return type == typeof(string)
					|| type == typeof(bool)
					|| type == typeof(byte)
					|| type == typeof(char)
					|| type == typeof(short)
					|| type == typeof(int)
					|| type == typeof(float)
					|| type == typeof(long)
					|| type == typeof(double)
					|| type == typeof(Type)
					|| type.IsEnum;
			}

			internal static void GetConstructors(Type type, out ConstructorInfo defCtor, out ConstructorInfo singleOneArgCtor)
			{
				defCtor = null;
				int oneArgCtorCount = 0;
				ConstructorInfo oneArgCtor = null;
				foreach(ConstructorInfo ci in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
				{
					ParameterInfo[] args = ci.GetParameters();
					if(args.Length == 0)
					{
						defCtor = ci;
					}
					else if(args.Length == 1)
					{
						// HACK special case for p/invoke StructLayout attribute
						if(type == typeof(System.Runtime.InteropServices.StructLayoutAttribute) && args[0].ParameterType == typeof(short))
						{
							// we skip this constructor, so that the other one will be visible
							continue;
						}
						if(IsSupportedType(args[0].ParameterType))
						{
							oneArgCtor = ci;
							oneArgCtorCount++;
						}
						else
						{
							// set to two to make sure we don't see the oneArgCtor as viable
							oneArgCtorCount = 2;
						}
					}
				}
				singleOneArgCtor = oneArgCtorCount == 1 ? oneArgCtor : null;
			}

			private class AttributeAnnotationMethodWrapper : DynamicOnlyMethodWrapper
			{
				private bool optional;

				internal AttributeAnnotationMethodWrapper(AttributeAnnotationTypeWrapper tw, string name, Type type, bool optional)
					: this(tw, name, MapType(type, false), optional)
				{
				}

				private static TypeWrapper MapType(Type type, bool isArray)
				{
					if(type == typeof(string))
					{
						return CoreClasses.java.lang.String.Wrapper;
					}
					else if(type == typeof(bool))
					{
						return PrimitiveTypeWrapper.BOOLEAN;
					}
					else if(type == typeof(byte))
					{
						return PrimitiveTypeWrapper.BYTE;
					}
					else if(type == typeof(char))
					{
						return PrimitiveTypeWrapper.CHAR;
					}
					else if(type == typeof(short))
					{
						return PrimitiveTypeWrapper.SHORT;
					}
					else if(type == typeof(int))
					{
						return PrimitiveTypeWrapper.INT;
					}
					else if(type == typeof(float))
					{
						return PrimitiveTypeWrapper.FLOAT;
					}
					else if(type == typeof(long))
					{
						return PrimitiveTypeWrapper.LONG;
					}
					else if(type == typeof(double))
					{
						return PrimitiveTypeWrapper.DOUBLE;
					}
					else if(type == typeof(Type))
					{
						return CoreClasses.java.lang.Class.Wrapper;
					}
					else if (type.IsEnum)
					{
						foreach (TypeWrapper tw in ClassLoaderWrapper.GetWrapperFromType(type).InnerClasses)
						{
							if (tw is EnumEnumTypeWrapper)
							{
								if (!isArray && AttributeHelper.IsDefined(type, typeof(FlagsAttribute)))
								{
									return tw.MakeArrayType(1);
								}
								return tw;
							}
						}
						throw new InvalidOperationException();
					}
					else if(!isArray && type.IsArray)
					{
						return MapType(type.GetElementType(), true).MakeArrayType(1);
					}
					else
					{
						throw new NotImplementedException();
					}
				}

				private AttributeAnnotationMethodWrapper(AttributeAnnotationTypeWrapper tw, string name, TypeWrapper returnType, bool optional)
					: base(tw, name, "()" + returnType.SigName, returnType, TypeWrapper.EmptyArray)
				{
					this.optional = optional;
				}

				internal bool IsOptional
				{
					get
					{
						return optional;
					}
				}
			}

			protected override void LazyPublishMembers()
			{
				List<MethodWrapper> methods = new List<MethodWrapper>();
				ConstructorInfo defCtor;
				ConstructorInfo singleOneArgCtor;
				GetConstructors(attributeType, out defCtor, out singleOneArgCtor);
				if(singleOneArgCtor != null)
				{
					methods.Add(new AttributeAnnotationMethodWrapper(this, "value", singleOneArgCtor.GetParameters()[0].ParameterType, defCtor != null));
				}
				foreach(PropertyInfo pi in attributeType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
				{
					if(pi.CanRead && pi.CanWrite && IsSupportedType(pi.PropertyType))
					{
						methods.Add(new AttributeAnnotationMethodWrapper(this, pi.Name, pi.PropertyType, true));
					}
				}
				foreach(FieldInfo fi in attributeType.GetFields(BindingFlags.Public | BindingFlags.Instance))
				{
					// TODO add other field validations to make sure it is appropriate
					if(!fi.IsInitOnly && IsSupportedType(fi.FieldType))
					{
						methods.Add(new AttributeAnnotationMethodWrapper(this, fi.Name, fi.FieldType, true));
					}
				}
				SetMethods(methods.ToArray());
				base.LazyPublishMembers();
			}

#if !STATIC_COMPILER && !FIRST_PASS
			internal override object GetAnnotationDefault(MethodWrapper mw)
			{
				if(((AttributeAnnotationMethodWrapper)mw).IsOptional)
				{
					if (mw.ReturnType == PrimitiveTypeWrapper.BOOLEAN)
					{
						return java.lang.Boolean.FALSE;
					}
					else if(mw.ReturnType == PrimitiveTypeWrapper.BYTE)
					{
						return java.lang.Byte.valueOf((byte)0);
					}
					else if(mw.ReturnType == PrimitiveTypeWrapper.CHAR)
					{
						return java.lang.Character.valueOf((char)0);
					}
					else if(mw.ReturnType == PrimitiveTypeWrapper.SHORT)
					{
						return java.lang.Short.valueOf((short)0);
					}
					else if(mw.ReturnType == PrimitiveTypeWrapper.INT)
					{
						return java.lang.Integer.valueOf(0);
					}
					else if(mw.ReturnType == PrimitiveTypeWrapper.FLOAT)
					{
						return java.lang.Float.valueOf(0F);
					}
					else if(mw.ReturnType == PrimitiveTypeWrapper.LONG)
					{
						return java.lang.Long.valueOf(0L);
					}
					else if(mw.ReturnType == PrimitiveTypeWrapper.DOUBLE)
					{
						return java.lang.Double.valueOf(0D);
					}
					else if(mw.ReturnType == CoreClasses.java.lang.String.Wrapper)
					{
						return "";
					}
					else if(mw.ReturnType == CoreClasses.java.lang.Class.Wrapper)
					{
						return (java.lang.Class)typeof(ikvm.@internal.__unspecified);
					}
					else if(mw.ReturnType is EnumEnumTypeWrapper)
					{
						EnumEnumTypeWrapper eetw = (EnumEnumTypeWrapper)mw.ReturnType;
						return eetw.GetUnspecifiedValue();
					}
					else if(mw.ReturnType.IsArray)
					{
						return Array.CreateInstance(mw.ReturnType.TypeAsArrayType, 0);
					}
				}
				return null;
			}
#endif // !STATIC_COMPILER && !FIRST_PASS

			internal override TypeWrapper DeclaringTypeWrapper
			{
				get
				{
					return ClassLoaderWrapper.GetWrapperFromType(attributeType);
				}
			}

			internal override Type TypeAsTBD
			{
				get
				{
					return fakeType;
				}
			}

			private sealed class ReturnValueAnnotationTypeWrapper : AttributeAnnotationTypeWrapperBase
			{
				private readonly Type fakeType;
				private readonly AttributeAnnotationTypeWrapper declaringType;

				internal ReturnValueAnnotationTypeWrapper(AttributeAnnotationTypeWrapper declaringType)
					: base(declaringType.Name + AttributeAnnotationReturnValueSuffix)
				{
#if STATIC_COMPILER
					this.fakeType = FakeTypes.GetAttributeReturnValueType(declaringType.attributeType);
#elif !FIRST_PASS
					this.fakeType = typeof(ikvm.@internal.AttributeAnnotationReturnValue<>).MakeGenericType(declaringType.attributeType);
#endif
					this.declaringType = declaringType;
				}

				protected override void LazyPublishMembers()
				{
					TypeWrapper tw = declaringType;
					if(declaringType.GetAttributeUsage().AllowMultiple)
					{
						tw = tw.MakeArrayType(1);
					}
					SetMethods(new MethodWrapper[] { new DynamicOnlyMethodWrapper(this, "value", "()" + tw.SigName, tw, TypeWrapper.EmptyArray) });
					SetFields(FieldWrapper.EmptyArray);
				}

				internal override TypeWrapper DeclaringTypeWrapper
				{
					get
					{
						return declaringType;
					}
				}

				internal override TypeWrapper[] InnerClasses
				{
					get
					{
						return TypeWrapper.EmptyArray;
					}
				}

				internal override Type TypeAsTBD
				{
					get
					{
						return fakeType;
					}
				}

#if !STATIC_COMPILER && !FIRST_PASS
				internal override object[] GetDeclaredAnnotations()
				{
					java.util.HashMap targetMap = new java.util.HashMap();
					targetMap.put("value", new java.lang.annotation.ElementType[] { java.lang.annotation.ElementType.METHOD });
					java.util.HashMap retentionMap = new java.util.HashMap();
					retentionMap.put("value", java.lang.annotation.RetentionPolicy.RUNTIME);
					return new object[] {
						java.lang.reflect.Proxy.newProxyInstance(null, new java.lang.Class[] { typeof(java.lang.annotation.Target) }, new sun.reflect.annotation.AnnotationInvocationHandler(typeof(java.lang.annotation.Target), targetMap)),
						java.lang.reflect.Proxy.newProxyInstance(null, new java.lang.Class[] { typeof(java.lang.annotation.Retention) }, new sun.reflect.annotation.AnnotationInvocationHandler(typeof(java.lang.annotation.Retention), retentionMap))
					};
				}
#endif

				private class ReturnValueAnnotation : Annotation
				{
					private AttributeAnnotationTypeWrapper type;

					internal ReturnValueAnnotation(AttributeAnnotationTypeWrapper type)
					{
						this.type = type;
					}

					internal override void ApplyReturnValue(ClassLoaderWrapper loader, MethodBuilder mb, ref ParameterBuilder pb, object annotation)
					{
						// TODO make sure the descriptor is correct
						Annotation ann = type.Annotation;
						object[] arr = (object[])annotation;
						for(int i = 2; i < arr.Length; i += 2)
						{
							if("value".Equals(arr[i]))
							{
								if(pb == null)
								{
									pb = mb.DefineParameter(0, ParameterAttributes.None, null);
								}
								object[] value = (object[])arr[i + 1];
								if(value[0].Equals(AnnotationDefaultAttribute.TAG_ANNOTATION))
								{
									ann.Apply(loader, pb, value);
								}
								else
								{
									for(int j = 1; j < value.Length; j++)
									{
										ann.Apply(loader, pb, value[j]);
									}
								}
								break;
							}
						}
					}

					internal override void Apply(ClassLoaderWrapper loader, MethodBuilder mb, object annotation)
					{
					}

					internal override void Apply(ClassLoaderWrapper loader, AssemblyBuilder ab, object annotation)
					{
					}

					internal override void Apply(ClassLoaderWrapper loader, ConstructorBuilder cb, object annotation)
					{
					}

					internal override void Apply(ClassLoaderWrapper loader, FieldBuilder fb, object annotation)
					{
					}

					internal override void Apply(ClassLoaderWrapper loader, ParameterBuilder pb, object annotation)
					{
					}

					internal override void Apply(ClassLoaderWrapper loader, TypeBuilder tb, object annotation)
					{
					}

					internal override void Apply(ClassLoaderWrapper loader, PropertyBuilder pb, object annotation)
					{
					}
				}

				internal override Annotation Annotation
				{
					get
					{
						return new ReturnValueAnnotation(declaringType);
					}
				}
			}

			private sealed class MultipleAnnotationTypeWrapper : AttributeAnnotationTypeWrapperBase
			{
				private readonly Type fakeType;
				private readonly AttributeAnnotationTypeWrapper declaringType;

				internal MultipleAnnotationTypeWrapper(AttributeAnnotationTypeWrapper declaringType)
					: base(declaringType.Name + AttributeAnnotationMultipleSuffix)
				{
#if STATIC_COMPILER
					this.fakeType = FakeTypes.GetAttributeMultipleType(declaringType.attributeType);
#elif !FIRST_PASS
					this.fakeType = typeof(ikvm.@internal.AttributeAnnotationMultiple<>).MakeGenericType(declaringType.attributeType);
#endif
					this.declaringType = declaringType;
				}

				protected override void LazyPublishMembers()
				{
					TypeWrapper tw = declaringType.MakeArrayType(1);
					SetMethods(new MethodWrapper[] { new DynamicOnlyMethodWrapper(this, "value", "()" + tw.SigName, tw, TypeWrapper.EmptyArray) });
					SetFields(FieldWrapper.EmptyArray);
				}

				internal override TypeWrapper DeclaringTypeWrapper
				{
					get
					{
						return declaringType;
					}
				}

				internal override TypeWrapper[] InnerClasses
				{
					get
					{
						return TypeWrapper.EmptyArray;
					}
				}

				internal override Type TypeAsTBD
				{
					get
					{
						return fakeType;
					}
				}

#if !STATIC_COMPILER
				internal override object[] GetDeclaredAnnotations()
				{
					return declaringType.GetDeclaredAnnotations();
				}
#endif

				private class MultipleAnnotation : Annotation
				{
					private AttributeAnnotationTypeWrapper type;

					internal MultipleAnnotation(AttributeAnnotationTypeWrapper type)
					{
						this.type = type;
					}

					private static object[] UnwrapArray(object annotation)
					{
						// TODO make sure the descriptor is correct
						object[] arr = (object[])annotation;
						for (int i = 2; i < arr.Length; i += 2)
						{
							if ("value".Equals(arr[i]))
							{
								object[] value = (object[])arr[i + 1];
								object[] rc = new object[value.Length - 1];
								Array.Copy(value, 1, rc, 0, rc.Length);
								return rc;
							}
						}
						return new object[0];
					}

					internal override void Apply(ClassLoaderWrapper loader, MethodBuilder mb, object annotation)
					{
						Annotation annot = type.Annotation;
						foreach(object ann in UnwrapArray(annotation))
						{
							annot.Apply(loader, mb, ann);
						}
					}

					internal override void Apply(ClassLoaderWrapper loader, AssemblyBuilder ab, object annotation)
					{
						Annotation annot = type.Annotation;
						foreach (object ann in UnwrapArray(annotation))
						{
							annot.Apply(loader, ab, ann);
						}
					}

					internal override void Apply(ClassLoaderWrapper loader, ConstructorBuilder cb, object annotation)
					{
						Annotation annot = type.Annotation;
						foreach (object ann in UnwrapArray(annotation))
						{
							annot.Apply(loader, cb, ann);
						}
					}

					internal override void Apply(ClassLoaderWrapper loader, FieldBuilder fb, object annotation)
					{
						Annotation annot = type.Annotation;
						foreach (object ann in UnwrapArray(annotation))
						{
							annot.Apply(loader, fb, ann);
						}
					}

					internal override void Apply(ClassLoaderWrapper loader, ParameterBuilder pb, object annotation)
					{
						Annotation annot = type.Annotation;
						foreach (object ann in UnwrapArray(annotation))
						{
							annot.Apply(loader, pb, ann);
						}
					}

					internal override void Apply(ClassLoaderWrapper loader, TypeBuilder tb, object annotation)
					{
						Annotation annot = type.Annotation;
						foreach (object ann in UnwrapArray(annotation))
						{
							annot.Apply(loader, tb, ann);
						}
					}

					internal override void Apply(ClassLoaderWrapper loader, PropertyBuilder pb, object annotation)
					{
						Annotation annot = type.Annotation;
						foreach (object ann in UnwrapArray(annotation))
						{
							annot.Apply(loader, pb, ann);
						}
					}
				}

				internal override Annotation Annotation
				{
					get
					{
						return new MultipleAnnotation(declaringType);
					}
				}
			}

			internal override TypeWrapper[] InnerClasses
			{
				get
				{
					lock(this)
					{
						if(innerClasses == null)
						{
							List<TypeWrapper> list = new List<TypeWrapper>();
							AttributeUsageAttribute attr = GetAttributeUsage();
							if((attr.ValidOn & AttributeTargets.ReturnValue) != 0)
							{
								list.Add(GetClassLoader().RegisterInitiatingLoader(new ReturnValueAnnotationTypeWrapper(this)));
							}
							if(attr.AllowMultiple)
							{
								list.Add(GetClassLoader().RegisterInitiatingLoader(new MultipleAnnotationTypeWrapper(this)));
							}
							innerClasses = list.ToArray();
						}
					}
					return innerClasses;
				}
			}

			internal override bool IsFakeTypeContainer
			{
				get
				{
					return true;
				}
			}

			private AttributeUsageAttribute GetAttributeUsage()
			{
				AttributeTargets validOn = AttributeTargets.All;
				bool allowMultiple = false;
				bool inherited = true;
				foreach(CustomAttributeData cad in CustomAttributeData.GetCustomAttributes(attributeType))
				{
					if(cad.Constructor.DeclaringType == typeof(AttributeUsageAttribute))
					{
						if(cad.ConstructorArguments.Count == 1 && cad.ConstructorArguments[0].ArgumentType == typeof(AttributeTargets))
						{
							validOn = (AttributeTargets)cad.ConstructorArguments[0].Value;
						}
						foreach(CustomAttributeNamedArgument cana in cad.NamedArguments)
						{
							if (cana.MemberInfo.Name == "AllowMultiple")
							{
								allowMultiple = (bool)cana.TypedValue.Value;
							}
							else if(cana.MemberInfo.Name == "Inherited")
							{
								inherited = (bool)cana.TypedValue.Value;
							}
						}
					}
				}
				AttributeUsageAttribute attr = new AttributeUsageAttribute(validOn);
				attr.AllowMultiple = allowMultiple;
				attr.Inherited = inherited;
				return attr;
			}

#if !STATIC_COMPILER && !FIRST_PASS
			internal override object[] GetDeclaredAnnotations()
			{
				// note that AttributeUsageAttribute.Inherited does not map to java.lang.annotation.Inherited
				AttributeTargets validOn = GetAttributeUsage().ValidOn;
				List<java.lang.annotation.ElementType> targets = new List<java.lang.annotation.ElementType>();
				if ((validOn & (AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Delegate | AttributeTargets.Assembly)) != 0)
				{
					targets.Add(java.lang.annotation.ElementType.TYPE);
				}
				if ((validOn & AttributeTargets.Constructor) != 0)
				{
					targets.Add(java.lang.annotation.ElementType.CONSTRUCTOR);
				}
				if ((validOn & AttributeTargets.Field) != 0)
				{
					targets.Add(java.lang.annotation.ElementType.FIELD);
				}
				if ((validOn & AttributeTargets.Method) != 0)
				{
					targets.Add(java.lang.annotation.ElementType.METHOD);
				}
				if ((validOn & AttributeTargets.Parameter) != 0)
				{
					targets.Add(java.lang.annotation.ElementType.PARAMETER);
				}
				java.util.HashMap targetMap = new java.util.HashMap();
				targetMap.put("value", targets.ToArray());
				java.util.HashMap retentionMap = new java.util.HashMap();
				retentionMap.put("value", java.lang.annotation.RetentionPolicy.RUNTIME);
				return new object[] {
					java.lang.reflect.Proxy.newProxyInstance(null, new java.lang.Class[] { typeof(java.lang.annotation.Target) }, new sun.reflect.annotation.AnnotationInvocationHandler(typeof(java.lang.annotation.Target), targetMap)),
					java.lang.reflect.Proxy.newProxyInstance(null, new java.lang.Class[] { typeof(java.lang.annotation.Retention) }, new sun.reflect.annotation.AnnotationInvocationHandler(typeof(java.lang.annotation.Retention), retentionMap))
				};
			}
#endif

			private class AttributeAnnotation : Annotation
			{
				private Type type;

				internal AttributeAnnotation(Type type)
				{
					this.type = type;
				}

				private CustomAttributeBuilder MakeCustomAttributeBuilder(ClassLoaderWrapper loader, object annotation)
				{
					object[] arr = (object[])annotation;
					ConstructorInfo defCtor;
					ConstructorInfo singleOneArgCtor;
					object ctorArg = null;
					GetConstructors(type, out defCtor, out singleOneArgCtor);
					List<PropertyInfo> properties = new List<PropertyInfo>();
					List<object> propertyValues = new List<object>();
					List<FieldInfo> fields = new List<FieldInfo>();
					List<object> fieldValues = new List<object>();
					for(int i = 2; i < arr.Length; i += 2)
					{
						string name = (string)arr[i];
						if(name == "value" && singleOneArgCtor != null)
						{
							ctorArg = ConvertValue(loader, singleOneArgCtor.GetParameters()[0].ParameterType, arr[i + 1]);
						}
						else
						{
							PropertyInfo pi = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
							if(pi != null)
							{
								properties.Add(pi);
								propertyValues.Add(ConvertValue(loader, pi.PropertyType, arr[i + 1]));
							}
							else
							{
								FieldInfo fi = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
								if(fi != null)
								{
									fields.Add(fi);
									fieldValues.Add(ConvertValue(loader, fi.FieldType, arr[i + 1]));
								}
							}
						}
					}
					if(ctorArg == null && defCtor == null)
					{
						// TODO required argument is missing
					}
					return new CustomAttributeBuilder(ctorArg == null ? defCtor : singleOneArgCtor,
						ctorArg == null ? new object[0] : new object[] { ctorArg },
						properties.ToArray(),
						propertyValues.ToArray(),
						fields.ToArray(),
						fieldValues.ToArray());
				}

				internal override void Apply(ClassLoaderWrapper loader, TypeBuilder tb, object annotation)
				{
					if(type == typeof(System.Runtime.InteropServices.StructLayoutAttribute) && tb.BaseType != typeof(object))
					{
						// we have to handle this explicitly, because if we apply an illegal StructLayoutAttribute,
						// TypeBuilder.CreateType() will later on throw an exception.
						Tracer.Error(Tracer.Runtime, "StructLayoutAttribute cannot be applied to {0}, because it does not directly extend cli.System.Object", tb.FullName);
						return;
					}
					if(type.IsSubclassOf(typeof(SecurityAttribute)))
					{
						SecurityAction action;
						PermissionSet permSet;
						if(MakeDeclSecurity(type, annotation, out action, out permSet))
						{
							tb.AddDeclarativeSecurity(action, permSet);
						}
					}
					else
					{
						tb.SetCustomAttribute(MakeCustomAttributeBuilder(loader, annotation));
					}
				}

				internal override void Apply(ClassLoaderWrapper loader, ConstructorBuilder cb, object annotation)
				{
					if(type.IsSubclassOf(typeof(SecurityAttribute)))
					{
						SecurityAction action;
						PermissionSet permSet;
						if(MakeDeclSecurity(type, annotation, out action, out permSet))
						{
							cb.AddDeclarativeSecurity(action, permSet);
						}
					}
					else
					{
						cb.SetCustomAttribute(MakeCustomAttributeBuilder(loader, annotation));
					}
				}

				internal override void Apply(ClassLoaderWrapper loader, MethodBuilder mb, object annotation)
				{
					if(type.IsSubclassOf(typeof(SecurityAttribute)))
					{
						SecurityAction action;
						PermissionSet permSet;
						if(MakeDeclSecurity(type, annotation, out action, out permSet))
						{
							mb.AddDeclarativeSecurity(action, permSet);
						}
					}
					else
					{
#if CLASSGC
						if(JVM.classUnloading && type == typeof(System.Runtime.InteropServices.DllImportAttribute))
						{
							// TODO PInvoke is not supported in RunAndCollect assemblies,
							// so we ignore the attribute.
							// We could forward the PInvoke to a non RunAndCollect assembly, but for now we don't bother.
							return;
						}
#endif
						mb.SetCustomAttribute(MakeCustomAttributeBuilder(loader, annotation));
					}
				}

				internal override void Apply(ClassLoaderWrapper loader, FieldBuilder fb, object annotation)
				{
					if(type.IsSubclassOf(typeof(SecurityAttribute)))
					{
						// you can't add declarative security to a field
					}
					else
					{
						fb.SetCustomAttribute(MakeCustomAttributeBuilder(loader, annotation));
					}
				}

				internal override void Apply(ClassLoaderWrapper loader, ParameterBuilder pb, object annotation)
				{
					if(type.IsSubclassOf(typeof(SecurityAttribute)))
					{
						// you can't add declarative security to a parameter
					}
					else
					{
						pb.SetCustomAttribute(MakeCustomAttributeBuilder(loader, annotation));
					}
				}

				internal override void Apply(ClassLoaderWrapper loader, AssemblyBuilder ab, object annotation)
				{
					if(type.IsSubclassOf(typeof(SecurityAttribute)))
					{
						// you can only add declarative security to an assembly when defining the assembly
					}
					else
					{
						ab.SetCustomAttribute(MakeCustomAttributeBuilder(loader, annotation));
					}
				}

				internal override void Apply(ClassLoaderWrapper loader, PropertyBuilder pb, object annotation)
				{
					if(type.IsSubclassOf(typeof(SecurityAttribute)))
					{
						// you can't add declarative security to a property
					}
					else
					{
						pb.SetCustomAttribute(MakeCustomAttributeBuilder(loader, annotation));
					}
				}
			}

			internal override Annotation Annotation
			{
				get
				{
					return new AttributeAnnotation(attributeType);
				}
			}
		}

		internal static TypeWrapper GetWrapperFromDotNetType(Type type)
		{
			TypeWrapper tw;
			lock (types)
			{
				types.TryGetValue(type, out tw);
			}
			if (tw == null)
			{
				tw = ClassLoaderWrapper.GetAssemblyClassLoader(type.Assembly).GetWrapperFromAssemblyType(type);
				lock (types)
				{
					types[type] = tw;
				}
			}
			return tw;
		}

		private static TypeWrapper GetBaseTypeWrapper(Type type)
		{
			if(type.IsInterface)
			{
				return null;
			}
			else if(ClassLoaderWrapper.IsRemappedType(type))
			{
				// Remapped types extend their alter ego
				// (e.g. cli.System.Object must appear to be derived from java.lang.Object)
				// except when they're sealed, of course.
				if(type.IsSealed)
				{
					return CoreClasses.java.lang.Object.Wrapper;
				}
				return ClassLoaderWrapper.GetWrapperFromType(type);
			}
			else if(ClassLoaderWrapper.IsRemappedType(type.BaseType))
			{
				return GetWrapperFromDotNetType(type.BaseType);
			}
			else
			{
				return ClassLoaderWrapper.GetWrapperFromType(type.BaseType);
			}
		}

		internal static TypeWrapper Create(Type type, string name)
		{
			if (type.ContainsGenericParameters)
			{
				return new OpenGenericTypeWrapper(type, name);
			}
			else
			{
				return new DotNetTypeWrapper(type, name);
			}
		}

		private DotNetTypeWrapper(Type type, string name)
			: base(GetModifiers(type), name, GetBaseTypeWrapper(type))
		{
			Debug.Assert(!(type.IsByRef), type.FullName);
			Debug.Assert(!(type.IsPointer), type.FullName);
			Debug.Assert(!(type.Name.EndsWith("[]")), type.FullName);
			Debug.Assert(!(type is TypeBuilder), type.FullName);
			Debug.Assert(!(AttributeHelper.IsJavaModule(type.Module)));

			this.type = type;
		}

		internal override ClassLoaderWrapper GetClassLoader()
		{
			if(type.IsGenericType)
			{
				return ClassLoaderWrapper.GetGenericClassLoader(this);
			}
			return ClassLoaderWrapper.GetAssemblyClassLoader(type.Assembly);
		}

		private sealed class MulticastDelegateCtorMethodWrapper : MethodWrapper
		{
			internal MulticastDelegateCtorMethodWrapper(TypeWrapper declaringType)
				: base(declaringType, "<init>", "()V", null, PrimitiveTypeWrapper.VOID, TypeWrapper.EmptyArray, Modifiers.Protected, MemberFlags.None)
			{
			}
		}

		private class DelegateMethodWrapper : MethodWrapper
		{
			private ConstructorInfo delegateConstructor;
			private DelegateInnerClassTypeWrapper iface;

			internal DelegateMethodWrapper(TypeWrapper declaringType, DelegateInnerClassTypeWrapper iface)
				: base(declaringType, "<init>", "(" + iface.SigName + ")V", null, PrimitiveTypeWrapper.VOID, new TypeWrapper[] { iface }, Modifiers.Public, MemberFlags.None)
			{
				this.delegateConstructor = declaringType.TypeAsTBD.GetConstructor(new Type[] { typeof(object), typeof(IntPtr) });
				this.iface = iface;
			}

			internal override void EmitNewobj(CodeEmitter ilgen, MethodAnalyzer ma, int opcodeIndex)
			{
				TypeWrapper targetType = ma == null ? null : ma.GetStackTypeWrapper(opcodeIndex, 0);
				if(targetType == null || targetType.IsInterface)
				{
					MethodInfo createDelegate = typeof(Delegate).GetMethod("CreateDelegate", new Type[] { typeof(Type), typeof(object), typeof(string) });
					LocalBuilder targetObj = ilgen.DeclareLocal(typeof(object));
					ilgen.Emit(OpCodes.Stloc, targetObj);
					ilgen.Emit(OpCodes.Ldtoken, delegateConstructor.DeclaringType);
					ilgen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", new Type[] { typeof(RuntimeTypeHandle) }));
					ilgen.Emit(OpCodes.Ldloc, targetObj);
					ilgen.Emit(OpCodes.Ldstr, "Invoke");
					ilgen.Emit(OpCodes.Call, createDelegate);
					ilgen.Emit(OpCodes.Castclass, delegateConstructor.DeclaringType);
				}
				else
				{
					ilgen.Emit(OpCodes.Dup);
					// we know that a DelegateInnerClassTypeWrapper has only one method
					Debug.Assert(iface.GetMethods().Length == 1);
					MethodWrapper mw = targetType.GetMethodWrapper("Invoke", iface.GetMethods()[0].Signature, true);
					// TODO linking here is not safe
					mw.Link();
					ilgen.Emit(OpCodes.Ldvirtftn, (MethodInfo)mw.GetMethod());
					ilgen.Emit(OpCodes.Newobj, delegateConstructor);
				}
			}
		}

		private class ByRefMethodWrapper : SmartMethodWrapper
		{
#if !STATIC_COMPILER
			private bool[] byrefs;
#endif
			private Type[] args;

			internal ByRefMethodWrapper(Type[] args, bool[] byrefs, TypeWrapper declaringType, string name, string sig, MethodBase method, TypeWrapper returnType, TypeWrapper[] parameterTypes, Modifiers modifiers, bool hideFromReflection)
				: base(declaringType, name, sig, method, returnType, parameterTypes, modifiers, hideFromReflection ? MemberFlags.HideFromReflection : MemberFlags.None)
			{
				this.args = args;
#if !STATIC_COMPILER
				this.byrefs = byrefs;
#endif
			}

			protected override void CallImpl(CodeEmitter ilgen)
			{
				MethodBase mb = GetMethod();
				MethodInfo mi = mb as MethodInfo;
				if(mi != null)
				{
					ilgen.Emit(OpCodes.Call, mi);
				}
				else
				{
					ilgen.Emit(OpCodes.Call, (ConstructorInfo)mb);
				}
			}

			protected override void CallvirtImpl(CodeEmitter ilgen)
			{
				ilgen.Emit(OpCodes.Callvirt, (MethodInfo)GetMethod());
			}

			protected override void NewobjImpl(CodeEmitter ilgen)
			{
				ilgen.Emit(OpCodes.Newobj, (ConstructorInfo)GetMethod());
			}

			protected override void PreEmit(CodeEmitter ilgen)
			{
				LocalBuilder[] locals = new LocalBuilder[args.Length];
				for(int i = args.Length - 1; i >= 0; i--)
				{
					Type type = args[i];
					if(type.IsByRef)
					{
						type = ArrayTypeWrapper.MakeArrayType(type.GetElementType(), 1);
					}
					locals[i] = ilgen.DeclareLocal(type);
					ilgen.Emit(OpCodes.Stloc, locals[i]);
				}
				for(int i = 0; i < args.Length; i++)
				{
					ilgen.Emit(OpCodes.Ldloc, locals[i]);
					if(args[i].IsByRef)
					{
						ilgen.Emit(OpCodes.Ldc_I4_0);
						ilgen.Emit(OpCodes.Ldelema, args[i].GetElementType());
					}
				}
				base.PreEmit(ilgen);
			}
		}

		internal static bool IsVisible(Type type)
		{
			return type.IsPublic || (type.IsNestedPublic && IsVisible(type.DeclaringType));
		}

		private class EnumWrapMethodWrapper : MethodWrapper
		{
			internal EnumWrapMethodWrapper(DotNetTypeWrapper tw, TypeWrapper fieldType)
				: base(tw, "wrap", "(" + fieldType.SigName + ")" + tw.SigName, null, tw, new TypeWrapper[] { fieldType }, Modifiers.Static | Modifiers.Public, MemberFlags.None)
			{
			}

			internal override void EmitCall(CodeEmitter ilgen)
			{
				// We don't actually need to do anything here!
				// The compiler will insert a boxing operation after calling us and that will
				// result in our argument being boxed (since that's still sitting on the stack).
			}
		}

		internal class EnumValueFieldWrapper : FieldWrapper
		{
			private Type underlyingType;

			internal EnumValueFieldWrapper(DotNetTypeWrapper tw, TypeWrapper fieldType)
				: base(tw, fieldType, "Value", fieldType.SigName, new ExModifiers(Modifiers.Public | Modifiers.Final, false), null)
			{
				underlyingType = Enum.GetUnderlyingType(tw.type);
			}

			protected override void EmitGetImpl(CodeEmitter ilgen)
			{
				// NOTE if the reference on the stack is null, we *want* the NullReferenceException, so we don't use TypeWrapper.EmitUnbox
				ilgen.LazyEmitUnbox(underlyingType);
				ilgen.LazyEmitLdobj(underlyingType);
			}

			protected override void EmitSetImpl(CodeEmitter ilgen)
			{
				// NOTE even though the field is final, JNI reflection can still be used to set its value!
				LocalBuilder temp = ilgen.AllocTempLocal(underlyingType);
				ilgen.Emit(OpCodes.Stloc, temp);
				ilgen.Emit(OpCodes.Unbox, underlyingType);
				ilgen.Emit(OpCodes.Ldloc, temp);
				ilgen.Emit(OpCodes.Stobj, underlyingType);
				ilgen.ReleaseTempLocal(temp);
			}

			// this method takes a boxed Enum and returns its value as a boxed primitive
			// of the subset of Java primitives (i.e. byte, short, int, long)
			internal static object GetEnumPrimitiveValue(object obj)
			{
				return GetEnumPrimitiveValue(Enum.GetUnderlyingType(obj.GetType()), obj);
			}

			// this method can be used to convert an enum value or its underlying value to a Java primitive
			internal static object GetEnumPrimitiveValue(Type underlyingType, object obj)
			{
				if(underlyingType == typeof(sbyte) || underlyingType == typeof(byte))
				{
					return unchecked((byte)((IConvertible)obj).ToInt32(null));
				}
				else if(underlyingType == typeof(short) || underlyingType == typeof(ushort))
				{
					return unchecked((short)((IConvertible)obj).ToInt32(null));
				}
				else if(underlyingType == typeof(int))
				{
					return ((IConvertible)obj).ToInt32(null);
				}
				else if(underlyingType == typeof(uint))
				{
					return unchecked((int)((IConvertible)obj).ToUInt32(null));
				}
				else if(underlyingType == typeof(long))
				{
					return ((IConvertible)obj).ToInt64(null);
				}
				else if(underlyingType == typeof(ulong))
				{
					return unchecked((long)((IConvertible)obj).ToUInt64(null));
				}
				else
				{
					throw new InvalidOperationException();
				}
			}
		}

		private class ValueTypeDefaultCtor : MethodWrapper
		{
			internal ValueTypeDefaultCtor(DotNetTypeWrapper tw)
				: base(tw, "<init>", "()V", null, PrimitiveTypeWrapper.VOID, TypeWrapper.EmptyArray, Modifiers.Public, MemberFlags.None)
			{
			}

			internal override void EmitNewobj(CodeEmitter ilgen, MethodAnalyzer ma, int opcodeIndex)
			{
				LocalBuilder local = ilgen.DeclareLocal(DeclaringType.TypeAsTBD);
				ilgen.Emit(OpCodes.Ldloc, local);
				ilgen.Emit(OpCodes.Box, DeclaringType.TypeAsTBD);
			}
		}

		private class FinalizeMethodWrapper : MethodWrapper
		{
			internal FinalizeMethodWrapper(DotNetTypeWrapper tw)
				: base(tw, "finalize", "()V", null, PrimitiveTypeWrapper.VOID, TypeWrapper.EmptyArray, Modifiers.Protected | Modifiers.Final, MemberFlags.None)
			{
			}

			internal override void EmitCall(CodeEmitter ilgen)
			{
				ilgen.Emit(OpCodes.Pop);
			}

			internal override void EmitCallvirt(CodeEmitter ilgen)
			{
				ilgen.Emit(OpCodes.Pop);
			}
		}

		private class CloneMethodWrapper : MethodWrapper
		{
			internal CloneMethodWrapper(DotNetTypeWrapper tw)
				: base(tw, "clone", "()Ljava.lang.Object;", null, CoreClasses.java.lang.Object.Wrapper, TypeWrapper.EmptyArray, Modifiers.Protected | Modifiers.Final, MemberFlags.None)
			{
			}

			internal override void EmitCall(CodeEmitter ilgen)
			{
				ilgen.Emit(OpCodes.Dup);
				ilgen.Emit(OpCodes.Isinst, ClassLoaderWrapper.LoadClassCritical("java.lang.Cloneable").TypeAsBaseType);
				CodeEmitterLabel label1 = ilgen.DefineLabel();
				ilgen.Emit(OpCodes.Brtrue_S, label1);
				CodeEmitterLabel label2 = ilgen.DefineLabel();
				ilgen.Emit(OpCodes.Brfalse_S, label2);
				ilgen.EmitThrow("java.lang.CloneNotSupportedException");
				ilgen.MarkLabel(label2);
				ilgen.EmitThrow("java.lang.NullPointerException");
				ilgen.MarkLabel(label1);
				ilgen.Emit(OpCodes.Call, typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null));
			}

			internal override void EmitCallvirt(CodeEmitter ilgen)
			{
				EmitCall(ilgen);
			}
		}

		protected override void LazyPublishMembers()
		{
			// special support for enums
			if(type.IsEnum)
			{
				Type underlyingType = Enum.GetUnderlyingType(type);
				Type javaUnderlyingType;
				if(underlyingType == typeof(sbyte))
				{
					javaUnderlyingType = typeof(byte);
				}
				else if(underlyingType == typeof(ushort))
				{
					javaUnderlyingType = typeof(short);
				}
				else if(underlyingType == typeof(uint))
				{
					javaUnderlyingType = typeof(int);
				}
				else if(underlyingType == typeof(ulong))
				{
					javaUnderlyingType = typeof(long);
				}
				else
				{
					javaUnderlyingType = underlyingType;
				}
				TypeWrapper fieldType = ClassLoaderWrapper.GetWrapperFromType(javaUnderlyingType);
				FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
				List<FieldWrapper> fieldsList = new List<FieldWrapper>();
				for(int i = 0; i < fields.Length; i++)
				{
					if(fields[i].FieldType == type)
					{
						string name = fields[i].Name;
						if(name == "Value")
						{
							name = "_Value";
						}
						else if(name.StartsWith("_") && name.EndsWith("Value"))
						{
							name = "_" + name;
						}
						object val = EnumValueFieldWrapper.GetEnumPrimitiveValue(underlyingType, fields[i].GetRawConstantValue());
						fieldsList.Add(new ConstantFieldWrapper(this, fieldType, name, fieldType.SigName, Modifiers.Public | Modifiers.Static | Modifiers.Final, fields[i], val, MemberFlags.None));
					}
				}
				fieldsList.Add(new EnumValueFieldWrapper(this, fieldType));
				SetFields(fieldsList.ToArray());
				SetMethods(new MethodWrapper[] { new EnumWrapMethodWrapper(this, fieldType) });
			}
			else
			{
				List<FieldWrapper> fieldsList = new List<FieldWrapper>();
				FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
				for(int i = 0; i < fields.Length; i++)
				{
					// TODO for remapped types, instance fields need to be converted to static getter/setter methods
					if(fields[i].FieldType.IsPointer)
					{
						// skip, pointer fields are not supported
					}
					else
					{
						// TODO handle name/signature clash
						fieldsList.Add(CreateFieldWrapperDotNet(AttributeHelper.GetModifiers(fields[i], true).Modifiers, fields[i].Name, fields[i].FieldType, fields[i]));
					}
				}
				SetFields(fieldsList.ToArray());

				Dictionary<string, MethodWrapper> methodsList = new Dictionary<string, MethodWrapper>();

				// special case for delegate constructors!
				if(IsDelegate(type))
				{
					TypeWrapper iface = InnerClasses[0];
					DelegateMethodWrapper mw = new DelegateMethodWrapper(this, (DelegateInnerClassTypeWrapper)iface);
					methodsList.Add(mw.Name + mw.Signature, mw);
				}

				// add a protected default constructor to MulticastDelegate to make it easier to define a delegate in Java
				if(type == typeof(MulticastDelegate))
				{
					methodsList.Add("<init>()V", new MulticastDelegateCtorMethodWrapper(this));
				}

				ConstructorInfo[] constructors = type.GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
				for(int i = 0; i < constructors.Length; i++)
				{
					string name;
					string sig;
					TypeWrapper[] args;
					TypeWrapper ret;
					if(MakeMethodDescriptor(constructors[i], out name, out sig, out args, out ret))
					{
						MethodWrapper mw = CreateMethodWrapper(name, sig, args, ret, constructors[i], false);
						string key = mw.Name + mw.Signature;
						if(!methodsList.ContainsKey(key))
						{
							methodsList.Add(key, mw);
						}
					}
				}

				if(type.IsValueType && !methodsList.ContainsKey("<init>()V"))
				{
					// Value types have an implicit default ctor
					methodsList.Add("<init>()V", new ValueTypeDefaultCtor(this));
				}

				MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
				for(int i = 0; i < methods.Length; i++)
				{
					if(methods[i].IsStatic && type.IsInterface)
					{
						// skip, Java cannot deal with static methods on interfaces
					}
					else
					{
						string name;
						string sig;
						TypeWrapper[] args;
						TypeWrapper ret;
						if(MakeMethodDescriptor(methods[i], out name, out sig, out args, out ret))
						{
							if(!methods[i].IsStatic && !methods[i].IsPrivate && BaseTypeWrapper != null)
							{
								MethodWrapper baseMethod = BaseTypeWrapper.GetMethodWrapper(name, sig, true);
								if(baseMethod != null && baseMethod.IsFinal && !baseMethod.IsStatic && !baseMethod.IsPrivate)
								{
									continue;
								}
							}
							MethodWrapper mw = CreateMethodWrapper(name, sig, args, ret, methods[i], false);
							string key = mw.Name + mw.Signature;
							MethodWrapper existing;
							methodsList.TryGetValue(key, out existing);
							if(existing == null || existing is ByRefMethodWrapper)
							{
								methodsList[key] = mw;
							}
						}
						else if(methods[i].IsAbstract)
						{
							this.HasUnsupportedAbstractMethods = true;
						}
					}
				}

				// make sure that all the interface methods that we implement are available as public methods,
				// otherwise javac won't like the class.
				if(!type.IsInterface)
				{
					Type[] interfaces = type.GetInterfaces();
					for(int i = 0; i < interfaces.Length; i++)
					{
						// we only handle public (or nested public) types, because we're potentially adding a
						// method that should be callable by anyone through the interface
						if(IsVisible(interfaces[i]))
						{
							InterfaceMapping map = type.GetInterfaceMap(interfaces[i]);
							for(int j = 0; j < map.InterfaceMethods.Length; j++)
							{
								if((!map.TargetMethods[j].IsPublic || map.TargetMethods[j].Name != map.InterfaceMethods[j].Name)
									&& map.TargetMethods[j].DeclaringType == type)
								{
									string name;
									string sig;
									TypeWrapper[] args;
									TypeWrapper ret;
									if(MakeMethodDescriptor(map.InterfaceMethods[j], out name, out sig, out args, out ret))
									{
										string key = name + sig;
										MethodWrapper existing;
										methodsList.TryGetValue(key, out existing);
										if(existing == null && BaseTypeWrapper != null)
										{
											MethodWrapper baseMethod = BaseTypeWrapper.GetMethodWrapper(name, sig, true);
											if(baseMethod != null && !baseMethod.IsStatic && baseMethod.IsPublic)
											{
												continue;
											}
										}
										if(existing == null || existing is ByRefMethodWrapper || existing.IsStatic || !existing.IsPublic)
										{
											// TODO if existing != null, we need to rename the existing method (but this is complicated because
											// it also affects subclasses). This is especially required is the existing method is abstract,
											// because otherwise we won't be able to create any subclasses in Java.
											methodsList[key] = CreateMethodWrapper(name, sig, args, ret, map.InterfaceMethods[j], true);
										}
									}
								}
							}
						}
					}
				}

				// for non-final remapped types, we need to add all the virtual methods in our alter ego (which
				// appears as our base class) and make them final (to prevent Java code from overriding these
				// methods, which don't really exist).
				if(ClassLoaderWrapper.IsRemappedType(type) && !type.IsSealed && !type.IsInterface)
				{
					// Finish the type, to make sure the methods are populated
					this.BaseTypeWrapper.Finish();
					TypeWrapper baseTypeWrapper = this.BaseTypeWrapper;
					while(baseTypeWrapper != null)
					{
						foreach(MethodWrapper m in baseTypeWrapper.GetMethods())
						{
							if(!m.IsStatic && !m.IsFinal && (m.IsPublic || m.IsProtected) && m.Name != "<init>")
							{
								string key = m.Name + m.Signature;
								if(!methodsList.ContainsKey(key))
								{
									if(m.IsProtected)
									{
										if(m.Name == "finalize" && m.Signature == "()V")
										{
											methodsList.Add(key, new FinalizeMethodWrapper(this));
										}
										else if(m.Name == "clone" && m.Signature == "()Ljava.lang.Object;")
										{
											methodsList.Add(key, new CloneMethodWrapper(this));
										}
										else
										{
											// there should be a special MethodWrapper for this method
											throw new InvalidOperationException("Missing protected method support for " + baseTypeWrapper.Name + "::" + m.Name + m.Signature);
										}
									}
									else
									{
										methodsList.Add(key, new BaseFinalMethodWrapper(this, m));
									}
								}
							}
						}
						baseTypeWrapper = baseTypeWrapper.BaseTypeWrapper;
					}
				}
				MethodWrapper[] methodArray = new MethodWrapper[methodsList.Count];
				methodsList.Values.CopyTo(methodArray, 0);
				SetMethods(methodArray);
			}
		}

		private class BaseFinalMethodWrapper : MethodWrapper
		{
			private MethodWrapper m;

			internal BaseFinalMethodWrapper(DotNetTypeWrapper tw, MethodWrapper m)
				: base(tw, m.Name, m.Signature, m.GetMethod(), m.ReturnType, m.GetParameters(), m.Modifiers | Modifiers.Final, MemberFlags.None)
			{
				this.m = m;
			}

			internal override void EmitCall(CodeEmitter ilgen)
			{
				// we direct EmitCall to EmitCallvirt, because we always want to end up at the instancehelper method
				// (EmitCall would go to our alter ego .NET type and that wouldn't be legal)
				m.EmitCallvirt(ilgen);
			}

			internal override void EmitCallvirt(CodeEmitter ilgen)
			{
				m.EmitCallvirt(ilgen);
			}
		}

		internal static bool IsUnsupportedAbstractMethod(MethodBase mb)
		{
			if(mb.IsAbstract)
			{
				MethodInfo mi = (MethodInfo)mb;
				if(mi.ReturnType.IsByRef || IsPointerType(mi.ReturnType))
				{
					return true;
				}
				foreach(ParameterInfo p in mi.GetParameters())
				{
					if(p.ParameterType.IsByRef || IsPointerType(p.ParameterType))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsPointerType(Type type)
		{
			while(type.HasElementType)
			{
				if(type.IsPointer)
				{
					return true;
				}
				type = type.GetElementType();
			}
			return false;
		}

		private bool MakeMethodDescriptor(MethodBase mb, out string name, out string sig, out TypeWrapper[] args, out TypeWrapper ret)
		{
			if(mb.IsGenericMethodDefinition)
			{
				name = null;
				sig = null;
				args = null;
				ret = null;
				return false;
			}
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append('(');
			ParameterInfo[] parameters = mb.GetParameters();
			args = new TypeWrapper[parameters.Length];
			for(int i = 0; i < parameters.Length; i++)
			{
				Type type = parameters[i].ParameterType;
				if(IsPointerType(type))
				{
					name = null;
					sig = null;
					args = null;
					ret = null;
					return false;
				}
				if(type.IsByRef)
				{
					type = ArrayTypeWrapper.MakeArrayType(type.GetElementType(), 1);
					if(mb.IsAbstract)
					{
						// Since we cannot override methods with byref arguments, we don't report abstract
						// methods with byref args.
						name = null;
						sig = null;
						args = null;
						ret = null;
						return false;
					}
				}
				TypeWrapper tw = ClassLoaderWrapper.GetWrapperFromType(type);
				args[i] = tw;
				sb.Append(tw.SigName);
			}
			sb.Append(')');
			if(mb is ConstructorInfo)
			{
				ret = PrimitiveTypeWrapper.VOID;
				if(mb.IsStatic)
				{
					name = "<clinit>";
				}
				else
				{
					name = "<init>";
				}
				sb.Append(ret.SigName);
				sig = sb.ToString();
				return true;
			}
			else
			{
				Type type = ((MethodInfo)mb).ReturnType;
				if(IsPointerType(type) || type.IsByRef)
				{
					name = null;
					sig = null;
					ret = null;
					return false;
				}
				ret = ClassLoaderWrapper.GetWrapperFromType(type);
				sb.Append(ret.SigName);
				name = mb.Name;
				sig = sb.ToString();
				return true;
			}
		}

		internal override TypeWrapper[] Interfaces
		{
			get
			{
				lock(this)
				{
					if(interfaces == null)
					{
						Type[] interfaceTypes = type.GetInterfaces();
						interfaces = new TypeWrapper[interfaceTypes.Length];
						for(int i = 0; i < interfaceTypes.Length; i++)
						{
							if(interfaceTypes[i].DeclaringType != null &&
								AttributeHelper.IsHideFromJava(interfaceTypes[i]) &&
								interfaceTypes[i].Name == "__Interface")
							{
								// we have to return the declaring type for ghost interfaces
								interfaces[i] = ClassLoaderWrapper.GetWrapperFromType(interfaceTypes[i].DeclaringType);
							}
							else
							{
								interfaces[i] = ClassLoaderWrapper.GetWrapperFromType(interfaceTypes[i]);
							}
						}
					}
					return interfaces;
				}
			}
		}

		private static bool IsAttribute(Type type)
		{
			if(!type.IsAbstract && type.IsSubclassOf(typeof(Attribute)) && IsVisible(type))
			{
				//
				// Based on the number of constructors and their arguments, we distinguish several types
				// of attributes:
				//                                   | def ctor | single 1-arg ctor
				// -----------------------------------------------------------------
				// complex only (i.e. Annotation{N}) |          |
				// all optional fields/properties    |    X     |
				// required "value"                  |          |   X
				// optional "value"                  |    X     |   X
				// -----------------------------------------------------------------
				// 
				// TODO currently we don't support "complex only" attributes.
				//
				ConstructorInfo defCtor;
				ConstructorInfo singleOneArgCtor;
				AttributeAnnotationTypeWrapper.GetConstructors(type, out defCtor, out singleOneArgCtor);
				return defCtor != null || singleOneArgCtor != null;
			}
			return false;
		}

		private static bool IsDelegate(Type type)
		{
			// HACK non-public delegates do not get the special treatment (because they are likely to refer to
			// non-public types in the arg list and they're not really useful anyway)
			// NOTE we don't have to check in what assembly the type lives, because this is a DotNetTypeWrapper,
			// we know that it is a different assembly.
			if(!type.IsAbstract && type.IsSubclassOf(typeof(MulticastDelegate)) && IsVisible(type))
			{
				MethodInfo invoke = type.GetMethod("Invoke");
				if(invoke != null)
				{
					foreach(ParameterInfo p in invoke.GetParameters())
					{
						// TODO at the moment we don't support delegates with pointer or byref parameters
						if(p.ParameterType.IsPointer || p.ParameterType.IsByRef)
						{
							return false;
						}
					}
					return true;
				}
			}
			return false;
		}

		internal override TypeWrapper[] InnerClasses
		{
			get
			{
				lock(this)
				{
					if(innerClasses == null)
					{
						Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
						List<TypeWrapper> list = new List<TypeWrapper>(nestedTypes.Length);
						for(int i = 0; i < nestedTypes.Length; i++)
						{
							if (!nestedTypes[i].IsGenericTypeDefinition)
							{
								list.Add(ClassLoaderWrapper.GetWrapperFromType(nestedTypes[i]));
							}
						}
						if(IsDelegate(type))
						{
							ClassLoaderWrapper classLoader = GetClassLoader();
							list.Add(classLoader.RegisterInitiatingLoader(new DelegateInnerClassTypeWrapper(Name + DelegateInterfaceSuffix, type, classLoader)));
						}
						if(IsAttribute(type))
						{
							list.Add(GetClassLoader().RegisterInitiatingLoader(new AttributeAnnotationTypeWrapper(Name + AttributeAnnotationSuffix, type)));
						}
						if(type.IsEnum && IsVisible(type))
						{
							list.Add(GetClassLoader().RegisterInitiatingLoader(new EnumEnumTypeWrapper(Name + EnumEnumSuffix, type)));
						}
						innerClasses = list.ToArray();
					}
				}
				return innerClasses;
			}
		}

		internal override bool IsFakeTypeContainer
		{
			get
			{
				return IsDelegate(type) || IsAttribute(type) || (type.IsEnum && IsVisible(type));
			}
		}

		internal override TypeWrapper DeclaringTypeWrapper
		{
			get
			{
				if(outerClass == null)
				{
					Type outer = type.DeclaringType;
					if(outer != null && !type.IsGenericType)
					{
						outerClass = ClassLoaderWrapper.GetWrapperFromType(outer);
					}
				}
				return outerClass;
			}
		}

		internal override Modifiers ReflectiveModifiers
		{
			get
			{
				if(DeclaringTypeWrapper != null)
				{
					return Modifiers | Modifiers.Static;
				}
				return Modifiers;
			}
		}

		private FieldWrapper CreateFieldWrapperDotNet(Modifiers modifiers, string name, Type fieldType, FieldInfo field)
		{
			TypeWrapper type = ClassLoaderWrapper.GetWrapperFromType(fieldType);
			if(field.IsLiteral)
			{
				return new ConstantFieldWrapper(this, type, name, type.SigName, modifiers, field, null, MemberFlags.None);
			}
			else
			{
				return FieldWrapper.Create(this, type, field, name, type.SigName, new ExModifiers(modifiers, false));
			}
		}

		private MethodWrapper CreateMethodWrapper(string name, string sig, TypeWrapper[] argTypeWrappers, TypeWrapper retTypeWrapper, MethodBase mb, bool privateInterfaceImplHack)
		{
			ExModifiers exmods = AttributeHelper.GetModifiers(mb, true);
			Modifiers mods = exmods.Modifiers;
			if(name == "Finalize" && sig == "()V" && !mb.IsStatic &&
				TypeAsBaseType.IsSubclassOf(CoreClasses.java.lang.Object.Wrapper.TypeAsBaseType))
			{
				// TODO if the .NET also has a "finalize" method, we need to hide that one (or rename it, or whatever)
				MethodWrapper mw = new SimpleCallMethodWrapper(this, "finalize", "()V", (MethodInfo)mb, null, null, mods, MemberFlags.None, SimpleOpCode.Call, SimpleOpCode.Callvirt);
				mw.SetDeclaredExceptions(new string[] { "java.lang.Throwable" });
				return mw;
			}
			ParameterInfo[] parameters = mb.GetParameters();
			Type[] args = new Type[parameters.Length];
			bool hasByRefArgs = false;
			bool[] byrefs = null;
			for(int i = 0; i < parameters.Length; i++)
			{
				args[i] = parameters[i].ParameterType;
				if(parameters[i].ParameterType.IsByRef)
				{
					if(byrefs == null)
					{
						byrefs = new bool[args.Length];
					}
					byrefs[i] = true;
					hasByRefArgs = true;
				}
			}
			if(privateInterfaceImplHack)
			{
				mods &= ~Modifiers.Abstract;
				mods |= Modifiers.Final;
			}
			if(hasByRefArgs)
			{
				if(!(mb is ConstructorInfo) && !mb.IsStatic)
				{
					mods |= Modifiers.Final;
				}
				return new ByRefMethodWrapper(args, byrefs, this, name, sig, mb, retTypeWrapper, argTypeWrappers, mods, false);
			}
			else
			{
				if(mb is ConstructorInfo)
				{
					return new SmartConstructorMethodWrapper(this, name, sig, (ConstructorInfo)mb, argTypeWrappers, mods, MemberFlags.None);
				}
				else
				{
					return new SmartCallMethodWrapper(this, name, sig, (MethodInfo)mb, retTypeWrapper, argTypeWrappers, mods, MemberFlags.None, SimpleOpCode.Call, SimpleOpCode.Callvirt);
				}
			}
		}

		internal override Type TypeAsTBD
		{
			get
			{
				return type;
			}
		}

		internal override bool IsRemapped
		{
			get
			{
				return ClassLoaderWrapper.IsRemappedType(type);
			}
		}

		internal override void EmitInstanceOf(TypeWrapper context, CodeEmitter ilgen)
		{
			if(IsRemapped)
			{
				TypeWrapper shadow = ClassLoaderWrapper.GetWrapperFromType(type);
				MethodInfo method = shadow.TypeAsBaseType.GetMethod("__<instanceof>");
				if(method != null)
				{
					ilgen.Emit(OpCodes.Call, method);
					return;
				}
			}
			ilgen.LazyEmit_instanceof(type);
		}

		internal override void EmitCheckcast(TypeWrapper context, CodeEmitter ilgen)
		{
			if(IsRemapped)
			{
				TypeWrapper shadow = ClassLoaderWrapper.GetWrapperFromType(type);
				MethodInfo method = shadow.TypeAsBaseType.GetMethod("__<checkcast>");
				if(method != null)
				{
					ilgen.Emit(OpCodes.Call, method);
					return;
				}
			}
			ilgen.EmitCastclass(type);
		}

		internal override void Finish()
		{
			if(BaseTypeWrapper != null)
			{
				BaseTypeWrapper.Finish();
			}
			foreach(TypeWrapper tw in this.Interfaces)
			{
				tw.Finish();
			}
		}

		internal override object[] GetDeclaredAnnotations()
		{
			if(type.Assembly.ReflectionOnly)
			{
				// TODO on Whidbey this must be implemented
				return null;
			}
			return type.GetCustomAttributes(false);
		}

		internal override object[] GetFieldAnnotations(FieldWrapper fw)
		{
			FieldInfo fi = fw.GetField();
			if(fi == null)
			{
				return null;
			}
			if(fi.DeclaringType.Assembly.ReflectionOnly)
			{
				// TODO on Whidbey this must be implemented
				return null;
			}
			return fi.GetCustomAttributes(false);
		}

		internal override object[] GetMethodAnnotations(MethodWrapper mw)
		{
			MethodBase mb = mw.GetMethod();
			if(mb == null)
			{
				return null;
			}
			if(mb.DeclaringType.Assembly.ReflectionOnly)
			{
				// TODO on Whidbey this must be implemented
				return null;
			}
			return mb.GetCustomAttributes(false);
		}

		internal override object[][] GetParameterAnnotations(MethodWrapper mw)
		{
			MethodBase mb = mw.GetMethod();
			if(mb == null)
			{
				return null;
			}
			if(mb.DeclaringType.Assembly.ReflectionOnly)
			{
				// TODO on Whidbey this must be implemented
				return null;
			}
			ParameterInfo[] parameters = mb.GetParameters();
			object[][] attribs = new object[parameters.Length][];
			for(int i = 0; i < parameters.Length; i++)
			{
				attribs[i] = parameters[i].GetCustomAttributes(false);
			}
			return attribs;
		}

		internal override bool IsFastClassLiteralSafe
		{
			get { return true; }
		}
	}

	sealed class ArrayTypeWrapper : TypeWrapper
	{
		private static TypeWrapper[] interfaces;
		private static MethodInfo clone;
		private readonly TypeWrapper ultimateElementTypeWrapper;
		private Type arrayType;
		private bool finished;

		internal ArrayTypeWrapper(TypeWrapper ultimateElementTypeWrapper, string name)
			: base(Modifiers.Final | Modifiers.Abstract | (ultimateElementTypeWrapper.Modifiers & Modifiers.Public), name, CoreClasses.java.lang.Object.Wrapper)
		{
			Debug.Assert(!ultimateElementTypeWrapper.IsArray);
			this.ultimateElementTypeWrapper = ultimateElementTypeWrapper;
			this.IsInternal = ultimateElementTypeWrapper.IsInternal;
		}

		internal override ClassLoaderWrapper GetClassLoader()
		{
			return ultimateElementTypeWrapper.GetClassLoader();
		}

		internal static MethodInfo CloneMethod
		{
			get
			{
				if(clone == null)
				{
					clone = typeof(Array).GetMethod("Clone", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
				}
				return clone;
			}
		}

		protected override void LazyPublishMembers()
		{
			MethodWrapper mw = new SimpleCallMethodWrapper(this, "clone", "()Ljava.lang.Object;", CloneMethod, CoreClasses.java.lang.Object.Wrapper, TypeWrapper.EmptyArray, Modifiers.Public, MemberFlags.HideFromReflection, SimpleOpCode.Callvirt, SimpleOpCode.Callvirt);
			mw.Link();
			SetMethods(new MethodWrapper[] { mw });
			SetFields(FieldWrapper.EmptyArray);
		}

		internal override Modifiers ReflectiveModifiers
		{
			get
			{
				return Modifiers.Final | Modifiers.Abstract | (ultimateElementTypeWrapper.ReflectiveModifiers & Modifiers.AccessMask);
			}
		}

		internal override string SigName
		{
			get
			{
				// for arrays the signature name is the same as the normal name
				return Name;
			}
		}

		internal override TypeWrapper[] Interfaces
		{
			get
			{
				if(interfaces == null)
				{
					TypeWrapper[] tw = new TypeWrapper[2];
					tw[0] = ClassLoaderWrapper.LoadClassCritical("java.lang.Cloneable");
					tw[1] = ClassLoaderWrapper.LoadClassCritical("java.io.Serializable");
					interfaces = tw;
				}
				return interfaces;
			}
		}

		internal override TypeWrapper[] InnerClasses
		{
			get
			{
				return TypeWrapper.EmptyArray;
			}
		}

		internal override TypeWrapper DeclaringTypeWrapper
		{
			get
			{
				return null;
			}
		}

		internal override Type TypeAsTBD
		{
			get
			{
				while (arrayType == null)
				{
					bool prevFinished = finished;
					Type type = MakeArrayType(ultimateElementTypeWrapper.TypeAsArrayType, this.ArrayRank);
					if (prevFinished)
					{
						// We were already finished prior to the call to MakeArrayType, so we can safely
						// set arrayType to the finished type.
						// Note that this takes advantage of the fact that once we've been finished,
						// we can never become unfinished.
						arrayType = type;
					}
					else
					{
						lock (this)
						{
							// To prevent a race with Finish, we can only set arrayType in this case
							// (inside the locked region) if we've not already finished. If we have
							// finished, we need to rerun MakeArrayType on the now finished element type.
							// Note that there is a benign race left, because it is possible that another
							// thread finishes right after we've set arrayType and exited the locked
							// region. This is not problem, because TypeAsTBD is only guaranteed to
							// return a finished type *after* Finish has been called.
							if (!finished)
							{
								arrayType = type;
							}
						}
					}
				}
				return arrayType;
			}
		}

		internal override void Finish()
		{
			if (!finished)
			{
				ultimateElementTypeWrapper.Finish();
				lock (this)
				{
					// Now that we've finished the element type, we must clear arrayType,
					// because it may still refer to a TypeBuilder. Note that we have to
					// do this atomically with setting "finished", to prevent a race
					// with TypeAsTBD.
					finished = true;
					arrayType = null;
				}
			}
		}

		internal override bool IsFastClassLiteralSafe
		{
			// here we have to deal with the somewhat strange fact that in Java you cannot represent primitive type class literals,
			// but you can represent arrays of primitive types as a class literal
			get { return ultimateElementTypeWrapper.IsFastClassLiteralSafe || ultimateElementTypeWrapper.IsPrimitive; }
		}

		internal static Type MakeArrayType(Type type, int dims)
		{
			// NOTE this is not just an optimization, but it is also required to
			// make sure that ReflectionOnly types stay ReflectionOnly types
			// (in particular instantiations of generic types from mscorlib that
			// have ReflectionOnly type parameters).
			for(int i = 0; i < dims; i++)
			{
				type = type.MakeArrayType();
			}
			return type;
		}
	}
}

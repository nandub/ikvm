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
using System.Reflection.Emit;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Xml;
using System.Diagnostics;

public class JVM
{
	private static bool debug = false;
	private static bool noJniStubs = false;
	private static bool isStaticCompiler = false;

	public static bool Debug
	{
		get
		{
			return debug;
		}
		set
		{
			debug = value;
		}
	}

	public static bool NoJniStubs
	{
		get
		{
			return noJniStubs;
		}
	}

	public static bool IsStaticCompiler
	{
		get
		{
			return isStaticCompiler;
		}
	}

	public static bool CleanStackTraces
	{
		get
		{
			return true;
		}
	}

	private class CompilerClassLoader : ClassLoaderWrapper
	{
		private Hashtable classes;
		private string assembly;
		private string path;
		private AssemblyBuilder assemblyBuilder;
		private ModuleBuilder moduleBuilder;

		internal CompilerClassLoader(string path, string assembly, Hashtable classes)
			: base(null)
		{
			this.classes = classes;
			this.assembly = assembly;
			this.path = path;
		}

		protected override ModuleBuilder CreateModuleBuilder()
		{
			AssemblyName name = new AssemblyName();
			name.Name = assembly;
			assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save);
			CustomAttributeBuilder ikvmAssemblyAttr = new CustomAttributeBuilder(typeof(IKVMAssemblyAttribute).GetConstructor(Type.EmptyTypes), new object[0]);
			assemblyBuilder.SetCustomAttribute(ikvmAssemblyAttr);
			moduleBuilder = assemblyBuilder.DefineDynamicModule(path, JVM.Debug);
			if(JVM.Debug)
			{
				CustomAttributeBuilder debugAttr = new CustomAttributeBuilder(typeof(DebuggableAttribute).GetConstructor(new Type[] { typeof(bool), typeof(bool) }), new object[] { true, true });
				moduleBuilder.SetCustomAttribute(debugAttr);
			}
			return moduleBuilder;
		}

		internal override TypeWrapper GetBootstrapType(string name)
		{
			TypeWrapper type = base.GetBootstrapType(name);
			if(type == null)
			{
				ClassFile f = (ClassFile)classes[name];
				if(f != null)
				{
					type = DefineClass(f);
				}
			}
			return type;
		}

		internal void SetMain(MethodInfo m, PEFileKinds target)
		{
			assemblyBuilder.SetEntryPoint(m, target);
		}

		internal void Save()
		{
			FinishAll();
			assemblyBuilder.Save(path);
		}

		internal void AddResources(Hashtable resources)
		{
			foreach(DictionaryEntry d in resources)
			{
				byte[] buf = (byte[])d.Value;
				if(buf.Length > 0)
				{
					moduleBuilder.DefineInitializedData((string)d.Key, buf, FieldAttributes.Public | FieldAttributes.Static);
				}
			}
			moduleBuilder.CreateGlobalFunctions();
		}
	}

	public static void Compile(string path, string assembly, string mainClass, PEFileKinds target, byte[][] classes, string[] references, bool nojni, Hashtable resources)
	{
		isStaticCompiler = true;
		noJniStubs = nojni;
		Hashtable h = new Hashtable();
		Console.WriteLine("Parsing class files");
		for(int i = 0; i < classes.Length; i++)
		{
			ClassFile f = new ClassFile(classes[i], 0, classes[i].Length, null);
			h[f.Name.Replace('/', '.')] = f;
		}
		Console.WriteLine("Constructing compiler");
		CompilerClassLoader loader = new CompilerClassLoader(path, assembly, h);
		ClassLoaderWrapper.SetBootstrapClassLoader(loader);
		foreach(string r in references)
		{
			Assembly.LoadFrom(r);
		}
		Console.WriteLine("Loading remapped types");
		loader.LoadRemappedTypes();
		Console.WriteLine("Compiling class files (1)");
		foreach(string s in h.Keys)
		{
			TypeWrapper wrapper = loader.LoadClassByDottedName(s);
			if(s == mainClass)
			{
				MethodWrapper mw = wrapper.GetMethodWrapper(new MethodDescriptor(loader, "main", "([Ljava/lang/String;)V"), false);
				if(mw == null)
				{
					Console.Error.WriteLine("Error: main method not found");
					return;
				}
				MethodInfo method = mw.GetMethod() as MethodInfo;
				if(method == null)
				{
					Console.Error.WriteLine("Error: redirected main method not supported");
					return;
				}
				loader.SetMain(method, target);
			}
		}
		Console.WriteLine("Compiling class files (2)");
		loader.AddResources(resources);
		loader.Save();
	}
	
	public static void SaveDebugImage(object mainClass)
	{
		ClassLoaderWrapper.SaveDebugImage(mainClass);
	}
}

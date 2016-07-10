using UnityEngine;
using System;
using System.Collections;
using System.Reflection;

public static class EditorFormulasUtils {

	public static Type GetTypeFromAssembly(string typeName, string assemblyName)
	{
		Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
		foreach(Assembly assembly in assemblies) {
			if(! (assembly.FullName.StartsWith(assemblyName))) { continue; }
			Type[] types = assembly.GetTypes();
			foreach(Type type in types) {
				//check for inline classes using +
				if(type.Name.Equals(typeName, StringComparison.CurrentCultureIgnoreCase) || type.Name.Contains('+' + typeName))
				{
					return type;
				}
			}
		}
		return null;
	}

	public static MethodInfo[] GetAllFormulaMethods()
	{
		var editorFormulasType = EditorFormulasUtils.GetTypeFromAssembly("EditorFormulas", "Assembly-CSharp-Editor");
		var methods = editorFormulasType.GetMethods(BindingFlags.Static | BindingFlags.Public);
		return methods;
	}

	public static MethodInfo GetFormulaMethod(string name)
	{
		var editorFormulasType = EditorFormulasUtils.GetTypeFromAssembly("EditorFormulas", "Assembly-CSharp-Editor");
		var method = editorFormulasType.GetMethod(name, BindingFlags.Static | BindingFlags.Public);
		return method;
	}
}

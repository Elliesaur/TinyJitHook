using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TinyJitHook.SJITHook;

namespace TinyJitHook.Extensions
{
    public static class JitHelper
    {
        public static void PrepareAssemblies(this AppDomain domain)
        {
            foreach (var asm in domain.GetAssemblies())
            {
                asm.PrepareMethods();
            }
        }
        public static void PrepareMethods(this Assembly asm)
        {
            foreach (Type t in asm.GetTypes())
            {
                if (t.ContainsGenericParameters || t.IsGenericType)
                    continue;

                foreach (MethodInfo mi in t.GetMethods())
                {
                    if (mi.IsAbstract || mi.IsGenericMethod || mi.IsGenericMethodDefinition)
                        continue;
                    if (mi.DeclaringType != null &&
                        (mi.DeclaringType.IsGenericType || mi.DeclaringType.IsGenericTypeDefinition))
                        continue;
                    RuntimeHelpers.PrepareMethod(mi.MethodHandle);
                }

                foreach (ConstructorInfo ci in t.GetConstructors())
                {
                    if (ci.IsAbstract)
                        continue;
                    RuntimeHelpers.PrepareMethod(ci.MethodHandle);
                }
            }
        }
        public static void PrepareOriginalCompileGetter(this IJitHook hook, bool is64Bit)
        {
            // Preload the original method property getter
            Type hookType = hook.GetType();
            Type[] genParams = hookType.GetGenericArguments();
            RuntimeTypeHandle[] genRTs = new RuntimeTypeHandle[genParams.Length];

            for (int i = 0; i < genParams.Length; i++)
            {
                genRTs[i] = genParams[i].TypeHandle;
            }

            RuntimeHelpers.PrepareMethod(
                hookType.GetMethod($"get_OriginalCompileMethod{(is64Bit ? "64" : "32")}").MethodHandle, genRTs);
        }
        public static IntPtr[] GetAssemblyScopes(this AppDomain domain)
        {
            Assembly[] asms = domain.GetAssemblies();
            var moduleScopes = new IntPtr[asms.Length];
            for (int i = 0; i < asms.Length; i++)
            {
                // The invoking assembly is generally at index 1.
                moduleScopes[i] = asms[i].ManifestModule.GetScope();
            }

            return moduleScopes;
        }
        public static Dictionary<IntPtr, Assembly> GetScopeMap(this AppDomain domain)
        {
            var moduleScopes = new Dictionary<IntPtr, Assembly>();
            foreach (var assembly in domain.GetAssemblies())
            {
                moduleScopes.Add(assembly.ManifestModule.GetScope(), assembly);
            }

            return moduleScopes;
        }
    }
}

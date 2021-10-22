//use the menu item "U3DMOBILE/Install Puerts" to install puerts,
//and add "U3DMOBILE_USE_PUERTS" on the project setting "Scripting Define Symbols".
#if U3DMOBILE_USE_PUERTS

using Puerts;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace U3DMobile.Editor
{
    [Configure]
    public static class PuertsConfig
    {
        private static readonly List<Type> specialCollection = new List<Type>()
        {
            typeof(UnityEngine.GameObject)
        };

        private static readonly HashSet<string> namespaceCollection = new HashSet<string>()
        {
            "U3DMobile",
        };

        [Binding]
        public static IEnumerable<Type> specialTypes
        {
            get { return specialCollection; }
        }

        [Binding]
        public static IEnumerable<Type> namespaceTypes
        {
            get { return GetNamespaceTypes(); }
        }

        private static IEnumerable<Type> GetNamespaceTypes()
        {
            List<Type> types = new List<Type>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                Type[] candidateTypes = assembly.GetTypes();
                foreach (Type type in candidateTypes)
                {
                    if (!type.IsPublic)
                    {
                        continue;
                    }
                    if (!namespaceCollection.Contains(type.Namespace))
                    {
                        continue;
                    }

                    types.Add(type);
                }
            }

            return types;
        }
    }
}

#endif

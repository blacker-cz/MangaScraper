using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Blacker.MangaScraper.Helpers
{
    static class ReflectionHelper
    {
        public static IEnumerable<Type> TypesImplementingInterface<T>()
        {
            return TypesImplementingInterface<T>(Enumerable.Empty<Type>());
        }

        public static IEnumerable<Type> TypesImplementingInterface<T>(IEnumerable<Type> except)
        {
            if (except == null)
                throw new ArgumentNullException("except");

            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => TypesImplementingInterface<T>(assembly, except));
        }

        public static IEnumerable<Type> TypesImplementingInterface<T>(Assembly assembly, IEnumerable<Type> except)
        {
            if(assembly == null)
                throw new ArgumentNullException("assembly");

            if (except == null)
                throw new ArgumentNullException("except");

            return assembly
                .GetTypes()
                .Where(type => typeof(T).IsAssignableFrom(type) && IsRealClass(type) && !except.Any(t => t.IsAssignableFrom(type))).ToList();
        }
        
        public static bool IsRealClass(Type testType)
        {
            if (testType == null)
                throw new ArgumentNullException("testType");

            return testType.IsAbstract == false
                && testType.IsGenericTypeDefinition == false
                && testType.IsInterface == false;
        }

        public static IEnumerable<T> GetInstances<T>()
        {
            return GetInstances<T>(Enumerable.Empty<Type>());
        }

        public static IEnumerable<T> GetInstances<T>(IEnumerable<Type> except)
        {
            if (except == null)
                throw new ArgumentNullException("except");

            return TypesImplementingInterface<T>(except).Select(x => CreateInstance<T>(x)).ToList();
        }

        public static IEnumerable<T> GetInstances<T>(Assembly assembly, IEnumerable<Type> except)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (except == null)
                throw new ArgumentNullException("except");

            return TypesImplementingInterface<T>(assembly, except).Select(x => CreateInstance<T>(x)).ToList();
        }

        public static T CreateInstance<T>(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            return (T)constructor.Invoke(null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacker.MangaScraper.Helpers
{
    class ReflectionHelper
    {
        public static IEnumerable<Type> TypesImplementingInterface<T>(IEnumerable<Type> except)
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(T).IsAssignableFrom(type) && IsRealClass(type) && !except.Any(t => t.IsAssignableFrom(type)));

        }
        
        public static bool IsRealClass(Type testType)
        {
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
            return TypesImplementingInterface<T>(except).Select(x => CreateInstance<T>(x));
        }

        public static T CreateInstance<T>(Type type)
        {
            var constructor = type.GetConstructor(Type.EmptyTypes);
            return (T)constructor.Invoke(null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace Blacker.MangaScraper.Helpers
{
    static class ReflectionHelper
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ReflectionHelper));

        public static IEnumerable<Type> TypesImplementingInterface<T>() where T : class
        {
            return TypesImplementingInterface<T>(Enumerable.Empty<Type>());
        }

        public static IEnumerable<Type> TypesImplementingInterface<T>(IEnumerable<Type> except) where T : class
        {
            if (except == null)
                throw new ArgumentNullException("except");

            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => TypesImplementingInterface<T>(assembly, except));
        }

        public static IEnumerable<Type> TypesImplementingInterface<T>(Assembly assembly, IEnumerable<Type> except) where T : class
        {
            if(assembly == null)
                throw new ArgumentNullException("assembly");

            if (except == null)
                throw new ArgumentNullException("except");

            return assembly
                .GetTypes()
                .Where(type => typeof (T).IsAssignableFrom(type) && IsRealClass(type) && !except.Any(t => t.IsAssignableFrom(type))).ToList();
        }
        
        public static bool IsRealClass(Type testType)
        {
            if (testType == null)
                throw new ArgumentNullException("testType");

            return testType.IsAbstract == false
                && testType.IsGenericTypeDefinition == false
                && testType.IsInterface == false;
        }

        public static IEnumerable<T> GetInstances<T>() where T : class
        {
            return GetInstances<T>(Enumerable.Empty<Type>());
        }

        public static IEnumerable<T> GetInstances<T>(IEnumerable<Type> except) where T : class
        {
            if (except == null)
                throw new ArgumentNullException("except");

            return TypesImplementingInterface<T>(except).Select(CreateInstance<T>).Where(i => i != null).ToList();
        }

        public static IEnumerable<T> GetInstances<T>(Assembly assembly, IEnumerable<Type> except) where T : class
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            if (except == null)
                throw new ArgumentNullException("except");

            return TypesImplementingInterface<T>(assembly, except).Select(CreateInstance<T>).Where(i => i != null).ToList();
        }

        public static T CreateInstance<T>(Type type) where T : class
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var constructor = type.GetConstructor(Type.EmptyTypes);

            try
            {
                if (constructor != null) 
                    return constructor.Invoke(null) as T;
            }
            catch (Exception ex)
            {
                _log.Error("Unable to create instance of the specified type.", ex);
            }

            return null;
        }
    }
}

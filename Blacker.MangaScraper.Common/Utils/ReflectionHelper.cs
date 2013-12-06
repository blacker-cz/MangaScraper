using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;

namespace Blacker.MangaScraper.Common.Utils
{
    public static class ReflectionHelper
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

            return TypesImplementingInterface<T>(AppDomain.CurrentDomain, except);
        }

        public static IEnumerable<Type> TypesImplementingInterface<T>(AppDomain appDomain, IEnumerable<Type> except) where T : class
        {
            if (appDomain == null) 
                throw new ArgumentNullException("appDomain");

            if (except == null)
                throw new ArgumentNullException("except");

            return appDomain
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

        public static IEnumerable<T> GetInstances<T>(AppDomain appDomain) where T : class
        {
            if (appDomain == null) 
                throw new ArgumentNullException("appDomain");

            return GetInstances<T>(appDomain, Enumerable.Empty<Type>());
        }

        public static IEnumerable<T> GetInstances<T>(IEnumerable<Type> except) where T : class
        {
            if (except == null)
                throw new ArgumentNullException("except");

            return TypesImplementingInterface<T>(except).Select(CreateInstance<T>).Where(i => i != null).ToList();
        }

        public static IEnumerable<T> GetInstances<T>(AppDomain appDomain, IEnumerable<Type> except) where T : class
        {
            if (appDomain == null) 
                throw new ArgumentNullException("appDomain");

            if (except == null)
                throw new ArgumentNullException("except");

            return TypesImplementingInterface<T>(appDomain, except).Select(CreateInstance<T>).Where(i => i != null).ToList();
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

        public static int LoadAssembliesFromDir(AppDomain targetAppDomain)
        {
            if (targetAppDomain == null)
                throw new ArgumentNullException("targetAppDomain");

            return LoadAssembliesFromDir(targetAppDomain, "*.dll");
        }

        public static int LoadAssembliesFromDir(AppDomain targetAppDomain, string searchPattern)
        {
            if (targetAppDomain == null) 
                throw new ArgumentNullException("targetAppDomain");

            if (String.IsNullOrEmpty(searchPattern)) 
                throw new ArgumentException("Search pattern must be not null or empty.", "searchPattern");

            try
            {
                var currentDir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

                return LoadAssembliesFromDir(targetAppDomain, currentDir, searchPattern);
            }
            catch (Exception ex)
            {
                _log.Error("Unable to load assemblies.", ex);

                return 0;
            }
        }

        public static int LoadAssembliesFromDir(AppDomain targetAppDomain, DirectoryInfo directory, string searchPattern)
        {
            if (targetAppDomain == null)
                throw new ArgumentNullException("targetAppDomain");

            if (directory == null) 
                throw new ArgumentNullException("directory");
            
            if (String.IsNullOrEmpty(searchPattern))
                throw new ArgumentException("Search pattern must be not null or empty.", "searchPattern");

            int counter = 0;

            try
            {
                FileInfo[] files = directory.GetFiles(searchPattern, SearchOption.TopDirectoryOnly);

                foreach (FileInfo file in files)
                {
                    try
                    {
                        // Load the file into the application domain.
                        AssemblyName assemblyName = AssemblyName.GetAssemblyName(file.FullName);
                        targetAppDomain.Load(assemblyName);

                        counter++;
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Unable to load assembly '" + file.FullName + "'. Skipping.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Unable to load assemblies.", ex);
            }

            return counter;
        }
    }
}

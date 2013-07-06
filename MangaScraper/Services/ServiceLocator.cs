using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using log4net;

namespace Blacker.MangaScraper.Services
{
    internal sealed class ServiceLocator : IServiceLocator
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(ServiceLocator));

        private static readonly object _syncRoot = new object();
        private static ServiceLocator _instance = null;

        private readonly ConcurrentDictionary<Type, KeyValuePair<Type, object>> _serviceTypes;

        private ServiceLocator()
        {
            _serviceTypes = new ConcurrentDictionary<Type, KeyValuePair<Type, object>>();
        }

        public static ServiceLocator Instance
        {
            get
            {
                lock (_syncRoot)
                {
                    return _instance ?? (_instance = new ServiceLocator());
                }
            }
        }

        public T GetService<T>()
        {
            KeyValuePair<Type, object> service;

            if (_serviceTypes.TryGetValue(typeof (T), out service))
            {
                if (service.Value == null)
                {
                    // use reflection to invoke the service
                    ConstructorInfo constructor = service.Key.GetConstructor(new Type[0]);
                    if (constructor == null) // this should not happen
                        throw new InvalidOperationException();

                    var newInstance = new KeyValuePair<Type, object>(service.Key, (T) constructor.Invoke(null));

                    _serviceTypes.TryUpdate(typeof (T), newInstance, service);

                    return (T) newInstance.Value;
                }
                else
                {
                    return (T) service.Value;
                }
            }
            else
            {
                throw new ApplicationException("The requested service is not registered.");
            }
        }

        public bool RegisterService(Type serviceType, Type implementationType)
        {
            if (implementationType.IsValueType)
                throw new ArgumentException("Registered service must not be value type.", "implementationType");

            ConstructorInfo constructor = implementationType.GetConstructor(new Type[0]);
            if (constructor == null)
                throw new ArgumentException("Registered service must have default constructor.", "implementationType");

            _log.DebugFormat("Registering service '{0}' with implementation '{1}'", serviceType, implementationType);

            return _serviceTypes.TryAdd(serviceType, new KeyValuePair<Type, object>(implementationType, null));
        }

        public bool UnregisterService(Type serviceType)
        {
            KeyValuePair<Type, object> tmp;

            _log.DebugFormat("Unregistering service '{0}'", serviceType);

            return _serviceTypes.TryRemove(serviceType, out tmp);
        }
    }
}

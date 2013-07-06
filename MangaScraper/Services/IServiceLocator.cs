using System;

namespace Blacker.MangaScraper.Services
{
    internal interface IServiceLocator
    {
        /// <summary>
        /// Get service
        /// </summary>
        /// <typeparam name="T">Implementation of type to return</typeparam>
        /// <returns>Instance of the type implementation</returns>
        /// <exception cref="ApplicationException">Thrown when service was not registered</exception>
        T GetService<T>();

        /// <summary>
        /// Register service to service locator
        /// </summary>
        /// <param name="serviceType">Type of the service which will be used to query implementation</param>
        /// <param name="implementationType">Type of the implementation</param>
        /// <returns>true if successful; false otherwise</returns>
        bool RegisterService(Type serviceType, Type implementationType);

        /// <summary>
        /// Unregister service from service locator
        /// </summary>
        /// <param name="serviceType">Service type</param>
        /// <returns>true if successful; false otherwise</returns>
        bool UnregisterService(Type serviceType);
    }
}
using System;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using MefContrib.Integration.Unity.Exporters;
using MefContrib.Integration.Unity.Extensions;
using Microsoft.Practices.Unity;

namespace MefContrib.Integration.Unity
{
    /// <summary>
    /// Contains extensions for the <see cref="IUnityContainer"/> interface.
    /// </summary>
    public static class UnityContainerExtensions
    {
        public static CompositionIntegration GetOrInitCompositionIntegration(this IUnityContainer unityContainer)
        {
            var compositionIntegration = unityContainer.Configure<CompositionIntegration>();
            if (compositionIntegration == null)
            {
                var unityExportProvider = new UnityExportProvider(unityContainer);
                compositionIntegration = new CompositionIntegration(true, unityExportProvider);
                unityContainer.AddExtension(compositionIntegration);
            }
            return compositionIntegration;
        }

        /// <summary>
        /// Registers a MEF catalog within Unity container.
        /// </summary>
        /// <param name="unityContainer">Unity container instance.</param>
        /// <param name="catalog">MEF catalog to be registered.</param>
        public static CompositionContainer RegisterCatalog(this IUnityContainer unityContainer, ComposablePartCatalog catalog)
        {
            lock (unityContainer)
            {
                var compositionIntegration = GetOrInitCompositionIntegration(unityContainer);

                compositionIntegration.Catalogs.Add(catalog);

                return compositionIntegration.CompositionContainer;
            }
        }

        public static CompositionContainer RegisterFallbackCatalog(this IUnityContainer unityContainer, ComposablePartCatalog catalog)
        {
            lock (unityContainer)
            {
                var compositionIntegration = GetOrInitCompositionIntegration(unityContainer);

                compositionIntegration.FallbackCatalogs.Add(catalog);

                return compositionIntegration.CompositionContainer;
            }
        }
        /// <summary>
        /// Returns whether a specified type has a type mapping registered in the container.
        /// </summary>
        /// <param name="container">The <see cref="IUnityContainer"/> to check for the type mapping.</param>
        /// <param name="type">The type to check if there is a type mapping for.</param>
        /// <returns><see langword="true"/> if there is a type mapping registered for <paramref name="type"/>.</returns>
        /// <remarks>In order to use this extension method, you first need to add the
        /// <see cref="IUnityContainer"/> extension to the <see cref="TypeRegistrationTrackerExtension"/>.
        /// </remarks>        
        public static bool IsTypeRegistered(this IUnityContainer container, Type type)
        {
            return TypeRegistrationTrackerExtension.IsTypeRegistered(container, type);
        }
    }
}
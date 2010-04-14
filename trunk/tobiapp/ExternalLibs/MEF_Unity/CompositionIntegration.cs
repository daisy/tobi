using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using MefContrib.Integration.Unity.Exporters;
using MefContrib.Integration.Unity.Extensions;
using MefContrib.Integration.Unity.Properties;
using Microsoft.Practices.ObjectBuilder2;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.ObjectBuilder;

namespace MefContrib.Integration.Unity
{
    /// <summary>
    /// Represents a Unity extension that adds integration with
    /// Managed Extensibility Framework.
    /// </summary>
    public sealed class CompositionIntegration : UnityContainerExtension, IDisposable
    {
        private readonly bool m_Register;

        private AggregateCatalog m_AggregateCatalog;
        private AggregateCatalog m_AggregateFallbackCatalog;
        private CatalogExportProvider m_FallbackCatalogExportProvider;

        private ExportProvider[] m_Providers;
        private CompositionContainer m_CompositionContainer;

        ///// <summary>
        ///// Initializes a new instance of <see cref="CompositionIntegration"/> class.
        ///// </summary>
        //public CompositionIntegration()
        //    : this(true)
        //{
        //}

        ///// <summary>
        ///// Initializes a new instance of <see cref="CompositionIntegration"/> class.
        ///// </summary>
        ///// <param name="providers">An array of export providers.</param>
        //public CompositionIntegration(params ExportProvider[] providers)
        //    : this(true, providers)
        //{
        //}

        /// <summary>
        /// Initializes a new instance of <see cref="CompositionIntegration"/> class.
        /// </summary>
        /// <param name="register">If true, <see cref="CompositionContainer"/> instance
        /// will be registered in the Unity container.</param>
        /// <param name="providers">An array of export providers.</param>
        public CompositionIntegration(bool register, params ExportProvider[] providers)
        {
            m_AggregateCatalog = new AggregateCatalog();
            m_AggregateFallbackCatalog = new AggregateCatalog();
            m_FallbackCatalogExportProvider = new CatalogExportProvider(m_AggregateFallbackCatalog);

            m_Register = register;

            if (providers == null)
            {
                m_Providers = new ExportProvider[] { m_FallbackCatalogExportProvider };
            }
            else
            {
                m_Providers = new ExportProvider[providers.Length + 1];
                m_Providers[0] = m_FallbackCatalogExportProvider;
                int i = 1;
                foreach (var exportProvider in providers)
                {
                    m_Providers[i++] = exportProvider;
                }
            }
        }

        protected override void Initialize()
        {
            TypeRegistrationTrackerExtension.RegisterIfMissing(Container);

            m_CompositionContainer = PrepareCompositionContainer();
#if NET40
            //IServiceLocator locator = ServiceLocator.Current;
            Context.Container.RegisterInstance(typeof(CompositionContainer), m_CompositionContainer);
#else
            Context.Locator.Add(typeof(CompositionContainer), m_CompositionContainer);
#endif


            Context.Strategies.AddNew<CompositionLifetimeStrategy>(UnityBuildStage.Lifetime);
            Context.Strategies.AddNew<CompositionStrategy>(UnityBuildStage.Initialization);
        }

        private CompositionContainer PrepareCompositionContainer()
        {
            // Create the MEF container based on the catalog
            var compositionContainer = new CompositionContainer(m_AggregateCatalog, m_Providers);
            m_FallbackCatalogExportProvider.SourceProvider = compositionContainer;

            // If desired, register an instance of CompositionContainer and Unity container in MEF,
            // this will also make CompositionContainer available to the Unity
            if (Register)
            {
                // Create composition batch and add the MEF container and the Unity
                // container to the MEF
                var batch = new CompositionBatch();
                batch.AddExportedValue(compositionContainer);
                batch.AddExportedValue(Container);

                // Prepare container
                compositionContainer.Compose(batch);
            }

            return compositionContainer;
        }

        /// <summary>
        /// Returns true if underlying <see cref="CompositionContainer"/> should be registered
        /// in the <see cref="IUnityContainer"/> container.
        /// </summary>
        public bool Register
        {
            get { return m_Register; }
        }

        /// <summary>
        /// Gets a collection of catalogs MEF is able to access.
        /// </summary>
        public ICollection<ComposablePartCatalog> Catalogs
        {
            get { return m_AggregateCatalog.Catalogs; }
        }

        public ICollection<ComposablePartCatalog> FallbackCatalogs
        {
            get { return m_AggregateFallbackCatalog.Catalogs; }
        }

        /// <summary>
        /// Gets <see cref="CompositionContainer"/> used by the extension.
        /// </summary>
        public CompositionContainer CompositionContainer
        {
            get { return m_CompositionContainer; }
        }

        #region Builder Strategies

        /// <summary>
        /// Represents a strategy which injects MEF dependencies to
        /// the Unity created object.
        /// </summary>
        private class CompositionStrategy : BuilderStrategy
        {
            public override void PostBuildUp(IBuilderContext context)
            {
                Type type = context.Existing.GetType();
                object[] attributes = type.GetCustomAttributes(typeof(PartNotComposableAttribute), false);

                if (attributes.Length == 0)
                {
                    CompositionContainer container = null;
#if NET40
                    IServiceLocator locator = ServiceLocator.Current;
                    container = locator.GetInstance<CompositionContainer>();
#else
                    container = context.Locator.Get<CompositionContainer>();
#endif
                    container.SatisfyImportsOnce(AttributedModelServices.CreatePart(context.Existing));
                }
            }
        }

        /// <summary>
        /// Represents a MEF lifetime strategy which tries to resolve desired
        /// component via MEF. If succeeded, build process is completed.
        /// </summary>
        private class CompositionLifetimeStrategy : BuilderStrategy
        {
            public override void PreBuildUp(IBuilderContext context)
            {
                CompositionContainer container = null;
#if NET40
                IServiceLocator locator = ServiceLocator.Current;
                container = locator.GetInstance<CompositionContainer>();
#else
                container = context.Locator.Get<CompositionContainer>();
#endif
                var buildKey = (NamedTypeBuildKey)context.BuildKey;

                try
                {
                    var exports = container.GetExports(buildKey.Type, null, buildKey.Name);

                    if (exports.Count() == 0)
                        return;

                    if (exports.Count() > 1)
                        throw new CompositionException(Resources.TooManyInstances);

                    if (exports.First().Metadata is IDictionary<string, object>)
                    {
                        var metadata = (IDictionary<string, object>)exports.First().Metadata;
                        if (metadata.ContainsKey(ExporterConstants.IsExternallyProvidedMetadataName) &&
                            true.Equals(metadata[ExporterConstants.IsExternallyProvidedMetadataName]))
                            return;
                    }

                    context.Existing = exports.First().Value;
                    context.BuildComplete = true;
                }
                catch (Exception)
                {
                    context.BuildComplete = false;
                    throw;
                }
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (m_CompositionContainer != null)
                m_CompositionContainer.Dispose();

            if (m_AggregateCatalog != null)
                m_AggregateCatalog.Dispose();

            if (m_AggregateFallbackCatalog != null)
                m_AggregateFallbackCatalog.Dispose();

            m_CompositionContainer = null;
            m_AggregateCatalog = null;
            m_AggregateFallbackCatalog = null;
            m_FallbackCatalogExportProvider = null;
            m_Providers = null;
        }

        #endregion
    }
}
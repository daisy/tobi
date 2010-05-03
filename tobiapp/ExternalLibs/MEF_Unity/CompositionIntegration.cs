using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using MefContrib.Integration.Unity.Extensions;
using MefContrib.Integration.Unity.Strategies;
using Microsoft.Practices.Composite.UnityExtensions;
using Microsoft.Practices.ObjectBuilder2;
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

        /// <summary>
        /// Gets a read-only collection of <see cref="ExportProvider"/>s registered in this extension.
        /// </summary>
        public IEnumerable<ExportProvider> Providers
        {
            get { return new List<ExportProvider>(m_Providers); }
        }

        protected override void Initialize()
        {
            TypeRegistrationTrackerExtension.RegisterIfMissing(Container);

            m_CompositionContainer = PrepareCompositionContainer();

            Debug.Assert(Container == Context.Container);

            Debug.Assert(Container.IsTypeRegistered(typeof(CompositionContainer))
                == Container.IsRegistered<CompositionContainer>());

            Debug.Assert(Container.IsTypeRegistered(typeof(CompositionContainer))
                == UnityContainerHelper.IsTypeRegistered(Container, typeof(CompositionContainer)));
            
//#if true || NET40
//            if (!Container.IsTypeRegistered(typeof(CompositionContainer)))
//            {
//                Context.Container.RegisterInstance(typeof(CompositionContainer), m_CompositionContainer);
//            }

//            //IServiceLocator locator = ServiceLocator.Current;
//#else
//            Context.Locator.Add(typeof(CompositionContainer), m_CompositionContainer);
//#endif

            Context.Policies.SetDefault<ICompositionContainerPolicy>(new CompositionContainerPolicy(m_CompositionContainer));
            Context.Strategies.AddNew<CompositionStrategy>(UnityBuildStage.TypeMapping);
            Context.Strategies.AddNew<ComposeStrategy>(UnityBuildStage.Initialization);
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
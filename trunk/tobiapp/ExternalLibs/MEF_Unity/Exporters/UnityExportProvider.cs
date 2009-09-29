using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using MefContrib.Integration.Unity.Extensions;
using MefContrib.Integration.Unity.Properties;
using Microsoft.Practices.Unity;

namespace MefContrib.Integration.Unity.Exporters
{
    /// <summary>
    /// Exposes all types registered in associated <see cref="IUnityContainer"/> container
    /// to MEF using <see cref="ExternalExportProvider"/> class.
    /// </summary>
    public sealed class UnityExportProvider : ExportProvider
    {
        private IUnityContainer m_UnityContainer;

        private readonly object m_SyncRoot = new object();
        private readonly Func<IUnityContainer> m_UnityContainerResolver;
        private readonly ExternalExportProvider m_ExternalExportProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="UnityExportProvider"/> class.
        /// </summary>
        /// <param name="unityContainerResolver">Delegate called when the container is needed for
        /// the first time.</param>
        public UnityExportProvider(Func<IUnityContainer> unityContainerResolver)
        {
            if (unityContainerResolver == null)
                throw new ArgumentNullException("unityContainerResolver");

            m_UnityContainerResolver = unityContainerResolver;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="UnityExportProvider"/> class.
        /// </summary>
        /// <param name="unityContainer">An instance of the <see cref="IUnityContainer"/> container.</param>
        public UnityExportProvider(IUnityContainer unityContainer)
        {
            if (unityContainer == null)
                throw new ArgumentNullException("unityContainer");

            m_UnityContainer = unityContainer;
            m_ExternalExportProvider = new ExternalExportProvider(UnityFactoryMethod);

            ConfigureUnityContainer();
        }

        private void ConfigureUnityContainer()
        {
            TypeRegistrationTrackerExtension.RegisterIfMissing(m_UnityContainer);

            m_UnityContainer.Configure<TypeRegistrationTrackerExtension>().Registering += (s, e) =>
                m_ExternalExportProvider.AddExportDefinition(e.TypeFrom, e.Name);

            m_UnityContainer.Configure<TypeRegistrationTrackerExtension>().RegisteringInstance += (s, e) =>
                m_ExternalExportProvider.AddExportDefinition(e.RegisteredType, e.Name);
        }

        private object UnityFactoryMethod(Type requestedType, string registrationName)
        {
            var obj = UnityContainer.Resolve(requestedType, registrationName);
            return obj;
        }

        protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
        {
            return m_ExternalExportProvider.GetExports(definition);
        }

        /// <summary>
        /// Gets associated <see cref="IUnityContainer"/> container.
        /// </summary>
        public IUnityContainer UnityContainer
        {
            get
            {
                if (m_UnityContainer == null)
                {
                    lock (m_SyncRoot)
                    {
                        if (m_UnityContainer == null)
                        {
                            m_UnityContainer = m_UnityContainerResolver.Invoke();

                            if (m_UnityContainer == null)
                                throw new Exception(Resources.UnityNullException);

                            ConfigureUnityContainer();
                        }
                    }
                }

                return m_UnityContainer;
            }
        }
    }
}
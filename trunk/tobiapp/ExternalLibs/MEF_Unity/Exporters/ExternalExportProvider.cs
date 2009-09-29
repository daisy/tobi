using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;

namespace MefContrib.Integration.Unity.Exporters
{
    /// <summary>
    /// Represents an external export provider.
    /// </summary>
    /// <remarks>
    /// This class can be used to build custom <see cref="ExportProvider"/> which
    /// provides exports from various data sources.
    /// </remarks>
    public class ExternalExportProvider : ExportProvider
    {
        private readonly Func<Type, string, object> m_FactoryMethod;
        private readonly List<ExternalExportDefinition> m_Definitions;

        /// <summary>
        /// Initializes a new instance of <see cref="ExternalExportProvider"/> class.
        /// </summary>
        /// <param name="factoryMethod">Method that is called when an instance os specific type
        /// is requested, optionally with given registration name.</param>
        public ExternalExportProvider(Func<Type, string, object> factoryMethod)
        {
            if (factoryMethod == null)
                throw new ArgumentNullException("factoryMethod");

            m_Definitions = new List<ExternalExportDefinition>();
            m_FactoryMethod = factoryMethod;
        }

        protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
        {
            return GetExportsCore(ReadOnlyDefinitions, definition.Constraint.Compile());
        }

        private IEnumerable<Export> GetExportsCore(
            IEnumerable<ExternalExportDefinition> exportDefinitions,
            Func<ExportDefinition, bool> constraint)
        {
            Debug.Assert(exportDefinitions != null);
            Debug.Assert(constraint != null);

            var exports = new List<Export>();
            foreach (var exportDefinition in exportDefinitions)
            {
                if (constraint(exportDefinition))
                {
                    exports.Add(CreateExport(exportDefinition));
                }
            }
            return exports;
        }

        private Export CreateExport(ExternalExportDefinition export)
        {
            return new Export(export, () => GetExportedObject(
                export.ServiceType, export.RegistrationName));
        }

        private object GetExportedObject(Type type, string contractName)
        {
            return m_FactoryMethod.Invoke(type, contractName);
        }

        /// <summary>
        /// Adds a new export definition.
        /// </summary>
        /// <param name="type">Type that is being exported.</param>
        public void AddExportDefinition(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            m_Definitions.Add(new ExternalExportDefinition(type));
        }

        /// <summary>
        /// Adds a new export definition.
        /// </summary>
        /// <param name="type">Type that is being exported.</param>
        /// <param name="registrationName">Registration name under which <paramref name="type"/>
        /// is being exported.</param>
        public void AddExportDefinition(Type type, string registrationName)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            m_Definitions.Add(new ExternalExportDefinition(type, registrationName));
        }

        /// <summary>
        /// Gets a read only list of definitions known to the export provider.
        /// </summary>
        public IList<ExternalExportDefinition> ReadOnlyDefinitions
        {
            get { return m_Definitions.AsReadOnly(); }
        }
    }
}
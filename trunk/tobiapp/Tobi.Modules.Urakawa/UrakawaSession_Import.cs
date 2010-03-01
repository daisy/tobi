using System;
using System.IO;
using Microsoft.Practices.Composite.Logging;
using urakawa.daisy.import;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        private const string XUK_DIR = "_XUK"; // prepend with '_' so it appears at the top of the alphabetical sorting in the file explorer window

        private bool doImport()
        {
            m_Logger.Log(String.Format(@"UrakawaSession.doImport() [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

            var converter = new Daisy3_Import(DocumentFilePath,
                Path.Combine(Path.GetDirectoryName(DocumentFilePath),
                XUK_DIR)); //Directory.GetParent(bookfile).FullName

            return DoWorkProgressUI("Importing ...",                   // TODO LOCALIZE Importing
                converter,
                () =>
                {
                    DocumentFilePath = null;
                    DocumentProject = null;
                },
                () =>
                {
                    DocumentFilePath = converter.XukPath;
                    DocumentProject = converter.Project;

                    AddRecentFile(new Uri(DocumentFilePath, UriKind.Absolute));
                });
        }
    }
}

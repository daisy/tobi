using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Practices.Composite.Events;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.UI;
using Tobi.Common.Validation;
using Tobi.Plugin.Validator.Metadata;
using urakawa;
using urakawa.metadata;
using urakawa.metadata.daisy;
using urakawa.commands;
using urakawa.data;
using urakawa.ExternalFiles;

namespace Tobi.Plugin.MetadataPane
{
    /// <summary>
    /// ViewModel for the MetadataPane
    /// </summary>
    [Export(typeof(MetadataPaneViewModel)), PartCreationPolicy(CreationPolicy.Shared)]
    public class MetadataPaneViewModel : ViewModelBase, IPartImportsSatisfiedNotification
    {
#pragma warning disable 1591 // non-documented method
        public void OnImportsSatisfied()
#pragma warning restore 1591
        {
            //#if DEBUG
            //            Debugger.Break();
            //#endif

        }

        private readonly IEventAggregator m_EventAggregator;
        private readonly ILoggerFacade m_Logger;

        private readonly IUrakawaSession m_UrakawaSession;

        public readonly IShellView m_ShellView;

        [ImportingConstructor]
        public MetadataPaneViewModel(
            IEventAggregator eventAggregator,
            [Import(typeof(IShellView), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IShellView shellView,
            ILoggerFacade logger,
            [Import(typeof(IUrakawaSession), RequiredCreationPolicy = CreationPolicy.Shared, AllowDefault = false)]
            IUrakawaSession session,
            [Import(typeof(MetadataValidator), RequiredCreationPolicy = CreationPolicy.Shared, AllowRecomposition = false)]
            MetadataValidator validator
            )
        {
            m_EventAggregator = eventAggregator;
            m_Logger = logger;

            m_Validator = validator;
            m_UrakawaSession = session;
            m_ShellView = shellView;

            m_MetadataCollection = null;

            ValidationItems = new ObservableCollection<ValidationItem>();

            if (m_Validator != null)
            {
                m_Validator.ValidatorStateRefreshed += OnValidatorStateRefreshed;
                resetValidationItems(m_Validator);
            }

            m_EventAggregator.GetEvent<ProjectLoadedEvent>().Subscribe(OnProjectLoaded, ProjectLoadedEvent.THREAD_OPTION);
            m_EventAggregator.GetEvent<ProjectUnLoadedEvent>().Subscribe(OnProjectUnLoaded, ProjectUnLoadedEvent.THREAD_OPTION);
        }

        public ObservableCollection<ValidationItem> ValidationItems { get; set; }
        private MetadataValidator m_Validator;

        private void resetValidationItems(MetadataValidator metadataValidator)
        {
            ValidationItems.Clear();

            if (metadataValidator.ValidationItems == null) // metadataValidator.IsValid
            {
                return;
            }

            foreach (var validationItem in metadataValidator.ValidationItems)
            {
                //if (!((MetadataValidationError)validationItem).Definition.IsReadOnly)
                ValidationItems.Add(validationItem);
            }

            RaisePropertyChanged(() => ValidationItems);
        }

        private void OnValidatorStateRefreshed(object sender, ValidatorStateRefreshedEventArgs e)
        {
            resetValidationItems((MetadataValidator)e.Validator);
        }


        private void OnProjectUnLoaded(Project obj)
        {
            OnProjectLoaded(null);
        }

        private void OnProjectLoaded(Project project)
        {
            //if (m_UrakawaSession.IsXukSpine)
            //{
            //    return;
            //}

            m_Logger.Log("MetadataPaneViewModel.OnProject(UN)Loaded" + (project == null ? "(null)" : ""),
                Category.Debug, Priority.Medium);

            m_MetadataCollection = null;

            RaisePropertyChanged(() => MetadataCollection);
        }


        //remove metadata entries with empty names
        public void removeEmptyMetadata()
        {
            Presentation presentation = m_UrakawaSession.DocumentProject.Presentations.Get(0);
            List<MetadataRemoveCommand> removalsList = new List<MetadataRemoveCommand>();

            foreach (Metadata m in presentation.Metadatas.ContentsAs_Enumerable)
            {
                if (string.IsNullOrEmpty(m.NameContentAttribute.Name)
                    || m.NameContentAttribute.Name == SupportedMetadata_Z39862005.MagicStringEmpty)
                {
                    MetadataRemoveCommand cmd = presentation.CommandFactory.CreateMetadataRemoveCommand(m);
                    removalsList.Add(cmd);
                }
            }
            foreach (MetadataRemoveCommand cmd in removalsList)
            {
                presentation.UndoRedoManager.Execute(cmd);
            }
        }


        private MetadataCollection m_MetadataCollection;
        public MetadataCollection MetadataCollection
        {
            get
            {
                if (m_UrakawaSession.DocumentProject == null)
                {
                    m_MetadataCollection = null;
                }
                else
                {
                    if (m_MetadataCollection == null)
                    {
                        Presentation presentation = m_UrakawaSession.DocumentProject.Presentations.Get(0);

                        m_MetadataCollection = new MetadataCollection(presentation.Metadatas.ContentsAs_Enumerable);
                        //SupportedMetadata_Z39862005.DefinitionSet.Definitions

                        presentation.Metadatas.ObjectAdded += m_MetadataCollection.OnMetadataAdded;
                        presentation.Metadatas.ObjectRemoved += m_MetadataCollection.OnMetadataDeleted;
                    }
                }

                return m_MetadataCollection;
            }
        }

        public void RemoveMetadata(NotifyingMetadataItem metadata)
        {
            Presentation presentation = m_UrakawaSession.DocumentProject.Presentations.Get(0);
            MetadataRemoveCommand cmd = presentation.CommandFactory.CreateMetadataRemoveCommand
                (metadata.UrakawaMetadata);
            presentation.UndoRedoManager.Execute(cmd);
        }

        public void AddEmptyMetadata()
        {
            Presentation presentation = m_UrakawaSession.DocumentProject.Presentations.Get(0);

            Metadata metadata = presentation.MetadataFactory.CreateMetadata();
            metadata.NameContentAttribute = new MetadataAttribute
            {
                Name = SupportedMetadata_Z39862005.MagicStringEmpty, //"",
                NamespaceUri = "",
                Value = SupportedMetadata_Z39862005.MagicStringEmpty
            };
            MetadataAddCommand cmd = presentation.CommandFactory.CreateMetadataAddCommand
                (metadata);
            presentation.UndoRedoManager.Execute(cmd);
        }

        public static readonly string METADATA_IMPORT_DIRECTORY =
            Path.Combine(ExternalFilesDataManager.STORAGE_FOLDER_PATH, "METADATA");
        public static readonly string METADATA_IMPORT_FILE =
            Path.Combine(METADATA_IMPORT_DIRECTORY, "metadata-import.txt");

        public List<Tuple<string, string>> readMetadataImport(out string text)
        {
            text = "";

            List<Tuple<string, string>> metadatas = new List<Tuple<string, string>>();

            if (!File.Exists(METADATA_IMPORT_FILE))
            {
                if (!Directory.Exists(METADATA_IMPORT_DIRECTORY))
                {
                    FileDataProvider.CreateDirectory(METADATA_IMPORT_DIRECTORY);
                }

                StreamWriter streamWriter = new StreamWriter(METADATA_IMPORT_FILE, false, Encoding.UTF8);
                try
                {
                    string line = "//";
                    streamWriter.WriteLine(line);
                    text += (line + '\n');

                    line = "// PLEASE EDIT THIS EXAMPLE (CHANGES WILL BE SAVED)";
                    streamWriter.WriteLine(line);
                    text += (line + '\n');

                    line = "//";
                    streamWriter.WriteLine(line);
                    text += (line + '\n');

                    line = "// Each metadata item is a [ key + value ] pair.";
                    streamWriter.WriteLine(line);
                    text += (line + '\n');

                    line = "//     [key] on one line";
                    streamWriter.WriteLine(line);
                    text += (line + '\n');

                    line = "//     [value] on the next line";
                    streamWriter.WriteLine(line);
                    text += (line + '\n');

                    line = "// (empty lines are ignored)";
                    streamWriter.WriteLine(line);
                    text += (line + '\n');

                    line = "//";
                    streamWriter.WriteLine(line);
                    text += (line + '\n');

                    line = "";
                    streamWriter.WriteLine(line);
                    text += (line + '\n');


                    string key = "dc:Creator";
                    string value = "Firstname Surname";
                    metadatas.Add(new Tuple<string, string>(key, value));
                    streamWriter.WriteLine(key);
                    text += (key + '\n');
                    streamWriter.WriteLine(value);
                    text += (value + '\n');
                    streamWriter.WriteLine("");
                    text += ("" + '\n');

                    key = "dc:Title";
                    value = "The Title";
                    metadatas.Add(new Tuple<string, string>(key, value));
                    streamWriter.WriteLine(key);
                    text += (key + '\n');
                    streamWriter.WriteLine(value);
                    text += (value + '\n');
                    streamWriter.WriteLine("");
                    text += ("" + '\n');

                    key = "dc:Identifier";
                    value = "UUID";
                    metadatas.Add(new Tuple<string, string>(key, value));
                    streamWriter.WriteLine(key);
                    text += (key + '\n');
                    streamWriter.WriteLine(value);
                    text += (value + '\n');
                    streamWriter.WriteLine("");
                    text += ("" + '\n');
                }
                finally
                {
                    streamWriter.Close();
                }
            }
            else
            {
                StreamReader streamReader = new StreamReader(METADATA_IMPORT_FILE, Encoding.UTF8);
                try
                {
                    string key = null;
                    string value = null;

                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        text += (line + '\n');
                        line = line.Trim();
                        if (string.IsNullOrEmpty(line)) continue;
                        if (line.StartsWith("//")) continue;

                        if (string.IsNullOrEmpty(key))
                        {
                            key = line;
                        }
                        else
                        {
                            value = line;

                            metadatas.Add(new Tuple<string, string>(key, value));

                            key = null;
                            value = null;
                        }
                    }
                }
                finally
                {
                    streamReader.Close();
                }
            }

            return metadatas;
        }

        public void ImportMetadata()
        {
            //m_ShellView.ExecuteShellProcess(MetadataPaneViewModel.METADATA_IMPORT_DIRECTORY);

            string text;
            List<Tuple<string, string>> metadatas = readMetadataImport(out text);

            var editBox = new TextBoxReadOnlyCaretVisible
            {
                Text = text,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.WrapWithOverflow
            };

            var windowPopup = new PopupModalWindow(m_ShellView,
                                                   UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_MetadataPane_Lang.Import),
                                                   new ScrollViewer { Content = editBox },
                                                   PopupModalWindow.DialogButtonsSet.OkCancel,
                                                   PopupModalWindow.DialogButton.Ok,
                                                   true, 500, 600, null, 40, null);

            windowPopup.EnableEnterKeyDefault = false;

            editBox.Loaded += new RoutedEventHandler((sender, ev) =>
            {
                //editBox.SelectAll();
                FocusHelper.FocusBeginInvoke(editBox);
            });

            windowPopup.ShowModal();


            if (PopupModalWindow.IsButtonOkYesApply(windowPopup.ClickedDialogButton))
            {
                if (!string.IsNullOrEmpty(editBox.Text))
                {
                    StreamWriter streamWriter = new StreamWriter(METADATA_IMPORT_FILE, false, Encoding.UTF8);
                    try
                    {
                        streamWriter.Write(editBox.Text);
                    }
                    finally
                    {
                        streamWriter.Close();
                    }

                    string newText;
                    metadatas = readMetadataImport(out newText);
                    //DebugFix.assert(newText.Equals(editBox.Text, StringComparison.Ordinal));

                    Presentation presentation = m_UrakawaSession.DocumentProject.Presentations.Get(0);

                    foreach (Tuple<string, string> md in metadatas)
                    {
                        List<NotifyingMetadataItem> toRemove = new List<NotifyingMetadataItem>();
                        foreach (NotifyingMetadataItem m in this.MetadataCollection.Metadatas)
                        {
                            if (m.Name.Equals(md.Item1, StringComparison.Ordinal))
                            {
                                if (!m.Definition.IsRepeatable)
                                {
                                    if (!toRemove.Contains(m))
                                    {
                                        toRemove.Add(m);
                                    }
                                }
                            }
                        }
                        foreach (var m in toRemove)
                        {
                            RemoveMetadata(m);
                        }

                        Metadata metadata = presentation.MetadataFactory.CreateMetadata();
                        metadata.NameContentAttribute = new MetadataAttribute
                        {
                            Name = md.Item1,
                            NamespaceUri = "",
                            Value = md.Item2
                        };
                        MetadataAddCommand cmd = presentation.CommandFactory.CreateMetadataAddCommand
                            (metadata);
                        presentation.UndoRedoManager.Execute(cmd);
                    }
                }
            }
        }


        public string GetViewModelDebugStringForMetaData()
        {
            string data = "";

            //iterate through our observable collection
            foreach (NotifyingMetadataItem m in this.MetadataCollection.Metadatas)
            {
                data += string.Format("{0} = {1}" + Environment.NewLine, m.Name, m.Content);

                foreach (var optAttr in m.UrakawaMetadata.OtherAttributes.ContentsAs_Enumerable)
                {
                    data += string.Format("-- {0} = {1} (NS: {2})" + Environment.NewLine, optAttr.Name, optAttr.Value, optAttr.NamespaceUri);
                }
            }
            return data;
        }

        public string GetDataModelDebugStringForMetaData()
        {
            string data = "";

            foreach (Metadata m in m_UrakawaSession.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_Enumerable)
            {
                data += string.Format("{0} = {1}" + Environment.NewLine, m.NameContentAttribute.Name, m.NameContentAttribute.Value);

                foreach (var optAttr in m.OtherAttributes.ContentsAs_Enumerable)
                {
                    data += string.Format("-- {0} = {1} (NS: {2})" + Environment.NewLine, optAttr.Name, optAttr.Value, optAttr.NamespaceUri);
                }
            }
            return data;
        }

        /// <summary>
        /// based on the existing metadata, return a list of metadata fields available
        /// for addition
        /// </summary>
        public ObservableCollection<string> AvailableMetadataNames
        {
            get
            {
                ObservableCollection<string> list = new ObservableCollection<string>();

                if (m_UrakawaSession.DocumentProject == null)
                {
                    return list;
                }

                List<MetadataDefinition> availableMetadata = new List<MetadataDefinition>();

                foreach (MetadataDefinition definition in SupportedMetadata_Z39862005.DefinitionSet.Definitions)
                {
                    //string name = definition.Name.ToLower();
                    bool exists = false;
                    foreach (Metadata item in m_UrakawaSession.DocumentProject.Presentations.Get(0).Metadatas.ContentsAs_Enumerable)
                    {
                        if (item.NameContentAttribute.Name.Equals(definition.Name, StringComparison.Ordinal)) //OrdinalIgnoreCase
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        availableMetadata.Add(definition);
                    }
                    else
                    {
                        if (definition.IsRepeatable)
                        {
                            availableMetadata.Add(definition);
                        }
                    }
                }

                foreach (MetadataDefinition metadata in availableMetadata)
                {
                    if (!metadata.IsReadOnly)
                    {
                        list.Add(metadata.Name);
                    }
                }
                return list;
            }
        }

        internal void SelectionChanged()
        {
            RaisePropertyChanged(() => AvailableMetadataNames); // triggers ComboBox ItemsSource data binding refresh
        }

        /*
         * SEE ValidationItemSelectedEvent
        //todo: scroll to the selected item
        public void OnValidationErrorSelected(ValidationErrorSelectedEventArgs e)
        {
            
        }
         * */
    }
}

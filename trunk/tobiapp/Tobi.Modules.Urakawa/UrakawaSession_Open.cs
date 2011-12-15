using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using AudioLib;
using Saxon.Api;
using Microsoft.Practices.Composite.Logging;
using Microsoft.Win32;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa;
using urakawa.core;
using urakawa.exception;
using urakawa.xuk;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public RichDelegateCommand OpenCommand { get; private set; }

        private void initCommands_Open()
        {
            //NewCommand = new RichDelegateCommand(
            //    UserInterfaceStrings.New,
            //    UserInterfaceStrings.New_,
            //    UserInterfaceStrings.New_KEYS,
            //    shellView.LoadTangoIcon("document-new"),
            //    ()=>
            //    {
            //        string currentAssemblyDirectoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //        openFile(currentAssemblyDirectoryName + @"\empty-dtbook-z3986-2005.xml");
            //    },
            //    ()=> true);
            //shellView.RegisterRichCommand(NewCommand);
            //
            OpenCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.CmdOpen_ShortDesc,
                Tobi_Plugin_Urakawa_Lang.CmdOpen_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"document-open"),
                () =>
                {
                    var dlg = new OpenFileDialog
                    {
                        FileName = @"",
                        DefaultExt = @".xml",
#if DEBUG
                        Filter = @"DTBook, OPF, OBI, XUK or EPUB (*.xml, *.opf, *.obi, *.xuk, *.epub)|*.xml;*.opf;*.obi;*.xuk;*.epub",
#else
                        Filter = @"DTBook, OPF, OBI or XUK (*.xml, *.opf, *.obi, *.xuk)|*.xml;*.opf;*.obi;*.xuk",
#endif //DEBUG
                        CheckFileExists = false,
                        CheckPathExists = false,
                        AddExtension = true,
                        DereferenceLinks = true,
                        Title = @"Tobi: " + UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CmdOpen_ShortDesc)
                    };

                    bool? result = false;

                    m_ShellView.DimBackgroundWhile(() => { result = dlg.ShowDialog(); });

                    if (result == false)
                    {
                        return;
                    }
                    try
                    {
                        OpenFile(dlg.FileName);
                    }
                    catch (Exception ex)
                    {
                        ExceptionHandler.Handle(ex, false, m_ShellView);
                    }
                },
                () => true,
                Settings_KeyGestures.Default,
                PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_Open));

            m_ShellView.RegisterRichCommand(OpenCommand);
        }

        public void TryOpenFile(string filename)
        {
            if (OpenCommand.CanExecute() && File.Exists(filename))
            {
                OpenFile(filename);
            }
        }

        public bool OpenFile(string filename)
        {
            var fileUri = new Uri(filename, UriKind.Absolute);
            AddRecentFile(fileUri);

            if (!File.Exists(fileUri.LocalPath)
                || !fileUri.IsFile)
            {
                var label = new TextBlock
                {
                    Text = Tobi_Plugin_Urakawa_Lang.CannotOpenLocalFile_,
                    Margin = new Thickness(8, 0, 8, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Focusable = true,
                    TextWrapping = TextWrapping.Wrap
                };

                var iconProvider = new ScalableGreyableImageProvider(m_ShellView.LoadTangoIcon("dialog-warning"), m_ShellView.MagnificationLevel);

                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                };
                panel.Children.Add(iconProvider.IconLarge);
                panel.Children.Add(label);

                var details = new TextBoxReadOnlyCaretVisible
                {
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6),
                    TextReadOnly = fileUri.ToString()
                };

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.CannotOpenLocalFile),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.Close,
                                                       PopupModalWindow.DialogButton.Close,
                                                       true, 300, 170, details, 40);

                windowPopup.ShowModal();

                return false;
            }

            // Closing is REQUIRED ! 
            PopupModalWindow.DialogButton button = CheckSaveDirtyAndClose(PopupModalWindow.DialogButtonsSet.YesNoCancel, Tobi_Plugin_Urakawa_Lang.UrakawaSession_OpenFile);
            if (PopupModalWindow.IsButtonEscCancel(button))
            {
                return false;
            }

            string ext = Path.GetExtension(fileUri.ToString()).ToLower();
            if (ext == @".xuk")
            {
                //todo: should we implement HTTP open ?
                if (!fileUri.IsFile)
                    throw new InvalidUriException("The URI to open must point to a local file! " + Environment.NewLine + fileUri.ToString());

                m_Logger.Log(String.Format(@"UrakawaSession.openFile(XUK) [{0}]", DocumentFilePath), Category.Debug, Priority.Medium);

                //fileUri.Scheme.ToLower() == "file"
                DocumentFilePath = fileUri.IsFile ? fileUri.LocalPath : filename;

                if (!File.Exists(DocumentFilePath))
                    throw new InvalidUriException("The import URI must point to an existing file! " + Environment.NewLine + fileUri.ToString());

                var project = new Project();

                //var backWorker = new BackgroundWorker
                //    {
                //        WorkerSupportsCancellation = true,
                //        WorkerReportsProgress = true
                //    };

                //var uri = new Uri(DocumentFilePath, UriKind.Absolute);
                //DocumentProject.OpenXuk(uri);

                var action = new OpenXukAction(project, fileUri)
                {
                    ShortDescription = Tobi_Plugin_Urakawa_Lang.UrakawaOpenAction_ShortDesc,
                    LongDescription = Tobi_Plugin_Urakawa_Lang.UrakawaOpenAction_LongDesc
                };

                bool cancelled = false;

                bool result = m_ShellView.RunModalCancellableProgressTask(true,
                    Tobi_Plugin_Urakawa_Lang.UrakawaOpenAction_ShortDesc, action,
                    () =>
                    {
                        cancelled = true;

                        DocumentFilePath = null;
                        DocumentProject = null;

                        //backWorker.CancelAsync();
                    },
                    () =>
                    {
                        cancelled = false;

                        if (project.Presentations.Count == 0)
                        {
                            Debug.Fail("Project does not contain a Presentation !" + Environment.NewLine + fileUri.ToString());
                            //workException = new XukException()
                        }
                        else
                            DocumentProject = project;
                    }
                    );

                if (!result)
                {
                    DebugFix.Assert(cancelled);
                    return false;
                }

                if (DocumentProject != null)
                {
                    //m_Logger.Log(@"-- PublishEvent [ProjectLoadedEvent] UrakawaSession.OpenFile", Category.Debug, Priority.Medium);

                    if (m_EventAggregator != null)
                    {
                        m_EventAggregator.GetEvent<ProjectLoadedEvent>().Publish(DocumentProject);
                    }

                    var treeNode = TreeNode.EnsureTreeNodeHasNoSignificantTextOnlySiblings(DocumentProject.Presentations.Get(0).RootNode, null);
                    if (treeNode != null)
                    {
                        //m_Logger.Log(@"-- PublishEvent [TreeNodeSelectedEvent] DocumentPaneView.OnFlowDocumentLoaded", Category.Debug, Priority.Medium);

                        PerformTreeNodeSelection(treeNode);
                        //m_EventAggregator.GetEvent<TreeNodeSelectedEvent>().Publish(treeNode);
                    }

                    return true;
                }
                //var progressBar = new ProgressBar
                //{
                //    IsIndeterminate = false,
                //    Height = 18,
                //    HorizontalAlignment = HorizontalAlignment.Stretch,
                //    Minimum = 0,
                //    Maximum = 100,
                //    Value = 0
                //};

                //var label = new TextBlock
                //{
                //    Text = action.ShortDescription,
                //    Margin = new Thickness(0, 0, 0, 8),
                //    HorizontalAlignment = HorizontalAlignment.Left,
                //    VerticalAlignment = VerticalAlignment.Top,
                //    Focusable = true,
                //};
                //var panel = new StackPanel
                //{
                //    Orientation = Orientation.Vertical,
                //    HorizontalAlignment = HorizontalAlignment.Stretch,
                //    VerticalAlignment = VerticalAlignment.Center,
                //};

                //panel.Children.Add(label);
                //panel.Children.Add(progressBar);

                //var details = new TextBoxReadOnlyCaretVisible(action.LongDescription);

                //var windowPopup = new PopupModalWindow(m_ShellView,
                //                                       UserInterfaceStrings.EscapeMnemonic(
                //                                           Tobi_Plugin_Urakawa_Lang.RunningTask),
                //                                       panel,
                //                                       PopupModalWindow.DialogButtonsSet.Cancel,
                //                                       PopupModalWindow.DialogButton.Cancel,
                //                                       false, 500, 150, details, 80);


                //Exception workException = null;
                //backWorker.DoWork += delegate(object s, DoWorkEventArgs args)
                //{
                //    //var dummy = (string)args.Argument;

                //    if (backWorker.CancellationPending)
                //    {
                //        args.Cancel = true;
                //        return;
                //    }

                //    action.Execute();

                //    args.Result = @"dummy result";
                //};

                //backWorker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
                //{
                //    progressBar.Value = args.ProgressPercentage;
                //};

                //backWorker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                //{
                //    workException = args.Error;

                //    if (cancelFlag)
                //    {
                //        DocumentFilePath = null;
                //        DocumentProject = null;
                //    }
                //    else if (args.Cancelled)
                //    {
                //        DocumentFilePath = null;
                //        DocumentProject = null;
                //        windowPopup.ForceClose(PopupModalWindow.DialogButton.Cancel);
                //    }
                //    else if (workException != null)
                //    {
                //        windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                //    }
                //    else
                //    {
                //        if (project.Presentations.Count == 0)
                //        {
                //            workException = new XukException("Project does not contain a Presentation !" + Environment.NewLine + fileUri.ToString());
                //        }
                //        else
                //            DocumentProject = project;
                //        windowPopup.ForceClose(PopupModalWindow.DialogButton.ESC);
                //    }


                //    //var result = (string)args.Result;

                //    backWorker = null;
                //};

                //backWorker.RunWorkerAsync(@"dummy arg");
                //windowPopup.ShowModal();

                //if (workException != null)
                //{
                //    throw workException;
                //}

                //if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Cancel)
                //{
                //    cancelFlag = true;
                //    return false;
                //}
            }
            else if (ext == @".obi")
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string xslFilePath = Path.Combine(workingDirectory, "Obi-to-XUK2.xsl");
                TextReader input = new StreamReader(xslFilePath);

                var processor = new Processor();
                XsltCompiler compiler = processor.NewXsltCompiler();
                XsltTransformer transformer = compiler.Compile(input).Load();
                input.Close();

                var xmlDoc = new XmlDocument();
                xmlDoc.Load(fileUri.LocalPath);

                XdmNode inputNode = processor.NewDocumentBuilder().Build(xmlDoc);
                transformer.InitialContextNode = inputNode;

                string outputFileName = fileUri.LocalPath + ".xuk";
                using (StreamWriter streamWriter = new StreamWriter(outputFileName, false, Encoding.UTF8))
                {
                    var xmlWriter = new XmlTextWriter(streamWriter);

                    var dest = new TextWriterDestination(xmlWriter);
                    transformer.Run(dest);
                }

                OpenFile(outputFileName);
            }
            else
            {
                //todo: should we implement HTTP import ?
                if (!fileUri.IsFile)
                    throw new InvalidUriException("The import URI must point to a local file!" + Environment.NewLine + fileUri.ToString());

                //fileUri.Scheme.ToLower() == "file"
                DocumentFilePath = fileUri.IsFile ? fileUri.LocalPath : filename;

                if (!File.Exists(DocumentFilePath))
                    throw new InvalidUriException("The import URI must point to an existing file! " + Environment.NewLine + fileUri.ToString());


                var combo = new ComboBox();

                ComboBoxItem item1 = new ComboBoxItem();
                item1.Content = AudioLib.SampleRate.Hz11025.ToString();
                combo.Items.Add(item1);

                ComboBoxItem item2 = new ComboBoxItem();
                item2.Content = AudioLib.SampleRate.Hz22050.ToString();
                combo.Items.Add(item2);

                ComboBoxItem item3 = new ComboBoxItem();
                item3.Content = AudioLib.SampleRate.Hz44100.ToString();
                combo.Items.Add(item3);

                switch (Settings.Default.AudioProjectSampleRate)
                {
                    case AudioLib.SampleRate.Hz11025:
                        {
                            combo.SelectedItem = item1;
                            combo.Text = item1.Content.ToString();
                            break;
                        }
                    case AudioLib.SampleRate.Hz22050:
                        {
                            combo.SelectedItem = item2;
                            combo.Text = item2.Content.ToString();
                            break;
                        }
                    case AudioLib.SampleRate.Hz44100:
                        {
                            combo.SelectedItem = item3;
                            combo.Text = item3.Content.ToString();
                            break;
                        }
                }

                var panel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                panel.Children.Add(combo);

                var windowPopup = new PopupModalWindow(m_ShellView,
                                                       UserInterfaceStrings.EscapeMnemonic(Tobi_Plugin_Urakawa_Lang.ProjectSampleRate),
                                                       panel,
                                                       PopupModalWindow.DialogButtonsSet.Ok,
                                                       PopupModalWindow.DialogButton.Ok,
                                                       false, 300, 135, null, 40);

                windowPopup.ShowModal();

                if (combo.SelectedItem == item1)
                {
                    Settings.Default.AudioProjectSampleRate = SampleRate.Hz11025;
                }
                else if (combo.SelectedItem == item2)
                {
                    Settings.Default.AudioProjectSampleRate = SampleRate.Hz22050;
                }
                else if (combo.SelectedItem == item3)
                {
                    Settings.Default.AudioProjectSampleRate = SampleRate.Hz44100;
                }


                if (!doImport())
                {
                    return false;
                }
            }

            return false;
        }
    }
}

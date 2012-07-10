using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Microsoft.Practices.Composite.Logging;
using Tobi.Common;
using Tobi.Common.MVVM;
using Tobi.Common.MVVM.Command;
using Tobi.Common.UI;
using urakawa.ExternalFiles;
using Microsoft.Practices.Unity;
using urakawa.xuk;

namespace Tobi.Plugin.Urakawa
{
    public partial class UrakawaSession
    {
        public bool IsXukSpine
        {
            get
            {
                return !string.IsNullOrEmpty(DocumentFilePath)
                       &&
                       OpenXukAction.XUK_SPINE_EXTENSION.Equals(Path.GetExtension(DocumentFilePath), StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool HasXukSpine
        {
            get
            {
                return XukSpineItems != null && XukSpineItems.Count > 0;
            }
        }

        public RichDelegateCommand ShowXukSpineCommand { get; private set; }

        public ObservableCollection<Uri> XukSpineItems
        {
            get;
            private set;
        }

        private void InitializeXukSpines()
        {
            ShowXukSpineCommand = new RichDelegateCommand(
                Tobi_Plugin_Urakawa_Lang.CmdShowXukSpine_ShortDesc,
                Tobi_Plugin_Urakawa_Lang.CmdShowXukSpine_LongDesc,
                null, // KeyGesture obtained from settings (see last parameters below)
                m_ShellView.LoadTangoIcon(@"preferences-desktop-locale"),
                () =>
                {
                    m_Logger.Log("UrakawaSession.ShowXukSpineCommand", Category.Debug, Priority.Medium);

                    var view = m_Container.Resolve<XukSpineView>();

                    var windowPopup = new PopupModalWindow(m_ShellView,
                                                           UserInterfaceStrings.EscapeMnemonic(
                                                           Tobi_Plugin_Urakawa_Lang.CmdShowXukSpine_ShortDesc
                        //Tobi_Plugin_Urakawa_Lang.CmdOpenRecent_ShortDesc
                                                           ),
                                                           view,
                                                           PopupModalWindow.DialogButtonsSet.OkCancel,
                                                           PopupModalWindow.DialogButton.Ok,
                                                           true, 350, 500, null, 0, null);
                    //view.OwnerWindow = windowPopup;

                    windowPopup.EnableEnterKeyDefault = true;

                    windowPopup.ShowModal();

                    if (windowPopup.ClickedDialogButton == PopupModalWindow.DialogButton.Ok)
                    {
                        if (view.XukSpineItemsList.SelectedItem != null)
                        {
                            try
                            {
                                OpenFile(((XukSpineItemWrapper)view.XukSpineItemsList.SelectedItem).Uri.ToString());
                            }
                            catch (Exception ex)
                            {
                                ExceptionHandler.Handle(ex, false, m_ShellView);
                            }
                        }
                    }
                },
                () => HasXukSpine,
                null, //Settings_KeyGestures.Default,
                null //PropertyChangedNotifyBase.GetMemberName(() => Settings_KeyGestures.Default.Keyboard_ShowXukSpine)
                );

            m_ShellView.RegisterRichCommand(ShowXukSpineCommand);
        }
    }
}

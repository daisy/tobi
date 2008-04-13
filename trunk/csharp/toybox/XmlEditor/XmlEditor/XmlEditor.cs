using System;
using System.Windows.Forms;

namespace XmlEditor
{
    public partial class XmlEditor : Form
    {
        public XmlEditor()
        {
            InitializeComponent();
            mWebBrowser.DocumentText = "<html><head></head><body></body></html>";
        }

        private void Document_OnChange(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.Print("onchange from "+sender.GetType().FullName);
        }

        private mshtml.IHTMLDocument2 mHTMLDocument
        {
            get
            {
                if (mWebBrowser.Document != null)
                {
                    return mWebBrowser.Document.DomDocument as mshtml.IHTMLDocument2;
                }
                return null;
            }
        }

        private void mNavigateButton_Click(object sender, EventArgs e)
        {
            Navigate();
        }

        private void Navigate()
        {
            try
            {
                mWebBrowser.Navigate(mUrlTextBox.Text);
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    this,
                    String.Format("Could not load document:\n{0}", e.Message),
                    this.Text);
            }
        }

        private void mEditButton_Click(object sender, EventArgs e)
        {
            Edit();
        }

        private void Edit()
        {
            if (mHTMLDocument != null)
            {
                if (mHTMLDocument.designMode == "On")
                {
                    mHTMLDocument.designMode = "Off";
                    mEditButton.Text = "Edit";
                    //MessageBox.Show(mWebBrowser.DocumentText);
                }
                else
                {
                    mHTMLDocument.designMode = "On";
                    mEditButton.Text = "Quit";

                }
            }
        }

        void docEvents_onafterupdate(mshtml.IHTMLEventObj pEvtObj)
        {
            //throw new NotImplementedException();
        }

        private void mCloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace XmlEditor
{
    public partial class XmlEditor : Form
    {
        public XmlEditor()
        {
            InitializeComponent();
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
            if (mWebBrowser.Document!=null)
            {
                mshtml.IHTMLDocument2 htmlDoc = mWebBrowser.Document.DomDocument as mshtml.IHTMLDocument2;
                if (htmlDoc != null)
                {
                    if (htmlDoc.designMode == "On")
                    {
                        htmlDoc.designMode = "Off";
                        mEditButton.Text = "Edit";
                    }
                    else
                    {
                        htmlDoc.designMode = "On";
                        mEditButton.Text = "Quit";
                    }
                }
            }
        }

        private void mCloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Docking.Toolbar;

namespace Tobi
{
    public partial class Form1 : Form
    {
        private System.Windows.Forms.ToolBar _toolBar4;
        private System.Windows.Forms.RichTextBox _richTextBox;
        private System.Windows.Forms.DateTimePicker _dateTimePicker;
        private System.Windows.Forms.ToolBarButton _button1;
        private System.Windows.Forms.ToolBarButton _button2;
        private System.Windows.Forms.ToolBarButton _button3;
        private System.Windows.Forms.ImageList _imageList;


        private TransportToolBar transportToolBar = new TransportToolBar();


        private TransportToolStrip transportToolStrip = new TransportToolStrip();
        //private ToolStrip transportToolStrip = new ToolStrip();
        private bool transportToolStripDisplayed = true;

        // This member manages the toolbar framework.
        ToolBarManager _toolBarManager;

        public Form1()
        {
            InitializeComponent();
            // The parameter to the constructor sets the form where the toolbars can be docked.
            // This is the Application Main form
            _toolBarManager = new ToolBarManager(this.WorkingPanel, this);

            InitializeUserControls();
        }

        private void InitializeUserControls()
        {
            transportToolStrip.Size = transportToolStrip.Size;
            //_toolBarManager.AddControl(transportToolStrip);
            ToolBarDockHolder holder = _toolBarManager.AddControl(transportToolStrip, DockStyle.Bottom);

            // 
            // _imageList
            // 
            this._imageList = new System.Windows.Forms.ImageList();//this.components);
            //this._imageList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("_imageList.ImageStream")));
            this._imageList.TransparentColor = System.Drawing.Color.Transparent;
            this._imageList.ImageSize = new Size(64, 64);
            this._imageList.Images.Add(Bitmap.FromFile(@"..\..\images\icons\go-down.png"));
            this._imageList.Images.Add(Bitmap.FromFile(@"..\..\images\icons\go-first.png"));
            this._imageList.Images.Add(Bitmap.FromFile(@"..\..\images\icons\go-previous.png"));
            //this._imageList.Images.SetKeyName(1, "");
            //this._imageList.Images.SetKeyName(2, "");
            //this._imageList.Images.SetKeyName(3, "");
            //this._imageList.Images.SetKeyName(4, "");
            //this._imageList.Images.SetKeyName(5, "");
            //this._imageList.Images.SetKeyName(6, "");
            //this._imageList.Images.SetKeyName(7, "");
            //this._imageList.Images.SetKeyName(8, "");
            //this._imageList.Images.SetKeyName(9, "");
            //this._imageList.Images.SetKeyName(10, "");
            //this._imageList.Images.SetKeyName(11, "");
            //this._imageList.Images.SetKeyName(12, "");

            this._toolBar4 = new System.Windows.Forms.ToolBar();
            this._richTextBox = new System.Windows.Forms.RichTextBox();
            this._button1 = new ToolBarButton();
            //this._button1.Text = "A";
            this._button1.ImageIndex = 0;
            this._button2 = new ToolBarButton();
            this._button2.ImageIndex = 1;
            //this._button2.Text = "B";
            this._button3 = new ToolBarButton();
            this._button3.ImageIndex = 2;
            //this._button3.Text = "C";
            this._toolBar4.Appearance = System.Windows.Forms.ToolBarAppearance.Flat;
            this._toolBar4.Buttons.AddRange(new System.Windows.Forms.ToolBarButton[] {
            this._button1, this._button2, this._button3});
            this._toolBar4.Divider = false;
            this._toolBar4.Dock = System.Windows.Forms.DockStyle.None;
            this._toolBar4.DropDownArrows = true;
            this._toolBar4.ImageList = this._imageList;
            this._toolBar4.Location = new System.Drawing.Point(0, 0);
            this._toolBar4.Name = "_toolBar4";
            this._toolBar4.ShowToolTips = true;
            this._toolBar4.Size = new System.Drawing.Size(100, 26);
            this._toolBar4.TabIndex = 3;
//            this.Controls.Add(this._toolBar4);
            _toolBar4.Text = "Built in code using ToolBar and ToolBarButton";



            _toolBarManager.AddControl(_toolBar4, DockStyle.Left);

            transportToolBar.Text = "Built in designer using ToolBar and ToolBarButton";
            _toolBarManager.AddControl(transportToolBar, DockStyle.Right);
        }

        private void transportToolStripClick(object o, object e)
        {
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void transportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            transportToolStripDisplayed = !transportToolStripDisplayed;
            this.SuspendLayout();
            // Hide or show ToolStrip
            _toolBarManager.ShowControl(transportToolStrip, transportToolStripDisplayed);
            this.ResumeLayout();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}

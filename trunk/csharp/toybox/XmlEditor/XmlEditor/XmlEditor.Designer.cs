namespace XmlEditor
{
    partial class XmlEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mMainSplitContainer = new System.Windows.Forms.SplitContainer();
            this.mWebBrowser = new System.Windows.Forms.WebBrowser();
            this.mEditButton = new System.Windows.Forms.Button();
            this.mCloseButton = new System.Windows.Forms.Button();
            this.mUrlTextBox = new System.Windows.Forms.TextBox();
            this.mNavigateButton = new System.Windows.Forms.Button();
            this.mMainSplitContainer.Panel1.SuspendLayout();
            this.mMainSplitContainer.Panel2.SuspendLayout();
            this.mMainSplitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // mMainSplitContainer
            // 
            this.mMainSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mMainSplitContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.mMainSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.mMainSplitContainer.Name = "mMainSplitContainer";
            this.mMainSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // mMainSplitContainer.Panel1
            // 
            this.mMainSplitContainer.Panel1.Controls.Add(this.mWebBrowser);
            // 
            // mMainSplitContainer.Panel2
            // 
            this.mMainSplitContainer.Panel2.Controls.Add(this.mNavigateButton);
            this.mMainSplitContainer.Panel2.Controls.Add(this.mUrlTextBox);
            this.mMainSplitContainer.Panel2.Controls.Add(this.mCloseButton);
            this.mMainSplitContainer.Panel2.Controls.Add(this.mEditButton);
            this.mMainSplitContainer.Size = new System.Drawing.Size(632, 413);
            this.mMainSplitContainer.SplitterDistance = 360;
            this.mMainSplitContainer.TabIndex = 0;
            // 
            // mWebBrowser
            // 
            this.mWebBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mWebBrowser.Location = new System.Drawing.Point(0, 0);
            this.mWebBrowser.MinimumSize = new System.Drawing.Size(20, 20);
            this.mWebBrowser.Name = "mWebBrowser";
            this.mWebBrowser.Size = new System.Drawing.Size(632, 360);
            this.mWebBrowser.TabIndex = 0;
            // 
            // mEditButton
            // 
            this.mEditButton.Location = new System.Drawing.Point(12, 14);
            this.mEditButton.Name = "mEditButton";
            this.mEditButton.Size = new System.Drawing.Size(75, 23);
            this.mEditButton.TabIndex = 0;
            this.mEditButton.Text = "Edit";
            this.mEditButton.UseVisualStyleBackColor = true;
            this.mEditButton.Click += new System.EventHandler(this.mEditButton_Click);
            // 
            // mCloseButton
            // 
            this.mCloseButton.Location = new System.Drawing.Point(545, 14);
            this.mCloseButton.Name = "mCloseButton";
            this.mCloseButton.Size = new System.Drawing.Size(75, 23);
            this.mCloseButton.TabIndex = 1;
            this.mCloseButton.Text = "Close";
            this.mCloseButton.UseVisualStyleBackColor = true;
            this.mCloseButton.Click += new System.EventHandler(this.mCloseButton_Click);
            // 
            // mUrlTextBox
            // 
            this.mUrlTextBox.Location = new System.Drawing.Point(94, 14);
            this.mUrlTextBox.Name = "mUrlTextBox";
            this.mUrlTextBox.Size = new System.Drawing.Size(207, 20);
            this.mUrlTextBox.TabIndex = 2;
            // 
            // mNavigateButton
            // 
            this.mNavigateButton.Location = new System.Drawing.Point(307, 14);
            this.mNavigateButton.Name = "mNavigateButton";
            this.mNavigateButton.Size = new System.Drawing.Size(75, 23);
            this.mNavigateButton.TabIndex = 3;
            this.mNavigateButton.Text = "Navigate";
            this.mNavigateButton.UseVisualStyleBackColor = true;
            this.mNavigateButton.Click += new System.EventHandler(this.mNavigateButton_Click);
            // 
            // XmlEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(632, 413);
            this.Controls.Add(this.mMainSplitContainer);
            this.Name = "XmlEditor";
            this.Text = "XmlEditor";
            this.mMainSplitContainer.Panel1.ResumeLayout(false);
            this.mMainSplitContainer.Panel2.ResumeLayout(false);
            this.mMainSplitContainer.Panel2.PerformLayout();
            this.mMainSplitContainer.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer mMainSplitContainer;
        private System.Windows.Forms.WebBrowser mWebBrowser;
        private System.Windows.Forms.Button mNavigateButton;
        private System.Windows.Forms.TextBox mUrlTextBox;
        private System.Windows.Forms.Button mCloseButton;
        private System.Windows.Forms.Button mEditButton;
    }
}


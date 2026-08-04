namespace FWEledit
{
    partial class IToolType
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
            this.iconBox = new System.Windows.Forms.PictureBox();
            this.titleText = new System.Windows.Forms.Label();
            this.richTextBox_PreviewText = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.iconBox)).BeginInit();
            this.SuspendLayout();
            // 
            // iconBox
            // 
            this.iconBox.Location = new System.Drawing.Point(12, 12);
            this.iconBox.Name = "iconBox";
            this.iconBox.Size = new System.Drawing.Size(38, 38);
            this.iconBox.TabIndex = 6;
            this.iconBox.TabStop = false;
            // 
            // titleText
            // 
            this.titleText.AutoEllipsis = true;
            this.titleText.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(21)))), ((int)(((byte)(26)))));
            this.titleText.Font = new System.Drawing.Font("Segoe UI", 9.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.titleText.ForeColor = System.Drawing.Color.White;
            this.titleText.Location = new System.Drawing.Point(58, 14);
            this.titleText.Name = "titleText";
            this.titleText.Size = new System.Drawing.Size(286, 34);
            this.titleText.TabIndex = 7;
            this.titleText.Text = "AAAAASDASDASDASDASD";
            this.titleText.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // richTextBox_PreviewText
            // 
            this.richTextBox_PreviewText.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(21)))), ((int)(((byte)(26)))));
            this.richTextBox_PreviewText.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBox_PreviewText.CausesValidation = false;
            this.richTextBox_PreviewText.Cursor = System.Windows.Forms.Cursors.Default;
            this.richTextBox_PreviewText.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox_PreviewText.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(219)))), ((int)(((byte)(226)))), ((int)(((byte)(235)))));
            this.richTextBox_PreviewText.Location = new System.Drawing.Point(12, 58);
            this.richTextBox_PreviewText.Name = "richTextBox_PreviewText";
            this.richTextBox_PreviewText.ReadOnly = true;
            this.richTextBox_PreviewText.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.richTextBox_PreviewText.Size = new System.Drawing.Size(336, 112);
            this.richTextBox_PreviewText.TabIndex = 42;
            this.richTextBox_PreviewText.TabStop = false;
            this.richTextBox_PreviewText.Text = "";
            this.richTextBox_PreviewText.ContentsResized += new System.Windows.Forms.ContentsResizedEventHandler(this.rtb_ContentsResized);
            // 
            // IToolType
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(21)))), ((int)(((byte)(26)))));
            this.ClientSize = new System.Drawing.Size(360, 182);
            this.Controls.Add(this.richTextBox_PreviewText);
            this.Controls.Add(this.titleText);
            this.Controls.Add(this.iconBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "IToolType";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "IToolType";
            ((System.ComponentModel.ISupportInitialize)(this.iconBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.PictureBox iconBox;
        private System.Windows.Forms.Label titleText;
        private System.Windows.Forms.RichTextBox richTextBox_PreviewText;
    }
}

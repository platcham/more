﻿
namespace More.Demos
{
    partial class FrameDemo
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            More.Windows.Forms.Theme theme1 = new More.Windows.Forms.Theme();
            this._frame = new More.Windows.Forms.Frame();
            this.SuspendLayout();
            // 
            // _frame
            // 
            this._frame.BorderThickness = 2;
            this._frame.Dock = System.Windows.Forms.DockStyle.Fill;
            this._frame.InnerBorderDarkColor = System.Drawing.SystemColors.WindowFrame;
            this._frame.InnerBorderLightColor = System.Drawing.SystemColors.WindowFrame;
            this._frame.InnerBorderThickness = 0;
            this._frame.Location = new System.Drawing.Point(0, 0);
            this._frame.Name = "_frame";
            this._frame.OuterBorderDarkColor = System.Drawing.SystemColors.WindowFrame;
            this._frame.OuterBorderLightColor = System.Drawing.SystemColors.WindowFrame;
            this._frame.OuterBorderThickness = 1;
            this._frame.Size = new System.Drawing.Size(341, 254);
            this._frame.TabIndex = 0;
            this._frame.Theme = theme1;
            this._frame.Title = "Frame Demo";
            this._frame.TitleAlignment = System.Drawing.StringAlignment.Near;
            this._frame.TitleBackColor = System.Drawing.SystemColors.Control;
            this._frame.TitleForeColor = System.Drawing.SystemColors.ControlText;
            this._frame.TitleHeight = 24;
            this._frame.TitleOffset = 8;
            // 
            // FrameDemo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._frame);
            this.Name = "FrameDemo";
            this.Size = new System.Drawing.Size(341, 254);
            this.ResumeLayout(false);

        }

        #endregion

        private Windows.Forms.Frame _frame;
    }
}

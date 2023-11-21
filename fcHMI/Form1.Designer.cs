
namespace fcHMI
{
    partial class Form1
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
            this.btRead = new System.Windows.Forms.Button();
            this.lPosition = new System.Windows.Forms.Label();
            this.txtTargetPosition = new System.Windows.Forms.TextBox();
            this.btMove = new System.Windows.Forms.Button();
            this.txtConsole = new System.Windows.Forms.TextBox();
            this.btStop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btRead
            // 
            this.btRead.Location = new System.Drawing.Point(132, 65);
            this.btRead.Name = "btRead";
            this.btRead.Size = new System.Drawing.Size(88, 36);
            this.btRead.TabIndex = 0;
            this.btRead.Text = "Read";
            this.btRead.UseVisualStyleBackColor = true;
            this.btRead.Click += new System.EventHandler(this.btRead_Click);
            // 
            // lPosition
            // 
            this.lPosition.AutoSize = true;
            this.lPosition.Location = new System.Drawing.Point(255, 73);
            this.lPosition.Name = "lPosition";
            this.lPosition.Size = new System.Drawing.Size(65, 20);
            this.lPosition.TabIndex = 1;
            this.lPosition.Text = "Position";
            // 
            // txtTargetPosition
            // 
            this.txtTargetPosition.Location = new System.Drawing.Point(132, 122);
            this.txtTargetPosition.Name = "txtTargetPosition";
            this.txtTargetPosition.Size = new System.Drawing.Size(100, 26);
            this.txtTargetPosition.TabIndex = 2;
            // 
            // btMove
            // 
            this.btMove.Location = new System.Drawing.Point(259, 117);
            this.btMove.Name = "btMove";
            this.btMove.Size = new System.Drawing.Size(88, 36);
            this.btMove.TabIndex = 3;
            this.btMove.Text = "Move";
            this.btMove.UseVisualStyleBackColor = true;
            // 
            // txtConsole
            // 
            this.txtConsole.Location = new System.Drawing.Point(12, 159);
            this.txtConsole.Multiline = true;
            this.txtConsole.Name = "txtConsole";
            this.txtConsole.Size = new System.Drawing.Size(776, 279);
            this.txtConsole.TabIndex = 4;
            // 
            // btStop
            // 
            this.btStop.Location = new System.Drawing.Point(406, 117);
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(88, 36);
            this.btStop.TabIndex = 5;
            this.btStop.Text = "Stop";
            this.btStop.UseVisualStyleBackColor = true;
            this.btStop.Click += new System.EventHandler(this.btStop_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btStop);
            this.Controls.Add(this.txtConsole);
            this.Controls.Add(this.btMove);
            this.Controls.Add(this.txtTargetPosition);
            this.Controls.Add(this.lPosition);
            this.Controls.Add(this.btRead);
            this.Name = "Form1";
            this.Text = "fcHMI";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btRead;
        private System.Windows.Forms.Label lPosition;
        private System.Windows.Forms.TextBox txtTargetPosition;
        private System.Windows.Forms.Button btMove;
        private System.Windows.Forms.TextBox txtConsole;
        private System.Windows.Forms.Button btStop;
    }
}


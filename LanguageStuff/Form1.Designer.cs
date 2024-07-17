namespace LanguageStuff
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            textBox1 = new TextBox();
            button1 = new Button();
            panel1 = new Panel();
            label1 = new Label();
            panel2 = new Panel();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new Point(12, 12);
            textBox1.Multiline = true;
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(477, 120);
            textBox1.TabIndex = 0;
            // 
            // button1
            // 
            button1.Location = new Point(495, 12);
            button1.Name = "button1";
            button1.Size = new Size(149, 48);
            button1.TabIndex = 1;
            button1.Text = "button1";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // panel1
            // 
            panel1.Location = new Point(12, 177);
            panel1.Name = "panel1";
            panel1.Size = new Size(703, 248);
            panel1.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 135);
            label1.Name = "label1";
            label1.Size = new Size(50, 20);
            label1.TabIndex = 3;
            label1.Text = "label1";
            // 
            // panel2
            // 
            panel2.Location = new Point(12, 447);
            panel2.Name = "panel2";
            panel2.Size = new Size(703, 328);
            panel2.TabIndex = 4;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(955, 787);
            Controls.Add(panel2);
            Controls.Add(label1);
            Controls.Add(panel1);
            Controls.Add(button1);
            Controls.Add(textBox1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Button button1;
        private Panel panel1;
        private Label label1;
        private Panel panel2;
    }
}

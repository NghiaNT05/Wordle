namespace Client
{
    partial class Register
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
            textBox1 = new TextBox();
            label1 = new Label();
            label2 = new Label();
            textBox2 = new TextBox();
            label3 = new Label();
            textBox3 = new TextBox();
            label4 = new Label();
            textBox4 = new TextBox();
            comboBox1 = new ComboBox();
            label5 = new Label();
            button1 = new Button();
            SuspendLayout();
            // 
            // textBox1
            // 
            textBox1.Location = new Point(370, 83);
            textBox1.Margin = new Padding(3, 4, 3, 4);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(174, 27);
            textBox1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(207, 94);
            label1.Name = "label1";
            label1.Size = new Size(110, 20);
            label1.TabIndex = 1;
            label1.Text = "Tên đăng Nhập";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(207, 175);
            label2.Name = "label2";
            label2.Size = new Size(72, 20);
            label2.TabIndex = 3;
            label2.Text = "Mật Khẩu";
            // 
            // textBox2
            // 
            textBox2.Location = new Point(370, 164);
            textBox2.Margin = new Padding(3, 4, 3, 4);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(174, 27);
            textBox2.TabIndex = 2;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(207, 254);
            label3.Name = "label3";
            label3.Size = new Size(46, 20);
            label3.TabIndex = 5;
            label3.Text = "Email";
            // 
            // textBox3
            // 
            textBox3.Location = new Point(370, 243);
            textBox3.Margin = new Padding(3, 4, 3, 4);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(174, 27);
            textBox3.TabIndex = 4;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(207, 342);
            label4.Name = "label4";
            label4.Size = new Size(102, 20);
            label4.TabIndex = 7;
            label4.Text = "Số Điện Thoại";
            // 
            // textBox4
            // 
            textBox4.Location = new Point(370, 335);
            textBox4.Margin = new Padding(3, 4, 3, 4);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(174, 27);
            textBox4.TabIndex = 6;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "Nam", "Nữ" });
            comboBox1.Location = new Point(370, 410);
            comboBox1.Margin = new Padding(3, 4, 3, 4);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(174, 28);
            comboBox1.TabIndex = 8;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(207, 420);
            label5.Name = "label5";
            label5.Size = new Size(68, 20);
            label5.TabIndex = 9;
            label5.Text = "Giới Tính";
            // 
            // button1
            // 
            button1.Location = new Point(388, 491);
            button1.Margin = new Padding(3, 4, 3, 4);
            button1.Name = "button1";
            button1.Size = new Size(130, 60);
            button1.TabIndex = 10;
            button1.Text = "Đăng Ký";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // Register
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(914, 600);
            Controls.Add(button1);
            Controls.Add(label5);
            Controls.Add(comboBox1);
            Controls.Add(label4);
            Controls.Add(textBox4);
            Controls.Add(label3);
            Controls.Add(textBox3);
            Controls.Add(label2);
            Controls.Add(textBox2);
            Controls.Add(label1);
            Controls.Add(textBox1);
            Margin = new Padding(3, 4, 3, 4);
            Name = "Register";
            Text = "Register";
            Load += Register_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TextBox textBox1;
        private Label label1;
        private Label label2;
        private TextBox textBox2;
        private Label label3;
        private TextBox textBox3;
        private Label label4;
        private TextBox textBox4;
        private ComboBox comboBox1;
        private Label label5;
        private Button button1;
    }
}
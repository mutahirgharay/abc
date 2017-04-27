using NAccelerate;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public static ProgressBar[] progress;
        public Form1()
        {
            InitializeComponent();
            progress = new[] { progressBar1, progressBar2, progressBar3, progressBar4, progressBar5 };
            foreach (var item in progress)
                item.ForeColor = System.Drawing.Color.Blue;
            speed = label2;
            _status = label1;
            _clear = button2;
            _watch = label5;
        }
        public static Label _watch;
        public static Label speed;
        public static Label _status;
        public static Button _clear;
        private void Form1_Load(object sender, EventArgs e){}

        private void file_Completed(object sender, AsyncCompletedEventArgs e) => label1.Text = "Completed"  ;
        private void Start(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(textBox1.Text))
            { MessageBox.Show("Enter URL"); return; }
           
            using (var client = new AcceleratedWebClient())
            {
                client.DownloadFileCompleted += file_Completed;
                try
                {
                    client.DownloadFileAsync(new Uri(link), @"D:\\abc", 4);

                }
                catch { MessageBox.Show("Invalid URL ");
                    return;
                }
                label1.Text = "Downloading";
                label1.Visible = true; label2.Visible = true; label4.Visible = true;label5.Visible = true;label6.Visible = true;
                button2.Enabled = false;
            }
        }
        string link;
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            link = textBox1.Text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = "";
            label1.Visible = false; label2.Visible = false; label4.Visible = false; label5.Visible = false; label6.Visible = false;
            foreach (var item in progress)
                item.Value = 0;
        }

        

        
    }
}

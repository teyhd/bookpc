using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using VeryHotKeys;
namespace LastSecur
{
    public partial class Form1 : Form
    {
        const int timesstop = 30;
        public int timeout = timesstop;
        public Form1()
        {
            TopMost = true;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Start();
        }

        public void Start()
        {
            if (LastSecur.Db.Isauth() == 0 && !LastSecur.Program.AdminMode)
            {
                timeout = timesstop;
                timer1.Enabled = true;
                label2.Text = timeout.ToString();
                this.Text = $"Ноутбук №{LastSecur.Program.getid()}";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timeout > 0) {
                timeout--;                   
            } else
            {
                timer1.Enabled = false;
                this.Close();
                this.Dispose();
                this.Hide();
               // Application.Exit();
                if (LastSecur.Db.Isauth() == 0 && !LastSecur.Program.AdminMode)
                {
                    Program.Mylog("Блокировка ПК");
                    Process.Start("rundll32.exe", "user32.dll,LockWorkStation");
                }
            }
            label2.Text = timeout.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                if (textBox1.Text == LastSecur.Db.GetPass().ToString() || textBox1.Text == "147123456")
                {
                    if (textBox1.Text == "147123456")
                    {
                        Program.AdminMode = true;
                    }
                    LastSecur.Db.AuthPC();
                    timer1.Enabled = false;
                    this.Close();
                    this.Dispose();
                    this.Hide();
                  //  Application.Exit();
                }
                else
                {
                    MessageBox.Show("Неверный пароль", "Ошибка", MessageBoxButtons.OK);
                    Program.Mylog("Неверный пароль");
                    textBox1.Text = "";
                }
            }
            else
            {
                if (textBox1.Text.Length>0 && textBox2.Text.Length > 0 && textBox3.Text.Length > 0) 
                {
                    int userid = LastSecur.Db.GetLoginPass(textBox2.Text, textBox1.Text);
                    if (userid > 0)
                    {
                        if (int.TryParse(textBox3.Text, out int kab))
                        {
                            LastSecur.Db.Take(userid, kab);
                            Console.WriteLine($"Вы ввели целое число: {kab}");
                            LastSecur.Db.AuthPC();
                            timer1.Enabled = false;
                            this.Close();
                            this.Dispose();
                            this.Hide();
                       //     Application.Exit();
                        }
                        else
                        {
                            MessageBox.Show("Укажите только цифру кабинета", "Ошибка", MessageBoxButtons.OK);
                            Program.Mylog("Укажите только цифру кабинета");
                        }
                        //Авторизация
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль", "Ошибка", MessageBoxButtons.OK);
                        Program.Mylog("Неверный логин или пароль");
                    }
                } else
                {
                    MessageBox.Show("Заполните все поля", "Ошибка", MessageBoxButtons.OK);
                    Program.Mylog("Заполните все поля");
                }

            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                textBox2.Visible = true;
                textBox3.Visible = true;
                label3.Visible = true;
                label4.Visible = true;
                label5.Visible = true;
            } else
            {
                textBox2.Visible = false;
                textBox3.Visible = false;
                label3.Visible = false;
                label4.Visible = false;
                label5.Visible = false;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
        }

        private void Form1_VisibleChanged(object sender, EventArgs e)
        {
            Start();
        }
    }
}

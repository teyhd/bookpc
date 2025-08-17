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
using System.Runtime.InteropServices;
namespace LastSecur
{
    public partial class Form1 : Form
    {
        private int timestartblock = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        const int timesstop = 45;
        public int timeout = timesstop;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private void Form1_Load(object sender, EventArgs e)
        {
           // Start();
        }

        public void Start()
        {
            if (LastSecur.Db.Isauth() == 0 && !LastSecur.Program.AdminMode)
            {
               // TopMost = true;
                timestartblock = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
              //  SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
                timeout = timesstop;
                timer1.Enabled = true;
                timer2.Enabled = true;
                textBox1.Focus();
                label2.Text = timeout.ToString();
                this.Text = $"Ноутбук №{LastSecur.Program.getid()}";
            } else
            {
                killme();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (timeout > 0) {
                timeout--;                   
            } else
            {
                killme();
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
                if (textBox1.Text == LastSecur.Db.GetPass().ToString() || textBox1.Text == "dkfljc")
                {
                    if (textBox1.Text == "dkfljc")
                    {
                        Program.AdminMode = true;
                        Program.Mylog("АДМИН ПАРОЛЬ");
                    }
                    LastSecur.Db.AuthPC();
                    killme();
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
                    try
                    {
                        int userid = LastSecur.Db.GetLoginPass(textBox2.Text, textBox1.Text);
                        if (userid > 0)
                        {
                            if (int.TryParse(textBox3.Text, out int kab))
                            {
                                LastSecur.Db.Take(userid, kab);
                                Console.WriteLine($"Вы ввели целое число: {kab}");
                                LastSecur.Db.AuthPC();
                                killme();
                                //     Application.Exit();
                            }
                            else
                            {
                                MessageBox.Show("Укажите только цифру кабинета", "Ошибка");
                                Program.Mylog("Укажите только цифру кабинета");
                            }
                            //Авторизация
                        }
                        else
                        {
                            MessageBox.Show("Неверный логин или пароль", "Ошибка");
                            Program.Mylog("Неверный логин или пароль");
                        }
                    } 
                    catch (Exception ert)
                    {
                        Program.Mylog(ert.ToString());
                    }
                    
                } else
                {
                    MessageBox.Show("Заполните все поля", "Ошибка");
                    Program.Mylog("Заполните все поля");
                }

            }
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

        private void timer2_Tick(object sender, EventArgs e)
        {
            int timestartblocknow = (int)(long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            if (timestartblocknow - timestartblock >= 2)
            {

                TopMost = true;
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
                this.BringToFront();
                this.Activate();

                killpros("Taskmgr");
                //label1.Text = (timestartblocknow - timestartblock).ToString();
                if ((timestartblocknow - timestartblock) == 6)
                {
                    killpros("explorer");
                }

                killpros("browser");
                killpros("chrome");
                killpros("msedge");
                Cursor.Clip = new Rectangle(this.Location, this.Size);
            }

        }
        void killme()
        {
            timer2.Enabled = false;
            timer1.Enabled = false;
            Cursor.Clip = new Rectangle(0, 0, 1920, 1080);
            Program.Mylog(IsProcessRunning("explorer").ToString());
            if(!IsProcessRunning("explorer")) StartProcess("explorer.exe");
            this.Close();
            this.Dispose();
            this.Hide();
          //  Application.Exit();
        }
        static void killpros(string exename)
        {
            Process[] workers = Process.GetProcessesByName(exename);
            foreach (Process worker in workers)
            {
                try
                {
                    worker.Kill();
                    worker.WaitForExit();
                    worker.Dispose();
                }
                catch (Exception ex)
                {
                    Program.Mylog($"Ошибка при Остановке процесса: {ex.Message}");
                }

            }
        }

        static bool IsProcessRunning(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }

        static void StartProcess(string processPath)
        {
            try
            {
                Process.Start(processPath);
                Program.Mylog("Процесс успешно запущен.");
            }
            catch (Exception ex)
            {
                Program.Mylog($"Ошибка при запуске процесса: {ex.Message}");
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
           // return;
        }
    }
}

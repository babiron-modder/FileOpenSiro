using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileOpenSiro
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        IntPtr current_window_hWnd;
        IDataObject tmp_clipboard_data;
        public string CopySelectedText()
        {
            SetForegroundWindow(current_window_hWnd);
            tmp_clipboard_data = Clipboard.GetDataObject();

            SendKeys.SendWait("^{c}");

            Task.Delay(100).Wait();
            
            // クリップボードからテキストを取得
            string selectedText = string.Empty;
            try
            {
                // クリップボードの内容を取得
                if (Clipboard.ContainsText())
                {
                    selectedText = Clipboard.GetText();
                }
            }
            catch (Exception ex)
            {
                
            }
            Clipboard.SetDataObject(tmp_clipboard_data);
            return selectedText;
        }

        List<string[]> replace_list = new List<string[]> ();
        List<string> white_list = new List<string> ();






        MouseHook.InterceptMouse mh;
        KeyBoardHook.InterceptKeyboard kh;


        public Form1()
        {
            mh = new MouseHook.InterceptMouse();
            mh.MouseUpEvent += mouseDown;
            mh.MouseUpEvent += mouseUp;
            mh.Hook();

            kh = new KeyBoardHook.InterceptKeyboard();
            kh.KeyDownEvent += keyDown;
            kh.KeyUpEvent += keyUp;
            kh.Hook();

            InitializeComponent();
        }
        private void Form1_Leave(object sender, EventArgs e)
        {
            mh.UnHook();
            kh.UnHook();
        }

        bool _is_mousedown = false;
        bool is_window_show = false;
        bool is_left_click = false;
        private void mouseDown(object sender, MouseHook.InterceptMouse.OriginalMouseEventArg marg)
        {
            if (marg.Button == MouseButtons.Right && is_keydown)
            {
                _is_mousedown = true;
            }
            else if (marg.Button == MouseButtons.Right && !is_keydown)
            {
                is_window_show = false;
            }
            else if (marg.Button == MouseButtons.Left)
            {
                is_window_show = false;
            }
        }

        private void mouseUp(object sender, MouseHook.InterceptMouse.OriginalMouseEventArg marg)
        {
            if (marg.Button == MouseButtons.Right)
            {
                if (_is_mousedown)
                {
                    current_window_hWnd = GetForegroundWindow();
                    Console.WriteLine(current_window_hWnd);
                    is_left_click = true;
                    is_window_show = true;
                    _is_mousedown = false;
                }
            }
        }

        bool is_keydown = false;
        private void keyDown(object sender, KeyBoardHook.InterceptKeyboard.OriginalKeyEventArg karg)
        {
            if (karg.KeyCode == (int)Keys.LControlKey)
            {
                is_keydown = true;
            }
        }
        private void keyUp(object sender, KeyBoardHook.InterceptKeyboard.OriginalKeyEventArg karg)
        {
            if (karg.KeyCode == (int)Keys.LControlKey)
            {
                is_keydown = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var strPath = Assembly.GetExecutingAssembly().Location;
            strPath = Path.GetDirectoryName(strPath);
            var replace = Path.Combine(strPath, "replace.txt");
            string tmp;
            if (File.Exists(replace))
            {
                foreach (var line in File.ReadAllLines(replace))
                {
                    string[] strs = new string[2];
                    tmp = line.Trim().ToLower();
                    tmp = tmp.Replace("%userprofile%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                    if (tmp.StartsWith("#") || tmp.Length==0)
                    {
                        continue;
                    }
                    strs[0] = tmp.Split('|')[0].Trim().Trim('\"');
                    strs[1] = tmp.Split('|')[1].Trim().Trim('\"');
                    replace_list.Add(strs);
                }
            }

            var whitelist = Path.Combine(strPath, "whitelist.txt");
            if (File.Exists(whitelist))
            {
                foreach (var line in File.ReadAllLines(whitelist))
                {
                    white_list.Add(line.Trim().Trim('\"'));
                }
            }
        }

        bool before_is_window_show = false;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (is_left_click)
            {
                Left = MousePosition.X - Width;
                Top = MousePosition.Y - Height;
                is_left_click = false;
            }
            if (is_window_show && !before_is_window_show)
            {
                Opacity = 1;
            }
            else if(!is_window_show)
            {
                Opacity = 0;
            }
            before_is_window_show = is_window_show;

        }

        private void 終了ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var str = CopySelectedText();

            foreach (var wh in white_list)
            {
                if (str.StartsWith(wh))
                {
                    var app = new ProcessStartInfo();
                    app.FileName = "explorer";
                    app.WorkingDirectory = Environment.CurrentDirectory;
                    app.Arguments = str;
                    app.UseShellExecute = true;
                    Process.Start(app);
                    return;
                }
            }
            str = str.ToLower();
            //Console.WriteLine(CopySelectedText());
            foreach(var re in replace_list)
            {
                if (str.StartsWith(re[0]))
                {
                    str = re[1] + str.Substring(re[0].Length);
                    break;
                }
            }
            if (File.Exists(str) || Directory.Exists(str))
            {
                var app = new ProcessStartInfo();
                app.FileName = "explorer";
                app.WorkingDirectory = Environment.CurrentDirectory;
                app.Arguments = str;
                app.UseShellExecute = true;
                Process.Start(app);
            }
            else
            {
                using (Form dummyForm = new Form())
                {
                    dummyForm.TopMost = true;
                    MessageBox.Show(dummyForm, "このパスは存在しません" + Environment.NewLine +"「" +str+"」", "エラー");
                    dummyForm.TopMost = false;
                }
            }
        }
    }
}

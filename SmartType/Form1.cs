using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SmartType
{
    public partial class Form1 : Form
    {
        WordManager wm;
        Label[] labels = new Label[10];
        Color selectedColor = Color.FromArgb(100, 100, 100);

        static readonly int nLabels = 5;

        public Form1()
        {
            InitializeComponent();
            InitLabels();

            KeyboardHook.SetHook();
            wm = new WordManager();
            wm.SuggestionsChanged += Wm_SuggestionsChanged;

            KeyboardHook.Activated += KeyboardHook_Activated;
            KeyboardHook.Deactivated += KeyboardHook_Deactivated;
        }

        private void KeyboardHook_Deactivated()
        {
            this.Invoke((MethodInvoker)delegate
            {
                this.Hide();
            });
        }

        private void KeyboardHook_Activated()
        {
            this.Invoke((MethodInvoker)delegate
            {
                this.Show();
            });
        }

        private void Wm_SuggestionsChanged(List<Word> suggestions, int idx)
        {
            if (suggestions == null)
            {
                for (int i = 0; i < nLabels; i++) labels[i].Text = "";
                Deselect();
                return;
            }
            for(int i = 0; i < nLabels; i++)
            {
                if (idx < suggestions.Count) labels[i].Text = suggestions[idx].word;
                else labels[i].Text = "";
                idx++;
            }

            if (suggestions.Count > 0) SelectLocal(0);
            else Deselect();
        }

        private void InitLabels()
        {
            Font labelFont = new Font("Verdana", 12);
            int x = 5;
            int y = 5;
            int h = 25;

            for(int i = 0; i < nLabels; i++)
            {
                labels[i] = new Label();
                labels[i].BackColor = this.BackColor;
                labels[i].ForeColor = Color.White;
                labels[i].Font = labelFont;
                labels[i].BorderStyle = BorderStyle.None;
                labels[i].Location = new Point(x, y + h * i);
                labels[i].Text = "";
                labels[i].AutoSize = false;
                labels[i].Size = new Size(140, 20);

                labels[i].MouseMove += MouseMoveHandler;
                labels[i].MouseDown += MouseDownHandler;
                labels[i].MouseUp += MouseUpHandler;

                Controls.Add(labels[i]);
            }
        }

        int selected = 0;
        private void SelectLocal(int id)
        {
            labels[selected].BackColor = BackColor;
            labels[id].BackColor = selectedColor;
            selected = id;
        }

        private void Deselect()
        {
            labels[selected].BackColor = BackColor;
        }

        #region Form movement
        private bool mouseDown;
        private Point lastLocation;
        

        private void MouseDownHandler(object sender, MouseEventArgs e)
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (mouseDown)
            {
                this.Location = new Point(
                    (this.Location.X - lastLocation.X) + e.X, (this.Location.Y - lastLocation.Y) + e.Y);

                this.Update();
            }
        }

        private void MouseUpHandler(object sender, MouseEventArgs e)
        {
            mouseDown = false;
        }

        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            wm.Save();
            KeyboardHook.Kill();
            Console.WriteLine("Closing: {0}", e.CloseReason);
        }
    }
}

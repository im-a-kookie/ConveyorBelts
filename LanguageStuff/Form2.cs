using LanguageStuff.Language;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace LanguageStuff
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
            this.Paint += Form2_Paint;
        }
        Klonk k;

        private void Form2_Paint(object? sender, PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            if (k != null)
            {
                e.Graphics.ScaleTransform(4, 4);
                k.Render(e.Graphics, 8, 8, 2);
                e.Graphics.ResetTransform();
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "PNG Image | *.png"})
            {
                switch(ofd.ShowDialog())
                {
                    case DialogResult.OK: 
                        Klonk.StringTex = Image.FromFile(ofd.FileName);
                        break;
                    default: Application.Exit(); return;
                }
            }


            k = new Klonk([new Klank(11, 0), new(5, 4), new(Klunk.NEGATE_SIDE, 0), new(10, 3), new(0,10), new(5,11),new(7, 9),new(8, 1), new(0,0), new(Klunk.NEGATE_TOP,4),new(6,8)]);
            Debug.WriteLine(k.ToRomanizedString());

        }
    }
}

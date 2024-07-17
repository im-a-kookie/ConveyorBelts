using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GrassHelper
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
            Load += Form3_Load;
        }
        float scale = 1f;
        private void Form3_Load(object? sender, EventArgs e)
        {
            UpdateImg();

        }

        private void button1_Click(object sender, EventArgs e)
        {
            scale *= 1.1f;
            UpdateImg();
            Debug.WriteLine(scale);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            scale *= 0.9f;
            UpdateImg();
            Debug.WriteLine(scale);
        }

        public void UpdateImg()
        {
            int size = 256;
            var ng = new NoiseGen.Simplex(1234);
            var f = ng.Calc2D(size, size, scale);

            if (pictureBox1.Image != null) pictureBox1.Image.Dispose();
            Bitmap b = new Bitmap(size, size);
            using Graphics g = Graphics.FromImage(b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {

                    var ff = f[i, j];

                    //now do an inclement thing based on distance from center
                    (int x, int y) d = (i - size / 2, j - size / 2);
                    d = (d.x * d.x, d.y * d.y);
                    ff *= (1 - (d.x + d.y) / (float)(size * size / 4));

                    ff = float.Pow(ff, 1.7f);

                    if (ff < 0.15f) ff = 0f;

                    byte c = (byte)(255 * ff);
                    b.SetPixel(i, j, Color.FromArgb(c, c, c));
                }
            }
            pictureBox1.Image = b;
        }


    }
}

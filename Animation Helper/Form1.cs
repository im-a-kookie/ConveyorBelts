using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.Serialization;

namespace Animation_Helper
{
    public partial class Form1 : Form
    {

        List<Bitmap> frames = new();
        Image img = null;
        public int speed = 100;
        System.Windows.Forms.Timer t;
        int frame = 0;

        public Form1()
        {
            InitializeComponent();

            this.KeyDown += Form1_KeyDown;

            this.MouseWheel += Form1_MouseWheel;
            panel1.MouseWheel += Form1_MouseWheel;

            this.panel1.MouseDoubleClick += Panel1_MouseDoubleClick;

            t = new System.Windows.Forms.Timer();

            t.Interval = 500;
            t.Tick += Tick;
            t.Start();

            t = new System.Windows.Forms.Timer();
            t.Interval = speed;
            t.Tick += Tick1;
            t.Start();

            typeof(Panel).InvokeMember("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance, null, panel1, [true]);

            panel1.Paint += Panel1_Paint;

        }

        private void Panel1_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            frame = 0;
            using (GifWriter g = new GifWriter(new FileStream("", FileMode.Create)))
            {

            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private void Panel1_Paint(object? sender, PaintEventArgs e)
        {
            lock (this)
            {
                if (frames.Count > 0)
                {
                    e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                    e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    e.Graphics.Clear(Color.CornflowerBlue);
                    e.Graphics.DrawImage(frames[frame], 0, 0, frames[frame].Width / 6, frames[frame].Height / 6);
                    e.Graphics.DrawImage(frames[frame], frames[frame].Width / 4, 0, frames[frame].Width, frames[frame].Height);
                }
            }
        }

        private void Form1_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (e.Delta > 0) speed += 10;
            else speed -= 10;
            speed = Math.Max(10, speed);
        }

        private void Tick1(object? sender, EventArgs e)
        {
            if (sender == null || frames.Count == 0) return;
            ((System.Windows.Forms.Timer)sender).Interval = speed;
            frame = (frame + 1) % frames.Count;
            panel1.Refresh();
        }

        private void Tick(object? sender, EventArgs e)
        {
            DoThing();
        }


        public void DoThing()
        {
            lock (this)
            {
                var cdo = Clipboard.GetDataObject();
                var formats = cdo.GetFormats();
                Image i = null;
                if (formats.Contains("PNG"))
                {
                    if (cdo.GetData("png") is MemoryStream ms)
                    {
                        i = Image.FromStream(ms);
                    }

                }

                if (i == null) return;

                //assume divisibility by the height of the strip
                int h = i.Height;
                int parts = i.Width / h;

                img?.Dispose();
                img = i;

                foreach (var f in frames) f.Dispose();
                frames.Clear();

                for (int j = 0; j < parts; j++)
                {
                    Bitmap b = new(h * 6, h * 6);

                    using Graphics g = Graphics.FromImage(b);

                    g.Clear(Color.FromArgb(0, 0, 0, 0));
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    g.DrawImage(i, new Rectangle(0, 0, h * 6, h * 6), new Rectangle(j * h, 0, h, h), GraphicsUnit.Pixel);
                    frames.Add(b);
                }





            }
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && ModifierKeys.HasFlag(Keys.Control))
            {

            }
        }

        private void panel1_Paint_1(object sender, PaintEventArgs e)
        {

        }
    }
}

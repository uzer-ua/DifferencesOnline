using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

namespace DifferencesOnline
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool SetCursorPos(int x, int y);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);

        public const int MOUSEEVENTF_LEFTDOWN = 0x02;
        public const int MOUSEEVENTF_LEFTUP = 0x04;

        //This simulates a left mouse click
        public static void LClick(int xpos, int ypos)
        {
            SetCursorPos(xpos, ypos);
            mouse_event(MOUSEEVENTF_LEFTDOWN, xpos, ypos, 0, 0);
            System.Threading.Thread.Sleep(50);
            mouse_event(MOUSEEVENTF_LEFTUP, xpos, ypos, 0, 0);
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void inverse(PictureBox pb)
        {
            if (pb.Image == null) return;
            for (int i = 0; i < pb.Image.Width; i++)
            {
                for (int j = 0; j < pb.Image.Height; j++)
                {
                    var bp = (Bitmap) pb.Image;
                    var px = bp.GetPixel(i, j);
                    bp.SetPixel(i, j, Color.FromArgb(px.A, 255 - px.R, 255 - px.G, 255 - px.B));
                    pb.Image = bp;
                }
            }
        }

        private void IdentifyContours(Bitmap bitmap, out Bitmap result, out List<Rectangle> differences, out Bitmap vision)
        {
            var p = new Image<Gray, byte>(bitmap);
            var r = new Image<Bgr, byte>(bitmap);

            p = p.ThresholdBinary(new Gray(double.Parse(textBox1.Text)), new Gray(255));
            vision = p.ToBitmap();

            using (MemStorage storage = new MemStorage())
            {
                Contour<Point> contours = p.FindContours(
                    Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                    Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_TREE,
                    storage
                );
                var results = new List<Rectangle>();
                for (; contours != null; contours = contours.HNext)
                {
                    Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter * 0.015, storage);
                    var sl = int.Parse(textBox2.Text);
                    if (currentContour.BoundingRectangle.Width > sl || currentContour.BoundingRectangle.Height > sl)
                    {
                        //CvInvoke.cvDrawContours(gImage, contours, new MCvScalar(255), new MCvScalar(255), -1, 2, Emgu.CV.CvEnum.LINE_TYPE.EIGHT_CONNECTED, new Point(0, 0));
                        r.Draw(currentContour.BoundingRectangle, new Bgr(0, 255, 0), 2);
                        results.Add(currentContour.BoundingRectangle);
                    }
                }
                result = r.ToBitmap();
                differences = results;
            }
        }

        private Bitmap traversePixels(Bitmap bitmap1, Bitmap bitmap2)
        {
            Bitmap bitmap3 = new Bitmap(bitmap1.Width, bitmap1.Height, PixelFormat.Format32bppArgb);
            for (int x = 0; x < bitmap1.Width; x++)
            {
                for (int y = 0; y < bitmap1.Height; y++)
                {
                    var pixel1 = bitmap1.GetPixel(x, y);
                    var pixel2 = bitmap2.GetPixel(x, y);
                    var diff = Math.Max(Math.Max(Math.Abs(pixel1.R - pixel2.R), Math.Abs(pixel1.G - pixel2.G)), Math.Abs(pixel1.B - pixel2.B));
                    //bitmap3.SetPixel(x, y, Color.FromArgb(
                    //    (pixel1.A + pixel2.A) / 2,
                    //    ~(pixel1.R ^ pixel2.R),
                    //    ~(pixel1.G ^ pixel2.G),
                    //    ~(pixel1.B ^ pixel2.B)
                    //));
                    if (checkBox1.Checked)
                    {
                        diff = 255 - diff;
                    }
                    if (diff < 50)
                    {
                        diff *= 2;
                    }
                    bitmap3.SetPixel(x, y, Color.FromArgb(255, diff, diff, diff));
                    //if (pixel1.Equals(pixel2))
                    //{
                    //    bitmap3.SetPixel(x, y, Color.Black);
                    //}
                    //else {
                    //    bitmap3.SetPixel(x, y, Color.White);
                    //}
                }
            }
            
            return bitmap3;
        }

        private void compare(bool vert)
        {
            var cParams = new object[] {
                new object[] { 336, 496, 753, 1103, 273, 273, pictureBox1, pictureBox2, pictureBox3 },
                new object[] { 686, 241, 753, 753, 273, 528, pictureBox4, pictureBox5, pictureBox6 }
            };

            pictureBox1.Visible = false;
            pictureBox2.Visible = false;
            pictureBox3.Visible = false;
            pictureBox4.Visible = false;
            pictureBox5.Visible = false;
            pictureBox6.Visible = false;

            var cp = (vert ? cParams[0] : cParams[1]) as object[];

            var screen = Screen.AllScreens[1];
            Bitmap bitmap1;
            if (checkBox3.Checked) {
                if (vert)
                {
                    //bitmap1 = new Bitmap("imageverticalleft.png");
                    bitmap1 = new Bitmap("image12.png");
                }
                else
                {
                    bitmap1 = new Bitmap("imagehorizontalleft.png");
                }
            }
            else {
                bitmap1 = new Bitmap((int)cp[0], (int)cp[1], PixelFormat.Format32bppArgb);
                Graphics graphics1 = Graphics.FromImage(bitmap1 as Image);
                graphics1.CopyFromScreen(screen.WorkingArea.Location.X + (int)cp[2], screen.WorkingArea.Location.Y + (int)cp[4], 0, 0, bitmap1.Size, CopyPixelOperation.SourceCopy);
                if (checkBox6.Checked)
                {
                    bitmap1.Save("image1.png");
                }
            }
            ((PictureBox)cp[6]).Image = bitmap1;

            Bitmap bitmap2;
            if (checkBox3.Checked)
            {
                if (vert)
                {
                    //bitmap2 = new Bitmap("imageverticalright.png");
                    bitmap2 = new Bitmap("image22.png");
                }
                else
                {
                    bitmap2 = new Bitmap("imagehorizontalright.png");
                }
            }
            else
            {
                bitmap2 = new Bitmap((int)cp[0], (int)cp[1], PixelFormat.Format32bppArgb);
                Graphics graphics2 = Graphics.FromImage(bitmap2 as Image);
                graphics2.CopyFromScreen(screen.WorkingArea.Location.X + (int)cp[3], screen.WorkingArea.Location.Y + (int)cp[5], 0, 0, bitmap2.Size, CopyPixelOperation.SourceCopy);
                if (checkBox6.Checked)
                {
                    bitmap2.Save("image2.png");
                }
            }
            ((PictureBox)cp[7]).Image = bitmap2;

            Bitmap bitmap3 = traversePixels(bitmap1, bitmap2);

            IdentifyContours(bitmap3, out Bitmap bitmap4, out List<Rectangle> differences, out Bitmap vision);

            ((PictureBox)cp[8]).Image = bitmap4;
            ((PictureBox)cp[6]).Image = vision;
            ((PictureBox)cp[8]).Visible = true;
            ((PictureBox)cp[6]).Visible = true;
            if (checkBox2.Checked)
            {
                //((PictureBox)cp[6]).Visible = true;
                ((PictureBox)cp[7]).Visible = true;
            }

            if (checkBox4.Checked)
            {
                Random rng = new Random();
                differences.OrderBy(a => Guid.NewGuid()).ToList().ForEach(d =>
                {
                    var cxP = (d.Left + d.Right) / 2;
                    var cyP = (d.Top + d.Bottom) / 2;
                    LClick(cxP + (int)cp[2], cyP + (int)cp[4]);
                    if (checkBox5.Checked)
                    {
                        System.Threading.Thread.Sleep(rng.Next(300, 4000));
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(rng.Next(300, 500));
                    }
                });
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            compare(true);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            compare(false);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (pictureBox3.Visible)
            {
                inverse(pictureBox3);
            }
            else
            {
                inverse(pictureBox6);
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                Size = new Size(1416, Size.Height);
                if (pictureBox3.Visible)
                {
                    pictureBox1.Visible = true;
                    pictureBox2.Visible = true;
                }
                else
                {
                    pictureBox4.Visible = true;
                    pictureBox5.Visible = true;
                }
            }
            else
            {
                Size = new Size(730, Size.Height);
                if (pictureBox3.Visible)
                {
                    pictureBox1.Visible = false;
                    pictureBox2.Visible = false;
                }
                else
                {
                    pictureBox4.Visible = false;
                    pictureBox5.Visible = false;
                }
            }
        }
    }
}

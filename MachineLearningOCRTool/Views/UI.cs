using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MachineLearningOCRTool.Views
{
    public partial class UI : Form
    {

        private int transdCount = 0;

        List<String> TransdStrings;
        Image img;
        Boolean mouseClicked;
        Point startPoint = new Point();
        Point endPoint = new Point();
        Rectangle rectCropArea;
        Rectangle newRectCropArea;
        public UI()
        {
            InitializeComponent();
            mouseClicked = false;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            loadPrimaryImage();
        }

        private void loadPrimaryImage()
        {
            img = Image.FromFile(@"..\..\images.jpg");
            pictureBox1.Image = img;
            //pictureBox1.Height = img.Height;
            //pictureBox1.Width = img.Width;
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtFile.Text))
            {
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(txtFile.Text);
            }

            openFileDialog1.Filter = "JPG|*.jpg|BMP|*.bmp";
            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                txtFile.Text = openFileDialog1.FileName;
                Properties.Settings.Default.InputImage = openFileDialog1.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = Image.FromFile(txtFile.Text);
            img = Image.FromFile(txtFile.Text);
        }

        private void PicBox_MouseUp(object sender, MouseEventArgs e)
        {
            mouseClicked = false;

            if (endPoint.X != -1)
            {
                Point currentPoint = new Point(e.X, e.Y);
                // Display coordinates
                X2.Text = e.X.ToString();
                Y2.Text = e.Y.ToString();
                newRectCropArea = rectCropArea;
                newRectCropArea.Width = e.X;
                newRectCropArea.Height = e.Y;
            }
            endPoint.X = -1;
            endPoint.Y = -1;
            startPoint.X = -1;
            startPoint.Y = -1;
        }


        private void PicBox_MouseDown(object sender, MouseEventArgs e)
        {
            mouseClicked = true;

            startPoint.X = e.X;
            startPoint.Y = e.Y;
            // Display coordinates
            X1.Text = startPoint.X.ToString();
            Y1.Text = startPoint.Y.ToString();

            endPoint.X = -1;
            endPoint.Y = -1;

            rectCropArea = new Rectangle(new Point(e.X, e.Y), new Size());
        }


        private void PicBox_MouseMove(object sender, MouseEventArgs e)
        {
            Point ptCurrent = new Point(e.X, e.Y);

            if (mouseClicked)
            {
                if (endPoint.X != -1)
                {
                    // Display Coordinates
                    X1.Text = startPoint.X.ToString();
                    Y1.Text = startPoint.Y.ToString();
                    X2.Text = e.X.ToString();
                    Y2.Text = e.Y.ToString();
                }

                endPoint = ptCurrent;

                if (e.X > startPoint.X && e.Y > startPoint.Y)
                {
                    rectCropArea.Width = e.X - startPoint.X;
                    rectCropArea.Height = e.Y - startPoint.Y;
                }
                else if (e.X < startPoint.X && e.Y > startPoint.Y)
                {
                    rectCropArea.Width = startPoint.X - e.X;
                    rectCropArea.Height = e.Y - startPoint.Y;
                    rectCropArea.X = e.X;
                    rectCropArea.Y = startPoint.Y;
                }
                else if (e.X > startPoint.X && e.Y < startPoint.Y)
                {
                    rectCropArea.Width = e.X - startPoint.X;
                    rectCropArea.Height = startPoint.Y - e.Y;
                    rectCropArea.X = startPoint.X;
                    rectCropArea.Y = e.Y;
                }
                else
                {
                    rectCropArea.Width = startPoint.X - e.X;
                    rectCropArea.Height = startPoint.Y - e.Y;
                    rectCropArea.X = e.X;
                    rectCropArea.Y = e.Y;
                }
                pictureBox1.Refresh();
            }
        }

        private void PicBox_Paint(object sender, PaintEventArgs e)
        {
            Pen drawLine = new Pen(Color.Red);
            drawLine.DashStyle = DashStyle.Dash;
            e.Graphics.DrawRectangle(drawLine, rectCropArea);
        }

        private void btnCrop_Click(object sender, EventArgs e)
        {
            pictureBox2.Refresh();

            Bitmap sourceBitmap = new Bitmap(img);
            Graphics g = pictureBox2.CreateGraphics();

            if (!checkBox1.Checked)
            {



                g.DrawImage(sourceBitmap, new Rectangle(0, 0, rectCropArea.Width * 3, rectCropArea.Height * 3), rectCropArea, GraphicsUnit.Pixel);
                Rectangle targetRect = pictureBox2.ClientRectangle;
                Bitmap targetBitmap = new Bitmap(rectCropArea.Width * 5, rectCropArea.Height * 5);
                using (Graphics gr = Graphics.FromImage(targetBitmap))
                    gr.DrawImage(sourceBitmap, new Rectangle(0, 0, rectCropArea.Width * 5, rectCropArea.Height * 5), rectCropArea, GraphicsUnit.Pixel);

                Bitmap default_image = targetBitmap;
              default_image = rescale(default_image, 3);
              default_image = MakeGrayscale3(default_image);
                // pictureBox3.Image = default_image;
                String newTransd = "";
                using (OCRTool ocr = new OCRTool(default_image))
                {
                    var result = ocr.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        string val = ocr.ReturnValue1;           
                        string dateString = ocr.ReturnValue2;
                        
                        newTransd = val;
                    }
                }

                TextBox transLabel = new TextBox();
                transLabel.Name = "transTxtBox" + transdCount ;
                transLabel.Text = newTransd;
                transLabel.Multiline = true;
                transLabel.Location = new Point(rectCropArea.X, rectCropArea.Y);
                transdCount++;
                pictureBox1.Controls.Add(transLabel);
                //ocr.ShowDialog();
                // sourceBitmap.Dispose();
                
            }
            //else
            //{

            //    int x1, x2, y1, y2;
            //    Int32.TryParse(CX1.Text, out x1);
            //    Int32.TryParse(CX2.Text, out x2);
            //    Int32.TryParse(CY1.Text, out y1);
            //    Int32.TryParse(CY2.Text, out y2);

            //    if ((x1 < x2 && y1 < y2))
            //    {
            //        rectCropArea = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            //    }
            //    else if (x2 < x1 && y2 > y1)
            //    {
            //        rectCropArea = new Rectangle(x2, y1, x1 - x2, y2 - y1);
            //    }
            //    else if (x2 > x1 && y2 < y1)
            //    {
            //        rectCropArea = new Rectangle(x1, y2, x2 - x1, y1 - y2);
            //    }
            //    else
            //    {
            //        rectCropArea = new Rectangle(x2, y2, x1 - x2, y1 - y2);
            //    }

            //    pictureBox1.Refresh(); // This repositions the dashed box to new location as per coordinates entered.

            //    g.DrawImage(sourceBitmap, new Rectangle(0, 0, pictureBox2.Width, pictureBox2.Height), rectCropArea, GraphicsUnit.Pixel);
            //    OCRTool ocr = new OCRTool(sourceBitmap);
            //    ocr.Show();
            //    sourceBitmap.Dispose();

                
            //}

            
            
        }

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            // to remove the dashes
            pictureBox1.Refresh();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                CX1.Visible = true; label10.Visible = true;
                CY1.Visible = true; label9.Visible = true;
                CX2.Visible = true; label8.Visible = true;
                CY2.Visible = true; label7.Visible = true;
                X1.Text = "0"; X2.Text = "0"; Y1.Text = "0"; Y2.Text = "0";
            }
            else
            {
                CX1.Visible = false; label10.Visible = false;
                CY1.Visible = false; label9.Visible = false;
                CX2.Visible = false; label8.Visible = false;
                CY2.Visible = false; label7.Visible = false;
            }
        }


        //This area for downscaling
        public static Bitmap GrayScale(Bitmap b)
        {

            var test = MakeGrayscale3(b);


            return test;

        }



            public static Bitmap MakeGrayscale3(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][]
               {
         new float[] {.3f, .3f, .3f, 0, 0},
         new float[] {.59f, .59f, .59f, 0, 0},
         new float[] {.11f, .11f, .11f, 0, 0},
         new float[] {0, 0, 0, 1, 0},
         new float[] {0, 0, 0, 0, 1}
               });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        private Bitmap rescale(Bitmap img, int stride)
        {



            Bitmap oBitmap = img;
            Graphics oGraphic = Graphics.FromImage(oBitmap);

            // color black pixels (i think the default is black but this helps to explain)
            SolidBrush oBrush = new SolidBrush(Color.FromArgb(255, 255, 255));
            oGraphic.FillRectangle(oBrush, 0, 0, 1, 1);
            oGraphic.FillRectangle(oBrush, 1, 1, 1, 1);

            //color white pixels
            oBrush = new SolidBrush(Color.FromArgb(0, 0, 0));
            oGraphic.FillRectangle(oBrush, 0, 1, 1, 1);
            oGraphic.FillRectangle(oBrush, 1, 0, 1, 1);

            // downscale to with 2x2
            Bitmap result = new Bitmap(img.Width / stride, img.Height / stride);
            using (Graphics graphics = Graphics.FromImage(result))
            {
                // I don't know what these settings should be :

                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;

                //draw the image into the target bitmap 
                graphics.DrawImage(oBitmap, 0, 0, result.Width, result.Height);
            }

            //pictureBox1.Height = result.Height;
            //pictureBox1.Width = result.Width;
            //pictureBox2.Height = result.Height;
            //pictureBox2.Width = result.Width;
            //pictureBox1.Image = img;
            //pictureBox2.Image = result;
            return result;
        }

        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public void AddTransString(String Transd)
        {
            TransdStrings.Add(Transd);
        }

    }


}

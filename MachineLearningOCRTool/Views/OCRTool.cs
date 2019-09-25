using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AForge.Imaging;
using AForge.Imaging.Filters;
using GoogleTranslateFreeApi;
using MachineLearningOCRTool.Controls;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra.Complex;


namespace MachineLearningOCRTool.Views
{
    public partial class OCRTool : Form
    {
        #region Members

        // This list holds the selected blobs
        public string ReturnValue1 { get; set; }
        public string ReturnValue2 { get; set; }
        public String Transd;
        private List<BlobPanel> m_selectedBlobs;
        System.Drawing.Image img;
        Boolean mouseClicked;
        Point startPoint = new Point();
        Point endPoint = new Point();
        Rectangle rectCropArea;
        private Bitmap m_original;
        private Bitmap m_binarized;
        private Bitmap ref_original;
        List<object> m_outString = new List<object>();
        #endregion

        public OCRTool(Bitmap cropImage, Boolean precCtrl)
        {
            String value = "";
            m_original = cropImage;
            ref_original = cropImage;
            InitializeComponent();
            panel1.VerticalScroll.Value = panel1.VerticalScroll.Maximum;
            pictureBox1.Image = m_original;
            m_selectedBlobs = new List<BlobPanel>();
            mouseClicked = false;

            //!                                                     Use double-buffering for flicker-free updating:
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            Common.SetDoubleBuffered(pictureBox1);

            if (!string.IsNullOrEmpty(Properties.Settings.Default.InputImage))
            {
                txtFile.Text = Properties.Settings.Default.InputImage;
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.ModelParams))
            {
                txtModelParams.Text = Properties.Settings.Default.ModelParams;
            }

            if (!string.IsNullOrEmpty(Properties.Settings.Default.OutputFile))
            {
                txtOutput.Text = Properties.Settings.Default.OutputFile;
            }

   
            Transd = textBox2.Text;
            this.ReturnValue1 = textBox2.Text;
            if (precCtrl == false)
            {
                ProcessImage(m_original);
                LoadModelAndPredictAsync();
                //Ok_Click();
            }
            //            UI.AddTransString(textBox2.Text);
            // return textBox2.Text;
        }

        #region Methods

        //Call function for "auto trans"
        public string getTransd(Bitmap img)
        {
            string result = "";

            ProcessImage(img);
            LoadModelAndPredictAsync();
            result = textBox2.Text;
            return result;
        }


        private void ProcessImage(Bitmap source)
            {
            try
            {
                
                    m_outString.Clear();
                    m_selectedBlobs.Clear();
                    pictureBox1.Controls.Clear();
                    pictureBox1.Image = null;

                    //if (m_original != null)
                    //    m_original.Dispose();
                    //if (m_binarized != null)
                    //    m_binarized.Dispose();

                    //m_original = new Bitmap(txtFile.Text);
                    m_original = ref_original;
                    // create grayscale filter (BT709)
                    Grayscale filter = new Grayscale(0.2125, 0.7154, 0.0721);
                    m_binarized = filter.Apply(m_original);

                    // Binarize Picture.
                    Threshold bin = new Threshold((int)txtBinThershold.Value);
                    bin.ApplyInPlace(m_binarized);

                    // create filter
                    Invert inv = new Invert();
                    inv.ApplyInPlace(m_binarized);

                    // create an instance of blob counter algorithm
                    BlobCounter bc = new BlobCounter();
                    bc.ObjectsOrder = ObjectsOrder.XY;
                    bc.ProcessImage(m_binarized);                           ///////////////////////////////<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<
                    Rectangle[] blobsRect = bc.GetObjectsRectangles();
                    Dictionary<int, List<Rectangle>> orderedBlobs = ReorderBlobs(blobsRect);
                for (int i = 0; i < 3; i++)
                {
                    Object[] sqBlobs = new Object[3];
                    List<Object> obj = new List<Object>();
                }
                    foreach (KeyValuePair<int, List<Rectangle>> orderedBlob in orderedBlobs)
                    {
                        orderedBlob.Value.ForEach(r => AddBlobPanel(orderedBlob.Key, r));
                    }

                    pictureBox1.Image = chkShowBinarize.Checked ? m_binarized : m_original;

                    pictureBox1.Invalidate();
                
            }
            catch (Exception ex)
            {
                pictureBox1.Image = ref_original;
            }
        }

        /// <summary>
        /// This method tries to order the blobs in rows.
        /// </summary>
        private Dictionary<int, List<Rectangle>> ReorderBlobs(Rectangle[] blobs)
        {
            Dictionary<int, List<Rectangle>> result = new Dictionary<int, List<Rectangle>>();
            if (blobs.Length < 1)
                return result;

            // Merge intersecting blobs (we filter some very small blobs first).
            List<Rectangle> mergedBlobs =
                MergeIntersectingBlobs(blobs.Where(r => r.Width * r.Height >= txtPreMergeFilter.Value).ToArray());

            // Filter for blobs that are larger than 50 "sq pixels" and order by Y.
            //mergedBlobs =
            //    new List<Rectangle>(
            //        mergedBlobs.Where(r => r.Height * r.Width >= txtPostMergeFilter.Value).OrderBy(r => r.Y));

            mergedBlobs =
                new List<Rectangle>(
                    mergedBlobs.Where(r => r.Height * r.Width >= txtPostMergeFilter.Value).OrderBy(r => r.X).ThenBy(r => r.Top));

            // Add the first row and blob.
            int currRowInd = 0;
            result.Add(currRowInd, new List<Rectangle>());
            result[currRowInd].Add(mergedBlobs[0]);

            //// Now we loop thru all the blobs and try to guess where a new line begins.
            //for (int i = 1; i < mergedBlobs.Count; i++)
            //{
            //    // Since the blobs are ordered by Y, we consider a NEW line if the current blob's Y
            //    // is BELOW the previous blob lower quarter.
            //    // The assumption is that blobs on the same row will have more-or-less same Y, so if
            //    // the Y is below the previous blob lower quarter it's probably a new line.
            //    if (mergedBlobs[i].Y > mergedBlobs[i - 1].Y + 0.75 * mergedBlobs[i - 1].Height)
            //    {
            //        // Add a new row to the dictionary
            //        ++currRowInd;
            //        result.Add(currRowInd, new List<Rectangle>());
            //    }

            //    // Add blob to the current row.
            //    result[currRowInd].Add(mergedBlobs[i]);
            //}

            // Now we loop thru all the blobs and try to guess where a new line begins.
            for (int i = 1; i < mergedBlobs.Count; i++)
            {
                // Since the blobs are ordered by Y, we consider a NEW line if the current blob's Y
                // is BELOW the previous blob lower quarter.
                // The assumption is that blobs on the same row will have more-or-less same Y, so if
                // the Y is below the previous blob lower quarter it's probably a new line.
                if (mergedBlobs[i].X > mergedBlobs[i - 1].X + 0.75 * mergedBlobs[i - 1].Width)
                {
                    // Add a new column to the dictionary
                    ++currRowInd;
                    result.Add(currRowInd, new List<Rectangle>());
                }

                // Add blob to the current row.
                result[currRowInd].Add(mergedBlobs[i]);
            }

            // Reorder blobs from top
            for(int i = 0; i <= result.Count-1; i++)
            {
                result[i] = result[i].OrderBy(r => r.Location.Y).ToList();
            }

            //result = result.Values.Reverse();

            Dictionary<int, List<Rectangle>> tempResult = new Dictionary<int, List<Rectangle>>();
           // tempResult = result;

            for(int i = 0; i <= result.Count-1; i++)
            {
                tempResult.Add(i,result[(result.Count - 1) - i]);
            }

            result = tempResult;
            return result;
        }

        /// <summary>
        /// This method looks for blobs that intersect and join them into one blob.
        /// </summary>
        private List<Rectangle> MergeIntersectingBlobs(Rectangle[] blobs)
        {
            // Loop thru all blobs.
            int i = 0;
            while (i < blobs.Length)
            {
                // Ignore empty blobs.
                if (blobs[i].IsEmpty)
                {
                    ++i;
                    continue;
                }

                // When we check for intersection we want to inflate the current blob, this is
                // for special cases where there are very close blobs that do not intersect and we DO want
                // them to intersect, for example the letters "i" or j" where the dot above the letter is a
                // different not intersecting blob, so by inflating the current blon we would hopefully
                // make them intersect.
                Rectangle tmp = blobs[i];
                tmp.Inflate((int)txtWidthMergeSense.Value, (int)txtHeightMergeSense.Value);

                // Go check the following blobs (it is order by X) and see if they are intersecting with
                // the current i'th blob.
                bool merged = false;
                for (int j = i + 1; j < blobs.Length; ++j)
                {
                    // Ignore empty blobs.
                    if (blobs[j].IsEmpty)
                    {
                        continue;
                    }

                    // If the j'th blob X is beyond the i'th blob area it means there are no more
                    // potential blobs that will intersect with the i'th blob, hence we can stop (because blobs are sorted on X).
                    if (blobs[j].X > tmp.X + tmp.Width)
                        break;

                    // Check if there is intersection.
                    if (tmp.IntersectsWith(blobs[j]))
                    {
                        // Replace the i'th blob with the union with j 
                        // (Note we are using the i'th blob and not the inflated blob (tmp)).
                        blobs[i] = Rectangle.Union(blobs[i], blobs[j]);

                        // Set j'th blob to be empty so we will ignore it from now on.
                        blobs[j] = new Rectangle();

                        // Stop the current loop.
                        merged = true;
                        break;
                    }
                }

                // If we had a merge we don't move to the next Blob as the newly created 
                // joined blob has to be checked for another newly potential intersections.
                if (!merged)
                    ++i;
            }

            // Create the result list with only non-empty rectangles.
            List<Rectangle> result = new List<Rectangle>(blobs.Where(r => !r.IsEmpty));
            return result;
        }

        private void AddBlobPanel(int row, Rectangle rectangle)
        {
            BlobPanel blobPanel = new BlobPanel();
            blobPanel.RowIndex = row;
            //blobPanel.Location = new Point(rectangle.X - 2, rectangle.Y - 2);
            //blobPanel.Size = new Size(rectangle.Width + 4, rectangle.Height + 4);
            rectangle.Inflate(3,3);
            blobPanel.Location = new Point(rectangle.X, rectangle.Y);
            blobPanel.Size = new Size(rectangle.Width, rectangle.Height);
            blobPanel.SelectedChanged += blobPanel_SelectedChanged;
            blobPanel.DeleteRequest += blobPanel_DeleteRequest;
            
            pictureBox1.Controls.Add(blobPanel);
        }
      
        /// <summary>
        /// ////////////////////////////////////////////////
        /// </summary>
        private void UpdateSelectedCount()
        {
            int count = pictureBox1.Controls.OfType<BlobPanel>().Count(panel => panel.Selected);
            lblSelected.Text = string.Format("{0} Blobs selected", count);
            lblSelected.ForeColor = count > 0 ? Color.Red : Color.Black;
        }

        private void MoveSelectedBlobs(int dx, int dy)
        {
            foreach (BlobPanel panel in m_selectedBlobs)
            {
                panel.Location = new Point(panel.Location.X + dx, panel.Location.Y + dy);
                panel.Invalidate();
            }
        }

        private void ExportBlobs()
        {
            if (string.IsNullOrEmpty(txtOutput.Text))
            {
                MessageBox.Show("Please enter an output file name.");
                return;
            }

            if (txtExtractedBackColor.Value == 0)
            {
                DialogResult dr = MessageBox.Show("Extraction back color is black, do you want to continue with that?", "Extract Back Color", MessageBoxButtons.YesNo);
                if (dr == DialogResult.No)
                    return;
            }
            
            // Check that we have any blobs.
            if (!pictureBox1.Controls.OfType<BlobPanel>().Any())
            {
                MessageBox.Show("There are no blobs to export.");
                return;
            }

            Cursor = Cursors.WaitCursor;
            if (File.Exists(txtOutput.Text))
            {
                DialogResult dr = MessageBox.Show("There is already file with the same name, overwrite?", "Save", MessageBoxButtons.OKCancel);
                if (dr == DialogResult.Cancel)
                {
                    Cursor = Cursors.Default;
                    return;
                }
            }

            // Save the output file name.
            Properties.Settings.Default.OutputFile = txtOutput.Text;
            Properties.Settings.Default.Save();

            using (StreamWriter sw = new StreamWriter(txtOutput.Text))
            {
                // Group by RowIndex and select the group Left, Right, Top, Bottom
                var rows = pictureBox1.Controls.OfType<BlobPanel>().GroupBy(bp => bp.RowIndex);

                foreach (IGrouping<int, BlobPanel> row in rows)
                {
                    row.ForEach(bp => ExportBlob(sw, row.Key+1, bp));
                }

                sw.Flush();
                sw.Close();
            }

            Cursor = Cursors.Default;
            MessageBox.Show("Finished.");
        }

        /// <summary>
        /// Crop the blob from the image
        /// </summary>
        private Bitmap CropBlob(BlobPanel blob, System.Drawing.Image source, int rotationAngel = 0)
        {
            // Create the target image, this is a squared image.
            int size = Math.Max(blob.Height, blob.Width);
            Bitmap newImage = new Bitmap(size, size, PixelFormat.Format24bppRgb);
            
            // Get the graphics object of the image.
            Graphics g = Graphics.FromImage(newImage);

            // Create the background color to use (the image we create is larger than the blob (as we squared it)
            // so this would be the color of the excess areas.
            Color bColor = Color.FromArgb((int)txtExtractedBackColor.Value, (int)txtExtractedBackColor.Value, (int)txtExtractedBackColor.Value);
            
            // Fill back color.
            g.FillRectangle(new SolidBrush(bColor), 0, 0, size, size);
            
            // Now we clip the blob from the PictureBox image.
            g.DrawImage(source, new Rectangle(0, 0, blob.Width, blob.Height), blob.Left, blob.Top, blob.Width, blob.Height, GraphicsUnit.Pixel);
            g.Dispose();

            if (rotationAngel != 0)
            {
                RotateBilinear filter = new RotateBilinear(rotationAngel, true);
                filter.FillColor = bColor;
                // apply the filter
                newImage = filter.Apply(newImage);
            }

            // Resize the image.
            ResizeBilinear resizefilter = new ResizeBilinear((int)txtExportSize.Value, (int)txtExportSize.Value);
            newImage = resizefilter.Apply(newImage);
            
            return newImage;
        }

        /// <summary>
        /// Write the blob image to a text file.
        /// </summary>
        private void ExportBlob(StreamWriter sw, int key, BlobPanel blob)
        {
            // Get the blob image.
            Bitmap newImage = CropBlob(blob, pictureBox1.Image);
            WriteImageToFile(sw, key, newImage);
            
            // Write a rotated version.
            newImage = CropBlob(blob, pictureBox1.Image, 10);
            WriteImageToFile(sw, key, newImage);
            
            // Write another rotated version (to the other dir).
            newImage = CropBlob(blob, pictureBox1.Image, -10);
            WriteImageToFile(sw, key, newImage);
        }

        private void WriteImageToFile(StreamWriter sw, int key, Bitmap newImage)
        {
            // Loop thru all pixels and write them.
            for (int i = 0; i < newImage.Height; i++)
            {
                for (int j = 0; j < newImage.Width; j++)
                {
                    Color pixel = newImage.GetPixel(i, j);
                    sw.Write(Common.GetColorAverage(pixel) + ",");
                }
            }

            sw.WriteLine(key);
        }


        private Bitmap rotateImageBlob(Bitmap imgIn)
        {

        
            //first rotate
            Bitmap rotatedImg45 = new Bitmap(imgIn.Width, imgIn.Height);
            //using (Graphics gr = Graphics.FromImage(imgIn))
            //{
            //    gr.TranslateTransform(imgIn.Width / 2, imgIn.Height / 2);
            //    gr.RotateTransform(45);
            //    gr.DrawImage(imgIn, new Point(0, 0));
            //}

            rotatedImg45.RotateFlip(RotateFlipType.Rotate90FlipX);

            //second rotate

            return rotatedImg45;
        }
        private Bitmap rotateNegImageBlob(Bitmap imgIn)
        {


            //first rotate
            Bitmap rotatedImg45 = new Bitmap(imgIn.Width, imgIn.Height);
            using (Graphics gr = Graphics.FromImage(imgIn))
            {
                gr.TranslateTransform(imgIn.Width / 2, imgIn.Height / 2);
                gr.RotateTransform(-45);
                gr.DrawImage(imgIn, new Point(0, 0));
            }

            //second rotate

            return rotatedImg45;
        }

        /// <summary>
        /// Load the model from the file and predict.
        /// </summary>
        private async System.Threading.Tasks.Task LoadModelAndPredictAsync()
        {
            if (!File.Exists(txtModelParams.Text))
            {
                MessageBox.Show("Model file not found.");
                return;
            }

            // Check if the user forgot to choose the back color.
            if (txtExtractedBackColor.Value == 0)
            {
                DialogResult dr = MessageBox.Show("Extraction back color is black, do you want to continue with prediction?", "Back Color", MessageBoxButtons.YesNo);
                if (dr == DialogResult.No)
                    return;
            }

            // Get the model params.
            MathNet.Numerics.LinearAlgebra.Complex.Matrix thetas = GetModelParamsFromFile();
            MathNet.Numerics.LinearAlgebra.Complex.Matrix thetas2 = GetModelParamsFromFile("a");
            MathNet.Numerics.LinearAlgebra.Complex.Matrix thetas3 = GetModelParamsFromFile("b");


            // Loop thru all blobs and predict.
            foreach (BlobPanel blob in pictureBox1.Controls.OfType<BlobPanel>())
            {
                // Redundancy 

                
                List<BlobPanel> obj = new List<BlobPanel>();
                for (int redo = 0; redo < 3; redo++)
                {


                    // Reset the blob's description.
                    blob.Description = string.Empty;

                    // Get the blob pixels.
                    for (int rdn = 0; rdn < 3; rdn++)
                    {
                        Vector xs = GetBlobPixels(blob)[rdn];
                       
                        // Get the model value (this is what to be used in the Sigmoid function).
                        var v = (thetas * xs);
                        if (redo == 1)
                        {
                            v = (thetas2 * xs);
                        }
                        else if (redo == 2)
                        {
                            v = (thetas3 * xs);
                        }
                        // This is for finding the maximum value of all letters predictions (1-vs-all), so
                        // we know what letter to choose.
                        double[] max = new double[3];
                        int[] maxIndex = { -1, -1, -1 };

                        // Loop thru the values
                        for (int i = 0; i < v.Count; i++)
                        {
                            // Get the final model prediction (Sigmoid).
                            v[i] = SpecialFunctions.Logistic(v[i].Real);

                            // Check if this prediction is in the "top 3".
                            for (int j = 0; j < max.Length; j++)
                            {
                                if (v[i].Real > max[j])
                                {
                                    max[j] = v[i].Real;
                                    maxIndex[j] = i;

                                    // We want to kepp max array sorted, so once we found a value
                                    // it is bigger than we stop.
                                    break;
                                }
                            }
                        }
                        blob.Value = max[0];
                        // Put the "top 3" in the description.
                        blob.Description += Common.Letters[maxIndex[0]] + " - " + max[0].ToString() + "\n";
                        blob.Description += Common.Letters[maxIndex[1]] + " - " + max[1].ToString() + "\n";
                        blob.Description += Common.Letters[maxIndex[2]] + " - " + max[2].ToString() + "\n";

                        // Save the selected letter in the blob.
                        blob.Title = Common.Letters[maxIndex[0]];


                        obj.Add(blob);
                    }
                }
                    var newObj = obj.OrderBy(Value => Math.Abs(blob.Value - 1)).First();

                    m_outString.Add(newObj.Title);
                    ////
                    ///
                    blob.Description = newObj.Description;
                    blob.Title = newObj.Title;
                    obj.Clear();
                
            }
            textBox1.Text = string.Join("", m_outString);

            var translator = new GoogleTranslator();

            Language from = Language.Japanese;
            Language to = GoogleTranslator.GetLanguageByName("English");

            TranslationResult result = await translator.TranslateLiteAsync(textBox1.Text, from, to);

            //The result is separated by the suggestions and the '\n' symbols
            string[] resultSeparated = result.FragmentedTranslation;

            //You can get all text using MergedTranslation property
            string resultMerged = result.MergedTranslation;

            //There is also original text transcription
            string transcription = result.TranslatedTextTranscription; // Kon'nichiwa! Ogenkidesuka?
            textBox2.Text = resultMerged;
            pictureBox1.Invalidate();
        }

        private List<Vector> GetBlobPixels(BlobPanel blob)
        {
            // Get the blob image.
            Bitmap newImage = CropBlob(blob, pictureBox1.Image);

            Bitmap rotImage = rotateImageBlob(newImage);
            Bitmap rotNegImage = rotateNegImageBlob(newImage);
            //Create List
            List<Vector> Lxs = new List<Vector>();

            //Add redundancy here
            //for (int rdn = 0; rdn < 3; rdn++)
            //{

                // Create the vector (Add the bias term).
                Vector xs = new DenseVector(newImage.Width * newImage.Height + 1);
            xs[0] = 1;
           

                // Loop thru the image pixels and add them to the vector.
                for (int i = 0; i < newImage.Height; i++)
                {
                    for (int j = 0; j < newImage.Width; j++)
                    {
                        Color pixel = newImage.GetPixel(i, j);
                        xs[1 + i * newImage.Width + j] = pixel.R;
                    }
                }
                Lxs.Add(xs);
            xs = new DenseVector(newImage.Width * newImage.Height + 1);
            xs[0] = 1;


            // Loop thru the image pixels and add them to the vector.
            for (int i = 0; i < newImage.Height; i++)
            {
                for (int j = 0; j < newImage.Width; j++)
                {
                    Color pixel = rotImage.GetPixel(i, j);
                    xs[1 + i * rotImage.Width + j] = pixel.R;
                }
            }
            Lxs.Add(xs);
            xs = new DenseVector(newImage.Width * newImage.Height + 1);
            xs[0] = 1;

             // Loop thru the image pixels and add them to the vector.
            for (int i = 0; i < newImage.Height; i++)
            {
                for (int j = 0; j < newImage.Width; j++)
                {
                    Color pixel = rotNegImage.GetPixel(i, j);
                    xs[1 + i * rotNegImage.Width + j] = pixel.R;
                }
            }
            Lxs.Add(xs);

            //}

            return Lxs;
        }

        /// <summary>
        /// Reads the model params from a file.
        /// </summary>
        private MathNet.Numerics.LinearAlgebra.Complex.Matrix GetModelParamsFromFile(String rdn = "")
        {
            // The model thetas (this is an intermidiate dictionary before we convert it to matrix).
            Dictionary<int, List<double>> allThetas = new Dictionary<int, List<double>>();

            // Open the model file.

            String modelStr = txtModelParams.Text.Substring(0,txtModelParams.Text.IndexOf("."))+ rdn + ".txt";
           
            using (StreamReader sr = new StreamReader(modelStr))
            {
                int rowIndex = 0;

                // Read the first line and loop till the end.
                string line = sr.ReadLine();
                while (!string.IsNullOrEmpty(line))
                {
                    // Create a new row in the dictionary.
                    allThetas.Add(rowIndex, new List<double>());

                    // Split the values.
                    string[] thetas = line.TrimStart().Split(' ');

                    // Loop thru all values and add them.
                    foreach (string currTheta in thetas)
                    {
                        // This Parse is a potential exception if there is not valid number there, ok for now...
                        double theta = double.Parse(currTheta);
                        allThetas[rowIndex].Add(theta);
                    }

                    // Get the next line and move to the next row index.
                    line = sr.ReadLine();
                    ++rowIndex;
                }
            }

            // Create the thetas matrix.
            MathNet.Numerics.LinearAlgebra.Complex.Matrix thetasM = new DenseMatrix(allThetas.Keys.Count, allThetas[0].Count);
            
            // Loop thru all dictionary vales (ordered by rows) and add it to the matrix.
            foreach (int row in allThetas.Keys.OrderBy(key => key))
            {
                for (int i = 0; i < allThetas[row].Count; i++)
                {
                    thetasM[row, i] = allThetas[row][i];
                }
            }

            return thetasM;
        }

        private void Ok_Click()
        {
            this.ReturnValue1 = textBox2.Text;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        #endregion

        #region Event Handlers

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.ReturnValue1 = textBox2.Text;
            
            this.DialogResult = DialogResult.OK;
            this.Close();
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

        private void btnOpenModelFile_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtModelParams.Text))
            {
                openFileDialog1.InitialDirectory = Path.GetDirectoryName(txtModelParams.Text);
            }

            openFileDialog1.Filter = "Text|*.txt";
            DialogResult dr = openFileDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                txtModelParams.Text = openFileDialog1.FileName;
                Properties.Settings.Default.ModelParams = openFileDialog1.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ProcessImage(m_original);
        }

        private void blobPanel_SelectedChanged(object sender, EventArgs e)
        {
            BlobPanel panel = sender as BlobPanel;
            if (panel == null) return;

            if (panel.Selected)
                m_selectedBlobs.Add(panel);
            else
                m_selectedBlobs.Remove(panel);
            
            UpdateSelectedCount();
        }

        private void blobPanel_DeleteRequest(object sender, EventArgs e)
        {
            if (m_selectedBlobs.Count < 1)
                return;

            DialogResult dr = MessageBox.Show(string.Format("Delete {0} selected blobs?", m_selectedBlobs.Count), "Delete Confirmation", MessageBoxButtons.YesNo);
            if (dr == DialogResult.No)
                return;
            
            foreach (BlobPanel selectedBlob in m_selectedBlobs)
            {
                pictureBox1.Controls.Remove(selectedBlob);
            }

            m_selectedBlobs.Clear();
            UpdateSelectedCount();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // Group by RowIndex and select the group Left, Right, Top, Bottom
            var rows = pictureBox1.Controls.OfType<BlobPanel>().GroupBy(bp => bp.RowIndex).Select(row => new
                {
                    RowIndex = row.Key,
                    Left = row.Min(blob => blob.Left),
                    Right = row.Max(blob => blob.Right),
                    Top = row.Min(blob => blob.Top),
                    Bottom = row.Max(blob => blob.Bottom),
                    Panles = row
                });

            // Draw the row rectangle.
            rows.ForEach(row =>
                {
                    // Draw the row rectangle if needed.
                    if (chkShowRows.Checked)
                    {
                        e.Graphics.DrawRectangle(new Pen(Color.Green, 1), row.Left - 2, row.Top - 2,
                                                                          (row.Right - row.Left) + 4, (row.Bottom - row.Top) + 4);
                    }

                    row.Panles.ForEach(panel =>
                        {
                            if (string.IsNullOrEmpty(panel.Title))
                                return;

                            e.Graphics.DrawString(panel.Title, Font, new SolidBrush(Color.Red), panel.Left + panel.Width / 2, panel.Top - 15);
                        });
                });
        }

        private void chkShowBinarize_CheckedChanged(object sender, EventArgs e)
        {
            pictureBox1.Image = chkShowBinarize.Checked ? m_binarized : m_original;
        }

        private void btnMoveUp_Click(object sender, EventArgs e)
        {
            MoveSelectedBlobs(0, -1 * (int)txtResizeInterval.Value);
        }

        private void btnMoveRight_Click(object sender, EventArgs e)
        {
            MoveSelectedBlobs((int)txtResizeInterval.Value, 0);
        }

        private void btnMoveDown_Click(object sender, EventArgs e)
        {
            MoveSelectedBlobs(0, (int)txtResizeInterval.Value);
        }

        private void btnMoveLeft_Click(object sender, EventArgs e)
        {
            MoveSelectedBlobs(-1 * (int)txtResizeInterval.Value, 0);
        }

        private void chkShowRows_CheckedChanged(object sender, EventArgs e)
        {
            pictureBox1.Invalidate();
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            ExportBlobs();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Here we take a 3x3 average of the picture color.

            int sumColor = 0;
            Point mouse = pictureBox1.PointToClient(MousePosition);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sumColor += Common.GetColorAverage(((Bitmap)pictureBox1.Image).GetPixel(mouse.X + j, mouse.Y + i));
                }
            }

            txtExtractedBackColor.Value = sumColor / 9;

            pictureBox1.Controls.OfType<BlobPanel>().ForEach(p => p.Selected = false);
            m_selectedBlobs.Clear();
            UpdateSelectedCount();
            pictureBox1.Invalidate();
        }

        private void btnPredict_Click(object sender, EventArgs e)
        {
            LoadModelAndPredictAsync();
        }


        // This area is for cropping




        #endregion

        private void CheckBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                chkShowBinarize.Visible = true;
                label5.Visible = true;
                txtBinThershold.Visible = true;
                label3.Visible = true;
                txtHeightMergeSense.Visible = true;
                label4.Visible = true;
                txtWidthMergeSense.Visible = true;
                label10.Visible = true;
                txtModelParams.Visible = true;
                btnOpenModelFile.Visible = true;
                btnPredict.Visible = true;
            }
            if (checkBox1.Checked == false)
            {
                chkShowBinarize.Visible = false;
                label5.Visible = false;
                txtBinThershold.Visible = false;
                label3.Visible = false;
                txtHeightMergeSense.Visible = false;
                label4.Visible = false;
                txtWidthMergeSense.Visible = false;
                label10.Visible = false;
                txtModelParams.Visible = false;
                btnOpenModelFile.Visible = false;
                btnPredict.Visible = false;
            }

        }
    }
}

using OSGeo.GDAL;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageByGDAL
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            int iOverview = -1;
           /// if (args.Length < 2) usage();
           // if (args.Length == 3) iOverview = int.Parse(args[2]);

            // Using early initialization of System.Console
            Console.WriteLine("");

            GdalConfiguration.ConfigureGdal();
            GdalConfiguration.ConfigureOgr();

            try
            {
                /* -------------------------------------------------------------------- */
                /*      Register driver(s).                                             */
                /* -------------------------------------------------------------------- */
                Gdal.AllRegister();

                /* -------------------------------------------------------------------- */
                /*      Open dataset.                                                   */
                /* -------------------------------------------------------------------- */
                string filename = @"D:\迅雷下载\GF6_WFV_E127.9_N46.8_20180823_L1A1119838015\验证集原始图像_8波段.tif";
                Dataset ds = Gdal.Open(filename, Access.GA_ReadOnly);

                if (ds == null)
                {
                    Console.WriteLine("Can't open " + filename);
                    System.Environment.Exit(-1);
                }

                Console.WriteLine("Raster dataset parameters:");
                Console.WriteLine("  Projection: " + ds.GetProjectionRef());
                Console.WriteLine("  RasterCount: " + ds.RasterCount);
                Console.WriteLine("  RasterSize (" + ds.RasterXSize + "," + ds.RasterYSize + ")");

                /* -------------------------------------------------------------------- */
                /*      Get driver                                                      */
                /* -------------------------------------------------------------------- */
                Driver drv = ds.GetDriver();

                if (drv == null)
                {
                    Console.WriteLine("Can't get driver.");
                    System.Environment.Exit(-1);
                }

                Console.WriteLine("Using driver " + drv.LongName);

                /* -------------------------------------------------------------------- */
                /*      Get raster band                                                 */
                /* -------------------------------------------------------------------- */
                for (int iBand = 1; iBand <= ds.RasterCount; iBand++)
                {
                    Band band = ds.GetRasterBand(iBand);
                    Console.WriteLine("Band " + iBand + " :");
                    Console.WriteLine("   DataType: " + band.DataType);
                    Console.WriteLine("   Size (" + band.XSize + "," + band.YSize + ")");
                    Console.WriteLine("   PaletteInterp: " + band.GetRasterColorInterpretation().ToString());

                    for (int iOver = 0; iOver < band.GetOverviewCount(); iOver++)
                    {
                        Band over = band.GetOverview(iOver);
                        Console.WriteLine("      OverView " + iOver + " :");
                        Console.WriteLine("         DataType: " + over.DataType);
                        Console.WriteLine("         Size (" + over.XSize + "," + over.YSize + ")");
                        Console.WriteLine("         PaletteInterp: " + over.GetRasterColorInterpretation().ToString());
                    }
                }

                /* -------------------------------------------------------------------- */
                /*      Processing the raster                                           */
                /* -------------------------------------------------------------------- */
                string outfilename = "";
                SaveBitmapBuffered(ds,outfilename, iOverview);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Application error: " + ex.Message);
            }

        }


        private static void SaveBitmapBuffered(Dataset ds, string filename, int iOverview)
        {
            // Get the GDAL Band objects from the Dataset
            Band redBand = ds.GetRasterBand(1);

            if (redBand.GetRasterColorInterpretation() == ColorInterp.GCI_PaletteIndex)
            {
                SaveBitmapPaletteBuffered(ds, filename, iOverview);
                return;
            }

            if (redBand.GetRasterColorInterpretation() == ColorInterp.GCI_GrayIndex)
            {
                SaveBitmapGrayBuffered(ds, filename, iOverview);
                return;
            }

            if (redBand.GetRasterColorInterpretation() != ColorInterp.GCI_RedBand)
            {
                Console.WriteLine("Non RGB images are not supported by this sample! ColorInterp = " +
                    redBand.GetRasterColorInterpretation().ToString());
                return;
            }

            if (ds.RasterCount < 3)
            {
                Console.WriteLine("The number of the raster bands is not enough to run this sample");
                System.Environment.Exit(-1);
            }

            if (iOverview >= 0 && redBand.GetOverviewCount() > iOverview)
                redBand = redBand.GetOverview(iOverview);

            Band greenBand = ds.GetRasterBand(2);

            if (greenBand.GetRasterColorInterpretation() != ColorInterp.GCI_GreenBand)
            {
                Console.WriteLine("Non RGB images are not supported by this sample! ColorInterp = " +
                    greenBand.GetRasterColorInterpretation().ToString());
                return;
            }

            if (iOverview >= 0 && greenBand.GetOverviewCount() > iOverview)
                greenBand = greenBand.GetOverview(iOverview);

            Band blueBand = ds.GetRasterBand(3);

            if (blueBand.GetRasterColorInterpretation() != ColorInterp.GCI_BlueBand)
            {
                Console.WriteLine("Non RGB images are not supported by this sample! ColorInterp = " +
                    blueBand.GetRasterColorInterpretation().ToString());
                return;
            }

            if (iOverview >= 0 && blueBand.GetOverviewCount() > iOverview)
                blueBand = blueBand.GetOverview(iOverview);

            // Get the width and height of the raster
            int width = redBand.XSize;
            int height = redBand.YSize;

            // Create a Bitmap to store the GDAL image in
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);

            DateTime start = DateTime.Now;

            byte[] r = new byte[width * height];
            byte[] g = new byte[width * height];
            byte[] b = new byte[width * height];

            redBand.ReadRaster(0, 0, width, height, r, width, height, 0, 0);
            greenBand.ReadRaster(0, 0, width, height, g, width, height, 0, 0);
            blueBand.ReadRaster(0, 0, width, height, b, width, height, 0, 0);
            TimeSpan renderTime = DateTime.Now - start;
            Console.WriteLine("SaveBitmapBuffered fetch time: " + renderTime.TotalMilliseconds + " ms");

            int i, j;
            for (i = 0; i < width; i++)
            {
                for (j = 0; j < height; j++)
                {
                    Color newColor = Color.FromArgb(Convert.ToInt32(r[i + j * width]), Convert.ToInt32(g[i + j * width]), Convert.ToInt32(b[i + j * width]));
                    bitmap.SetPixel(i, j, newColor);
                }
            }

            bitmap.Save(filename);
        }

        private static void SaveBitmapPaletteBuffered(Dataset ds, string filename, int iOverview)
        {
            // Get the GDAL Band objects from the Dataset
            Band band = ds.GetRasterBand(1);
            if (iOverview >= 0 && band.GetOverviewCount() > iOverview)
                band = band.GetOverview(iOverview);

            ColorTable ct = band.GetRasterColorTable();
            if (ct == null)
            {
                Console.WriteLine("   Band has no color table!");
                return;
            }

            if (ct.GetPaletteInterpretation() != PaletteInterp.GPI_RGB)
            {
                Console.WriteLine("   Only RGB palette interp is supported by this sample!");
                return;
            }

            // Get the width and height of the Dataset
            int width = band.XSize;
            int height = band.YSize;

            // Create a Bitmap to store the GDAL image in
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);

            DateTime start = DateTime.Now;

            byte[] r = new byte[width * height];

            band.ReadRaster(0, 0, width, height, r, width, height, 0, 0);
            TimeSpan renderTime = DateTime.Now - start;
            Console.WriteLine("SaveBitmapBuffered fetch time: " + renderTime.TotalMilliseconds + " ms");

            int i, j;
            for (i = 0; i < width; i++)
            {
                for (j = 0; j < height; j++)
                {
                    ColorEntry entry = ct.GetColorEntry(r[i + j * width]);
                    Color newColor = Color.FromArgb(Convert.ToInt32(entry.c1), Convert.ToInt32(entry.c2), Convert.ToInt32(entry.c3));
                    bitmap.SetPixel(i, j, newColor);
                }
            }

            bitmap.Save(filename);
        }

        private static void SaveBitmapGrayBuffered(Dataset ds, string filename, int iOverview)
        {
            // Get the GDAL Band objects from the Dataset
            Band band = ds.GetRasterBand(1);
            if (iOverview >= 0 && band.GetOverviewCount() > iOverview)
                band = band.GetOverview(iOverview);

            // Get the width and height of the Dataset
            int width = band.XSize;
            int height = band.YSize;

            // Create a Bitmap to store the GDAL image in
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppRgb);

            DateTime start = DateTime.Now;

            byte[] r = new byte[width * height];

            band.ReadRaster(0, 0, width, height, r, width, height, 0, 0);
            TimeSpan renderTime = DateTime.Now - start;
            Console.WriteLine("SaveBitmapBuffered fetch time: " + renderTime.TotalMilliseconds + " ms");

            int i, j;
            for (i = 0; i < width; i++)
            {
                for (j = 0; j < height; j++)
                {
                    Color newColor = Color.FromArgb(Convert.ToInt32(r[i + j * width]), Convert.ToInt32(r[i + j * width]), Convert.ToInt32(r[i + j * width]));
                    bitmap.SetPixel(i, j, newColor);
                }
            }

            bitmap.Save(filename);
        }
    }
}

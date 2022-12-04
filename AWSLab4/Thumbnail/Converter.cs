using GrapeCity.Documents.Drawing;
using GrapeCity.Documents.Imaging;
using GrapeCity.Documents.Text;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Thumbnail
{
    public class GcImagingOperations
    {
        public static Stream GetConvertedImage(byte[] stream)
        {
            using (var bmp = new GcBitmap())
            {
                bmp.Load(stream);
                // Add watermark 
                var newImg = new GcBitmap();
                newImg.Load(stream);
                using (var g = bmp.CreateGraphics(Color.White))
                {
                    g.DrawImage(
                       newImg,
                       new RectangleF(0, 0, bmp.Width, bmp.Height),
                       null,
                       ImageAlign.Default
                       );
                    g.DrawString("DOCUMENT", new TextFormat
                    {
                        FontSize = 22,
                        ForeColor = Color.FromArgb(128, Color.Yellow),
                        Font = FontCollection.SystemFonts.DefaultFont
                    },
                    new RectangleF(0, 0, bmp.Width, bmp.Height),
                    TextAlignment.Center, ParagraphAlignment.Center, false);
                }
                //  Convert to grayscale 
                //bmp.ApplyEffect(GrayscaleEffect.Get(GrayscaleStandard.BT601));

                //  Resize to thumbnail 
                var resizedImage = bmp.Resize(100, 100, InterpolationMode.NearestNeighbor);

                return GetBase64(resizedImage);
            }
        }
        #region helper 
        private static Stream GetBase64(GcBitmap bmp)
        {
            MemoryStream m = new MemoryStream();
               
               bmp.SaveAsPng(m);
                m.Flush();
                m.Position = 0;
                return m;
        }
        #endregion
    }
}

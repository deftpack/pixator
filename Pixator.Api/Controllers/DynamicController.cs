using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace Pixator.Api.Controllers
{
    public class DynamicController : ApiController
    {
        const int minColumnNumber = 3;
        const int maxColumnNumber = 20;

        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        public HttpResponseMessage Get(int width, int height, int seed)
        {
            var randomGenerator = new Random(seed);
            var image = new Bitmap(width, height);
            var graphics = Graphics.FromImage(image);
            var columnWidths = new List<int>();

            while (columnWidths.Sum() < width)
            {
                var nextColumnWidth = randomGenerator.Next(width / maxColumnNumber, width / minColumnNumber);
                columnWidths.Add(columnWidths.Sum() + nextColumnWidth > width || columnWidths.Count + 1 == maxColumnNumber ? width - columnWidths.Sum() : nextColumnWidth);
            }

            var columnColors = columnWidths.Select(cw => Color.FromArgb(randomGenerator.Next(255), randomGenerator.Next(255), randomGenerator.Next(255))).ToArray();

            //var horizontalCursor = 0;
            //for(var i = 0; i < columnWidths.Count; i++)
            //{
            //    var currentColor = columnColors[i];
            //    var previousColor = columnColors[i == 0 ? 0 : i - 1];
            //    var nextColor = columnColors[i + 1 == columnColors.Length ? i : i + 1];
            var rectangle = new Rectangle(0, 0, width, height);
            var graphicsPath = new GraphicsPath();
            graphicsPath.AddEllipse(rectangle);
            var brush = new PathGradientBrush(graphicsPath);
            brush.CenterPoint = new PointF(rectangle.Width / 2, rectangle.Height / 2);
            brush.CenterColor = Color.White;
            brush.SurroundColors = columnColors;
            brush.SetBlendTriangularShape(0.5f, 1.0f);
            brush.FocusScales = new PointF(0, 0);
            graphics.FillPath(brush, graphicsPath);
            //    horizontalCursor += columnWidths[i];
            //}

            var imageStream = new MemoryStream();
            image.Save(imageStream, ImageFormat.Png);

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new ByteArrayContent(imageStream.ToArray());
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return result;
        }
    }
}

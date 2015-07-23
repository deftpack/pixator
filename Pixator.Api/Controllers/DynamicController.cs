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
        //const int minColumnNumber = 30;
        //const int maxColumnNumber = 200;

        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        public HttpResponseMessage Get(int width, int height, int seed)
        {
            var minColumnNumber = width / 5;
            var maxColumnNumber = width / 2;
            var randomGenerator = new Random(seed);
            var image = new Bitmap(width, height);
            var graphics = Graphics.FromImage(image);
            var columnWidths = new List<int>();

            while (columnWidths.Sum() < width)
            {
                var nextColumnWidth = randomGenerator.Next(width / maxColumnNumber, width / minColumnNumber);
                columnWidths.Add(columnWidths.Sum() + nextColumnWidth > width || columnWidths.Count + 1 == maxColumnNumber ? width - columnWidths.Sum() : nextColumnWidth);
            }

            var columnColors = Enumerable.Range(0, columnWidths.Count + 1).Select(cw =>
                Color.FromArgb(randomGenerator.Next(255), randomGenerator.Next(255), randomGenerator.Next(255))).ToArray();

            var horizontalCursor = 0;
            var rectangle = new Rectangle(0, 0, width, height);
            var graphicsPath = new GraphicsPath();
            graphicsPath.AddRectangle(rectangle);
            var brush = new LinearGradientBrush(rectangle, Color.Black, Color.Black, 0, false);
            var colorBlend = new ColorBlend(columnColors.Length);
            colorBlend.Colors = columnColors;
            var positions = new List<float> { 0f };
            positions.AddRange(columnWidths.Select(x =>
            {
                horizontalCursor += x;
                return (float)horizontalCursor / width;
            }));
            colorBlend.Positions = positions.ToArray();

            brush.InterpolationColors = colorBlend;
            var roygbiv = new { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Indigo, Color.Violet };

            graphics.FillPath(brush, graphicsPath);

            var imageStream = new MemoryStream();
            image.Save(imageStream, ImageFormat.Png);

            HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new ByteArrayContent(imageStream.ToArray());
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return result;
        }
    }
}

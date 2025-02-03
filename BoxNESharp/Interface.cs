using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxNESharp {
    internal interface IVideoComponent {
        public void DrawScreen(Color[,] pixels, int pixelSizeX, int pixelSizeY);
    }
}

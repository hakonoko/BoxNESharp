using DxLibDLL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading.Tasks;

namespace BoxNESharp {
    internal class VideoComponent : IVideoComponent {
        public void DrawScreen(Color[,] pixels, int pixelSizeX, int pixelSizeY) {
            DX.ProcessMessage();
            DX.ClearDrawScreen(); //裏画面をクリアする

            StringBuilder sb = new StringBuilder();
            for (int y = 0; y < pixels.GetLength(0); y++) {
                for (int x = 0; x < pixels.GetLength(1); x++) {
                    var color = pixels[y, x];
                    DX.DrawBox(x * pixelSizeX, y * pixelSizeY, (x + 1) * pixelSizeX, (y + 1) * pixelSizeY, DX.GetColor(color.R, color.G, color.B), DX.TRUE);
                    //DX.DrawPixel(x * pixelSizeX, y * pixelSizeY, DX.GetColor(color.R, color.G, color.B));
                    sb.Append(color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2") + "," );
                }
                sb.AppendLine();
            }
            BoxNESharp.DebugLog(sb.ToString());

            DX.ScreenFlip(); //2つの画面を入れ替える

            Logger.GetInstance().Debug(sb.ToString());
        }
    }
}

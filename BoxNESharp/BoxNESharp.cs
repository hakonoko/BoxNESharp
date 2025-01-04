using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DxLibDLL;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace BoxNESharp {
    internal partial class BoxNESharp {
        const string WINDOW_TITLE = "BoxNESharp";

        /// <summary>
        /// ファミコンの解像度
        /// </summary>
        const int ORIGINAL_W = 256, ORIGINAL_H = 240;

        /// <summary>
        /// ウィンドウサイズ倍率
        /// </summary>
        const int WIN_SCALE_FACTOR = 2;

        public static int WindowSizeW { get => ORIGINAL_W * WIN_SCALE_FACTOR; }
        public static int WindowSizeH { get => ORIGINAL_H * WIN_SCALE_FACTOR; }

        public static int DotSizeW { get => WindowSizeW / ORIGINAL_W; }
        public static int DotSizeH { get => WindowSizeH / ORIGINAL_H; }

        // CPU
        static CPU cpu = CPU.GetInstance();

        /// <summary>
        /// メイン関数
        /// </summary>
        public static void Main() {
            DX.SetWindowText(WINDOW_TITLE);
            DX.SetOutApplicationLogValidFlag(DX.FALSE);
            DX.SetAlwaysRunFlag(DX.TRUE);
            DX.SetWindowSizeChangeEnableFlag(DX.TRUE);
            DX.SetFullScreenResolutionMode(DX.DX_FSRESOLUTIONMODE_DESKTOP);
            DX.ChangeWindowMode(DX.TRUE);
            DX.SetGraphMode(WindowSizeW, WindowSizeH, 32);

            // DXライブラリの初期化
            if (DX.DxLib_Init() == -1)
                return;

            DX.SetDrawScreen(DX.DX_SCREEN_BACK); //裏画面処理を設定する

            // ファイル選択
            var path = PickAndShow();

            DebugLog(path);

            int cnt = 0;

            // 無限ループ
            while (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) == 0) {
                DX.ProcessMessage();
                DX.ClearDrawScreen(); //裏画面をクリアする

                //cpu.Fetch();

                DX.ScreenFlip(); //2つの画面を入れ替える

                if (cnt > 100) {
                    break;
                }
            }

            // DXライブラリ終了

            DX.DxLib_End();
        }

        static string PickAndShow() {
            string path = string.Empty;
            using (CommonOpenFileDialog cofd = new CommonOpenFileDialog()) {
                cofd.IsFolderPicker = false;

                if (cofd.ShowDialog() == CommonFileDialogResult.Ok) {
                    path = cofd.FileName;
                }
            }
            return path;
        }

        public static void DebugLog(string text) {
            System.Diagnostics.Debug.WriteLine(text);
        }
    }
}
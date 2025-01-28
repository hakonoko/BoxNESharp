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
        // PPU
        static PPU ppu = PPU.GetInstance();

        public static int Cycle { get; private set; }

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

            //裏画面処理を設定する
            DX.SetDrawScreen(DX.DX_SCREEN_BACK);

            // ファイル選択
            var path = FilePicker();

            if (string.IsNullOrEmpty(path)) {
                DebugLog("ファイルが選択されていません。");
                DX.DxLib_End();
                return;
            }

            DebugLog("");
            DebugLog($"FilePath: {path}");

            // romファイル読み込み
            byte[] rom = ReadFile(path);

            //DebugLog($"Length: {rom.Length.ToString()} (0x{rom.Length.ToString("X2")})");
            //StringBuilder sb = new StringBuilder();
            //for (int i = 0; i < rom.Length; i++) {
            //    sb.Append(rom[i].ToString("X2"));
            //    sb.Append(" ");
            //    if (i % 16 == 15) {
            //        DebugLog(sb.ToString());
            //        sb.Clear();
            //    }
            //}
            //if (sb.Length > 0) {
            //    DebugLog(sb.ToString());
            //}

            // ROMをCPUに設定
            cpu.SetRom(rom, 0);

            int cnt = 0;

            // 無限ループ
            while (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) == 0) {
                DX.ProcessMessage();
                DX.ClearDrawScreen(); //裏画面をクリアする

                var cycle = cpu.Fetch();
                Cycle += cycle;

                if(Cycle > 332) {
                    Cycle -= 332;
                }

                DX.ScreenFlip(); //2つの画面を入れ替える

                cnt++;
                //if (cnt >= 1104) {
                if (cnt >= 10000) {
                    //cpu.DebugExportRAM();
                    ppu.DebugExportVRAM();
                    break;
                }
            }
            DebugLog("End.", false);

            while (true) {
                if (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) != 0) {
                    break;
                }
            }
            // DXライブラリ終了
            DX.DxLib_End();
        }

        static string FilePicker() {
            string path = string.Empty;
            using (CommonOpenFileDialog cofd = new CommonOpenFileDialog()) {
                cofd.IsFolderPicker = false;

                if (cofd.ShowDialog() == CommonFileDialogResult.Ok) {
                    path = cofd.FileName;
                }
            }
            return path;
        }

        static byte[] ReadFile(string path) {
            byte[] file = [];
            try {
                file = File.ReadAllBytes(path);
            } catch (Exception e) {
                DebugLog(e.Message);
            }
            return file;
        }

        static int logNum = -5;
        /// <summary>
        /// ログを出力する
        /// </summary>
        public static void DebugLog(string text, bool exportLogFile = true) {
            //System.Diagnostics.Debug.WriteLine(text);
            //if(logNum >= 1000) {
                Console.WriteLine($"{logNum.ToString("D4")}: {text}");
                if (exportLogFile) {
                    Logger.GetInstance().Debug(text);
                }
            //}
            logNum++;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DxLibDLL;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Printing;

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

        public static int WINDOW_SIZE_W { get => ORIGINAL_W * WIN_SCALE_FACTOR; }
        public static int WINDOW_SIZE_H { get => ORIGINAL_H * WIN_SCALE_FACTOR; }

        public static int DOT_SIZE_X { get => WINDOW_SIZE_W / ORIGINAL_W; }
        public static int DOT_SIZE_Y { get => WINDOW_SIZE_H / ORIGINAL_H; }

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
            DX.SetGraphMode(WINDOW_SIZE_W, WINDOW_SIZE_H, 32);

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

            // VideoComponentを設定
            VideoComponent videoComponent = new VideoComponent();
            ppu.SetVideoComponent(videoComponent);

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
                var cycle = cpu.Fetch();

                // PPUにサイクルを渡す
                ppu.Run(cycle);

                Cycle += cycle;

                if(Cycle > 332) {
                    Cycle -= 332;
                }

                cnt++;
                //if (cnt >= 1104) {
                if (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) != 0) {
                    //cpu.DebugExportRAM();
                    //ppu.DebugExportVRAM();
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DxLibDLL;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Printing;
using System.Windows;
using R3;
using System.Windows.Input;
using System.Windows.Threading;

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
        // Controller
        static Controller controller = Controller.GetInstance();

        public static int Cycle { get; private set; }

        // FPS計算用
        static int frameCount = 0;
        static int fps = 0;
        static long lastFpsUpdateTime = 0;

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

            // VideoComponentを設定
            VideoComponent videoComponent = new VideoComponent();
            ppu.SetVideoComponent(videoComponent);

#if NESTEST
            var path = @"C:\Users\hakonoko\Documents\nes\nestest.nes";
#else
            // ファイル選択
            var path = FilePicker();

            if (string.IsNullOrEmpty(path)) {
                DebugLog("ファイルが選択されていません。");
                DX.DxLib_End();
                return;
            }
#endif
            // romファイル読み込み
            byte[] rom = ReadFile(path);

            DebugLog("");
            DebugLog($"FilePath: {path}");

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

            // コントローラーを初期化
            controller.Initialize();

            Execute();
        }

        static async void Execute() {
            int cnt = 0;

            // 無限ループ
            while (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) == 0) {
#if false
                while (true) {
                    if (Keyboard.IsKeyDown(Key.W))
                        return;
                    await Task.Delay(100);
                }
#endif
                try {
                    // コントローラーの入力状態を更新
                    controller.Update();

                    var cycle = cpu.Fetch();
                    logNum++;

                    // PPUにサイクルを渡す
                    ppu.Run(cycle * 3);
                    Cycle += cycle;

                    if (Cycle > 332) {
                        Cycle -= 332;
                    }

                    // FPS計算とタイトルバー更新
                    UpdateFPS();
                } catch(Exception e) {
                    DebugLog($"例外発生！！！ \r\n {e.Message}");
                    cnt = 999999;
                }

                cnt++;
                if (DX.CheckHitKey(DX.KEY_INPUT_ESCAPE) != 0) {
                    cpu.DebugExportRAM();
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
        static Logger log = Logger.GetInstance();
        /// <summary>
        /// FPSを更新してタイトルバーに表示する
        /// </summary>
        static void UpdateFPS() {
            frameCount++;

            // 現在の時間を取得（ミリ秒単位）
            long currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            // 1秒ごとにFPSを計算してタイトルを更新
            if (currentTime - lastFpsUpdateTime >= 1000) {
                fps = frameCount;
                frameCount = 0;
                lastFpsUpdateTime = currentTime;

                // タイトルバーにFPSを表示
                string titleWithFps = $"{WINDOW_TITLE} - FPS: {fps}";
                DX.SetWindowText(titleWithFps);
            }
        }

        /// <summary>
        /// ログを出力する
        /// </summary>
        public static void DebugLog(string text, bool exportLogFile = true) {
            //System.Diagnostics.Debug.WriteLine(text);
            //if(logNum >= 1000) {
            exportLogFile = false;
                Console.WriteLine(text);
                if (exportLogFile) {
                    log.Debug(text);
                }
            //}
        }
    }
}

using DxLibDLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxNESharp {
    /// <summary>
    /// ファミコンコントローラーエミュレーター
    /// 実機準拠の入力システムを実装
    /// </summary>
    internal class Controller {
        #region Singleton
        private static Controller _instance = new Controller();

        public static Controller GetInstance() {
            return _instance;
        }
        #endregion

        /// <summary>
        /// コントローラーの入力データ構造
        /// 実機同様のビット配置
        /// </summary>
        private class ControllerData {
            /// <summary>
            /// 現在の入力状態（8ビット）
            /// Bit 0: Aボタン
            /// Bit 1: Bボタン
            /// Bit 2: セレクトボタン
            /// Bit 3: スタートボタン
            /// Bit 4: 上ボタン
            /// Bit 5: 下ボタン
            /// Bit 6: 左ボタン
            /// Bit 7: 右ボタン
            /// </summary>
            public byte CurrentData = 0x00;

            /// <summary>
            /// 前回の入力状態（ラグ検知用）
            /// </summary>
            public byte PreviousData = 0x00;

            /// <summary>
            /// キー状態バッファ（フレーム間保持用）
            /// </summary>
            private bool[] keyBuffer = new bool[256];

            /// <summary>
            /// シフトレジスタ（実機の4021シフトレジスタをエミュレート）
            /// </summary>
            public byte ShiftRegister = 0x00;

            /// <summary>
            /// ストローブ状態
            /// </summary>
            public bool Strobe = false;

            /// <summary>
            /// 現在の読み取りビット位置
            /// </summary>
            public int ReadBit = 0;

            /// <summary>
            /// 入力状態を更新する（リアルタイムのキー状態を取得）
            /// </summary>
            public void UpdateInput() {
                PreviousData = CurrentData;
                byte keyState = 0;

                // キーの状態をバッファから取得（より正確な入力検知）
                keyState |= GetBufferedKeyState(DX.KEY_INPUT_UP) ? (byte)0x10 : (byte)0x00;      // 上 (Bit 4)
                keyState |= GetBufferedKeyState(DX.KEY_INPUT_DOWN) ? (byte)0x20 : (byte)0x00;    // 下 (Bit 5)
                keyState |= GetBufferedKeyState(DX.KEY_INPUT_LEFT) ? (byte)0x40 : (byte)0x00;    // 左 (Bit 6)
                keyState |= GetBufferedKeyState(DX.KEY_INPUT_RIGHT) ? (byte)0x80 : (byte)0x00;   // 右 (Bit 7)

                // ボタン
                keyState |= GetBufferedKeyState(DX.KEY_INPUT_S) ? (byte)0x01 : (byte)0x00;       // A (Bit 0)
                keyState |= GetBufferedKeyState(DX.KEY_INPUT_D) ? (byte)0x02 : (byte)0x00;       // B (Bit 1)
                keyState |= GetBufferedKeyState(DX.KEY_INPUT_W) ? (byte)0x04 : (byte)0x00;       // セレクト (Bit 2)
                keyState |= GetBufferedKeyState(DX.KEY_INPUT_E) ? (byte)0x08 : (byte)0x00;       // スタート (Bit 3)

                CurrentData = keyState;

                // キー状態バッファを更新
                UpdateKeyBuffer();
            }

            /// <summary>
            /// キー状態バッファを更新する（より正確な入力検知）
            /// </summary>
            private void UpdateKeyBuffer() {
                // 現在のキー状態をバッファに保存（フレーム間保持用）
                // より正確なキー状態取得方法を使用
                keyBuffer[DX.KEY_INPUT_UP] = DX.CheckHitKey(DX.KEY_INPUT_UP) != 0;
                keyBuffer[DX.KEY_INPUT_DOWN] = DX.CheckHitKey(DX.KEY_INPUT_DOWN) != 0;
                keyBuffer[DX.KEY_INPUT_LEFT] = DX.CheckHitKey(DX.KEY_INPUT_LEFT) != 0;
                keyBuffer[DX.KEY_INPUT_RIGHT] = DX.CheckHitKey(DX.KEY_INPUT_RIGHT) != 0;
                keyBuffer[DX.KEY_INPUT_S] = DX.CheckHitKey(DX.KEY_INPUT_S) != 0;
                keyBuffer[DX.KEY_INPUT_D] = DX.CheckHitKey(DX.KEY_INPUT_D) != 0;
                keyBuffer[DX.KEY_INPUT_W] = DX.CheckHitKey(DX.KEY_INPUT_W) != 0;
                keyBuffer[DX.KEY_INPUT_E] = DX.CheckHitKey(DX.KEY_INPUT_E) != 0;
            }

            /// <summary>
            /// キー状態バッファを初期化する
            /// </summary>
            public void InitializeKeyBuffer() {
                // 初期状態ではすべてのキーを離された状態に設定
                for (int i = 0; i < keyBuffer.Length; i++) {
                    keyBuffer[i] = false;
                }
            }

            /// <summary>
            /// バッファからキーの状態を取得（フレーム間保持用）
            /// </summary>
            private bool GetBufferedKeyState(int keyCode) {
                return keyBuffer[keyCode];
            }

            /// <summary>
            /// ストローブ信号を設定（実機準拠）
            /// </summary>
            public void SetStrobe(bool strobe) {
                if (Strobe && !strobe) {
                    // ストローブが解除された時、シフトレジスタに現在の入力データを設定
                    // 前回の状態と比較して変更を検知し、より正確な状態をキャプチャ
                    ShiftRegister = CurrentData;
                    ReadBit = 0;
                }
                Strobe = strobe;
            }

            /// <summary>
            /// 次のビットを読み取る（実機準拠のシリアル読み取り）
            /// </summary>
            public byte ReadNextBit() {
                if (Strobe) {
                    // ストローブ中は常にAボタン（Bit 0）の状態を返す
                    return (byte)(CurrentData & 0x01);
                } else {
                    // シフトレジスタから1ビット読み取り
                    byte bit = (byte)((ShiftRegister >> ReadBit) & 0x01);
                    ReadBit++;

                    // 8ビット読み終わったら0を返す（実機準拠）
                    if (ReadBit >= 8) {
                        ReadBit = 8;
                        return 0;
                    }

                    return bit;
                }
            }
        }

        // 1Pと2Pのコントローラーデータ
        private ControllerData controller1P = new ControllerData();
        private ControllerData controller2P = new ControllerData();

        /// <summary>
        /// コントローラーの状態を更新する
        /// </summary>
        public void Update() {
            // より効率的な入力状態更新
            controller1P.UpdateInput();
            //controller2P.UpdateInput();

            //入力状態の変更を検知してログ出力（デバッグ用）
            //var (controller1, controller2) = GetCurrentInput();
            //if (controller1 != 0 || controller2 != 0) {
            //    //入力状態が変化したらログ出力（テスト用）
            //    BoxNESharp.DebugLog($"Controller Input - 1P: {controller1.ToString("X2")}, 2P: {controller2.ToString("X2")}");
            //}
        }

        /// <summary>
        /// コントローラーを初期化する
        /// </summary>
        public void Initialize() {
            controller1P.InitializeKeyBuffer();
            controller2P.InitializeKeyBuffer();
        }

        /// <summary>
        /// コントローラーレジスタ読み取り（実機準拠）
        /// </summary>
        /// <param name="address">アドレス（0x4016 or 0x4017）</param>
        /// <returns>コントローラーの入力データ</returns>
        public byte ReadController(ushort address) {
            switch (address) {
                case 0x4016:
                    return controller1P.ReadNextBit();
                case 0x4017:
                    return controller2P.ReadNextBit();
                default:
                    return 0x00;
            }
        }

        /// <summary>
        /// コントローラーレジスタ書き込み（ストローブ制御）
        /// </summary>
        /// <param name="address">アドレス（0x4016 or 0x4017）</param>
        /// <param name="data">書き込みデータ</param>
        public void WriteController(ushort address, byte data) {
            bool strobe = (data & 0x01) != 0;

            switch (address) {
                case 0x4016:
                    controller1P.SetStrobe(strobe);
                    controller2P.SetStrobe(strobe); // 両方のコントローラーに同じストローブを適用
                    break;
                case 0x4017:
                    // 2Pコントローラー用の書き込み（通常は使用されないが、実機準拠で実装）
                    controller2P.SetStrobe(strobe);
                    break;
            }
        }

        /// <summary>
        /// デバッグ用：現在の入力状態を取得
        /// </summary>
        public (byte controller1, byte controller2) GetCurrentInput() {
            return (controller1P.CurrentData, controller2P.CurrentData);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxNESharp {
    internal partial class BoxNESharp {
        /// <summary>
        /// PPU
        /// </summary>
        class PPU {
            /// <summary>
            /// PPUのインスタンス
            /// </summary>
            private static PPU instance = new PPU();
            /// <summary>
            /// PPUのインスタンスを取得
            /// </summary>
            /// <returns>PPUのインスタンス</returns>
            public static PPU GetInstance() {
                return instance;
            }
            /// <summary>
            /// PPUのコンストラクタ
            /// </summary>
            private PPU() {
            }

            /// <summary> CPUのサイクル数 </summary>
            int cycle = 0;

            /// <summary> 現在のスキャンライン </summary>
            int line = 0;

            #region Memory
            private class Memory {
                public byte[] VRAM = new byte[0x3FFF];
            }
            #endregion

            Memory Mem = new Memory();

            public void SetCHRROM(byte[] chrRom) {
                // CHRROMの設定
                for (int i = 0; i < chrRom.Length; i++) {
                    // CHRROMのアドレスは0x0000～0x1FFF
                    // 0x0000～0x0FFFは背景パターンテーブル1
                    // 0x1000～0x1FFFは背景パターンテーブル2
                    Mem.VRAM[i] = chrRom[i];
                }
            }

            public void WriteVRAM(ushort address, byte data) {
                Mem.VRAM[address] = data;
            }

            public byte ReadVRAM(ushort address) {
                return Mem.VRAM[address];
            }

            public void Run(int cycle) {
                this.cycle += cycle;

                // 341サイクルごとに1ライン描画
                if (this.cycle >= 341) {
                    this.cycle -= 341;

                    // 1ライン描画
                    line++;
                    // TODO: 描画処理
                }
            }

            public void DebugExportVRAM() {
                // VRAMのデバッグ出力
                DebugLog("");
                DebugLog("PPU RAM DATA");
                StringBuilder sb = new();
                for (int i = 0; i < Mem.VRAM.Length; i++) {
                    if (i % 16 == 0) {
                        sb.Append($"0x{i.ToString("X4")}: ");
                    }
                    sb.Append(Mem.VRAM[i].ToString("X2"));
                    sb.Append(" ");
                    if (i % 16 == 15) {
                        DebugLog(sb.ToString());
                        sb.Clear();
                    }
                }
            }   
        }
    }
}

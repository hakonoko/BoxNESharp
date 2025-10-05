using DxLibDLL;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxNESharp {
    internal partial class BoxNESharp {
        /// <summary>
        /// PPU
        /// </summary>
        class PPU {
            IVideoComponent videoComponent;

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

            public void SetVideoComponent(IVideoComponent videoComponent) {
                GetInstance().videoComponent = videoComponent;
            }

            /// <summary>
            /// PPUのコンストラクタ
            /// </summary>
            private PPU() {
            }

            #region Palette
            Dictionary<int, Color> colorDictionary = new Dictionary<int, Color>
            {
                {0x00, Color.FromArgb(0x62, 0x62, 0x62)},
                {0x01, Color.FromArgb(0x00, 0x1F, 0xB2)},
                {0x02, Color.FromArgb(0x24, 0x04, 0xC8)},
                {0x03, Color.FromArgb(0x52, 0x00, 0xB2)},
                {0x04, Color.FromArgb(0x73, 0x00, 0x76)},
                {0x05, Color.FromArgb(0x80, 0x00, 0x24)},
                {0x06, Color.FromArgb(0x73, 0x0B, 0x00)},
                {0x07, Color.FromArgb(0x52, 0x28, 0x00)},
                {0x08, Color.FromArgb(0x24, 0x44, 0x00)},
                {0x09, Color.FromArgb(0x00, 0x57, 0x00)},
                {0x0A, Color.FromArgb(0x00, 0x5C, 0x00)},
                {0x0B, Color.FromArgb(0x00, 0x53, 0x24)},
                {0x0C, Color.FromArgb(0x00, 0x3C, 0x76)},
                {0x0D, Color.FromArgb(0x00, 0x00, 0x00)},
                {0x0E, Color.FromArgb(0x00, 0x00, 0x00)},
                {0x0F, Color.FromArgb(0x00, 0x00, 0x00)},
                {0x10, Color.FromArgb(0xAB, 0xAB, 0xAB)},
                {0x11, Color.FromArgb(0x0D, 0x57, 0xFF)},
                {0x12, Color.FromArgb(0x4B, 0x30, 0xFF)},
                {0x13, Color.FromArgb(0x8A, 0x13, 0xFF)},
                {0x14, Color.FromArgb(0xBC, 0x08, 0xD6)},
                {0x15, Color.FromArgb(0xD2, 0x12, 0x69)},
                {0x16, Color.FromArgb(0xC7, 0x2E, 0x00)},
                {0x17, Color.FromArgb(0x9D, 0x54, 0x00)},
                {0x18, Color.FromArgb(0x60, 0x7B, 0x00)},
                {0x19, Color.FromArgb(0x20, 0x98, 0x00)},
                {0x1A, Color.FromArgb(0x00, 0xA3, 0x00)},
                {0x1B, Color.FromArgb(0x00, 0x99, 0x42)},
                {0x1C, Color.FromArgb(0x00, 0x7D, 0xB4)},
                {0x1D, Color.FromArgb(0x00, 0x00, 0x00)},
                {0x1E, Color.FromArgb(0x00, 0x00, 0x00)},
                {0x1F, Color.FromArgb(0x00, 0x00, 0x00)},
                {0x20, Color.FromArgb(0xFF, 0xFF, 0xFF)},
                {0x21, Color.FromArgb(0x53, 0xAE, 0xFF)},
                {0x22, Color.FromArgb(0x90, 0x85, 0xFF)},
                {0x23, Color.FromArgb(0xD3, 0x65, 0xFF)},
                {0x24, Color.FromArgb(0xFF, 0x57, 0xFF)},
                {0x25, Color.FromArgb(0xFF, 0x5D, 0xCF)},
                {0x26, Color.FromArgb(0xFF, 0x77, 0x57)},
                {0x27, Color.FromArgb(0xFA, 0x9E, 0x00)},
                {0x28, Color.FromArgb(0xBD, 0xC7, 0x00)},
                {0x29, Color.FromArgb(0x7A, 0xE7, 0x00)},
                {0x2A, Color.FromArgb(0x43, 0xF6, 0x11)},
                {0x2B, Color.FromArgb(0x26, 0xEF, 0x7E)},
                {0x2C, Color.FromArgb(0x2C, 0xD5, 0xF6)},
                {0x2D, Color.FromArgb(0x4E, 0x4E, 0x4E)},
                {0x2E, Color.FromArgb(0x00, 0x00, 0x00)},
                {0x2F, Color.FromArgb(0x00, 0x00, 0x00)},
                {0x30, Color.FromArgb(0xFF, 0xFF, 0xFF)},
                {0x31, Color.FromArgb(0xB6, 0xE1, 0xFF)},
                {0x32, Color.FromArgb(0xCE, 0xD1, 0xFF)},
                {0x33, Color.FromArgb(0xE9, 0xC3, 0xFF)},
                {0x34, Color.FromArgb(0xFF, 0xBC, 0xFF)},
                {0x35, Color.FromArgb(0xFF, 0xBD, 0xF4)},
                {0x36, Color.FromArgb(0xFF, 0xC6, 0xC3)},
                {0x37, Color.FromArgb(0xFF, 0xD5, 0x9A)},
                {0x38, Color.FromArgb(0xE9, 0xE6, 0x81)},
                {0x39, Color.FromArgb(0xCE, 0xF4, 0x81)},
                {0x3A, Color.FromArgb(0xB6, 0xFB, 0x9A)},
                {0x3B, Color.FromArgb(0xA9, 0xFA, 0xC3)},
                {0x3C, Color.FromArgb(0xA9, 0xF0, 0xF4)},
                {0x3D, Color.FromArgb(0xB8, 0xB8, 0xB8)},
                {0x3E, Color.FromArgb(0x00, 0x00, 0x00)},
                {0x3F, Color.FromArgb(0x00, 0x00, 0x00)}
            };

            #endregion

            #region PPU Register
            private class Register {
                public byte PPUCTRL = 0;
                public byte PPUMASK = 0;
                public byte PPUSTATUS = 0;
                public byte OAMADDR = 0;
            }
            #endregion

            #region Memory
            private class Memory {
                public byte[] VRAM = new byte[0x4000];
            }
            #endregion

            /// <summary> PPUレジスタ </summary>
            Register Reg = new Register();

            /// <summary> メモリ </summary>
            Memory Mem = new Memory();

            /// <summary> CPUのサイクル数 </summary>
            int cycle = 0;

            /// <summary> 現在のスキャンライン </summary>
            int line = 0;

            public void SetCHRROM(byte[] chrRom) {
                // CHRROMの設定
                for (int i = 0; i < chrRom.Length; i++) {
                    // CHRROMのアドレスは0x0000～0x1FFF
                    // 0x0000～0x0FFFは背景パターンテーブル1
                    // 0x1000～0x1FFFは背景パターンテーブル2
                    Mem.VRAM[i] = chrRom[i];
                }
            }

            /// <summary>
            /// PPUADDRへの書込み回数
            /// </summary>
            byte ppu_addr_write_cnt = 0;
            /// <summary>
            /// PPUADDR 
            /// </summary>
            ushort ppu_addr = 0;

            public byte DebugReadPPUCTRL { get => Reg.PPUCTRL; }
            public byte DebugReadPPUMASK { get => Reg.PPUMASK; }
            public byte DebugReadPPUSTATUS { get => Reg.PPUSTATUS; }

            private byte ReadVRAM(ushort address) {
                return Mem.VRAM[address];
            }

            private void WriteVRAM(ushort address, byte data) {
                Mem.VRAM[address] = data;
            }

            /// <summary>
            /// VRAMへの書き込み
            /// </summary>
            /// <param name="address"></param>
            /// <param name="data"></param>
            public void WriteVRAMFromCPU(ushort address, byte data) {
                //DebugLog($"WriteVRAMFromRegister: 0x{address.ToString("X4")}, 0x{data.ToString("X2")}");
                switch (address) {
                    case 0x2000:
                        Reg.PPUCTRL = data;
                        break;
                    case 0x2001:
                        Reg.PPUMASK = data;
                        break;
                    case 0x2002:
                        // 読み込みのみ
                        break;
                    case 0x2003:
                        // OAMADDR
                        Reg.OAMADDR = data;
                        break;
                    case 0x2004:
                        // OAMDATA
                        // OAMADDRの値を使ってOAMDATAに書き込む
                        WriteVRAM(Reg.OAMADDR, data);
                        Reg.OAMADDR++;
                        break;
                    case 0x2005:
                        // TODO
                        //// PPUSCROLL
                        //if (ppu_addr_write_cnt == 0) {
                        //    ppu_addr = (ushort)(data << 8);
                        //} else {
                        //    ppu_addr |= data;
                        //}
                        //ppu_addr_write_cnt++;
                        //Mem.VRAM[address] = data;
                        break;
                    case 0x2006:
                        // PPUADDR
                        if (ppu_addr_write_cnt == 0) {
                            // 1回目の書き込み: 上位バイト
                            ppu_addr = (ushort)((ppu_addr & 0x00FF) | (data << 8));
                            ppu_addr_write_cnt = 1;
                        } else {
                            // 2回目の書き込み: 下位バイト -> トグルをクリア
                            ppu_addr = (ushort)((ppu_addr & 0xFF00) | data);
                            ppu_addr_write_cnt = 0;
                        }
                        //Mem.VRAM[address] = data;
                        break;
                    case 0x2007:
                        // PPUDATA
                        WriteVRAM((ushort)(ppu_addr & 0x3FFF), data);
                        // PPUADDRのインクリメント (PPUCTRL bit2: 0->+1, 1->+32)
                        ushort inc = (ushort)(((Reg.PPUCTRL & 0x04) != 0) ? 32 : 1);
                        ppu_addr = (ushort)((ppu_addr + inc) & 0x3FFF);
                        break;
                    default:
                        // Mem.VRAM[address] = data;
                        break;
                }
            }

            byte ppu_read_buffer = 0;
            /// <summary>
            /// VRAMの読み込み
            /// </summary>
            /// <param name="address"></param>
            /// <returns></returns>
            public byte ReadVRAMFromCPU(ushort address) {
                if (address == 0x2002) {
                    // PPUSTATUS
                    byte ret = Reg.PPUSTATUS;
                    // VBlankフラグをクリア
                    Reg.PPUSTATUS &= 0x7F;
                    // 0x2005/0x2006 の書込み順序(アドレス/スクロールラッチ)をクリア
                    ppu_addr_write_cnt = 0;

                    return ret;
                } else if (address == 0x2007) {
                    // PPUDATA
                    // パレットテーブル以外はバッファを噛まして値を返す
                    byte ret;
                    ushort addr14 = (ushort)(ppu_addr & 0x3FFF);
                    if (addr14 >= 0x3F00) {
                        // パレットはバッファを介さない
                        ret = ReadVRAM(addr14);
                    } else {
                        ret = ppu_read_buffer;
                        ppu_read_buffer = ReadVRAM(addr14);
                    }
                    // PPUADDRのインクリメント
                    DebugLog($"ReadVRAM 0x2007: {ret}");
                    ushort inc = (ushort)(((Reg.PPUCTRL & 0x04) != 0) ? 32 : 1);
                    ppu_addr = (ushort)((ppu_addr + inc) & 0x3FFF);
                    return ret;
                }
                return Mem.VRAM[address];
            }

            public void Run(int cycle) {
                this.cycle += cycle;

                // 341サイクルごとに1ライン描画
                if (this.cycle >= 341) {
                    this.cycle -= 341;

                    // 1ライン描画
                    line++;

                    // 描画処理
                    //if (line <= 241 && line % 8 == 0) {
                    if (line == 240) {
                        //DebugLog($"タイルの描画");
                        // タイルの描画
                        DrawBackground();
                    }

                    if (line == 241) {
                        DebugLog("VBlank Start");
                        // VBlank開始
                        Reg.PPUSTATUS |= 0x80;
                        if ((Reg.PPUCTRL & 0x80) > 1) {
                            // NMI割り込み
                            cpu.NMI();
                        }
                    }

                    if (line == 261) {
                        DebugLog("VBlank End");
                        // VBlank終了
                        // VBlankの終了時にPPUSTATUSのVBlankフラグをクリア
                        Reg.PPUSTATUS &= 0x7F;
                        line = 0;
                    }
                }
            }

            int tileY = 0;

            private void DrawBackground() {
                //Xタイルは32個, Yタイルは30個
                // Pixelsは[y,x]
                Color[,] pixels = new Color[8 * 30, 8 * 32];
                int[,] intPixels = new int[8 * 30, 8 * 32];

                for (int tileY = 0; tileY < 30; tileY++) {
                    for (int tileX = 0; tileX < 32; tileX++) {
                        (var tile, var paletteID) = BuildTile(tileX, tileY);
                        // タイルの描画
                        for (int y = 0; y < 8; y++) {
                            for (int x = 0; x < 8; x++) {
                                int shift = 7 - x;
                                // lowとhighを合成
                                var color = (tile[y + 8] >> shift) & 0x01;
                                color |= ((tile[y] >> shift) & 0x01) << 1;
                                // colorの値は0～3
                                var colorId = ReadVRAM((ushort)(0x3F00 + (paletteID * 4) + color));

                                var colorValue = colorDictionary[colorId];

                                // 配列にDotの色を格納
                                pixels[tileY * 8 + y, tileX * 8 + x] = colorValue;
                                // デバッグ用
                                intPixels[tileY * 8 + y, tileX * 8 + x] = color;
                            }
                        }
                    }
                }

                // 画面に描画
                videoComponent.DrawScreen(pixels, DOT_SIZE_X, DOT_SIZE_Y);

                //StringBuilder sb = new StringBuilder();
                //sb.AppendLine();
                //for (int y = 0; y < intPixels.GetLength(0); y++) {
                //    for (int x = 0; x < intPixels.GetLength(1); x++) {
                //        var color = intPixels[y, x];
                //        sb.Append(color);
                //    }
                //    sb.AppendLine();
                //}
                //DebugLog(sb.ToString());
            }

            private (byte[], int) BuildTile(int tileX, int tileY) {
                var nameTableId = tileY * 30 + tileX;

                var patternID = GetPatternID(tileX, tileY);
                var attribute = GetAttribute(tileX, tileY);
                var paletteID = GetPalleteID(tileX, tileY, attribute);

                //if(patternID != 0)
                //    DebugLog($"pattern: {patternID.ToString("X2")}, X: {tileX.ToString("D2")}, Y: {tileY.ToString("D2")}");
                
                //タイル作成
                var blockAddrOffset = (ushort)((Reg.PPUCTRL & 0x10) > 0 ? 0x1000 : 0x0000);
                byte[] low = new byte[8];
                byte[] high = new byte[8];
                // パターンテーブルのアドレスは0x0000～または0x1000~
                for (int i = 0; i < 8; i++) {
                    low[i] = ReadVRAM((ushort)(blockAddrOffset + patternID * 16 + i));
                    high[i] = ReadVRAM((ushort)(blockAddrOffset + patternID * 16 + i + 8));
                }

                byte[] tile = new byte[16];
                for (int i = 0; i < tile.Length; i++) {
                    tile[i] = i < 8 ? low[i] : high[i - 8] ;
                }
                //byte[] tile = low.Concat(high).ToArray();

                //var spriteID = GetSpriteID(tileX, tileY);
                //var sprite = GetSprite(spriteID, blockID); 

                return (tile, paletteID);
            }

            private byte GetPatternID(int x, int y) {
                // TODO
                // スタート地点は0x2000, 0x2400, 0x2800,0x2C00から、サイズは0x03BF(960)
                // どのパターンテーブルから取得するかはregisterから取得する
                //var addr = (ushort)(0x2000 + (y * 0x1F) + x);
                var addr = (ushort)(0x2000 + (y * 0x20) + x);
                return ReadVRAM(addr);
            }

            private byte GetAttribute(int x, int y) {
                // TODO
                // スタート地点は0x23C0, 0x27C0, 0x2BC0,0x2FC0から、サイズは0x0040(64)
                // どの属性テーブルから取得するかはregisterから取得する。
                var baseAddr = 0x2000 + (Reg.PPUCTRL & 0x03) * 0x0400 + 0x03C0;
                var addr = (ushort)(baseAddr + (y / 4) * 8 + (x / 4));
                return ReadVRAM(addr);
            }

            private int GetPalleteID(int x, int y, byte attribute) {
                // 4x4タイル（32x32ピクセル）内での相対位置を計算
                var attrX = (x / 2) % 2;  // 0 or 1
                var attrY = (y / 2) % 2;  // 0 or 1
                var num = attrY * 2 + attrX;
                var id = num switch {
                    0b00 => (byte)(attribute & 0b11),
                    0b01 => (byte)((attribute >> 2) & 0b11),
                    0b10 => (byte)((attribute >> 4) & 0b11),
                    0b11 => (byte)((attribute >> 6) & 0b11),
                    _ => throw new Exception("Invalid attribute")
                };
                return id;
            }

            private byte GetSpriteID(int x, int y) {
                // TODO
                return 0;
            }

            private byte[] GetSprite(byte spriteID, byte blockID) {
                // TODO
                return [];
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

using DxLibDLL;
using MS.WindowsAPICodePack.Internal;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BoxNESharp {
    internal partial class BoxNESharp {
        class CPU {
            PPU ppu = PPU.GetInstance();
            Controller controller = Controller.GetInstance();

            #region Singlton
            private static CPU _instance = new CPU();

            public static CPU GetInstance() {
                return _instance;
            }
            #endregion

            #region 内部クラス
            private class DebugData {
                public Register reg = new();
                public Operand operand = default!;
                public StringBuilder read = new();
                public StringBuilder addressOrData = new();

                public void SetReg(Register reg) {
                    this.reg.Set(reg);
                }

                public void Clear() {
                    operand = default!;
                    read.Clear();
                    addressOrData.Clear();
                }
            }

            /// <summary>
            /// メモリ
            /// </summary>
            private class Memory {
                public byte[] RAM = new byte[0x10000];

                //public byte[] WRAM = new byte[0x0800];
                //public byte[] PPU = new byte[0x0008];
                //public byte[] APU = new byte[0x0020];
                //public byte[] EXTROM = new byte[0x1FE0];
                //public byte[] EXTRAM = new byte[0x2000];
                //public byte[] PRGROM = new byte[0x4000];
                //public byte[] PRGRAM = new byte[0x4000];
            }

            /// <summary>
            /// CPU Register
            /// </summary>
            private class Register {
                /// <summary>
                /// Accumulator
                /// </summary>
                public byte A = 0;
                /// <summary>
                /// Index Register X
                /// </summary>
                public byte X = 0;
                /// <summary>
                /// Index Register Y
                /// </summary>
                public byte Y = 0;
                /// <summary>
                /// Program Counter
                /// </summary>
                public ushort PC = 0;
                /// <summary>
                /// Stack Pointer
                /// </summary>
                public byte SP = 0;
                /// <summary>
                /// Processer Status Register
                /// </summary>
                public byte P = 0b00100000; // 5bit目は常に1

                public bool Negative { get => (P & 0b10000000) > 0; set => P = (byte)((P & ~(0b10000000)) | (value ? 1 : 0) << 7); }
                public bool Overflow { get => (P & 0b01000000) > 0; set => P = (byte)((P & ~(0b01000000)) | (value ? 1 : 0) << 6); }
                public bool Break { get => (P & 0b00010000) > 0; set => P = (byte)((P & ~(0b00010000)) | (value ? 1 : 0) << 4); }
                public bool Decimal { get => (P & 0b00001000) > 0; set => P = (byte)((P & ~(0b00001000)) | (value ? 1 : 0) << 3); }
                public bool Interrupt { get => (P & 0b00000100) > 0; set => P = (byte)((P & ~(0b00000100)) | (value ? 1 : 0) << 2); }
                public bool Zero { get => (P & 0b00000010) > 0; set => P = (byte)((P & ~(0b00000010)) | (value ? 1 : 0) << 1); }
                public bool Carry { get => (P & 0b00000001) > 0; set => P = (byte)((P & ~(0b00000001)) | (value ? 1 : 0)); }

                public Register() { }

                public void Set(Register reg) {
                    this.A = reg.A;
                    this.X = reg.X;
                    this.Y = reg.Y;
                    this.PC = reg.PC;
                    this.SP = reg.SP;
                    this.P = reg.P;
                }
            }
            #endregion

            #region クラス初期化
            private Memory Mem = new();
            private Register Reg = new();
            #endregion

            #region enum
            public enum Instruction {
                ADC,
                AND,
                ASL,
                BCC,
                BCS,
                BEQ,
                BIT,
                BMI,
                BNE,
                BPL,
                BRK,
                BVC,
                BVS,
                CLC,
                CLD,
                CLI,
                CLV,
                CMP,
                CPX,
                CPY,
                DEC,
                DEX,
                DEY,
                EOR,
                INC,
                INX,
                INY,
                JMP,
                JSR,
                LDA,
                LDX,
                LDY,
                LSR,
                NOP,
                ORA,
                PHA,
                PHP,
                PLA,
                PLP,
                ROL,
                ROR,
                RTI,
                RTS,
                SBC,
                SEC,
                SED,
                SEI,
                STA,
                STX,
                STY,
                TAX,
                TAY,
                TSX,
                TXA,
                TXS,
                TYA
            }

            public enum AddressingMode {
                Accumulator,
                Immediate,
                ZeroPage,
                ZeroPageX,
                ZeroPageY,
                Absolute,
                AbsoluteX,
                AbsoluteY,
                Implied,
                Relative,
                Indirect,
                IndirectX,
                IndirectY,
            }
            #endregion

            #region オペランド

            public struct Operand {
                public Instruction Instruction;
                public AddressingMode AddressingMode;
                public int Cycle;

                public Operand(Instruction instruction, AddressingMode addressingMode, int cycle) {
                    this.Instruction = instruction;
                    this.AddressingMode = addressingMode;
                    this.Cycle = cycle;
                }
            }

            Dictionary<byte, Operand> operandDic = new() {
                // ADC
                { 0x69, new Operand(Instruction.ADC, AddressingMode.Immediate, 2) },
                { 0x65, new Operand(Instruction.ADC, AddressingMode.ZeroPage,3) },
                { 0x75, new Operand(Instruction.ADC, AddressingMode.ZeroPageX,4) },
                { 0x6D, new Operand(Instruction.ADC, AddressingMode.Absolute,4) },
                { 0x7D, new Operand(Instruction.ADC, AddressingMode.AbsoluteX,4) },
                { 0x79, new Operand(Instruction.ADC, AddressingMode.AbsoluteY,4) },
                { 0x61, new Operand(Instruction.ADC, AddressingMode.IndirectX,6) },
                { 0x71, new Operand(Instruction.ADC, AddressingMode.IndirectY,5) },

                // AND
                { 0x29, new Operand(Instruction.AND, AddressingMode.Immediate,2) },
                { 0x25, new Operand(Instruction.AND, AddressingMode.ZeroPage,3) },
                { 0x35, new Operand(Instruction.AND, AddressingMode.ZeroPageX,4) },
                { 0x2D, new Operand(Instruction.AND, AddressingMode.Absolute,4) },
                { 0x3D, new Operand(Instruction.AND, AddressingMode.AbsoluteX,4) },
                { 0x39, new Operand(Instruction.AND, AddressingMode.AbsoluteY,4) },
                { 0x21, new Operand(Instruction.AND, AddressingMode.IndirectX,6) },
                { 0x31, new Operand(Instruction.AND, AddressingMode.IndirectY,5) },

                // ASL
                { 0x0A, new Operand(Instruction.ASL, AddressingMode.Accumulator,2) },
                { 0x06, new Operand(Instruction.ASL, AddressingMode.ZeroPage,5) },
                { 0x16, new Operand(Instruction.ASL, AddressingMode.ZeroPageX,6) },
                { 0x0E, new Operand(Instruction.ASL, AddressingMode.Absolute,6) },
                { 0x1E, new Operand(Instruction.ASL, AddressingMode.AbsoluteX,7) },

                // BCC
                { 0x90, new Operand(Instruction.BCC, AddressingMode.Relative,2) },
                // BCS
                { 0xB0, new Operand(Instruction.BCS, AddressingMode.Relative,2) },
                // BEQ
                { 0xF0, new Operand(Instruction.BEQ, AddressingMode.Relative,2) },
                // BIT
                { 0x24, new Operand(Instruction.BIT, AddressingMode.ZeroPage,3) },
                { 0x2C, new Operand(Instruction.BIT, AddressingMode.Absolute,4) },
                // BMI
                { 0x30, new Operand(Instruction.BMI, AddressingMode.Relative,2) },
                // BNE
                { 0xD0, new Operand(Instruction.BNE, AddressingMode.Relative,2) },
                // BPL
                { 0x10, new Operand(Instruction.BPL, AddressingMode.Relative,2) },
                // BRK
                { 0x00, new Operand(Instruction.BRK, AddressingMode.Implied,7) },
                // BVC
                { 0x50, new Operand(Instruction.BVC, AddressingMode.Relative,2) },
                // BVS
                { 0x70, new Operand(Instruction.BVS, AddressingMode.Relative,2) },
                // CLC
                { 0x18, new Operand(Instruction.CLC, AddressingMode.Implied,2) },
                // CLD
                { 0xD8, new Operand(Instruction.CLD, AddressingMode.Implied,2) },
                // CLI
                { 0x58, new Operand(Instruction.CLI, AddressingMode.Implied,2) },
                // CLV
                { 0xB8, new Operand(Instruction.CLV, AddressingMode.Implied,2) },

                // CMP
                { 0xC9, new Operand(Instruction.CMP, AddressingMode.Immediate,2) },
                { 0xC5, new Operand(Instruction.CMP, AddressingMode.ZeroPage,3) },
                { 0xD5, new Operand(Instruction.CMP, AddressingMode.ZeroPageX,4) },
                { 0xCD, new Operand(Instruction.CMP, AddressingMode.Absolute,4) },
                { 0xDD, new Operand(Instruction.CMP, AddressingMode.AbsoluteX,4) },
                { 0xD9, new Operand(Instruction.CMP, AddressingMode.AbsoluteY,4) },
                { 0xC1, new Operand(Instruction.CMP, AddressingMode.IndirectX,6) },
                { 0xD1, new Operand(Instruction.CMP, AddressingMode.IndirectY,5) },

                // CPX
                { 0xE0, new Operand(Instruction.CPX, AddressingMode.Immediate,2) },
                { 0xE4, new Operand(Instruction.CPX, AddressingMode.ZeroPage,3) },
                { 0xEC, new Operand(Instruction.CPX, AddressingMode.Absolute,4) },

                // CPY
                { 0xC0, new Operand(Instruction.CPY, AddressingMode.Immediate,2) },
                { 0xC4, new Operand(Instruction.CPY, AddressingMode.ZeroPage,3) },
                { 0xCC, new Operand(Instruction.CPY, AddressingMode.Absolute,4) },

                // DEC
                { 0xC6, new Operand(Instruction.DEC, AddressingMode.ZeroPage,5) },
                { 0xD6, new Operand(Instruction.DEC, AddressingMode.ZeroPageX,6) },
                { 0xCE, new Operand(Instruction.DEC, AddressingMode.Absolute,6) },
                { 0xDE, new Operand(Instruction.DEC, AddressingMode.AbsoluteX,7) },

                // DEX
                { 0xCA, new Operand(Instruction.DEX, AddressingMode.Implied,2) },
                // DEY
                { 0x88, new Operand(Instruction.DEY, AddressingMode.Implied,2) },

                // EOR
                { 0x49, new Operand(Instruction.EOR, AddressingMode.Immediate,2) },
                { 0x45, new Operand(Instruction.EOR, AddressingMode.ZeroPage,3) },
                { 0x55, new Operand(Instruction.EOR, AddressingMode.ZeroPageX,4) },
                { 0x4D, new Operand(Instruction.EOR, AddressingMode.Absolute,4) },
                { 0x5D, new Operand(Instruction.EOR, AddressingMode.AbsoluteX,4) },
                { 0x59, new Operand(Instruction.EOR, AddressingMode.AbsoluteY,4) },
                { 0x41, new Operand(Instruction.EOR, AddressingMode.IndirectX,6) },
                { 0x51, new Operand(Instruction.EOR, AddressingMode.IndirectY,5) },

                // INC
                { 0xE6, new Operand(Instruction.INC, AddressingMode.ZeroPage,5) },
                { 0xF6, new Operand(Instruction.INC, AddressingMode.ZeroPageX,6) },
                { 0xEE, new Operand(Instruction.INC, AddressingMode.Absolute,6) },
                { 0xFE, new Operand(Instruction.INC, AddressingMode.AbsoluteX,7) },

                // INX
                { 0xE8, new Operand(Instruction.INX, AddressingMode.Implied,2) },
                // INY
                { 0xC8, new Operand(Instruction.INY, AddressingMode.Implied,2) },

                // JMP
                { 0x4C, new Operand(Instruction.JMP, AddressingMode.Absolute,3) },
                { 0x6C, new Operand(Instruction.JMP, AddressingMode.Indirect,5) },
                // JSR
                { 0x20, new Operand(Instruction.JSR, AddressingMode.Absolute,6) },

                // LDA
                { 0xA9, new Operand(Instruction.LDA, AddressingMode.Immediate,2) },
                { 0xA5, new Operand(Instruction.LDA, AddressingMode.ZeroPage,3) },
                { 0xB5, new Operand(Instruction.LDA, AddressingMode.ZeroPageX,4) },
                { 0xAD, new Operand(Instruction.LDA, AddressingMode.Absolute,4) },
                { 0xBD, new Operand(Instruction.LDA, AddressingMode.AbsoluteX,4) },
                { 0xB9, new Operand(Instruction.LDA, AddressingMode.AbsoluteY,4) },
                { 0xA1, new Operand(Instruction.LDA, AddressingMode.IndirectX,6) },
                { 0xB1, new Operand(Instruction.LDA, AddressingMode.IndirectY,5) },

                // LDX
                { 0xA2, new Operand(Instruction.LDX, AddressingMode.Immediate,2) },
                { 0xA6, new Operand(Instruction.LDX, AddressingMode.ZeroPage,3) },
                { 0xB6, new Operand(Instruction.LDX, AddressingMode.ZeroPageY,4) },
                { 0xAE, new Operand(Instruction.LDX, AddressingMode.Absolute,4) },
                { 0xBE, new Operand(Instruction.LDX, AddressingMode.AbsoluteY,4) },

                // LDY
                { 0xA0, new Operand(Instruction.LDY, AddressingMode.Immediate,2) },
                { 0xA4, new Operand(Instruction.LDY, AddressingMode.ZeroPage,3) },
                { 0xB4, new Operand(Instruction.LDY, AddressingMode.ZeroPageX,4) },
                { 0xAC, new Operand(Instruction.LDY, AddressingMode.Absolute,4) },
                { 0xBC, new Operand(Instruction.LDY, AddressingMode.AbsoluteX,4) },

                // LSR
                { 0x4A, new Operand(Instruction.LSR, AddressingMode.Accumulator,2) },
                { 0x46, new Operand(Instruction.LSR, AddressingMode.ZeroPage,5) },
                { 0x56, new Operand(Instruction.LSR, AddressingMode.ZeroPageX,6) },
                { 0x4E, new Operand(Instruction.LSR, AddressingMode.Absolute,6) },
                { 0x5E, new Operand(Instruction.LSR, AddressingMode.AbsoluteX,7) },

                // NOP
                { 0xEA, new Operand(Instruction.NOP, AddressingMode.Implied,2) },
                { 0x04, new Operand(Instruction.NOP, AddressingMode.ZeroPage,3) },
                { 0x44, new Operand(Instruction.NOP, AddressingMode.ZeroPage,3) },
                { 0x64, new Operand(Instruction.NOP, AddressingMode.ZeroPage,3) },
                { 0x0C, new Operand(Instruction.NOP, AddressingMode.Absolute,3) },
                { 0x14, new Operand(Instruction.NOP, AddressingMode.ZeroPageX,4) },
                { 0x34, new Operand(Instruction.NOP, AddressingMode.ZeroPageX,4) },
                { 0x54, new Operand(Instruction.NOP, AddressingMode.ZeroPageX,4) },
                { 0x74, new Operand(Instruction.NOP, AddressingMode.ZeroPageX,4) },
                { 0xD4, new Operand(Instruction.NOP, AddressingMode.ZeroPageX,4) },
                { 0xF4, new Operand(Instruction.NOP, AddressingMode.ZeroPageX,4) },

                // ORA
                { 0x09, new Operand(Instruction.ORA, AddressingMode.Immediate,2) },
                { 0x05, new Operand(Instruction.ORA, AddressingMode.ZeroPage,3) },
                { 0x15, new Operand(Instruction.ORA, AddressingMode.ZeroPageX,4) },
                { 0x0D, new Operand(Instruction.ORA, AddressingMode.Absolute,4) },
                { 0x1D, new Operand(Instruction.ORA, AddressingMode.AbsoluteX,4) },
                { 0x19, new Operand(Instruction.ORA, AddressingMode.AbsoluteY,4) },
                { 0x01, new Operand(Instruction.ORA, AddressingMode.IndirectX,6) },
                { 0x11, new Operand(Instruction.ORA, AddressingMode.IndirectY,5) },

                // PHA
                { 0x48, new Operand(Instruction.PHA, AddressingMode.Implied,3) },
                // PHP
                { 0x08, new Operand(Instruction.PHP, AddressingMode.Implied,3) },
                // PLA
                { 0x68, new Operand(Instruction.PLA, AddressingMode.Implied,4) },
                // PLP
                { 0x28, new Operand(Instruction.PLP, AddressingMode.Implied,4) },

                // ROL
                { 0x2A, new Operand(Instruction.ROL, AddressingMode.Accumulator,2) },
                { 0x26, new Operand(Instruction.ROL, AddressingMode.ZeroPage,5) },
                { 0x36, new Operand(Instruction.ROL, AddressingMode.ZeroPageX,6) },
                { 0x2E, new Operand(Instruction.ROL, AddressingMode.Absolute,6) },
                { 0x3E, new Operand(Instruction.ROL, AddressingMode.AbsoluteX,7) },

                // ROR
                { 0x6A, new Operand(Instruction.ROR, AddressingMode.Accumulator,2) },
                { 0x66, new Operand(Instruction.ROR, AddressingMode.ZeroPage,5) },
                { 0x76, new Operand(Instruction.ROR, AddressingMode.ZeroPageX,6) },
                { 0x6E, new Operand(Instruction.ROR, AddressingMode.Absolute,6) },
                { 0x7E, new Operand(Instruction.ROR, AddressingMode.AbsoluteX,7) },

                // RTI
                { 0x40, new Operand(Instruction.RTI, AddressingMode.Implied,6) },
                // RTS
                { 0x60, new Operand(Instruction.RTS, AddressingMode.Implied,6) },

                // SBC
                { 0xE9, new Operand(Instruction.SBC, AddressingMode.Immediate,2) },
                { 0xE5, new Operand(Instruction.SBC, AddressingMode.ZeroPage,3) },
                { 0xF5, new Operand(Instruction.SBC, AddressingMode.ZeroPageX,4) },
                { 0xED, new Operand(Instruction.SBC, AddressingMode.Absolute,4) },
                { 0xFD, new Operand(Instruction.SBC, AddressingMode.AbsoluteX,4) },
                { 0xF9, new Operand(Instruction.SBC, AddressingMode.AbsoluteY,4) },
                { 0xE1, new Operand(Instruction.SBC, AddressingMode.IndirectX,6) },
                { 0xF1, new Operand(Instruction.SBC, AddressingMode.IndirectY,5) },

                // SEC
                { 0x38, new Operand(Instruction.SEC, AddressingMode.Implied,2) },
                // SED
                { 0xF8, new Operand(Instruction.SED, AddressingMode.Implied,2) },
                // SEI
                { 0x78, new Operand(Instruction.SEI, AddressingMode.Implied,2) },

                // STA
                { 0x85, new Operand(Instruction.STA, AddressingMode.ZeroPage,3) },
                { 0x95, new Operand(Instruction.STA, AddressingMode.ZeroPageX,4) },
                { 0x8D, new Operand(Instruction.STA, AddressingMode.Absolute,4) },
                { 0x9D, new Operand(Instruction.STA, AddressingMode.AbsoluteX,5) },
                { 0x99, new Operand(Instruction.STA, AddressingMode.AbsoluteY,5) },
                { 0x81, new Operand(Instruction.STA, AddressingMode.IndirectX,6) },
                { 0x91, new Operand(Instruction.STA, AddressingMode.IndirectY,6) },

                // STX
                { 0x86, new Operand(Instruction.STX, AddressingMode.ZeroPage,3) },
                { 0x96, new Operand(Instruction.STX, AddressingMode.ZeroPageY,4) },
                { 0x8E, new Operand(Instruction.STX, AddressingMode.Absolute,4) },

                // STY
                { 0x84, new Operand(Instruction.STY, AddressingMode.ZeroPage,3) },
                { 0x94, new Operand(Instruction.STY, AddressingMode.ZeroPageX,4) },
                { 0x8C, new Operand(Instruction.STY, AddressingMode.Absolute,4) },

                // TAX
                { 0xAA, new Operand(Instruction.TAX, AddressingMode.Implied,2) },
                // TAY
                { 0xA8, new Operand(Instruction.TAY, AddressingMode.Implied,2) },
                // TSX
                { 0xBA, new Operand(Instruction.TSX, AddressingMode.Implied,2) },
                // TXA
                { 0x8A, new Operand(Instruction.TXA, AddressingMode.Implied,2) },
                // TXS
                { 0x9A, new Operand(Instruction.TXS, AddressingMode.Implied,2) },
                // TYA
                { 0x98, new Operand(Instruction.TYA, AddressingMode.Implied,2) },
            };
            #endregion

            public void SetRom(byte[] rom, int mapper) {
                int headerSize = 0x0010;
                var prgSize = (rom[4] * 0x4000);  // 16KB units
                var chrSize = (rom[5] * 0x2000);  // 8KB units

                var chrStartIndex = headerSize + prgSize;
                var chrEndIndex = chrStartIndex + chrSize;

                DebugLog($"PRGROM Size: {prgSize.ToString()} (0x{prgSize.ToString("X4")})");
                DebugLog($"CHRROM Size: {chrSize.ToString()} (0x{chrSize.ToString("X4")})");

                DebugLog($"PRG Index Start: 0x{headerSize.ToString("X4")} End: 0x{(chrStartIndex - 1).ToString("X4")}");
                DebugLog($"CHR Index Start: 0x{chrStartIndex.ToString("X4")} End: 0x{chrEndIndex.ToString("X4")}");

                // TODO : Mapper別の対応 現在は0だけ
                Array.Copy(rom, headerSize, Mem.RAM, 0x8000, prgSize);
                if (rom[4] == 1) {
                    Array.Copy(rom, headerSize, Mem.RAM, 0xC000, prgSize);
                }

                ppu.SetCHRROM(rom[chrStartIndex..chrEndIndex]);

                // リセット処理
                Reset();
                // BRKの7サイクル
                Cycle += 7;

#if NESTEST
                //nestestのテスト用
                Reg.PC = 0xC000;
#endif
            }

            public void DebugExportRAM() {
                DebugLog("");
                DebugLog("CPU RAM DATA");
                StringBuilder sb = new();
                for (int i = 0; i < Mem.RAM.Length; i++) {
                    if (i % 16 == 0) {
                        sb.Append($"0x{i.ToString("X4")}: ");
                    }
                    sb.Append(Mem.RAM[i].ToString("X2"));
                    sb.Append(" ");
                    if (i % 16 == 15) {
                        DebugLog(sb.ToString());
                        sb.Clear();
                    }
                }
                DebugLog(sb.ToString());
            }

            DebugData debugData = new();

            /// <summary>
            /// メイン処理　1クロックごと
            /// </summary>
            public int Fetch() {
                debugData.Clear();
                debugData.SetReg(Reg);

                // 命令コードの取得
                var opeCode = Read(Reg.PC);
                Reg.PC++;

                // オペランドの取得
                var operand = operandDic[opeCode];

                // アドレスまたはデータの取得
                ushort baseAddress = 0;
                ushort addressOrData = 0;
                switch (operand.AddressingMode) {
                    case AddressingMode.Accumulator:
                    case AddressingMode.Implied:
                        break;

                    case AddressingMode.Relative:
                        addressOrData = Reg.PC;
                        Reg.PC++;
                        break;

                    case AddressingMode.Immediate:
                        addressOrData = Read(Reg.PC);
                        Reg.PC++;
                        break;

                    case AddressingMode.Absolute:
                        addressOrData = ReadWord(Reg.PC);
                        Reg.PC += 2;
                        break;

                    case AddressingMode.AbsoluteX:
                        baseAddress = ReadWord(Reg.PC);
                        addressOrData = (ushort)(baseAddress + Reg.X);
                        Reg.PC += 2;
                        break;

                    case AddressingMode.AbsoluteY:
                        baseAddress = ReadWord(Reg.PC);
                        addressOrData = (ushort)(baseAddress + Reg.Y);
                        Reg.PC += 2;
                        break;

                    case AddressingMode.ZeroPage:
                        addressOrData = Read(Reg.PC);
                        Reg.PC++;
                        break;

                    case AddressingMode.ZeroPageX:
                        baseAddress = Read(Reg.PC);
                        addressOrData = (ushort)((baseAddress + Reg.X) % 0x100);
                        Reg.PC++;
                        break;

                    case AddressingMode.ZeroPageY:
                        baseAddress = Read(Reg.PC);
                        addressOrData = (ushort)((baseAddress + Reg.Y) % 0x100);
                        Reg.PC++;
                        break;

                    case AddressingMode.Indirect:
                        baseAddress = ReadWord(Reg.PC);
                        addressOrData = (ushort)(Read(baseAddress)
                                        |  Read((ushort)((baseAddress & 0xFF00) | ((baseAddress + 1) & 0x00FF))) << 8);
                        Reg.PC += 2;
                        break;

                    case AddressingMode.IndirectX:
                        baseAddress = Read(Reg.PC);
                        addressOrData = (ushort)(Read((ushort)((baseAddress + Reg.X) % 0x100))
                                        | (Read((ushort)((baseAddress + Reg.X + 1) % 0x100)) << 8));
                        Reg.PC++;
                        break;

                    case AddressingMode.IndirectY:
                        baseAddress = Read(Reg.PC);
                        addressOrData = (ushort)((Read(baseAddress) 
                                        |  (Read((ushort)((baseAddress + 1) % 0x100)) << 8)) + Reg.Y);
                        Reg.PC++;
                        break;
                }

                // 命令実行
                switch (operand.Instruction) {
                    case Instruction.ADC:
                        ADC(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.SBC:
                        SBC(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.AND:
                        AND(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.ORA:
                        ORA(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.EOR:
                        EOR(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.ASL:
                        ASL(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.LSR:
                        LSR(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.ROL:
                        ROL(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.ROR:
                        ROR(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.BCC:
                        BCC(addressOrData);
                        break;
                    case Instruction.BCS:
                        BCS(addressOrData);
                        break;
                    case Instruction.BEQ:
                        BEQ(addressOrData);
                        break;
                    case Instruction.BNE:
                        BNE(addressOrData);
                        break;
                    case Instruction.BVC:
                        BVC(addressOrData);
                        break;
                    case Instruction.BVS:
                        BVS(addressOrData);
                        break;
                    case Instruction.BPL:
                        BPL(addressOrData);
                        break;
                    case Instruction.BMI:
                        BMI(addressOrData);
                        break;
                    case Instruction.BIT:
                        BIT(addressOrData);
                        break;
                    case Instruction.JMP:
                        JMP(addressOrData);
                        break;
                    case Instruction.JSR:
                        JSR(addressOrData);
                        break;
                    case Instruction.RTS:
                        RTS();
                        break;
                    case Instruction.BRK:
                        BRK();
                        break;
                    case Instruction.RTI:
                        RTI();
                        break;
                    case Instruction.CMP:
                        CMP(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.CPX:
                        CPX(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.CPY:
                        CPY(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.INC:
                        INC(addressOrData);
                        break;
                    case Instruction.DEC:
                        DEC(addressOrData);
                        break;
                    case Instruction.INX:
                        INX();
                        break;
                    case Instruction.INY:
                        INY();
                        break;
                    case Instruction.DEX:
                        DEX();
                        break;
                    case Instruction.DEY:
                        DEY();
                        break;
                    case Instruction.CLC:
                        CLC();
                        break;
                    case Instruction.SEC:
                        SEC();
                        break;
                    case Instruction.CLI:
                        CLI();
                        break;
                    case Instruction.SEI:
                        SEI();
                        break;
                    case Instruction.CLD:
                        CLD();
                        break;
                    case Instruction.SED:
                        SED();
                        break;
                    case Instruction.CLV:
                        CLV();
                        break;
                    case Instruction.LDA:
                        LDA(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.LDX:
                        LDX(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.LDY:
                        LDY(operand.AddressingMode, addressOrData);
                        break;
                    case Instruction.STA:
                        STA(addressOrData);
                        break;
                    case Instruction.STX:
                        STX(addressOrData);
                        break;
                    case Instruction.STY:
                        STY(addressOrData);
                        break;
                    case Instruction.TAX:
                        TAX();
                        break;
                    case Instruction.TXA:
                        TXA();
                        break;
                    case Instruction.TAY:
                        TAY();
                        break;
                    case Instruction.TYA:
                        TYA();
                        break;
                    case Instruction.TSX:
                        TSX();
                        break;
                    case Instruction.TXS:
                        TXS();
                        break;
                    case Instruction.PHA:
                        PHA();
                        break;
                    case Instruction.PLA:
                        PLA();
                        break;
                    case Instruction.PHP:
                        PHP();
                        break;
                    case Instruction.PLP:
                        PLP();
                        break;
                    case Instruction.NOP:
                        NOP();
                        break;
                }

                debugData.operand = operand;

                debugData.addressOrData.Append(addressOrData.ToString("X4"));

                // デバッグログ
                CPU_DebugLog(debugData);

                return operand.Cycle;
            }

            int logNum = 1;
            private void CPU_DebugLog(DebugData debugData) {
                StringBuilder sb = new();
                sb.Append($"{logNum++.ToString("D4")}:");

                sb.Append($" {debugData.reg.PC.ToString("X4")}");
                sb.Append($" {debugData.read.ToString().PadRight(9, ' ')}");
                sb.Append($" {debugData.operand.Instruction}");
                sb.Append($" {debugData.addressOrData.ToString().PadRight(6,' ')}");
                sb.Append($"A:{debugData.reg.A.ToString("X2")}");
                sb.Append($" X:{debugData.reg.X.ToString("X2")}");
                sb.Append($" Y:{debugData.reg.Y.ToString("X2")}");
                sb.Append($" P:{debugData.reg.P.ToString("X2")}");
                sb.Append($" SP:{debugData.reg.SP.ToString("X2")}");
                sb.Append($" CYC:{Cycle.ToString().PadRight(5, ' ')}");
                sb.Append($" {debugData.operand.AddressingMode}");

                //sb.Append($" \t");
                //sb.Append($" PPU CTRL: {ppu.DebugReadPPUCTRL.ToString("X4")}");
                //sb.Append($" MASK: {ppu.DebugReadPPUMASK.ToString("X4")}");
                //sb.Append($" STAT: {ppu.DebugReadPPUSTATUS.ToString("X4")}");

                DebugLog(sb.ToString());
            }

            #region Read/Write
            /// <summary>
            /// 1バイト読込み
            /// </summary>
            /// <returns></returns>
            byte Read(ushort address) {
                byte data;
                if((0x2000 <= address && address <= 0x2007) || address == 0x4014) {
                    data = ppu.ReadVRAMFromCPU(address);
                } else if(address == 0x4016 || address == 0x4017) {
                    data = controller.ReadController(address);
                } else {
                    data = Mem.RAM[address];
                }
#if CPULOG
                if(debugData.read.Length < 9) debugData.read.Append($"{data.ToString("X2")} ");
#endif
                return data;
            }

            /// <summary>
            /// 2バイト読込み
            /// </summary>
            /// <returns></returns>
            ushort ReadWord(ushort address) {
                return (ushort)(Read(address) | (Read((ushort)(address + 1)) << 8));
            }

            /// <summary>
            /// 書込み
            /// </summary>
            /// <param name="address"></param>
            /// <param name="data"></param>
            void Write(ushort address, byte data) {
                if ((0x2000 <= address && address <= 0x2007) || address == 0x4014) {
                    ppu.WriteVRAMFromCPU(address, data);
                } else if(address == 0x4016 || address == 0x4017) {
                    controller.WriteController(address, data);
                } else {
                    Mem.RAM[address] = data;
                }
            }
            #endregion

            #region Pop/Push
            void Push(byte data) {
                Write((ushort)(0x0100 | Reg.SP), data);
                //DebugLog($"Push: {data.ToString("X2")} to 0x{Reg.SP.ToString("X2")}");
                Reg.SP--;
            }

            byte Pop() {
                Reg.SP++;
                var result = Read((ushort)(0x0100 | Reg.SP));
                //DebugLog($"Pop: {result.ToString("X2")} from 0x{Reg.SP.ToString("X2")}");
                return result;
            }
            #endregion

            #region Instruction

            #region Calculation
            void ADC(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                var result = (ushort)(Reg.A + data + (Reg.Carry ? 1 : 0));
                var resultByte = (byte)(result & 0xFF);
                Reg.Carry = result > 0xFF;
                Reg.Zero = resultByte == 0;
                Reg.Overflow = ((resultByte ^ Reg.A) & (resultByte ^ data) & 0x80) > 0;
                Reg.Negative = (resultByte & 0x80) > 0;
                Reg.A = resultByte;
            }

            void SBC(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                var result = Reg.A - data - (!Reg.Carry ? 1 : 0);
                var resultByte = (byte)(result & 0xFF);
                Reg.Carry = !(result < 0);
                Reg.Zero = resultByte == 0;
                Reg.Overflow = (byte)((resultByte ^ Reg.A) & (resultByte ^ ~data) & 0x80) > 0;
                Reg.Negative = (resultByte & 0x80) > 0;
                Reg.A = resultByte;
            }
            #endregion

            #region Logic
            void AND(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                var result = (byte)(Reg.A & data);
                Reg.Zero = result == 0;
                Reg.Negative = (result & 0x80) > 0;
                Reg.A = result;
            }

            void ORA(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                var result = (byte)(Reg.A | data);
                Reg.Zero = result == 0;
                Reg.Negative = (result & 0x80) > 0;
                Reg.A = result;
            }

            void EOR(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                var result = (byte)(Reg.A ^ data);
                Reg.Zero = result == 0;
                Reg.Negative = (result & 0x80) > 0;
                Reg.A = result;
            }
            #endregion

            #region Shift and Lotation
            void ASL(AddressingMode mode, ushort address) {
                if(mode == AddressingMode.Accumulator) {
                    var result = (byte)(Reg.A << 1);
                    Reg.Carry = (Reg.A & 0x80) > 0;
                    Reg.Zero = result == 0;
                    Reg.Negative = (result & 0x80) > 0;
                    Reg.A = result;
                } else {
                    byte data = Read(address);
                    var result = (byte)(data << 1);
                    Reg.Carry = (data & 0x80) > 0;
                    Reg.Zero = result == 0;
                    Reg.Negative = (result & 0x80) > 0;
                    Write(address, result);
                }
            }

            void LSR(AddressingMode mode, ushort address) {
                if (mode == AddressingMode.Accumulator) {
                    var result = (byte)(Reg.A >> 1);
                    Reg.Carry = (Reg.A & 0x01) > 0;
                    Reg.Zero = result == 0;
                    Reg.Negative = (result & 0x80) > 0;
                    Reg.A = result;
                } else {
                    var data = Read(address);
                    var result = (byte)(data >> 1);
                    Reg.Carry = (data & 0x01) > 0;
                    Reg.Zero = result == 0;
                    Reg.Negative = (result & 0x80) > 0;
                    Write(address, result);
                }
            }

            void ROL(AddressingMode mode, ushort address) {
                if (mode == AddressingMode.Accumulator) {
                    var result = (byte)((Reg.A << 1) | (Reg.Carry ? 1 : 0));
                    Reg.Carry = (Reg.A & 0x80) > 0;
                    Reg.Zero = result == 0;
                    Reg.Negative = (result & 0x80) > 0;
                    Reg.A = result;
                } else {
                    var data = Read(address);
                    var result = (byte)((data << 1) | (Reg.Carry ? 1 : 0));
                    Reg.Carry = (data & 0x80) > 0;
                    Reg.Zero = result == 0;
                    Reg.Negative = (result & 0x80) > 0;
                    Write(address, result);
                }
            }

            void ROR(AddressingMode mode, ushort address) {
                if (mode == AddressingMode.Accumulator) {
                    var result = (byte)((Reg.A >> 1) | ((Reg.Carry ? 1 : 0) << 7));
                    Reg.Carry = (Reg.A & 0x01) > 0;
                    Reg.Zero = result == 0;
                    Reg.Negative = (result & 0x80) > 0;
                    Reg.A = result;
                } else {
                    var data = Read(address);
                    var result = (byte)((data >> 1) | (Reg.Carry ? 1 : 0) << 7);
                    Reg.Carry = (data & 0x01) > 0;
                    Reg.Zero = result == 0;
                    Reg.Negative = (result & 0x80) > 0;
                    Write(address, result);
                }
            }
            #endregion

            #region Branch
            void BCC(ushort address) {
                var data = Read(address);
                if (!Reg.Carry) {
                    Reg.PC = (ushort)(Reg.PC + ((data & 0x80) > 0 ? ((~data & 0x7F) + 1) * -1 : data));
                }
            }

            void BCS(ushort address) {
                var data = Read(address);
                if (Reg.Carry) {
                    Reg.PC = (ushort)(Reg.PC + ((data & 0x80) > 0 ? ((~data & 0x7F) + 1) * -1 : data));
                }
            }

            void BNE(ushort address) {
                var data = Read(address);
                if (!Reg.Zero) {
                    Reg.PC = (ushort)(Reg.PC + ((data & 0x80) > 0 ? ((~data & 0x7F) + 1) * -1 : data));
                }
            }

            void BEQ(ushort address) {
                var data = Read(address);
                if (Reg.Zero) {
                    Reg.PC = (ushort)(Reg.PC + ((data & 0x80) > 0 ? ((~data & 0x7F) + 1) * -1 : data));
                }
            }

            void BVC(ushort address) {
                var data = Read(address);
                if (!Reg.Overflow) {
                    Reg.PC = (ushort)(Reg.PC + ((data & 0x80) > 0 ? ((~data & 0x7F) + 1) * -1 : data));
                }
            }

            void BVS(ushort address) {
                var data = Read(address);
                if (Reg.Overflow) {
                    Reg.PC = (ushort)(Reg.PC + ((data & 0x80) > 0 ? ((~data & 0x7F) + 1) * -1 : data));
                }
            }

            void BPL(ushort address) {
                var data = Read(address);
                if (!Reg.Negative) {
                    Reg.PC = (ushort)(Reg.PC + ((data & 0x80) > 0 ? ((~data & 0x7F) + 1) * -1 : data));
                }
            }

            void BMI(ushort address) {
                var data = Read(address);
                if (Reg.Negative) {
                    Reg.PC = (ushort)(Reg.PC + ((data & 0x80) > 0 ? ((~data & 0x7F) + 1) * -1 : data));
                }
            }
            #endregion

            #region Bit Test
            void BIT(ushort address) {
                var data = Read(address);
                var result = (byte)(Reg.A & data);
                Reg.Overflow = (data & 0x40) > 0;
                Reg.Zero = result == 0;
                Reg.Negative = (data & 0x80) > 0;
            }
            #endregion

            #region Jump
            void JMP(ushort address) {
                Reg.PC = address;
            }

            void JSR(ushort address) {
                var data = Reg.PC - 1;
                Push((byte)((data & 0xFF00) >> 8));
                Push((byte)(data & 0x00FF));
                Reg.PC = address;
            }

            void RTS() {
                var low = Pop();
                var high = Pop();
                Reg.PC = (ushort)((high << 8 | low) + 1) ;
            }
            #endregion

            #region Interrupt
            void BRK() {
                if (Reg.Interrupt)
                    return;
                Reg.Break = true;
                Reg.PC++;
                Push((byte)((Reg.PC & 0xFF00) >> 8));
                Push((byte)(Reg.PC & 0x00FF));
                Push(Reg.P);
                Reg.Interrupt = true;
                Reg.PC = ReadWord(0xFFFE);
            }
            
            void RTI() {
                var status = Pop();
                byte mask = 0b11001111;
                byte temp1 = (byte)(status & mask);
                byte temp2 = (byte)(Reg.P & (byte)(~mask));
                Reg.P = (byte)(temp1 | temp2);

                var low = Pop();
                var high = Pop();
                Reg.PC = (ushort)((high << 8) | low);
            }

            public void NMI() {
                Reg.Break = false;
                Push((byte)((Reg.PC & 0xFF00) >> 8));
                Push((byte)(Reg.PC & 0x00FF));
                Push(Reg.P);
                Reg.Interrupt = true;
                Reg.PC = ReadWord(0xFFFA);
            }

            //void IRQ() {
            //    if (Reg.Interrupt)
            //        return;
            //    Reg.Break = false;
            //    //Reg.PC++;
            //    Push((byte)((Reg.PC & 0xFF00) >> 8));
            //    Push((byte)(Reg.PC & 0x00FF));
            //    Push(Reg.P);
            //    Reg.Interrupt = true;
            //    Reg.PC = ReadWord(0xFFFE);
            //}
            #endregion

            #region Compare
            void CMP(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                var result = (byte)(Reg.A - data);
                Reg.Carry = Reg.A >= data;
                Reg.Zero = Reg.A == data;
                Reg.Negative = (result & 0x80) > 0;
            }

            void CPX(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                var result = (byte)(Reg.X - data);
                Reg.Carry = Reg.X >= data;
                Reg.Zero = Reg.X == data;
                Reg.Negative = (result & 0x80) > 0;
            }

            void CPY(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                var result = (byte)(Reg.Y - data);
                Reg.Carry = Reg.Y >= data;
                Reg.Zero = Reg.Y == data;
                Reg.Negative = (result & 0x80) > 0;
            }
            #endregion

            #region Increment/Decrement
            void INC(ushort address) {
                var data = Read(address);
                var result = (byte)(data + 1);
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Write(address, result);
            }
            
            void DEC(ushort address) {
                var data = Read(address);
                var result = (byte)(data - 1);
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Write(address, result);
            }

            void INX() {
                var result = (byte)(Reg.X + 1);
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.X = result;
            }

            void DEX() {
                var result = (byte)(Reg.X - 1);
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.X = result;
            }

            void INY() {
                var result = (byte)(Reg.Y + 1);
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.Y = result;
            }

            void DEY() {
                var result = (byte)(Reg.Y - 1);
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.Y = result;
            }
            #endregion

            #region Flag Operation
            void CLC() {
                Reg.Carry = false;
            }

            void SEC() {
                Reg.Carry = true;
            }

            void CLI() {
                Reg.Interrupt = false;
            }

            void SEI() {
                Reg.Interrupt = true;
            }

            void CLD() {
                Reg.Decimal = false;
            }

            void SED() {
                Reg.Decimal = true;
            }

            void CLV() {
                Reg.Overflow = false;
            }
            #endregion

            #region Load/Store
            void LDA(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                Reg.Negative = (data & 0x80) > 0;
                Reg.Zero = data == 0;
                Reg.A = data;
            }

            void LDX(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                Reg.Negative = (data & 0x80) > 0;
                Reg.Zero = data == 0;
                Reg.X = data;
            }

            void LDY(AddressingMode mode, ushort addressOrData) {
                byte data = (byte)(mode == AddressingMode.Immediate ? addressOrData : Read(addressOrData));
                Reg.Negative = (data & 0x80) > 0;
                Reg.Zero = data == 0;
                Reg.Y = data;
            }

            void STA(ushort address) {
                Write(address, Reg.A);
            }

            void STX(ushort address) {
                Write(address, Reg.X);
            }

            void STY(ushort address) {
                Write(address, Reg.Y);
            }
            #endregion

            #region Register Transfar
            void TAX() {
                var result = Reg.A;
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.X = result;
            }

            void TXA() {
                var result = Reg.X;
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.A = result;
            }

            void TAY() {
                var result = Reg.A;
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.Y = result;
            }

            void TYA() {
                var result = Reg.Y;
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.A = result;
            }

            void TSX() {
                var result = Reg.SP;
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.X = result;
            }

            void TXS() {
                var result = Reg.X;
                Reg.SP = result;
            }
            #endregion

            #region Stack
            void PHA() {
                Push(Reg.A);
            }

            void PLA() {
                var result = Pop();
                Reg.Negative = (result & 0x80) > 0;
                Reg.Zero = result == 0;
                Reg.A = result;
            }
            
            void PHP() {
                //($0100 + SP) = NV11DIZC
                var result = (byte)(Reg.P | 0b00110000);
                Push(result);
            }

            void PLP() {
                var result = Pop();

                byte mask = 0b11001111;
                byte temp1 = (byte)(result & mask);
                byte temp2 = (byte)(Reg.P & (byte)(~mask));
                Reg.P = (byte)(temp1 | temp2);

                //Reg.Carry = (result & 1) > 0;
                //Reg.Zero = (result & (1 << 1)) > 0;
                //Reg.Interrupt = (result & (1 << 2 )) > 0;
                //Reg.Decimal = (result & (1 << 3)) > 0;
                //Reg.Overflow = (result & (1 << 6)) > 0;
                //Reg.Carry = (result & (1 << 7)) > 0;
                //Reg.P = result;
            }
            #endregion

            #region NOP
            void NOP() {
                // NOP
            }
            #endregion
            void Reset() {
                Reg.Interrupt = true;

                // PCを初期化
                Reg.PC = ReadWord(0xFFFC);
                // SPを初期化
                Reg.SP = 0xFD;
            }
            #endregion



        }
    }
}

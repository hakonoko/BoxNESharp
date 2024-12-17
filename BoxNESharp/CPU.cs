using DxLibDLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BoxNESharp {
    internal partial class BoxNESharp {
        class CPU {
            #region Singlton
            private static CPU _instance = new CPU();

            public static CPU GetInstance() {
                return _instance;
            }
            #endregion

            #region クラス
            /// <summary>
            /// メモリ
            /// </summary>
            private class Memory {
                public byte[] RAM = new byte[0xFFFF];

                public byte[] WRAM = new byte[0x0800];
                public byte[] PPU = new byte[0x0008];
                public byte[] APU = new byte[0x0020];
                public byte[] EXTROM = new byte[0x1FE0];
                public byte[] EXTRAM = new byte[0x2000];
                public byte[] PRGROM = new byte[0x4000];
                public byte[] PRGRAM = new byte[0x4000];
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

            Dictionary<byte, Operand> opeCodeDic = new() {
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
                { 0xCE, new Operand(Instruction.DEC, AddressingMode.AbsoluteX,7) },

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
                { 0xED, new Operand(Instruction.SBC, AddressingMode.AbsoluteX,4) },
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

            /// <summary>
            /// コンストラクタ
            /// </summary>
            private CPU() {
                Reset();
            }

            /// <summary>
            /// メイン処理　1クロックごと
            /// </summary>
            public void Clock() {

                var opeCode = Read(Reg.PC);
                Reg.PC--;

                for (int i = 0; i < 236; i++) {
                    DX.DrawFillBox(i * DotSizeW, i * DotSizeH, (i * DotSizeW) + DotSizeW, (i * DotSizeH) + DotSizeH, DX.GetColor(0, 255, 0));
                }

                DX.DrawString(0, 0, Reg.P.ToString("b8"), DX.GetColor(255, 255, 255));

                if (DX.CheckHitKey(DX.KEY_INPUT_1) > 0) {
                    Reg.Negative = !Reg.Negative;
                }
                if (DX.CheckHitKey(DX.KEY_INPUT_2) > 0) {
                    Reg.Overflow = !Reg.Overflow;
                }
                if (DX.CheckHitKey(DX.KEY_INPUT_3) > 0) {
                    Reg.Break = !Reg.Break;
                }
                if (DX.CheckHitKey(DX.KEY_INPUT_4) > 0) {
                    Reg.Decimal = !Reg.Decimal;
                }
                if (DX.CheckHitKey(DX.KEY_INPUT_5) > 0) {
                    Reg.Interrupt = !Reg.Interrupt;
                }
                if (DX.CheckHitKey(DX.KEY_INPUT_6) > 0) {
                    Reg.Zero = !Reg.Zero;
                }
                if (DX.CheckHitKey(DX.KEY_INPUT_7) > 0) {
                    Reg.Carry = !Reg.Carry;
                }
            }

            /// <summary>
            /// 1バイト読込
            /// </summary>
            /// <returns></returns>
            byte Read(ushort address) {
                return Mem.RAM[address];
            }

            /// <summary>
            /// 2バイト読込
            /// </summary>
            /// <returns></returns>
            ushort ReadWord(ushort address) {
                return (ushort)(Read(address) | (Read((ushort)(address + 1)) << 8));
            }

            void Reset() {
                Reg.Interrupt = true;

                // PCを初期化
                Reg.PC = 0xFFFD;

                Reg.PC = ReadWord(Reg.PC);
            }


        }
    }
}

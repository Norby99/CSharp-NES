﻿namespace CSharp_NES.Hardware.CPU
{
    /// <summary>
    /// Operations that the CPU can do
    /// NOTE: All the returns values indicates if the 
    /// selected instraction can require an other CPU
    /// cycle to execute. This doesn't mean that it
    /// will use an other cycle.
    /// </summary>
    internal class Opcodes
    {
        private ICPU CPU;
        private Registers RG;
        private InternalVar IV;

        private ushort stackPDef;
        private List<Instruction> Lookups;

        /// <summary>
        /// Links the opcodes to the CPU
        /// </summary>
        /// <param name="cpu"> The CPU to be connected to </param>
        /// <param name="rg"> The registers of the CPU </param>
        /// <param name="iv"> Internal variable of the CPU </param>
        /// <param name="stckPD"> The defualt stack pointer initial position </param>
        public Opcodes(ICPU cpu, Registers rg, InternalVar iv, ushort stckPD, List<Instruction> lookups)
        {
            CPU = cpu;
            RG = rg;
            IV = iv;

            stackPDef = stckPD;
            Lookups = lookups;
        }

        // OpcodesFN

        /// <summary>
        /// Adds two registers
        /// </summary>
        public byte ADC()
        {
            CPU.Fetch();
            // converting all data to uint16 to easly check if there was an overflow or underflow
            UInt16 temp = (UInt16)((UInt16)RG.A + (UInt16)IV.Fetched + (UInt16)CPU.GetFlag(FLAGS6502.C));
            CPU.SetFlag(FLAGS6502.C, temp > 255);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x80) != 0);   // can't convert int to bool workaround
            CPU.SetFlag(FLAGS6502.V, ((UInt16)(~((UInt16)RG.A ^ (UInt16)IV.Fetched) & ((UInt16)RG.A ^ temp)) & 0x0080) != 0);   // checking if there is an overflow or underflow + bool tweek

            RG.A = (byte)(temp & 0x00FF);
            return 1;
        }

        public byte AND()
        {
            CPU.Fetch();
            RG.A = (byte)(RG.A & IV.Fetched);
            CPU.SetFlag(FLAGS6502.Z, RG.A == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.A & 0x80) != 0);   // it checks if the 7th bit is a 1. The conversion to bool must be done in this fancy way (TODO: check if it can be done with a bool cast)
            return 1;
        }

        /// <summary>
        /// Arithmetic Shift left
        /// </summary>
        public byte ASL()
        {
            CPU.Fetch();
            UInt16 temp = (UInt16)(IV.Fetched << 1);
            CPU.SetFlag(FLAGS6502.C, (temp & 0xFF00) > 0);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x00);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x80) != 0);

            if (Lookups[IV.Opcode].Addressmode.Method.Name == "IMP")     //! not sure if this works
                RG.A = (byte)(temp & 0x00FF);
            else
                CPU.Write(IV.AddrAbs, (byte)(temp & 0x00FF));
            return 0;
        }

        /// <summary>
        /// Branch if Carry clear
        /// </summary>
        public byte BCC()
        {
            if (CPU.GetFlag(FLAGS6502.C) == 0)
            {
                IV.Cycles++;
                IV.AddrAbs = (ushort)(RG.PC + IV.AddrRel);

                if ((IV.AddrAbs & 0xFF00) != (RG.PC & 0xFF00))
                {
                    IV.Cycles++;
                }

                RG.PC = IV.AddrAbs;
            }

            return 0;
        }

        /// <summary>
        /// Branch if Carry is set
        /// </summary>
        public byte BCS()
        {
            if (CPU.GetFlag(FLAGS6502.C) == 1)
            {
                IV.Cycles++;
                IV.AddrAbs = (ushort)(RG.PC + IV.AddrRel);

                if ((IV.AddrAbs & 0xFF00) != (RG.PC & 0xFF00))
                {
                    IV.Cycles++;
                }

                RG.PC = IV.AddrAbs;
            }

            return 0;
        }

        /// <summary>
        /// Branch if equal
        /// </summary>
        public byte BEQ()
        {
            if (CPU.GetFlag(FLAGS6502.Z) == 1)
            {
                IV.Cycles++;
                IV.AddrAbs = (ushort)(RG.PC + IV.AddrRel);

                if ((IV.AddrAbs & 0xFF00) != (RG.PC & 0xFF00))
                {
                    IV.Cycles++;
                }

                RG.PC = IV.AddrAbs;
            }

            return 0;
        }

        /// <summary>
        /// BIT (short for "BIT test") is the mnemonic for a machine language instruction which
        /// tests specific bits in the contents of the address specified, and sets the zero,
        /// negative, and overflow flags accordingly, all without affecting the contents of
        /// the accumulator.
        /// Whilst BIT is mainly used for checking the state of particular bits in memory.
        /// </summary>
        public byte BIT()
        {
            CPU.Fetch();
            UInt16 temp = (UInt16)(RG.A & IV.Fetched);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x00);
            CPU.SetFlag(FLAGS6502.N, (IV.Fetched & (1 << 7)) != 0);
            CPU.SetFlag(FLAGS6502.V, (IV.Fetched & (1 << 6)) != 0);
            return 0;
        }

        /// <summary>
        /// Branch if negative
        /// </summary>
        public byte BMI()
        {
            if (CPU.GetFlag(FLAGS6502.N) == 1)
            {
                IV.Cycles++;
                IV.AddrAbs = (ushort)(RG.PC + IV.AddrRel);

                if ((IV.AddrAbs & 0xFF00) != (RG.PC & 0xFF00))
                {
                    IV.Cycles++;
                }

                RG.PC = IV.AddrAbs;
            }

            return 0;
        }

        /// <summary>
        /// Branch if not equal
        /// </summary>
        public byte BNE()
        {
            if (CPU.GetFlag(FLAGS6502.Z) == 0)
            {
                IV.Cycles++;
                IV.AddrAbs = (ushort)(RG.PC + IV.AddrRel);

                if ((IV.AddrAbs & 0xFF00) != (RG.PC & 0xFF00))
                {
                    IV.Cycles++;
                }

                RG.PC = IV.AddrAbs;
            }

            return 0;
        }

        /// <summary>
        /// Branch if positive
        /// </summary>
        public byte BPL()
        {
            if (CPU.GetFlag(FLAGS6502.N) == 0)
            {
                IV.Cycles++;
                IV.AddrAbs = (ushort)(RG.PC + IV.AddrRel);

                if ((IV.AddrAbs & 0xFF00) != (RG.PC & 0xFF00))
                {
                    IV.Cycles++;
                }

                RG.PC = IV.AddrAbs;
            }

            return 0;
        }

        /// <summary>
        /// Instruction: Break
        /// Program Sourced Interrupt
        /// </summary>
        public byte BRK()
        {
            RG.PC++;

            CPU.SetFlag(FLAGS6502.I, true);
            CPU.Write((ushort)(0x0100 + RG.STKP), (byte)((RG.PC >> 8) & 0x00FF));
            RG.STKP--;
            CPU.Write((ushort)(0x0100 + RG.STKP), (byte)(RG.PC & 0x00FF));
            RG.STKP--;

            CPU.SetFlag(FLAGS6502.B, true);
            CPU.Write((ushort)(0x0100 + RG.STKP), RG.Status);
            RG.STKP--;
            CPU.SetFlag(FLAGS6502.B, false);

            RG.PC = (ushort)((UInt16)CPU.Read(0xFFFE) | ((UInt16)CPU.Read(0xFFFF) << 8));
            return 0;
        }

        /// <summary>
        /// Branch if overflow
        /// </summary>
        public byte BVC()
        {
            if (CPU.GetFlag(FLAGS6502.V) == 0)
            {
                IV.Cycles++;
                IV.AddrAbs = (ushort)(RG.PC + IV.AddrRel);

                if ((IV.AddrAbs & 0xFF00) != (RG.PC & 0xFF00))
                {
                    IV.Cycles++;
                }

                RG.PC = IV.AddrAbs;
            }

            return 0;
        }

        /// <summary>
        /// Branch if not overflowed
        /// </summary>
        public byte BVS()
        {
            if (CPU.GetFlag(FLAGS6502.V) == 1)
            {
                IV.Cycles++;
                IV.AddrAbs = (ushort)(RG.PC + IV.AddrRel);

                if ((IV.AddrAbs & 0xFF00) != (RG.PC & 0xFF00))
                {
                    IV.Cycles++;
                }

                RG.PC = IV.AddrAbs;
            }

            return 0;
        }

        /// <summary>
        /// Clear the Carry bit
        /// </summary>
        public byte CLC()
        {
            CPU.SetFlag(FLAGS6502.C, false);
            return 0;
        }

        public byte CLD()
        {
            CPU.SetFlag(FLAGS6502.D, false);
            return 0;
        }

        /// <summary>
        /// Instruction: Disable Interrupts / Clear Interrupt Flag
        /// </summary>
        public byte CLI()
        {
            CPU.SetFlag(FLAGS6502.I, false);
            return 0;
        }

        /// <summary>
        /// Instruction: Clear Overflow Flag
        /// </summary>
        public byte CLV()
        {
            CPU.SetFlag(FLAGS6502.V, false);
            return 0;
        }

        /// <summary>
        /// Instruction: Compare Accumulator
        /// Function:    C <- A >= M      Z <- (A - M) == 0
        /// Flags Out:   N, C, Z
        /// </summary>
        public byte CMP()
        {
            CPU.Fetch();
            UInt16 temp = (UInt16)((UInt16)RG.A - (UInt16)IV.Fetched);
            CPU.SetFlag(FLAGS6502.C, RG.A >= IV.Fetched);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x0000);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x0080) != 0);
            return 1;
        }

        /// <summary>
        /// Instruction: Compare X Register
        /// Function:    C <- X >= M      Z <- (X - M) == 0
        /// Flags Out:   N, C, Z
        /// </summary>
        public byte CPX()
        {
            CPU.Fetch();
            UInt16 temp = (UInt16)((UInt16)RG.X - (UInt16)IV.Fetched);
            CPU.SetFlag(FLAGS6502.C, RG.X >= IV.Fetched);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x0000);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x0080) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Compare Y Register
        /// Function:    C <- Y >= M      Z <- (Y - M) == 0
        /// Flags Out:   N, C, Z
        /// </summary>
        public byte CPY()
        {
            CPU.Fetch();
            UInt16 temp = (UInt16)((UInt16)RG.Y - (UInt16)IV.Fetched);
            CPU.SetFlag(FLAGS6502.C, RG.Y >= IV.Fetched);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x0000);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x0080) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Decrement Value at Memory Location
        /// Function:    M = M - 1
        /// Flags Out:   N, Z
        /// </summary>
        public byte DEC()
        {
            CPU.Fetch();
            UInt16 temp = (UInt16)(IV.Fetched - 1);
            CPU.Write(IV.AddrAbs, (byte)(temp & 0x00FF));
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x0000);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x0080) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Decrement X Register
        /// Function:    X = X - 1
        /// Flags Out:   N, Z
        /// </summary>
        public byte DEX()
        {
            RG.X--;
            CPU.SetFlag(FLAGS6502.Z, RG.X == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.X & 0x80) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Decrement Y Register
        /// Function:    Y = Y - 1
        /// Flags Out:   N, Z
        /// </summary>
        public byte DEY()
        {
            RG.Y--;
            CPU.SetFlag(FLAGS6502.Z, RG.Y == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.Y & 0x80) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Bitwise Logic XOR
        /// Function:    A = A xor M
        /// Flags Out:   N, Z
        /// </summary>
        public byte EOR()
        {
            CPU.Fetch();
            RG.A = (byte)(RG.A ^ IV.Fetched);
            CPU.SetFlag(FLAGS6502.Z, RG.A == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.A & 0x80) != 0);
            return 1;
        }

        /// <summary>
        /// Instruction: Increment Value at Memory Location
        /// Function:    M = M + 1
        /// Flags Out:   N, Z
        /// </summary>
        public byte INC()
        {
            CPU.Fetch();
            byte temp = (byte)(IV.Fetched + 1);
            CPU.Write(IV.AddrAbs, (byte)(temp & 0x00FF));
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x0000);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x0080) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Increment X Register
        /// Function:    X = X + 1
        /// Flags Out:   N, Z
        /// </summary>
        public byte INX()
        {
            RG.X++;
            CPU.SetFlag(FLAGS6502.Z, RG.X == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.X & 0x80) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Increment Y Register
        /// Function:    Y = Y + 1
        /// Flags Out:   N, Z
        /// </summary>
        public byte INY()
        {
            RG.Y++;
            CPU.SetFlag(FLAGS6502.Z, RG.Y == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.Y & 0x80) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Jump To Location
        /// Function:    pc = address
        /// </summary>
        public byte JMP()
        {
            RG.PC = IV.AddrAbs;
            return 0;
        }

        /// <summary>
        /// Instruction: Jump To Sub-Routine
        /// Function:    Push current pc to stack, pc = address
        /// </summary>
        public byte JSR()
        {
            RG.PC--;

            CPU.Write((ushort)(0x0100 + RG.STKP), (byte)((RG.PC >> 8) & 0x00FF));
            RG.STKP--;
            CPU.Write((ushort)(0x0100 + RG.STKP), (byte)(RG.PC & 0x00FF));
            RG.STKP--;

            RG.PC = IV.AddrAbs;
            return 0;
        }

        /// <summary>
        /// Instruction: Load The Accumulator
        /// Function:    A = M
        /// Flags Out:   N, Z
        /// </summary>
        public byte LDA()
        {
            CPU.Fetch();
            
            RG.A = IV.Fetched;
            CPU.SetFlag(FLAGS6502.Z, RG.A == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.A & 0x80) != 0);

            return 1;
        }

        /// <summary>
        /// Instruction: Load The X Register
        /// Function:    X = M
        /// Flags Out:   N, Z
        /// </summary>
        public byte LDX()
        {
            CPU.Fetch();

            RG.X = IV.Fetched;
            CPU.SetFlag(FLAGS6502.Z, RG.X == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.X & 0x80) != 0);

            return 1;
        }

        /// <summary>
        /// Instruction: Load The Y Register
        /// Function:    Y = M
        /// Flags Out:   N, Z
        /// </summary>
        public byte LDY()
        {
            CPU.Fetch();

            RG.Y = IV.Fetched;
            CPU.SetFlag(FLAGS6502.Z, RG.Y == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.Y & 0x80) != 0);

            return 1;
        }

        public byte LSR()
        {
            CPU.Fetch();

            CPU.SetFlag(FLAGS6502.C, (IV.Fetched & 0x0001) != 0);
            UInt16 temp = (UInt16)(IV.Fetched >> 1);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x0000);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x0080) != 0);

            if (Lookups[IV.Opcode].Addressmode.Method.Name == "IMP")     //! not sure if this works
            {
                RG.A = (byte)(temp & 0x00FF);
            }
            else
            {
                CPU.Write(IV.AddrAbs, (byte)(temp & 0x00FF));
            }

            return 0;
        }

        /// <summary>
        /// No Operation.
        /// There are a lot of them, I've implemented only a couple of them.
        /// </summary>
        public byte NOP()
        {
            byte loops = 0;

            switch (IV.Opcode)
            {
                case 0x1C:
                case 0x3C:
                case 0x5C:
                case 0x7C:
                case 0xDC:
                case 0xFC:
                    loops = 1;
                    break;
            }

            return loops;
        }

        /// <summary>
        /// Instruction: Bitwise Logic OR
        /// Function:    A = A | M
        /// Flags Out:   N, Z
        /// </summary>
        public byte ORA()
        {
            CPU.Fetch();

            RG.A = (byte)(RG.A | IV.Fetched);
            CPU.SetFlag(FLAGS6502.Z, RG.A == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.A & 0x80) != 0);

            return 1;
        }

        /// <summary>
        /// Push the accumulator to the stack
        /// </summary>
        public byte PHA()
        {
            CPU.Write((ushort)(stackPDef + RG.STKP), RG.A);
            RG.STKP--;
            return 0;
        }

        /// <summary>
        /// Instruction: Push Status Register to Stack
        /// Function:    status -> stack
        /// Note:        Break flag is set to 1 before push
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public byte PHP()
        {
            CPU.Write((ushort)(0x0100 + RG.STKP), (byte)(RG.Status | (byte)FLAGS6502.B | (byte)FLAGS6502.U));
            CPU.SetFlag(FLAGS6502.B, false);
            CPU.SetFlag(FLAGS6502.U, false);
            RG.STKP--;

            return 0;
        }

        /// <summary>
        /// Pops data from the stack and adds it to the accumulator
        /// </summary>
        public byte PLA()
        {
            RG.STKP++;
            RG.A = CPU.Read((ushort)(stackPDef + RG.STKP));
            CPU.SetFlag(FLAGS6502.Z, RG.A == 0x0000);
            CPU.SetFlag(FLAGS6502.N, (RG.A & 0x0080) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Pop Status Register off Stack
        /// Function:    Status <- stack
        /// </summary>
        public byte PLP()
        {
            RG.STKP++;
            RG.Status = CPU.Read((ushort)(0x0100 + RG.STKP));
            CPU.SetFlag(FLAGS6502.U, true);

            return 0;
        }

        public byte ROL()
        {
            CPU.Fetch();

            UInt16 temp = (UInt16)((IV.Fetched << 1) | CPU.GetFlag(FLAGS6502.C));
            CPU.SetFlag(FLAGS6502.C, (temp & 0xFF00) != 0);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x0000);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x0080) != 0);

            if (Lookups[IV.Opcode].Addressmode.Method.Name == "IMP")     //! not sure if this works
            {
                RG.A = (byte)(temp & 0x00FF);
            }
            else
            {
                CPU.Write(IV.AddrAbs, (byte)(temp & 0x00FF));
            }

            return 0;
        }

        public byte ROR()
        {
            CPU.Fetch();

            UInt16 temp = (UInt16)((CPU.GetFlag(FLAGS6502.C) << 7) | (IV.Fetched >> 1));
            CPU.SetFlag(FLAGS6502.C, (IV.Fetched & 0x01) != 0);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0x00);
            CPU.SetFlag(FLAGS6502.N, (temp & 0x0080) != 0);

            if (Lookups[IV.Opcode].Addressmode.Method.Name == "IMP")     //! not sure if this works
            {
                RG.A = (byte)(temp & 0x00FF);
            }
            else
            {
                CPU.Write(IV.AddrAbs, (byte)(temp & 0x00FF));
            }

            return 0;
        }

        /// <summary>
        /// After servicing an interrupt,
        /// this function returns from it
        /// </summary>
        public byte RTI()
        {
            RG.STKP++;
            RG.Status = CPU.Read((ushort)(stackPDef + RG.STKP));
            RG.Status = (byte)(RG.Status & ~(byte)FLAGS6502.B);
            RG.Status = (byte)(RG.Status & ~(byte)FLAGS6502.U);

            RG.STKP++;
            RG.PC = CPU.Read((ushort)(stackPDef + RG.STKP));
            RG.STKP++;
            RG.PC = (ushort)(RG.PC | CPU.Read((ushort)(stackPDef + RG.STKP)) << 8);

            return 0;
        }

        public byte RTS()
        {
            RG.STKP++;
            RG.PC = CPU.Read((ushort)(0x0100 + RG.STKP));
            RG.STKP++;
            RG.PC = (ushort)(RG.PC | CPU.Read((ushort)(0x0100 + RG.STKP)) << 8);
            RG.PC++;

            return 0;
        }

        /// <summary>
        /// Subtracts two registers
        /// </summary>
        public byte SBC()
        {
            CPU.Fetch();

            // inverting the value to be negative so I can add the two registers together
            UInt16 value = (UInt16)((UInt16)IV.Fetched ^ 0x00FF);

            // converting all data to uint16 to easly check if there was an overflow or underflow
            UInt16 temp = (UInt16)((UInt16)RG.A + (UInt16)value + (UInt16)CPU.GetFlag(FLAGS6502.C));
            CPU.SetFlag(FLAGS6502.C, (temp & 0xFF00) != 0);
            CPU.SetFlag(FLAGS6502.Z, (temp & 0x00FF) == 0);
            CPU.SetFlag(FLAGS6502.V, ((UInt16)((temp ^ (UInt16)RG.A) & (temp ^ value)) & 0x0080) != 0);   // checking if there is an overflow or underflow + bool tweek
            CPU.SetFlag(FLAGS6502.N, (temp & 0x0080) != 0);   // can't convert int to bool workaround

            RG.A = (byte)(temp & 0x00FF);
            return 1;
        }

        /// <summary>
        /// Instruction: Set Carry Flag
        /// Function:    C = 1
        /// </summary>
        public byte SEC()
        {
            CPU.SetFlag(FLAGS6502.C, true);
            return 0;
        }

        /// <summary>
        /// Instruction: Set Decimal Flag
        /// Function:    D = 1
        /// </summary>
        public byte SED()
        {
            CPU.SetFlag(FLAGS6502.D, true);
            return 0;
        }

        /// <summary>
        /// Instruction: Set Interrupt Flag / Enable Interrupts
        /// Function:    I = 1
        /// </summary>
        public byte SEI()
        {
            CPU.SetFlag(FLAGS6502.I, true);
            return 0;
        }

        /// <summary>
        /// Instruction: Store Accumulator at Address
        ///  Function:    M = A
        /// </summary>
        public byte STA()
        {
            CPU.Write(IV.AddrAbs, RG.A);
            return 0;
        }

        /// <summary>
        /// Instruction: Store X Register at Address
        /// Function:    M = X
        /// </summary>
        public byte STX()
        {
            CPU.Write(IV.AddrAbs, RG.X);
            return 0;
        }

        /// <summary>
        /// Instruction: Store Y Register at Address
        /// Function:    M = Y
        /// </summary>
        public byte STY()
        {
            CPU.Write(IV.AddrAbs, RG.Y);
            return 0;
        }

        /// <summary>
        /// Instruction: Transfer Accumulator to X Register
        /// Function:    X = A
        /// Flags Out:   N, Z
        /// </summary>
        public byte TAX()
        {
            RG.X = RG.A;
            CPU.SetFlag(FLAGS6502.Z, RG.X == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.X & 0x80) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Transfer Accumulator to Y Register
        /// Function:    Y = A
        /// Flags Out:   N, Z
        /// </summary>
        public byte TAY()
        {
            RG.Y = RG.A;
            CPU.SetFlag(FLAGS6502.Z, RG.Y == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.Y & 0x80) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Transfer Stack Pointer to X Register
        /// Function:    X = stack pointer
        /// Flags Out:   N, Z
        /// </summary>
        public byte TSX()
        {
            RG.X = RG.STKP;
            CPU.SetFlag(FLAGS6502.Z, RG.X == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.X & 0x80) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Transfer X Register to Accumulator
        /// Function:    A = X
        /// Flags Out:   N, Z
        /// </summary>
        public byte TXA()
        {
            RG.A = RG.X;
            CPU.SetFlag(FLAGS6502.Z, RG.A == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.A & 0x80) != 0);
            return 0;
        }

        /// <summary>
        /// Instruction: Transfer X Register to Stack Pointer
        /// Function:    stack pointer = X
        /// </summary>
        public byte TXS()
        {
            RG.STKP = RG.X;
            return 0;
        }

        /// <summary>
        /// Instruction: Transfer Y Register to Accumulator
        /// Function:    A = Y
        /// Flags Out:   N, Z
        /// </summary>
        public byte TYA()
        {
            RG.A = RG.Y;
            CPU.SetFlag(FLAGS6502.Z, RG.A == 0x00);
            CPU.SetFlag(FLAGS6502.N, (RG.A & 0x80) != 0);
            return 0;
        }

        // Illigal OpcodesFN
        public byte XXX()
        {
            throw new NotImplementedException();
        }
    }
}

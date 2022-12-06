﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Emulator
{
    public class CPU
    {
        private List<byte> m_rom;
        private int m_PC;//Program Counter: This is the current instruction pointer. 16-bit register.
        private ushort SP;// Stack Pointer. 16-bit register
        private ushort A;// Accumulator. 8-bit register
        private ushort B;// Register B. 8-bit register
        private ushort C;// Register C. 8-bit register
        private ushort D;// Register D. 8-bit register
        private ushort E;// Register E. 8-bit register
        private ushort H;// Register H. 8-bit register
        private ushort L;// Register L. 8-bit register
        private ushort BC;// Virtual register BC (16-bit) combinaison of registers B and C
        private ushort DE;// Virtual register DE (16-bit) combinaison of registers D and E
        private ushort HL;// Virtual register HL (16-bit) combinaison of registers H and L

        private ushort SIGN = 0;        // Sign flag
        private ushort ZERO = 0;        // Zero flag
        private ushort HALFCARRY = 0;   //Half-carry (or Auxiliary Carry) flag
        private bool PARITY = false;    //Parity flag
        private ushort CARRY = 0;       //Carry flag

        private bool INTERRUPT = false; //Interrupt Enabled flag
        private bool CRASHED;           //Special flag that tells if the CPU is currently crashed (stopped)

        private int instruction_per_frame = 4000; // Approximate real machine speed

        // Interrupt handling
        private int interrupt_alternate = 0;
        private int half_instruction_per_frame = 0;

        // Addionnal debug fields, not used by CPU
        private byte BIT0 = 1;
        private byte BIT4 = 16;
        private byte BIT5 = 32;
        private byte BIT6 = 64;
        private byte BIT7 = 128;

        private ushort m_source = 0;
        private ushort m_value = 0;
        private byte m_byte = 0;

        private int m_instructionCounter = 0;

        private IO m_io;
        private Label m_label;

        public CPU(List<byte> rom, IO io, Label label)
        {
            m_PC = 0;
            m_rom = rom;
            m_io = io;
            m_label = label;
            half_instruction_per_frame = instruction_per_frame / 2;
            Reset();
        }

        public void Run()
        {
            for (int i = 0; i < instruction_per_frame; i++)
            {
                ExecuteInstruction();
            }
        }

        // All opcodes are 1 byte wide
        public void ExecuteInstruction()
        {
            if (!CRASHED)
            {
                m_byte = FetchRomByte();

                switch (m_byte)
                {
                    case 0x00:
                        NOP();
                        break;
                    case 0xc3:
                    case 0xc2:
                    case 0xca:
                    case 0xd2:
                    case 0xda:
                    case 0xf2:
                    case 0xfa:
                        Instruction_JMP(m_byte);
                        break;
                    case 0x01:
                    case 0x11:
                    case 0x21:
                    case 0x31:
                        Instruction_LXI(m_byte);
                        break;
                    case 0x3e:
                    case 0x06:
                    case 0x0e:
                    case 0x16:
                    case 0x1e:
                    case 0x26:
                    case 0x2e:
                    case 0x36:
                        Instruction_MVI(m_byte);
                        break;
                    case 0xcd:
                    case 0xc4:
                    case 0xcc:
                    case 0xd4:
                    case 0xdc:
                        Instruction_CALL(m_byte);
                        break;
                    case 0x0a:
                    case 0x1a:
                    case 0x3a:
                        Instruction_LDA(m_byte);
                        break;
                    case 0x77:
                    case 0x70:
                    case 0x71:
                    case 0x72:
                    case 0x73:
                    case 0x74:
                    case 0x75:
                        Instruction_MOVHL(m_byte);
                        break;
                    case 0x03:
                    case 0x13:
                    case 0x23:
                    case 0x33:
                        Instruction_INX(m_byte);
                        break;
                    case 0x0b:
                    case 0x1b:
                    case 0x2b:
                    case 0x3b:
                        Instruction_DCX(m_byte);
                        break;
                    case 0x3d:
                    case 0x05:
                    case 0x0d:
                    case 0x15:
                    case 0x1d:
                    case 0x25:
                    case 0x2d:
                    case 0x35:
                        Instruction_DEC(m_byte);
                        break;
                    case 0x3c:
                    case 0x04:
                    case 0x0c:
                    case 0x14:
                    case 0x1c:
                    case 0x24:
                    case 0x2c:
                    case 0x34:
                        Instruction_INC(m_byte);
                        break;
                    case 0xc9:
                    case 0xc0:
                    case 0xc8:
                    case 0xd0:
                    case 0xd8:
                        Instruction_RET(m_byte);
                        break;
                    case 0x7F:
                    case 0x78:
                    case 0x79:
                    case 0x7A:
                    case 0x7B:
                    case 0x7C:
                    case 0x7D:
                    case 0x7E:
                        Instruction_MOV(m_byte);
                        break;
                    case 0x47:
                    case 0x40:
                    case 0x41:
                    case 0x42:
                    case 0x43:
                    case 0x44:
                    case 0x45:
                    case 0x46:
                        Instruction_MOV(m_byte);
                        break;
                    case 0x4f:
                    case 0x48:
                    case 0x49:
                    case 0x4a:
                    case 0x4b:
                    case 0x4c:
                    case 0x4d:
                    case 0x4e:
                        Instruction_MOV(m_byte);
                        break;
                    case 0x57:
                    case 0x50:
                    case 0x51:
                    case 0x52:
                    case 0x53:
                    case 0x54:
                    case 0x55:
                    case 0x56:
                        Instruction_MOV(m_byte);
                        break;
                    case 0x5f:
                    case 0x58:
                    case 0x59:
                    case 0x5a:
                    case 0x5b:
                    case 0x5c:
                    case 0x5d:
                    case 0x5e:
                        Instruction_MOV(m_byte);
                        break;
                    case 0x67:
                    case 0x60:
                    case 0x61:
                    case 0x62:
                    case 0x63:
                    case 0x64:
                    case 0x65:
                    case 0x66:
                        Instruction_MOV(m_byte);
                        break;
                    case 0x6f:
                    case 0x68:
                    case 0x69:
                    case 0x6a:
                    case 0x6b:
                    case 0x6c:
                    case 0x6d:
                    case 0x6e:
                        Instruction_MOV(m_byte);
                        break;
                    case 0xbf:
                    case 0xb8:
                    case 0xb9:
                    case 0xba:
                    case 0xbb:
                    case 0xbc:
                    case 0xbd:
                    case 0xbe:
                    case 0xfe:
                        Instruction_CMP(m_byte);
                        break;
                    case 0xc5:
                    case 0xd5:
                    case 0xe5:
                    case 0xf5:
                        Instruction_PUSH(m_byte);
                        break;
                    case 0xc1:
                    case 0xd1:
                    case 0xe1:
                    case 0xf1:
                        Instruction_POP(m_byte);
                        break;
                    case 0x09:
                    case 0x19:
                    case 0x29:
                    case 0x39:
                        Instruction_DAD(m_byte);
                        break;
                    case 0xeb:
                        Instruction_XCHG();
                        break;
                    case 0xe3:
                        Instruction_XTHL();
                        break;
                    case 0xd3:
                        Instruction_OUTP();
                        break;
                    case 0xdb:
                        Instruction_INP();
                        break;
                    case 0xe9:
                        Instruction_PCHL(m_byte);
                        break;
                    case 0xc7:
                    case 0xcf:
                    case 0xd7:
                    case 0xdf:
                    case 0xe7:
                    case 0xef:
                    case 0xf7:
                    case 0xff:
                        Instruction_RST(m_byte);
                        break;
                    case 0x07:
                        Instruction_RLC(m_byte);
                        break;
                    case 0x17:
                        Instruction_RAL(m_byte);
                        break;
                    case 0x0f:
                        Instruction_RRC(m_byte);
                        break;
                    case 0x1f:
                        Instruction_RAR(m_byte);
                        break;
                    case 0xa7:
                    case 0xa0:
                    case 0xa1:
                    case 0xa2:
                    case 0xa3:
                    case 0xa4:
                    case 0xa5:
                    case 0xa6:
                    case 0xe6:
                        Instruction_AND(m_byte);
                        break;
                    case 0x80:
                    case 0x81:
                    case 0x82:
                    case 0x83:
                    case 0x84:
                    case 0x85:
                    case 0x86:
                    case 0x87:
                    case 0xc6:
                        Instruction_ADD(m_byte);
                        break;
                    case 0x02:
                    case 0x12:
                    case 0x32:
                        Instruction_STA(m_byte);
                        break;
                    case 0xaf:
                    case 0xa8:
                    case 0xa9:
                    case 0xaa:
                    case 0xab:
                    case 0xac:
                    case 0xad:
                    case 0xae:
                    case 0xee:
                        Instruction_XOR(m_byte);
                        break;
                    case 0xf3:
                        Instruction_DI();
                        break;
                    case 0xfb:
                        Instruction_EI();
                        break;
                    case 0x37:
                        Instruction_STC();
                        break;
                    case 0x3f:
                        Instruction_CMC();
                        break;
                    case 0xb7:
                    case 0xb0:
                    case 0xb1:
                    case 0xb2:
                    case 0xb3:
                    case 0xb4:
                    case 0xb5:
                    case 0xb6:
                    case 0xf6:
                        Instruction_OR(m_byte);
                        break;
                    case 0x97:
                    case 0x90:
                    case 0x91:
                    case 0x92:
                    case 0x93:
                    case 0x94:
                    case 0x95:
                    case 0x96:
                    case 0xd6:
                        Instruction_SUB(m_byte);
                        break;
                    case 0x2a:
                        Instruction_LHLD(m_byte);
                        break;
                    case 0x22:
                        Instruction_SHLD(m_byte);
                        break;
                    case 0xde:
                        Instruction_SBBI(m_byte);
                        break;
                    case 0x27:
                        Instruction_DAA(m_byte);
                        break;
                    case 0x2f:
                        Instruction_CMA(m_byte);
                        break;
                    case 0x8f:
                    case 0x88:
                    case 0x89:
                    case 0x8a:
                    case 0x8b:
                    case 0x8c:
                    case 0x8d:
                    case 0x8e:
                    case 0xce:
                        Instruction_ADC(m_byte);
                        break;
                    default:
                        CRASHED = true;
                        MessageBox.Show("Emulator Crashed @ instruction : " + m_instructionCounter.ToString() + " " + m_byte.ToString());
                        break;
                }

                m_instructionCounter++;
                if (m_instructionCounter >= half_instruction_per_frame)
                {

                    if (INTERRUPT)
                    {
                        // There are two interrupts that occur every frame (address $08 and $10)
                        if (interrupt_alternate == 0)
                        {
                            CallInterrupt(0x08);
                        }
                        else
                        {
                            CallInterrupt(0x10);
                        }
                    }
                    interrupt_alternate = 1 - interrupt_alternate;
                    m_instructionCounter = 0;
                }
            }
        }

        private void CallInterrupt(short inAddress)
        {
            // Call the interrupt by pushing current PC on the stack and then jump to interrupt address
            INTERRUPT = false;
            StackPush((ushort)m_PC);
            m_PC = inAddress;
        }

        private void NOP()
        {
            // No Operation - Do nothing !
        }

        private void Instruction_JMP(byte m_byte)
        {
            ushort data16 = FetchRomShort();
            var m_condition = true;

            switch (m_byte)
            {
                case 0xc3:
                    // Do nothing apart from incrementing the Programme Counter
                    break;
                case 0xc2:
                    m_condition = !Convert.ToBoolean(ZERO);
                    break;
                case 0xca:
                    m_condition = Convert.ToBoolean(ZERO);
                    break;
                case 0xd2:
                    m_condition = !Convert.ToBoolean(CARRY);
                    break;
                case 0xda:
                    m_condition = Convert.ToBoolean(CARRY);
                    break;
                case 0xf2:
                    m_condition = !Convert.ToBoolean(SIGN);
                    break;
                case 0xfa:
                    m_condition = Convert.ToBoolean(SIGN);
                    break;
            }
            if (m_condition)
            {
                m_PC = data16;
            }
        }

        private void Instruction_LXI(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x01:
                    SetBC(FetchRomShort());
                    break;
                case 0x11:
                    SetDE(FetchRomShort());
                    break;
                case 0x21:
                    SetHL(FetchRomShort());
                    break;
                case 0x31:
                    SetSP(FetchRomShort());
                    break;
            }
        }

        private void Instruction_MVI(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x3e:
                    SetA(FetchRomByte());
                    break;
                case 0x06:
                    SetB(FetchRomByte());
                    break;
                case 0x0e:
                    SetC(FetchRomByte());
                    break;
                case 0x16:
                    SetD(FetchRomByte());
                    break;
                case 0x1e:
                    SetE(FetchRomByte());
                    break;
                case 0x26:
                    SetH(FetchRomByte());
                    break;
                case 0x2e:
                    SetL(FetchRomByte());
                    break;
                case 0x36:
                    WriteByte(HL, FetchRomByte());
                    break;
            }
        }

        private void Instruction_CALL(byte m_byte)
        {
            ushort data16 = FetchRomShort();
            bool m_condition = true;

            switch (m_byte)
            {
                case 0xcd:
                    break;
                case 0xc4:
                    m_condition = !Convert.ToBoolean(ZERO);
                    break;
                case 0xcc:
                    m_condition = Convert.ToBoolean(ZERO);
                    break;
                case 0xd4:
                    m_condition = !Convert.ToBoolean(CARRY);
                    break;
                case 0xdc:
                    m_condition = Convert.ToBoolean(CARRY);
                    break;
            }
            if (m_condition)
            {
                StackPush((ushort)m_PC);
                m_PC = data16;
            }
        }

        private void Instruction_LDA(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x0a:
                    m_source = BC;
                    break;
                case 0x1a:
                    m_source = DE;
                    break;
                case 0x3a:
                    m_source = FetchRomShort();
                    break;
            }
            SetA(ReadByte(m_source));
        }

        private void Instruction_MOVHL(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x77:
                    WriteByte(HL, A);
                    break;
                case 0x70:
                    WriteByte(HL, B);
                    break;
                case 0x71:
                    WriteByte(HL, C);
                    break;
                case 0x72:
                    WriteByte(HL, D);
                    break;
                case 0x73:
                    WriteByte(HL, E);
                    break;
                case 0x74:
                    WriteByte(HL, H);
                    break;
                case 0x75:
                    WriteByte(HL, L);
                    break;
            }
        }

        private void Instruction_INX(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x03:
                    SetBC(BC + 1);
                    break;
                case 0x13:
                    SetDE(DE + 1);
                    break;
                case 0x23:
                    SetHL(HL + 1);
                    break;
                case 0x33:
                    SetSP(SP + 1);
                    break;
            }
        }

        private void Instruction_DCX(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x0b:
                    SetBC(BC - 1);
                    break;
                case 0x1b:
                    SetDE(DE - 1);
                    break;
                case 0x2b:
                    SetHL(HL - 1);
                    break;
                case 0x3b:
                    SetSP(SP - 1);
                    break;
            }
        }

        private void Instruction_DEC(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x3d:
                    SetA(PerformDec(A));
                    break;
                case 0x05:
                    SetB(PerformDec(B));
                    break;
                case 0x0d:
                    SetC(PerformDec(C));
                    break;
                case 0x15:
                    SetD(PerformDec(D));
                    break;
                case 0x1d:
                    SetE(PerformDec(E));
                    break;
                case 0x25:
                    SetH(PerformDec(H));
                    break;
                case 0x2d:
                    SetL(PerformDec(L));
                    break;
                case 0x35:
                    WriteByte(HL, (byte)PerformDec(ReadByte(HL)));
                    break;
            }
        }

        private void Instruction_INC(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x3c:
                    SetA(PerformInc(A));
                    break;
                case 0x04:
                    SetB(PerformInc(B));
                    break;
                case 0x0c:
                    SetC(PerformInc(C));
                    break;
                case 0x14:
                    SetD(PerformInc(D));
                    break;
                case 0x1c:
                    SetE(PerformInc(E));
                    break;
                case 0x24:
                    SetH(PerformInc(H));
                    break;
                case 0x2c:
                    SetL(PerformInc(L));
                    break;
                case 0x34:
                    WriteByte(HL, (byte)PerformInc(ReadByte(HL)));
                    break;
            }
        }

        private void Instruction_RET(byte m_byte)
        {
            bool m_condition = true;

            switch (m_byte)
            {
                case 0xc9:
                    break;
                case 0xc0:
                    m_condition = !Convert.ToBoolean(ZERO);
                    break;
                case 0xc8:
                    m_condition = Convert.ToBoolean(ZERO);
                    break;
                case 0xd0:
                    m_condition = !Convert.ToBoolean(CARRY);
                    break;
                case 0xd8:
                    m_condition = Convert.ToBoolean(CARRY);
                    break;
            }
            if (m_condition)
            {
                m_PC = StackPop();
            }
        }

        private void Instruction_MOV(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x7F:
                    SetA(A);
                    break;
                case 0x78:
                    SetA(B);
                    break;
                case 0x79:
                    SetA(C);
                    break;
                case 0x7A:
                    SetA(D);
                    break;
                case 0x7B:
                    SetA(E);
                    break;
                case 0x7C:
                    SetA(H);
                    break;
                case 0x7D:
                    SetA(L);
                    break;
                case 0x7E:
                    SetA(ReadByte(HL));
                    break;
                case 0x47:
                    SetB(A);
                    break;
                case 0x40:
                    SetB(B);
                    break;
                case 0x41:
                    SetB(C);
                    break;
                case 0x42:
                    SetB(D);
                    break;
                case 0x43:
                    SetB(E);
                    break;
                case 0x44:
                    SetB(H);
                    break;
                case 0x45:
                    SetB(L);
                    break;
                case 0x46:
                    SetB(ReadByte(HL));
                    break;
                case 0x4f:
                    SetC(A);
                    break;
                case 0x48:
                    SetC(B);
                    break;
                case 0x49:
                    SetC(C);
                    break;
                case 0x4a:
                    SetC(D);
                    break;
                case 0x4b:
                    SetC(E);
                    break;
                case 0x4c:
                    SetC(H);
                    break;
                case 0x4d:
                    SetC(L);
                    break;
                case 0x4e:
                    SetC(ReadByte(HL));
                    break;
                case 0x57:
                    SetD(A);
                    break;
                case 0x50:
                    SetD(B);
                    break;
                case 0x51:
                    SetD(C);
                    break;
                case 0x52:
                    SetD(D);
                    break;
                case 0x53:
                    SetD(E);
                    break;
                case 0x54:
                    SetD(H);
                    break;
                case 0x55:
                    SetD(L);
                    break;
                case 0x56:
                    SetD(ReadByte(HL));
                    break;
                case 0x5f:
                    SetE(A);
                    break;
                case 0x58:
                    SetE(B);
                    break;
                case 0x59:
                    SetE(C);
                    break;
                case 0x5a:
                    SetE(D);
                    break;
                case 0x5b:
                    SetE(E);
                    break;
                case 0x5c:
                    SetE(H);
                    break;
                case 0x5d:
                    SetE(L);
                    break;
                case 0x5e:
                    SetE(ReadByte(HL));
                    break;
                case 0x67:
                    SetH(A);
                    break;
                case 0x60:
                    SetH(B);
                    break;
                case 0x61:
                    SetH(C);
                    break;
                case 0x62:
                    SetH(D);
                    break;
                case 0x63:
                    SetH(E);
                    break;
                case 0x64:
                    SetH(H);
                    break;
                case 0x65:
                    SetH(L);
                    break;
                case 0x66:
                    SetH(ReadByte(HL));
                    break;
                case 0x6f:
                    SetL(A);
                    break;
                case 0x68:
                    SetL(B);
                    break;
                case 0x69:
                    SetL(C);
                    break;
                case 0x6a:
                    SetL(D);
                    break;
                case 0x6b:
                    SetL(E);
                    break;
                case 0x6c:
                    SetL(H);
                    break;
                case 0x6d:
                    SetL(L);
                    break;
                case 0x6e:
                    SetL(ReadByte(HL));
                    break;
            }
        }

        private void Instruction_CMP(byte m_byte)
        {
            switch (m_byte)
            {
                case 0xbf:
                    m_value = A;
                    break;
                case 0xb8:
                    m_value = B;
                    break;
                case 0xb9:
                    m_value = C;
                    break;
                case 0xba:
                    m_value = D;
                    break;
                case 0xbb:
                    m_value = E;
                    break;
                case 0xbc:
                    m_value = H;
                    break;
                case 0xbd:
                    m_value = L;
                    break;
                case 0xbe:
                    m_value = ReadByte(HL);
                    break;
                case 0xfe:
                    m_value = FetchRomByte();
                    break;
            }
            PerformCompSub((byte)m_value);
        }

        private void Instruction_PUSH(byte m_byte)
        {
            switch (m_byte)
            {
                case 0xc5:
                    m_value = BC;
                    break;
                case 0xd5:
                    m_value = DE;
                    break;
                case 0xe5:
                    m_value = HL;
                    break;
                case 0xf5:
                    m_value = (ushort)(A << 8);
                    if (Convert.ToBoolean(SIGN))
                    {
                        m_value = (ushort)(m_value | BIT7);
                    }
                    if (Convert.ToBoolean(ZERO))
                    {
                        m_value = (ushort)(m_value | BIT6);
                    }
                    if (INTERRUPT)
                    {
                        m_value = (ushort)(m_value | BIT5);
                    }
                    if (Convert.ToBoolean(HALFCARRY))
                    {
                        m_value = (ushort)(m_value | BIT4);
                    }
                    if (Convert.ToBoolean(CARRY))
                    {
                        m_value = (ushort)(m_value | BIT0);
                    }
                    break;
            }
            StackPush(m_value);
        }

        private void Instruction_POP(byte m_byte)
        {
            m_value = StackPop();
            switch (m_byte)
            {
                case 0xc1:
                    SetBC(m_value);
                    break;
                case 0xd1:
                    SetDE(m_value);
                    break;
                case 0xe1:
                    SetHL(m_value);
                    break;
                case 0xf1:
                    A = (byte)(m_value >> 8);
                    SIGN = (ushort)(m_value & 0x80);
                    ZERO = (ushort)(m_value & 0x40);
                    INTERRUPT = Convert.ToBoolean(m_value & 0x20);
                    HALFCARRY = (ushort)(m_value & BIT4);
                    CARRY = (ushort)(m_value & BIT0);
                    break;
            }
        }

        private void Instruction_DAD(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x09:
                    AddHL(BC);
                    break;
                case 0x19:
                    AddHL(DE);
                    break;
                case 0x29:
                    AddHL(HL);
                    break;
                case 0x39:
                    AddHL(SP);
                    break;
            }
        }

        private void Instruction_XCHG()
        {
            ushort temp = DE;
            SetDE(HL);
            SetHL(temp);
        }

        private void Instruction_XTHL()
        {
            ushort temp = H;
            SetH(ReadByte(SP + 1));
            WriteByte((ushort)(SP + 1), temp);
            temp = L;
            SetL(ReadByte(SP));
            WriteByte(SP, temp);
        }

        private void Instruction_OUTP()
        {
            byte port = FetchRomByte();
            m_io.OutputPort(port, (byte)A);
        }

        private void Instruction_INP()
        {
            byte port = FetchRomByte();
            SetA(m_io.InputPort(port));
        }

        private void Instruction_PCHL(byte m_byte)
        {
            m_PC = HL;
        }

        private void Instruction_RST(byte m_byte)
        {
            ushort address = 0;
            switch (m_byte)
            {
                case 0xc7:
                    address = 0x00;
                    break;
                case 0xcf:
                    address = 0x08;
                    break;
                case 0xd7:
                    address = 0x10;
                    break;
                case 0xdf:
                    address = 0x18;
                    break;
                case 0xe7:
                    address = 0x20;
                    break;
                case 0xef:
                    address = 0x28;
                    break;
                case 0xf7:
                    address = 0x30;
                    break;
                case 0xff:
                    address = 0x38;
                    break;
            }
            StackPush((ushort)m_PC);
            m_PC = address;
        }

        private void Instruction_RLC(byte m_byte)
        {
            SetA((ushort)((A << 1) | (A >> 7)));
            var temp = (A & 1);
            bool testCarry = Convert.ToBoolean(temp);
            CARRY = (ushort)(A & BIT0);
        }

        private void Instruction_RAL(byte m_byte)
        {
            ushort temp = A;
            SetA((ushort)(A << 1));
            if (Convert.ToBoolean(CARRY))
            {
                SetA((ushort)(A | BIT0));
            }

            CARRY = (ushort)(temp & 0x80);
        }

        private void Instruction_RRC(byte m_byte)
        {
            SetA((ushort)((A >> 1) | (A << 7)));
            CARRY = (ushort)(A & BIT7);
        }

        private void Instruction_RAR(byte m_byte)
        {
            ushort temp = A;
            SetA((ushort)(A >> 1));
            if (Convert.ToBoolean(CARRY))
            {
                SetA((ushort)(A | BIT7));
            }
            CARRY = (ushort)(temp & 1);
        }

        private void Instruction_AND(byte m_byte)
        {
            switch (m_byte)
            {
                case 0xa7:
                    PerformAnd(A);
                    break;
                case 0xa0:
                    PerformAnd(B);
                    break;
                case 0xa1:
                    PerformAnd(C);
                    break;
                case 0xa2:
                    PerformAnd(D);
                    break;
                case 0xa3:
                    PerformAnd(E);
                    break;
                case 0xa4:
                    PerformAnd(H);
                    break;
                case 0xa5:
                    PerformAnd(L);
                    break;
                case 0xa6:
                    PerformAnd(ReadByte(HL));
                    break;
                case 0xe6:
                    byte immediate = FetchRomByte();
                    PerformAnd(immediate);
                    break;
            }
        }

        private void Instruction_ADD(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x87:
                    PerformByteAdd(A, 0);
                    break;
                case 0x80:
                    PerformByteAdd(B, 0);
                    break;
                case 0x81:
                    PerformByteAdd(C, 0);
                    break;
                case 0x82:
                    PerformByteAdd(D, 0);
                    break;
                case 0x83:
                    PerformByteAdd(E, 0);
                    break;
                case 0x84:
                    PerformByteAdd(H, 0);
                    break;
                case 0x85:
                    PerformByteAdd(L, 0);
                    break;
                case 0x86:
                    PerformByteAdd(ReadByte(HL), 0);
                    break;
                case 0xc6:
                    byte immediate = FetchRomByte();
                    PerformByteAdd(immediate, 0);
                    break;
            }
        }

        private void Instruction_STA(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x02:
                    WriteByte(BC, A);
                    break;
                case 0x12:
                    WriteByte(DE, A);
                    break;
                case 0x32:
                    ushort immediate = FetchRomShort();
                    WriteByte(immediate, A);
                    break;
            }
        }

        private void Instruction_XOR(byte m_byte)
        {
            switch (m_byte)
            {
                case 0xaf:
                    PerformXor(A);
                    break;
                case 0xa8:
                    PerformXor(B);
                    break;
                case 0xa9:
                    PerformXor(C);
                    break;
                case 0xaa:
                    PerformXor(D);
                    break;
                case 0xab:
                    PerformXor(E);
                    break;
                case 0xac:
                    PerformXor(H);
                    break;
                case 0xad:
                    PerformXor(L);
                    break;
                case 0xae:
                    PerformXor(ReadByte(HL));
                    break;
                case 0xee:
                    byte immediate = FetchRomByte();
                    PerformXor(immediate);
                    break;
            }
        }

        private void Instruction_DI()
        {
            INTERRUPT = false;
        }

        private void Instruction_EI()
        {
            INTERRUPT = true;
        }

        private void Instruction_STC()
        {
            CARRY = 1;
        }

        private void Instruction_CMC()
        {
            CARRY = 0;
        }

        private void Instruction_OR(byte m_byte)
        {
            switch (m_byte)
            {
                case 0xb7:
                    PerformOr(A);
                    break;
                case 0xb0:
                    PerformOr(B);
                    break;
                case 0xb1:
                    PerformOr(C);
                    break;
                case 0xb2:
                    PerformOr(D);
                    break;
                case 0xb3:
                    PerformOr(E);
                    break;
                case 0xb4:
                    PerformOr(H);
                    break;
                case 0xb5:
                    PerformOr(L);
                    break;
                case 0xb6:
                    PerformOr(ReadByte(HL));
                    break;
                case 0xf6:
                    byte immediate = FetchRomByte();
                    PerformOr(immediate);
                    break;
            }
        }

        private void Instruction_SUB(byte m_byte)
        {
            switch (m_byte)
            {
                case 0x97:
                    PerformByteSub(A, 0);
                    break;
                case 0x90:
                    PerformByteSub(B, 0);
                    break;
                case 0x91:
                    PerformByteSub(C, 0);
                    break;
                case 0x92:
                    PerformByteSub(D, 0);
                    break;
                case 0x93:
                    PerformByteSub(E, 0);
                    break;
                case 0x94:
                    PerformByteSub(H, 0);
                    break;
                case 0x95:
                    PerformByteSub(L, 0);
                    break;
                case 0x96:
                    PerformByteSub(ReadByte(HL), 0);
                    break;
                case 0xd6:
                    byte immediate = FetchRomByte();
                    PerformByteSub(immediate, 0);
                    break;
            }
        }

        private void Instruction_LHLD(byte m_byte)
        {
            ushort immediate = FetchRomShort();
            SetHL(ReadShort(immediate));
        }

        private void Instruction_SHLD(byte m_byte)
        {
            ushort immediate = FetchRomShort();
            WriteShort(immediate, HL);
        }

        private void Instruction_SBBI(byte m_byte)
        {
            byte immediate = FetchRomByte();
            byte carryvalue = 0;
            if (Convert.ToBoolean(CARRY))
            {
                carryvalue = 1;
            }
            PerformByteSub(immediate, carryvalue);
        }

        private void Instruction_DAA(byte m_byte)
        {
            if (((A & 0x0F) > 9) || Convert.ToBoolean(HALFCARRY))
            {
                A += 0x06;
                HALFCARRY = 1;
            }
            else
            {
                HALFCARRY = 0;
            }

            if ((A > 0x9F) || (Convert.ToBoolean(CARRY)))
            {
                A += 0x60;
                CARRY = 1;
            }
            else
            {
                CARRY = 0;
            }
            setFlagZeroSign();
        }

        private void Instruction_CMA(byte m_byte)
        {
            SetA((ushort)(A ^ 0xff));
        }

        private void Instruction_ADC(byte m_byte)
        {
            byte carryvalue = 0;
            if (Convert.ToBoolean(CARRY))
            {
                carryvalue = 1;
            }
            switch (m_byte)
            {
                case 0x8f:
                    PerformByteAdd(A, carryvalue);
                    break;
                case 0x88:
                    PerformByteAdd(B, carryvalue);
                    break;
                case 0x89:
                    PerformByteAdd(C, carryvalue);
                    break;
                case 0x8a:
                    PerformByteAdd(D, carryvalue);
                    break;
                case 0x8b:
                    PerformByteAdd(E, carryvalue);
                    break;
                case 0x8c:
                    PerformByteAdd(H, carryvalue);
                    break;
                case 0x8d:
                    PerformByteAdd(L, carryvalue);
                    break;
                case 0x8e:
                    PerformByteAdd(ReadByte(HL), carryvalue);
                    break;
                case 0xce:
                    byte immediate = FetchRomByte();
                    PerformByteAdd(immediate, carryvalue);
                    break;
            }
        }

        private void SetA(ushort inByte)
        {
            A = (ushort)(inByte & 0xFF);
        }

        private void SetB(int inByte)
        {
            B = (ushort)(inByte & 0xFF);
            BC = (ushort)((B << 8) | C);
        }

        private void SetC(int inByte)
        {
            C = (ushort)(inByte & 0xFF);
            BC = (ushort)((B << 8) | C);
        }

        private void SetD(int inByte)
        {
            D = (ushort)inByte;
            DE = (ushort)((D << 8) + E);
        }

        private void SetE(int inByte)
        {
            E = (byte)inByte;
            DE = (ushort)((D << 8) + E);
        }

        private void SetH(int inByte)
        {
            H = (ushort)(inByte);
            HL = (ushort)((H << 8) + L);
        }

        private void SetL(int inByte)
        {
            L = (ushort)inByte;
            HL = (ushort)((H << 8) + L);
        }

        private void SetBC(int inShort)
        {
            BC = (ushort)inShort;
            B = (ushort)(BC >> 8);
            C = (ushort)(BC & 0xFF);
        }

        private void SetDE(int inShort)
        {
            DE = (ushort)inShort;
            D = (ushort)(DE >> 8);
            E = (ushort)(DE & 0xFF);
        }

        private void SetHL(int inShort)
        {
            HL = (ushort)inShort;
            H = (ushort)(HL >> 8);
            L = (ushort)(HL & 0xFF);
        }

        private void SetSP(int inShort)
        {
            SP = (ushort)inShort;
        }

        private byte FetchRomByte()
        {
            byte m_value = m_rom[m_PC];
            m_PC += 1;
            return m_value;
        }

        private ushort FetchRomShort()
        {
            byte[] bytes = new byte[2];
            bytes[0] = m_rom[m_PC + 0];
            bytes[1] = m_rom[m_PC + 1];
            m_PC += 2;
            return BitConverter.ToUInt16(bytes, 0);
        }

        private byte ReadByte(int count)
        {
            return m_rom[count];
        }

        private ushort ReadShort(ushort inAddress)
        {
            return (ushort)((m_rom[inAddress + 1] << 8) + (m_rom[inAddress + 0]));
        }

        private void WriteShort(ushort inAddress, ushort inWord)
        {
            m_rom[inAddress + 1] = (byte)(inWord >> 8);
            m_rom[inAddress + 0] = (byte)(inWord);
        }

        private void WriteByte(ushort inAddress, ushort inByte)
        {
            m_rom[inAddress] = (byte)(inByte);
        }

        private void StackPush(ushort inValue)
        {
            SP -= 2;
            WriteShort(SP, inValue);
        }

        private ushort StackPop()
        {
            ushort temp = ReadShort(SP);
            SP += 2;
            return temp;
        }

        private ushort PerformDec(ushort inSource)
        {
            ushort value = (ushort)((inSource - 1) & 0xFF);
            HALFCARRY = Convert.ToUInt16((value & 0x0F) == 0);
            bool zeroTest = Convert.ToBoolean((value & 255));
            ZERO = Convert.ToUInt16((value & 255) == 0);
            SIGN = (ushort)(value & 128);
            return value;
        }

        private ushort PerformInc(ushort inSource)
        {
            ushort value = (ushort)(inSource + 1);
            HALFCARRY = Convert.ToUInt16((value & 0xF) < 0 || (value & 0xF) > 0);
            ZERO = Convert.ToUInt16((value & 255) == 0);
            SIGN = (ushort)(value & 128);
            return value;
        }

        private void setFlagZeroSign()
        {
            ZERO = Convert.ToUInt16(A == 0);
            SIGN = (ushort)(A & 128);
        }

        private void PerformAnd(ushort inValue)
        {
            SetA((ushort)(A & inValue));
            CARRY = 0;
            HALFCARRY = 0;
            setFlagZeroSign();
        }

        private void PerformXor(ushort inValue)
        {
            SetA((ushort)(A ^ inValue));
            CARRY = 0;
            HALFCARRY = 0;
            setFlagZeroSign();
        }

        private void PerformOr(ushort inValue)
        {
            SetA((ushort)(A | inValue));
            CARRY = 0;
            HALFCARRY = 0;
            setFlagZeroSign();
        }

        private void PerformByteAdd(ushort inValue, short inCarryValue)
        {
            int value = A + inValue + inCarryValue;
            HALFCARRY = (ushort)((A ^ inValue ^ value) & 0x10);
            SetA((ushort)(value));

            if (value > 255)
            {
                CARRY = 1;
            }
            else
            {
                CARRY = 0;
            }

            setFlagZeroSign();
        }

        private void PerformByteSub(ushort inValue, ushort inCarryValue)
        {
            byte value = (byte)(A - inValue - inCarryValue);

            if ((value >= A) && (inValue | inCarryValue) > 0)
            {
                CARRY = 1;
            }
            else
            {
                CARRY = 0;
            }
            HALFCARRY = (ushort)((A ^ inValue ^ value) & 0x10);
            SetA(value);
            setFlagZeroSign();
        }

        private void PerformCompSub(byte inValue)
        {
            var value = (this.A - inValue) & 0xFF;
            if ((value >= this.A) && Convert.ToBoolean(inValue))
            {
                CARRY = inValue;
            }
            else
            {
                CARRY = 0;
            }

            HALFCARRY = (ushort)((A ^ inValue ^ value) & 0x10);
            ZERO = Convert.ToUInt16(value == 0);
            SIGN = (ushort)(value & 128);
        }


        private void AddHL(ushort inValue)
        {
            int value = HL + inValue;
            SetHL(value);
            CARRY = Convert.ToUInt16(value > 65535);
        }

        private void Reset()
        {
            m_PC = 0;
            A = 0;
            BC = 0;
            DE = 0;
            HL = 0;
            SIGN = 0;
            ZERO = 0;
            HALFCARRY = 0;
            PARITY = false;
            CARRY = 0;
            INTERRUPT = false;
            CRASHED = false;
        }
    }
}
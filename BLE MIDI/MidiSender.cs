using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace kshoji.BleMidi
{
    /// <summary>
    /// Represents BLE MIDI Output Device
    /// </summary>
    public partial class MidiOutputDevice
    {
        /// <summary>
        /// Sends MIDI message to output device.
        /// </summary>
        /// <param name="byte1"></param>
        private async void SendMidiMessage(int byte1)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            byte[] writeBuffer = new byte[] { (byte)byte1 };

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// Sends MIDI message to output device.
        /// </summary>
        /// <param name="byte1"></param>
        /// <param name="byte2"></param>
        /// <param name="byte3"></param>
        private async void SendMidiMessage(int byte1, int byte2, int byte3)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            byte[] writeBuffer = new byte[3];

            writeBuffer[0] = (byte)byte1;
            writeBuffer[1] = (byte)byte2;
            writeBuffer[2] = (byte)byte3;

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// SysEx
        /// </summary>
        /// <param name="systemExclusive">start with 'F0', and end with 'F7'</param>
        public async void SendMidiSystemExclusive(byte[] systemExclusive)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            // split into 20 bytes. BLE can't send more than 20 bytes.
            byte[] buffer = new byte[20];
            for (int i = 0; i < systemExclusive.Length; i += 20)
            {
                if (i + 20 <= systemExclusive.Length)
                {
                    Array.Copy(systemExclusive, i, buffer, 0, 20);

                    await midiOutputCharacteristic.WriteValueAsync(buffer.AsBuffer());
                }
                else
                {
                    // last message
                    buffer = new byte[systemExclusive.Length - i];
                    Array.Copy(systemExclusive, i, buffer, 0, systemExclusive.Length - i);

                    await midiOutputCharacteristic.WriteValueAsync(buffer.AsBuffer());
                }
            }
        }

        /// <summary>
        /// Note-off
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void SendMidiNoteOff(int channel, int note, int velocity)
        {
            SendMidiMessage(0x80 | (channel & 0xf), note, velocity);
        }

        /// <summary>
        /// Note-on
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public void SendMidiNoteOn(int channel, int note, int velocity)
        {
            SendMidiMessage(0x90 | (channel & 0xf), note, velocity);
        }

        /// <summary>
        /// Poly-KeyPress
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="pressure">0-127</param>
        public void SendMidiPolyphonicAftertouch(int channel, int note, int pressure)
        {
            SendMidiMessage(0xa0 | (channel & 0xf), note, pressure);
        }

        /// <summary>
        /// Control Change
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="function">0-127</param>
        /// <param name="value">0-127</param>
        public void SendMidiControlChange(int channel, int function, int value)
        {
            SendMidiMessage(0xb0 | (channel & 0xf), function, value);
        }

        /// <summary>
        /// Program Change
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="program">0-127</param>
        public void SendMidiProgramChange(int channel, int program)
        {
            SendMidiMessage(0xc0 | (channel & 0xf), program, 0);
        }

        /// <summary>
        /// Channel Pressure
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="pressure">0-127</param>
        public void SendMidiChannelAftertouch(int channel, int pressure)
        {
            SendMidiMessage(0xd0 | (channel & 0xf), pressure, 0);
        }

        /// <summary>
        /// PitchBend Change
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="amount">0(low)-8192(center)-16383(high)</param>
        public void SendMidiPitchWheel(int channel, int amount)
        {
            SendMidiMessage(0xe0 | (channel & 0xf), amount & 0x7f, (amount >> 7) & 0x7f);
        }

        /// <summary>
        /// MIDI Time Code(MTC) Quarter Frame
        /// </summary>
        /// <param name="timing">0-127</param>
        public async void SendMidiTimeCodeQuarterFrame(int timing)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            byte[] writeBuffer = new byte[2];

            writeBuffer[0] = (byte)0xf1;
            writeBuffer[1] = (byte)(timing & 0x7f);

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// Song Select
        /// </summary>
        /// <param name="song">0-127</param>
        public async void SendMidiSongSelect(int song)
        {
            if (midiOutputCharacteristic == null)
            {
                return;
            }

            byte[] writeBuffer = new byte[2];

            writeBuffer[0] = (byte)0xf3;
            writeBuffer[1] = (byte)(song & 0x7f);

            await midiOutputCharacteristic.WriteValueAsync(writeBuffer.AsBuffer());
        }

        /// <summary>
        /// Song Position Pointer
        /// </summary>
        /// <param name="position">0-16383</param>
        public void SendMidiSongPositionPointer(int position)
        {
            SendMidiMessage(0xf2, position & 0x7f, (position >> 7) & 0x7f);
        }

        /// <summary>
        /// Tune Request
        /// </summary>
        public void SendMidiTuneRequest()
        {
            SendMidiMessage(0xf6);
        }

        /// <summary>
        /// Timing Clock
        /// </summary>
        public void SendMidiTimingClock()
        {
            SendMidiMessage(0xf8);
        }

        /// <summary>
        /// Start Playing
        /// </summary>
        public void SendMidiStart()
        {
            SendMidiMessage(0xfa);
        }

        /// <summary>
        /// Continue Playing
        /// </summary>
        public void SendMidiContinue()
        {
            SendMidiMessage(0xfb);
        }

        /// <summary>
        /// Stop Playing
        /// </summary>
        public void SendMidiStop()
        {
            SendMidiMessage(0xfc);
        }

        /// <summary>
        /// Active Sensing
        /// </summary>
        public void SendMidiActiveSensing()
        {
            SendMidiMessage(0xfe);
        }

        /// <summary>
        /// Reset Device
        /// </summary>
        public void SendMidiReset()
        {
            SendMidiMessage(0xff);
        }

        /// <summary>
        /// RPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7bits or 14bits</param>
        public void sendRPNMessage(int channel, int function, int value)
        {
            sendRPNMessage(channel, (function >> 7) & 0x7f, function & 0x7f, value);
        }

        /// <summary>
        /// RPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="functionMSB">higher 7bits</param>
        /// <param name="functionLSB">lower 7bits</param>
        /// <param name="value">7bits or 14bits</param>
        public void sendRPNMessage(int channel, int functionMSB, int functionLSB, int value)
        {
            // send the function
            SendMidiControlChange(channel, 101, functionMSB & 0x7f);
            SendMidiControlChange(channel, 100, functionLSB & 0x7f);

            // send the value
            if ((value >> 7) > 0)
            {
                SendMidiControlChange(channel, 6, (value >> 7) & 0x7f);
                SendMidiControlChange(channel, 38, value & 0x7f);
            }
            else
            {
                SendMidiControlChange(channel, 6, value & 0x7f);
            }

            // send the NULL function
            SendMidiControlChange(channel, 101, 0x7f);
            SendMidiControlChange(channel, 100, 0x7f);
        }

        /// <summary>
        /// NRPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7bits or 14bits</param>
        public void sendNRPNMessage(int channel, int function, int value)
        {
            sendNRPNMessage(channel, (function >> 7) & 0x7f, function & 0x7f, value);
        }

        /// <summary>
        /// NRPN message
        /// </summary>
        /// <param name="channel">0-15</param>
        /// <param name="functionMSB">higher 7bits</param>
        /// <param name="functionLSB">lower 7bits</param>
        /// <param name="value">7bits or 14bits</param>
        public void sendNRPNMessage(int channel, int functionMSB, int functionLSB, int value)
        {
            // send the function
            SendMidiControlChange(channel, 99, functionMSB & 0x7f);
            SendMidiControlChange(channel, 98, functionLSB & 0x7f);

            // send the value
            if ((value >> 7) > 0)
            {
                SendMidiControlChange(channel, 6, (value >> 7) & 0x7f);
                SendMidiControlChange(channel, 38, value & 0x7f);
            }
            else
            {
                SendMidiControlChange(channel, 6, value & 0x7f);
            }

            // send the NULL function
            SendMidiControlChange(channel, 101, 0x7f);
            SendMidiControlChange(channel, 100, 0x7f);
        }
    }
}

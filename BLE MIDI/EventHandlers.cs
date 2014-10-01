namespace kshoji.BleMidi
{
    /// <summary>
    /// Event listener for MIDI events
    /// </summary>
    public partial class MidiInputDevice
    {
        /// <summary>
        /// SysEx
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="systemExclusive">received message</param>
        public delegate void SystemExclusiveDelegate(MidiInputDevice sender, byte[] systemExclusive);
        public event SystemExclusiveDelegate SystemExclusive;

        /// <summary>
        /// Note-off
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public delegate void NoteOffDelegate(MidiInputDevice sender, int channel, int note, int velocity);
        public event NoteOffDelegate NoteOff;

        /// <summary>
        /// Note-on
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="velocity">0-127</param>
        public delegate void NoteOnDelegate(MidiInputDevice sender, int channel, int note, int velocity);
        public event NoteOnDelegate NoteOn;

        /// <summary>
        /// Poly-KeyPress
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="note">0-127</param>
        /// <param name="pressure">0-127</param>
        public delegate void PolyphonicAftertouchDelegate(MidiInputDevice sender, int channel, int note, int pressure);
        public event PolyphonicAftertouchDelegate PolyphonicAftertouch;

        /// <summary>
        /// Control Change
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="function">0-127</param>
        /// <param name="value">0-127</param>
        public delegate void ControlChangeDelegate(MidiInputDevice sender, int channel, int function, int value);
        public event ControlChangeDelegate ControlChange;

        /// <summary>
        /// Program Change
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="program">0-127</param>
        public delegate void ProgramChangeDelegate(MidiInputDevice sender, int channel, int program);
        public event ProgramChangeDelegate ProgramChange;

        /// <summary>
        /// Channel Pressure
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="pressure">0-127</param>
        public delegate void ChannelAftertouchDelegate(MidiInputDevice sender, int channel, int pressure);
        public event ChannelAftertouchDelegate ChannelAftertouch;

        /// <summary>
        /// PitchBend Change
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="amount">0(low)-8192(center)-16383(high)</param>
        public delegate void PitchWheelDelegate(MidiInputDevice sender, int channel, int amount);
        public event PitchWheelDelegate PitchWheel;

        /// <summary>
        /// MIDI Time Code(MTC) Quarter Frame
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="timing">0-127</param>
        public delegate void TimeCodeQuarterFrameDelegate(MidiInputDevice sender, int timing);
        public event TimeCodeQuarterFrameDelegate TimeCodeQuarterFrame;

        /// <summary>
        /// Song Select
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="song">0-127</param>
        public delegate void SongSelectDelegate(MidiInputDevice sender, int song);
        public event SongSelectDelegate SongSelect;

        /// <summary>
        /// Song Position Pointer
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="position">0-16383</param>
        public delegate void SongPositionPointerDelegate(MidiInputDevice sender, int position);
        public event SongPositionPointerDelegate SongPositionPointer;

        /// <summary>
        /// Tune Request
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void TuneRequestDelegate(MidiInputDevice sender);
        public event TuneRequestDelegate TuneRequest;

        /// <summary>
        /// Timing Clock
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void TimingClockDelegate(MidiInputDevice sender);
        public event TimingClockDelegate TimingClock;

        /// <summary>
        /// Start Playing
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void StartDelegate(MidiInputDevice sender);
        public event StartDelegate Start;

        /// <summary>
        /// Continue Playing
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void ContinueDelegate(MidiInputDevice sender);
        public event ContinueDelegate Continue;

        /// <summary>
        /// Stop Playing
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void StopDelegate(MidiInputDevice sender);
        public event StopDelegate Stop;

        /// <summary>
        /// Active Sensing
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void ActiveSensingDelegate(MidiInputDevice sender);
        public event ActiveSensingDelegate ActiveSensing;

        /// <summary>
        /// Reset Device
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        public delegate void ResetDelegate(MidiInputDevice sender);
        public event ResetDelegate Reset;

        /// <summary>
        /// RPN message<br />
        /// invoked when value's MSB or LSB changed
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7 bits or 14 bits</param>
        public delegate void RPNMessageDelegate(MidiInputDevice sender, int channel, int function, int value);
        public event RPNMessageDelegate RPNMessage;

        /// <summary>
        /// NRPN message<br />
        /// invoked when value's MSB or LSB changed
        /// </summary>
        /// <param name="sender">the device sent this message</param>
        /// <param name="channel">0-15</param>
        /// <param name="function">14bits</param>
        /// <param name="value">7 bits or 14 bits</param>
        public delegate void NRPNMessageDelegate(MidiInputDevice sender, int channel, int function, int value);
        public event NRPNMessageDelegate NRPNMessage;
    }

    /// <summary>
    /// Listener for MIDI attached / detached events
    /// </summary>
    public sealed partial class BleMidilCentralProvider
    {
        /// <summary>
        /// MIDI input device has been attached
        /// </summary>
        /// <param name="midiInputDevice"></param>
        public delegate void MidiInputDeviceAttachedDelegate(MidiInputDevice midiInputDevice);
        public event MidiInputDeviceAttachedDelegate MidiInputDeviceAttached;

        /// <summary>
        /// MIDI output device has been attached
        /// </summary>
        /// <param name="midiOutputDevice"></param>
        public delegate void MidiOutputDeviceAttachedDelegate(MidiOutputDevice midiOutputDevice);
        public event MidiOutputDeviceAttachedDelegate MidiOutputDeviceAttached;

        /// <summary>
        /// MIDI input device has been detached
        /// </summary>
        /// <param name="midiInputDevice"></param>
        public delegate void MidiInputDeviceDetachedDelegate(MidiInputDevice midiInputDevice);
        public event MidiInputDeviceDetachedDelegate MidiInputDeviceDetached;

        /// <summary>
        /// MIDI output device has been detached
        /// </summary>
        /// <param name="midiOutputDevice"></param>
        public delegate void MidiOutputDeviceDetachedDelegate(MidiOutputDevice midiOutputDevice);
        public event MidiOutputDeviceDetachedDelegate MidiOutputDeviceDetached;
    }
}

using System;
using System.Runtime.InteropServices;
using UnityEngine;
using RtMidiDll = RtMidi.Unmanaged;

namespace Minis
{
    //
    // MIDI probe class used for enumerating MIDI ports
    //
    // This is actually an RtMidi input object without any input functionality.
    //
    unsafe sealed class MidiProbe : IDisposable
    {
        RtMidiDll.Wrapper* _rtmidi;

        public MidiProbe()
        {
            _rtmidi = RtMidiDll.InCreateDefault();

            if (_rtmidi == null || !_rtmidi->ok)
                Debug.LogWarning("Failed to create an RtMidi device object.");
        }

        ~MidiProbe()
        {
            if (_rtmidi == null || !_rtmidi->ok) return;
            RtMidiDll.InFree(_rtmidi);
        }

        public void Dispose()
        {
            if (_rtmidi == null || !_rtmidi->ok) return;

            RtMidiDll.InFree(_rtmidi);
            _rtmidi = null;

            GC.SuppressFinalize(this);
        }

        public int PortCount
        {
            get
            {
                if (_rtmidi == null || !_rtmidi->ok) return 0;
                return (int)RtMidiDll.GetPortCount(_rtmidi);
            }
        }

        public string GetPortName(int portNumber)
        {
            if (_rtmidi == null || !_rtmidi->ok) return null;
            return Marshal.PtrToStringAnsi(RtMidiDll.GetPortName(_rtmidi, (uint)portNumber));
        }
    }
}

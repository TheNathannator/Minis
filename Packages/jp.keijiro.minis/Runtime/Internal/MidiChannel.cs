using System;
using UnityEngine.InputSystem.LowLevel;

namespace Minis
{
    //
    // MIDI device driver class that manages all MIDI ports (interfaces) found
    // in the system.
    //
    sealed class MidiChannel : IDisposable
    {
        private readonly ThreadedMidiDevice _device;

        private bool[] _activeNotes = new bool[128];

        public MidiChannel(ThreadedMidiDevice pending)
        {
            _device = pending;
        }

        public void Dispose()
        {
            if (_device.device != null)
                MidiSystemWrangler.QueueDeviceRemoval(_device);
        }

        #region MIDI event receiver (invoked from MidiPort)

        internal void ProcessNoteOn(byte note, byte velocity)
        {
            // Consecutive note ons need to have a note off inserted in-between
            if (_activeNotes[note])
                ProcessNoteOff(note);

            SendDeltaEvent(note, velocity);
            _activeNotes[note] = true;
        }

        internal void ProcessNoteOff(byte note)
        {
            SendDeltaEvent(note, 0);
            _activeNotes[note] = false;
        }

        internal void ProcessControlChange(byte number, byte value)
        {
            SendDeltaEvent(number + 128u, value);
        }

        unsafe void SendDeltaEvent(uint offset, byte value)
        {
            if (_device.device == null)
                return;

            var device = _device.device;
            var delta = new DeltaStateEvent()
            {
                baseEvent = new InputEvent(DeltaStateEvent.Type, sizeof(DeltaStateEvent), device.deviceId),
                stateFormat = MidiDeviceState.Format,
                stateOffset = offset
            };

            // DeltaStateEvent always contains one byte in its state data as a field
            *(byte*)delta.deltaState = value;

            MidiSystemWrangler.QueueEvent(&delta.baseEvent);
        }

        #endregion
    }
}
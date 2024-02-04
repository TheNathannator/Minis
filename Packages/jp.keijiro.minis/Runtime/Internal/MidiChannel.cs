using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Minis
{
    //
    // MIDI device driver class that manages all MIDI ports (interfaces) found
    // in the system.
    //
    sealed class MidiChannel : System.IDisposable
    {
        private readonly ThreadedMidiDevice _pending;

        public MidiChannel(ThreadedMidiDevice pending)
        {
            _pending = pending;
        }

        public void CheckClaimed()
        {
            if (!_pending.claimed)
            {
                if (_pending.device == null)
                    return;

                _pending.claimed = true;
            }
        }

        public void Dispose()
        {
            if (_pending.claimed)
                InputSystem.RemoveDevice(_pending.device);
        }

        #region MIDI event receiver (invoked from MidiPort)

        internal void ProcessNoteOn(byte note, byte velocity)
        {
            SendDeltaEvent(note, velocity);
        }

        internal void ProcessNoteOff(byte note)
        {
            SendDeltaEvent(note, 0);
        }

        internal void ProcessControlChange(byte number, byte value)
        {
            SendDeltaEvent(number + 128u, value);
        }

        unsafe void SendDeltaEvent(uint offset, byte value)
        {
            CheckClaimed();
            if (!_pending.claimed)
                return;

            var device = _pending.device;
            var delta = new DeltaStateEvent()
            {
                baseEvent = new InputEvent(DeltaStateEvent.Type, sizeof(DeltaStateEvent), device.deviceId),
                stateFormat = device.stateBlock.format,
                stateOffset = offset
            };

            // DeltaStateEvent always contains one byte in its state data as a field
            *(byte*)delta.deltaState = value;

            MidiSystemWrangler.QueueEvent(&delta.baseEvent);
        }

        #endregion
    }
}
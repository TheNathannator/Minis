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
        private readonly MidiDevice _device;

        public MidiChannel(MidiDevice device)
        {
            _device = device;
        }

        public void Dispose()
        {
            InputSystem.RemoveDevice(_device);
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
            if (_device == null)
                return;

            var delta = new DeltaStateEvent()
            {
                baseEvent = new InputEvent(DeltaStateEvent.Type, sizeof(DeltaStateEvent), _device.deviceId),
                stateFormat = _device.stateBlock.format,
                stateOffset = offset
            };

            // DeltaStateEvent always contains one byte in its state data as a field
            *(byte*)delta.deltaState = value;

            MidiSystemWrangler.QueueEvent(&delta.baseEvent);
        }

        #endregion
    }
}
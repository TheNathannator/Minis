using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Minis
{
    //
    // Custom control class for MIDI nots
    //
    public class MidiNoteControl : ButtonControl
    {
        internal static void Initialize()
        {
            InputSystem.RegisterLayout<MidiNoteControl>("MidiNote");
        }

        public MidiNoteControl()
        {
            m_StateBlock.format = InputStateBlock.FormatByte;

            // AxisControl parameters
            normalize = true;
            normalizeMax = 0.49803921568f;

            // ButtonControl parameters
            pressPoint = 1.0f / 127;
        }

        // Calculate note number from offset
        public int noteNumber => (int)stateOffsetRelativeToDeviceRoot;

        // Current velocity value; Returns zero when key off.
        public float velocity => ReadValue();
    }
}

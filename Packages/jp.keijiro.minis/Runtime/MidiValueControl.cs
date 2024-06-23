using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Minis
{
    //
    // Custom control class for MIDI controls
    //
    public class MidiValueControl : AxisControl
    {
        internal static void Initialize()
        {
            InputSystem.RegisterLayout<MidiValueControl>("MidiValue");
        }

        public MidiValueControl()
        {
            m_StateBlock.format = InputStateBlock.FormatByte;

            // AxisControl parameters
            normalize = true;
            normalizeMax = 0.49803921568f;
        }

        // Calculate control number from offset
        public int controlNumber => (int)stateOffsetRelativeToDeviceRoot - 128;
    }
}

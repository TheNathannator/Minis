using UnityEditor;

namespace Minis
{
    /// <summary>
    /// Handles initialization of the package.
    /// </summary>
    internal static partial class Initialization
    {
        /// <summary>
        /// Initializes everything.
        /// </summary>
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#endif
        internal static void Initialize()
        {
            MidiButtonControl.Initialize();
            MidiAxisControl.Initialize();
            MidiDevice.Initialize();
        }
    }
}
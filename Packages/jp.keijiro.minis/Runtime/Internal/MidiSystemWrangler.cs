using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;

namespace Minis
{
    //
    // Wrangler class that installs/uninstalls MIDI subsystems on system events
    //
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    static class MidiSystemWrangler
    {
        #region Internal objects and methods

        static MidiDriver _driver;

        static void RegisterLayout()
        {
            InputSystem.RegisterLayout<MidiNoteControl>("MidiNote");
            InputSystem.RegisterLayout<MidiValueControl>("MidiValue");

            InputSystem.RegisterLayout<MidiDevice>(
                matches: new InputDeviceMatcher().WithInterface("Minis")
            );
        }

        #endregion

        #region System initialization/finalization callback

        #if UNITY_EDITOR

        //
        // On Editor, we use InitializeOnLoad to install the subsystem. At the
        // same time, we use AssemblyReloadEvents to temporarily disable the
        // system to avoid issue #1192379.
        // #FIXME This workaround should be removed when the issue is solved.
        //

        static MidiSystemWrangler()
        {
            Initialize();
        }

        #endif

        //
        // On Player, we use RuntimeInitializeOnLoadMethod to install the
        // subsystems. We don't do anything about finalization.
        //

    #if !UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod]
    #endif
        static void Initialize()
        {
            RegisterLayout();
            _driver = new MidiDriver();

            // We use the input system's onBeforeUpdate callback to queue events
            // just before it processes its event queue. This is called in both
            // edit mode and play mode.
            InputSystem.onBeforeUpdate += () => _driver?.Update();

        #if UNITY_EDITOR

            // Uninstall the driver on domain reload.
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += () => {
                _driver?.Dispose();
                _driver = null;
            };

            // Reinstall the driver after domain reload.
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += () => {
                _driver = _driver ?? new MidiDriver();
            };
        #endif
        }

        #endregion
    }
}

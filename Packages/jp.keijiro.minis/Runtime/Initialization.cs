using System;
using UnityEditor;
using UnityEngine;

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
#if UNITY_EDITOR
            // Uninstall the driver on domain reload.
            AssemblyReloadEvents.beforeAssemblyReload += Uninitialize;
            // Uninstall the driver when quitting.
            EditorApplication.quitting += Uninitialize;
#else
            // Uninstall the driver when quitting.
            Application.quitting += Uninitialize;
#endif

            try
            {
                MidiSystemWrangler.Initialize();
            }
            catch (Exception ex)
            {
                Debug.LogError("[Minis] Failed to initialize backends!");
                Debug.LogException(ex);
            }
        }

        internal static void Uninitialize()
        {
#if UNITY_EDITOR
            AssemblyReloadEvents.beforeAssemblyReload -= Uninitialize;
            EditorApplication.quitting -= Uninitialize;
#else
            Application.quitting -= Uninitialize;
#endif

            try
            {
                MidiSystemWrangler.Uninitialize();
            }
            catch (Exception ex)
            {
                Debug.LogError("[Minis] Failed to uninitialize backends!");
                Debug.LogException(ex);
            }
        }
    }
}
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting;

[assembly: AlwaysLinkAssembly]

namespace Minis.Backend
{
    /// <summary>
    /// Handles initialization of the package.
    /// </summary>
    internal static partial class Initialization
    {
        private static MidiBackend _backend;

        /// <summary>
        /// Initializes everything.
        /// </summary>
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.BeforeSceneLoad)]
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

            if (MidiBackend.TryCreate(out var backend))
            {
                _backend = backend;
                _backend.Start();
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
                _backend?.Dispose();
                _backend = null;
            }
            catch (Exception ex)
            {
                Debug.LogError("[Minis] Failed to uninitialize backends!");
                Debug.LogException(ex);
            }
        }
    }
}
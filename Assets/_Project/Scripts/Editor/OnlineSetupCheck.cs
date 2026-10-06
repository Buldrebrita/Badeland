using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Badeland.EditorTools
{
    /// <summary>
    /// Always-available check for the online setup. The networking scripts only compile once the Netcode package
    /// is installed, so when the "Create S4" menu is missing this says which step is not working.
    /// Menu: Badeland > Check Online Setup.
    /// </summary>
    public static class OnlineSetupCheck
    {
        [MenuItem("Badeland/Check Online Setup")]
        public static void Check()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            string[] names = assemblies.Select(a => a.GetName().Name).ToArray();

            bool netcodePackage = names.Contains("Unity.Netcode.Runtime");
            bool networking = names.Contains("Badeland.Networking");
            bool networkingEditor = names.Contains("Badeland.Networking.Editor");
            string[] netcodeAssemblies = names.Where(n => n.StartsWith("Unity.Netcode", StringComparison.Ordinal)).ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("Netcode package loaded: " + Yes(netcodePackage));
            sb.AppendLine("Badeland.Networking compiled: " + Yes(networking));
            sb.AppendLine("Badeland.Networking.Editor compiled: " + Yes(networkingEditor));
            sb.AppendLine();
            sb.AppendLine("Netcode assemblies found: " + (netcodeAssemblies.Length == 0 ? "none" : string.Join(", ", netcodeAssemblies)));
            sb.AppendLine();

            if (!netcodePackage)
                sb.AppendLine("Next: install the package com.unity.netcode.gameobjects (Window > Package Manager > + > Install package by name).");
            else if (!networking)
                sb.AppendLine("Netcode is installed but the Badeland networking scripts did not compile. Open the Console (Window > General > Console) and send me the red errors. If there are none, restart Unity once.");
            else if (!networkingEditor)
                sb.AppendLine("The networking scripts compiled but the editor tools did not. Check the Console for red errors.");
            else
                sb.AppendLine("All good: the menu Badeland > Create S4 Network Test Scene should be there.");

            Debug.Log("Badeland online setup check:\n" + sb);
            EditorUtility.DisplayDialog("Badeland online setup", sb.ToString(), "OK");
        }

        static string Yes(bool value) => value ? "yes" : "NO";
    }
}

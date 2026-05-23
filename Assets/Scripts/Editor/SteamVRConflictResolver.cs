using UnityEngine;
using UnityEditor;
using System;
using System.Diagnostics;

/// <summary>
/// Detector y resolutor automático de conflictos SteamVR + Meta Quest
/// </summary>
public class SteamVRConflictResolver : EditorWindow
{
    private string currentRuntime = "";
    private bool metaLinkRunning = false;
    private bool steamVRRunning = false;
    private bool conflictDetected = false;

    [MenuItem("Tools/TITA/Resolver Conflictos SteamVR")]
    public static void ShowWindow()
    {
        var window = GetWindow<SteamVRConflictResolver>("SteamVR Conflict Resolver");
        window.RefreshStatus();
    }

    void OnGUI()
    {
        GUILayout.Label("SteamVR Conflict Resolver", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Status display
        GUILayout.BeginVertical("box");
        GUILayout.Label("Estado Actual del Sistema:", EditorStyles.boldLabel);
        
        GUILayout.Label($"OpenXR Runtime: {(string.IsNullOrEmpty(currentRuntime) ? "Verificando..." : currentRuntime)}");
        
        Color originalColor = GUI.color;
        
        // Meta Horizon Link status
        GUI.color = metaLinkRunning ? Color.green : Color.red;
        GUILayout.Label($"Meta Horizon Link: {(metaLinkRunning ? "✅ Ejecutándose" : "❌ No ejecutándose")}");
        
        // SteamVR status
        GUI.color = steamVRRunning ? Color.yellow : Color.green;
        GUILayout.Label($"SteamVR: {(steamVRRunning ? "⚠️ Ejecutándose (puede causar conflictos)" : "✅ No ejecutándose")}");
        
        // Conflict detection
        GUI.color = conflictDetected ? Color.red : Color.green;
        GUILayout.Label($"Conflictos: {(conflictDetected ? "❌ Detectados" : "✅ No detectados")}");
        
        GUI.color = originalColor;
        GUILayout.EndVertical();
        
        GUILayout.Space(10);

        // Action buttons
        if (GUILayout.Button("🔍 Refrescar Estado"))
        {
            RefreshStatus();
        }

        if (conflictDetected)
        {
            GUILayout.Space(5);
            GUI.color = Color.red;
            GUILayout.BeginVertical("box");
            GUILayout.Label("⚠️ CONFLICTOS DETECTADOS", EditorStyles.boldLabel);
            
            if (steamVRRunning && metaLinkRunning)
            {
                GUILayout.Label("• SteamVR y Meta Link ejecutándose simultáneamente");
            }
            
            if (currentRuntime.Contains("steam", StringComparison.OrdinalIgnoreCase))
            {
                GUILayout.Label("• SteamVR configurado como OpenXR runtime");
            }
            
            GUILayout.EndVertical();
            GUI.color = originalColor;
            
            GUILayout.Space(5);
            if (GUILayout.Button("🛠️ Resolver Conflictos Automáticamente"))
            {
                ResolveConflicts();
            }
        }

        GUILayout.Space(10);
        
        // Manual actions
        GUILayout.Label("Acciones Manuales:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("🎯 Configurar Meta como Runtime OpenXR"))
        {
            SetMetaAsOpenXRRuntime();
        }
        
        if (GUILayout.Button("🛑 Cerrar SteamVR"))
        {
            CloseSteamVR();
        }
        
        if (GUILayout.Button("▶️ Iniciar Meta Horizon Link"))
        {
            StartMetaHorizonLink();
        }

        GUILayout.Space(10);
        
        if (GUILayout.Button("📋 Ver Guía Completa de Resolución"))
        {
            ShowFullGuide();
        }
    }

    void RefreshStatus()
    {
        UnityEngine.Debug.Log("=== REFRESH STATUS STEAMVR CONFLICTS ===");
        
        // Check OpenXR runtime
        currentRuntime = GetOpenXRRuntime();
        UnityEngine.Debug.Log($"OpenXR Runtime: {currentRuntime}");
        
        // Check running processes
        metaLinkRunning = IsProcessRunning("OVRServer_x64");
        steamVRRunning = IsProcessRunning("vrserver") || IsProcessRunning("vrstartup");
        
        UnityEngine.Debug.Log($"Meta Horizon Link: {metaLinkRunning}");
        UnityEngine.Debug.Log($"SteamVR: {steamVRRunning}");
        
        // Detect conflicts
        conflictDetected = DetectConflicts();
        
        if (conflictDetected)
        {
            UnityEngine.Debug.LogWarning("⚠️ Conflictos detectados entre SteamVR y Meta Quest");
        }
        else
        {
            UnityEngine.Debug.Log("✅ No se detectaron conflictos");
        }
        
        Repaint();
    }

    bool DetectConflicts()
    {
        // Conflict 1: Both SteamVR and Meta running
        if (steamVRRunning && metaLinkRunning)
            return true;
            
        // Conflict 2: SteamVR as OpenXR runtime but Meta Link installed
        if (currentRuntime.Contains("steam", StringComparison.OrdinalIgnoreCase) && 
            System.IO.Directory.Exists(@"C:\Program Files\Meta Horizon"))
            return true;
            
        return false;
    }

    void ResolveConflicts()
    {
        UnityEngine.Debug.Log("=== RESOLVIENDO CONFLICTOS AUTOMÁTICAMENTE ===");
        
        // Step 1: Close SteamVR if running
        if (steamVRRunning)
        {
            UnityEngine.Debug.Log("🛑 Cerrando SteamVR...");
            CloseSteamVR();
        }
        
        // Step 2: Set Meta as OpenXR runtime
        UnityEngine.Debug.Log("🎯 Configurando Meta como OpenXR runtime...");
        SetMetaAsOpenXRRuntime();
        
        // Step 3: Ensure Meta Horizon Link is running
        if (!metaLinkRunning)
        {
            UnityEngine.Debug.Log("▶️ Iniciando Meta Horizon Link...");
            StartMetaHorizonLink();
        }
        
        // Step 4: Refresh status after changes
        EditorApplication.delayCall += () => {
            RefreshStatus();
            UnityEngine.Debug.Log("✅ Resolución de conflictos completada. Verificar estado.");
        };
    }

    string GetOpenXRRuntime()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-Command \"Get-ItemProperty -Path 'HKLM:\\SOFTWARE\\Khronos\\OpenXR\\1' -Name ActiveRuntime -ErrorAction SilentlyContinue | Select-Object -ExpandProperty ActiveRuntime\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            
            if (process != null)
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"No se pudo leer OpenXR runtime: {e.Message}");
        }
        return "No detectado";
    }

    bool IsProcessRunning(string processName)
    {
        try
        {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    void SetMetaAsOpenXRRuntime()
    {
        try
        {
            string metaRuntimePath = @"C:\Program Files\Meta Horizon\Support\oculus-runtime\oculus_openxr_64.json";
            
            if (!System.IO.File.Exists(metaRuntimePath))
            {
                UnityEngine.Debug.LogError("❌ Meta OpenXR runtime no encontrado. ¿Meta Horizon Link instalado?");
                return;
            }
            
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = $"-Command \"Start-Process 'reg' -ArgumentList 'add', 'HKEY_LOCAL_MACHINE\\SOFTWARE\\Khronos\\OpenXR\\1', '/v', 'ActiveRuntime', '/t', 'REG_SZ', '/d', '{metaRuntimePath}', '/f' -Verb RunAs\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });
            
            if (process != null)
            {
                process.WaitForExit();
                UnityEngine.Debug.Log("✅ Meta configurado como OpenXR runtime");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"❌ Error configurando Meta OpenXR runtime: {e.Message}");
            UnityEngine.Debug.LogError("Ejecutar como administrador: reg add \"HKEY_LOCAL_MACHINE\\SOFTWARE\\Khronos\\OpenXR\\1\" /v ActiveRuntime /t REG_SZ /d \"C:\\Program Files\\Meta Horizon\\Support\\oculus-runtime\\oculus_openxr_64.json\" /f");
        }
    }

    void CloseSteamVR()
    {
        try
        {
            foreach (var processName in new[] { "vrserver", "vrstartup", "vrcompositor", "vrmonitor" })
            {
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    process.Kill();
                    UnityEngine.Debug.Log($"🛑 Cerrado: {processName}");
                }
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"Error cerrando SteamVR: {e.Message}");
        }
    }

    void StartMetaHorizonLink()
    {
        try
        {
            string[] possiblePaths = {
                @"C:\Program Files\Meta Horizon\Meta Horizon Link.exe",
                @"C:\Program Files\Oculus\OculusClient.exe"
            };
            
            foreach (string path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    Process.Start(path);
                    UnityEngine.Debug.Log($"▶️ Iniciando: {path}");
                    return;
                }
            }
            
            UnityEngine.Debug.LogWarning("❌ No se encontró Meta Horizon Link. Iniciar manualmente.");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"Error iniciando Meta Horizon Link: {e.Message}");
        }
    }

    void ShowFullGuide()
    {
        string guidePath = Application.dataPath + "/../STEAMVR_CONFLICT_RESOLUTION.md";
        if (System.IO.File.Exists(guidePath))
        {
            Application.OpenURL(guidePath);
        }
        else
        {
            UnityEngine.Debug.LogError("Guía no encontrada en: " + guidePath);
        }
    }
}
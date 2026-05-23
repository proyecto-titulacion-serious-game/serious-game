using UnityEngine;
using UnityEditor;
using System;
using System.Diagnostics;
using System.IO;

/// <summary>
/// Diagnóstico avanzado de OpenXR Runtime y detección de conflictos SteamVR/Meta
/// </summary>
public class XRRuntimeDiagnostic : EditorWindow
{
    [MenuItem("Tools/TITA/Diagnosticar OpenXR Runtime")]
    public static void ShowWindow()
    {
        GetWindow<XRRuntimeDiagnostic>("XR Runtime Diagnostic");
    }

    void OnGUI()
    {
        GUILayout.Label("XR Runtime Diagnostic", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Analizar OpenXR Runtime"))
        {
            DiagnoseOpenXRRuntime();
        }
        
        if (GUILayout.Button("Detectar Conflictos SteamVR/Meta"))
        {
            DetectRuntimeConflicts();
        }
        
        if (GUILayout.Button("Configurar Meta como Runtime Activo"))
        {
            SetMetaAsActiveRuntime();
        }
        
        if (GUILayout.Button("Verificar Drivers Meta Quest"))
        {
            CheckMetaQuestDrivers();
        }
    }

    static void DiagnoseOpenXRRuntime()
    {
        UnityEngine.Debug.Log("=== DIAGNÓSTICO OPENXR RUNTIME ===");
        
        // Verificar registry Windows para OpenXR runtime activo
        try
        {
            string activeRuntime = GetActiveOpenXRRuntime();
            UnityEngine.Debug.Log($"🎯 OpenXR Runtime Activo: {activeRuntime}");
            
            if (activeRuntime.Contains("SteamVR"))
            {
                UnityEngine.Debug.LogWarning("⚠️  SteamVR está configurado como runtime OpenXR");
                UnityEngine.Debug.LogWarning("   Esto puede causar conflictos con Meta Quest Link");
                UnityEngine.Debug.LogWarning("   Solución: Cambiar a Meta OpenXR Runtime");
            }
            else if (activeRuntime.Contains("Meta") || activeRuntime.Contains("Oculus"))
            {
                UnityEngine.Debug.Log("✅ Meta OpenXR Runtime activo (correcto para Quest)");
            }
            else
            {
                UnityEngine.Debug.LogError($"❌ Runtime desconocido: {activeRuntime}");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"❌ Error leyendo registry OpenXR: {e.Message}");
        }
    }

    static string GetActiveOpenXRRuntime()
    {
        try
        {
            // Leer registry de Windows para OpenXR runtime activo
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "reg",
                    Arguments = @"query ""HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1"" /v ActiveRuntime",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            if (output.Contains("ActiveRuntime"))
            {
                string[] lines = output.Split('\n');
                foreach (string line in lines)
                {
                    if (line.Contains("ActiveRuntime"))
                    {
                        string[] parts = line.Split(new string[] { "REG_SZ" }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            return parts[1].Trim();
                        }
                    }
                }
            }
            
            return "No encontrado";
        }
        catch
        {
            return "Error accediendo registry";
        }
    }

    static void DetectRuntimeConflicts()
    {
        UnityEngine.Debug.Log("=== DETECCIÓN DE CONFLICTOS RUNTIME ===");
        
        // Verificar SteamVR instalado
        bool steamVRInstalled = IsSteamVRInstalled();
        UnityEngine.Debug.Log($"SteamVR Instalado: {(steamVRInstalled ? "✅ Sí" : "❌ No")}");
        
        // Verificar Meta Horizon Link instalado
        bool metaLinkInstalled = IsMetaHorizonLinkInstalled();
        UnityEngine.Debug.Log($"Meta Horizon Link: {(metaLinkInstalled ? "✅ Sí" : "❌ No")}");
        
        // Verificar procesos en ejecución
        bool steamVRRunning = IsProcessRunning("vrserver");
        bool metaLinkRunning = IsProcessRunning("OVRServer_x64");
        
        UnityEngine.Debug.Log($"SteamVR ejecutándose: {(steamVRRunning ? "⚠️  Sí" : "❌ No")}");
        UnityEngine.Debug.Log($"Meta Link ejecutándose: {(metaLinkRunning ? "✅ Sí" : "❌ No")}");
        
        // Análisis de conflictos
        if (steamVRRunning && metaLinkRunning)
        {
            UnityEngine.Debug.LogWarning("⚠️  CONFLICTO DETECTADO: SteamVR y Meta Link ejecutándose simultáneamente");
            UnityEngine.Debug.LogWarning("   Solución: Cerrar SteamVR o configurar Meta como runtime predeterminado");
        }
        
        string activeRuntime = GetActiveOpenXRRuntime();
        if (steamVRInstalled && activeRuntime.Contains("SteamVR") && metaLinkInstalled)
        {
            UnityEngine.Debug.LogWarning("⚠️  CONFIGURACIÓN CONFLICTIVA:");
            UnityEngine.Debug.LogWarning("   - SteamVR configurado como OpenXR runtime");
            UnityEngine.Debug.LogWarning("   - Meta Horizon Link instalado");
            UnityEngine.Debug.LogWarning("   - Recomendación: Cambiar a Meta OpenXR Runtime");
        }
    }

    static bool IsSteamVRInstalled()
    {
        string[] steamPaths = {
            @"C:\Program Files (x86)\Steam\steamapps\common\SteamVR",
            @"C:\Program Files\Steam\steamapps\common\SteamVR",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) + @"\Steam\steamapps\common\SteamVR"
        };
        
        foreach (string path in steamPaths)
        {
            if (Directory.Exists(path))
                return true;
        }
        return false;
    }

    static bool IsMetaHorizonLinkInstalled()
    {
        string[] metaPaths = {
            @"C:\Program Files\Oculus",
            @"C:\Program Files (x86)\Oculus",
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) + @"\Oculus"
        };
        
        foreach (string path in metaPaths)
        {
            if (Directory.Exists(path))
                return true;
        }
        return false;
    }

    static bool IsProcessRunning(string processName)
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

    static void SetMetaAsActiveRuntime()
    {
        UnityEngine.Debug.Log("=== CONFIGURANDO META COMO RUNTIME ACTIVO ===");
        UnityEngine.Debug.Log("⚠️  ACCIÓN MANUAL REQUERIDA:");
        UnityEngine.Debug.Log("1. Abrir Meta Horizon Link");
        UnityEngine.Debug.Log("2. Ir a Settings → General → OpenXR Runtime");
        UnityEngine.Debug.Log("3. Seleccionar 'Meta' como Active Runtime");
        UnityEngine.Debug.Log("4. Reiniciar Unity");
        
        UnityEngine.Debug.Log("\n🔧 ALTERNATIVA - Comando PowerShell (ejecutar como Admin):");
        UnityEngine.Debug.Log(@"reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Khronos\OpenXR\1"" /v ActiveRuntime /t REG_SZ /d ""C:\Program Files\Oculus\Support\oculus-runtime\oculus_openxr_64.json"" /f");
    }

    static void CheckMetaQuestDrivers()
    {
        UnityEngine.Debug.Log("=== VERIFICACIÓN DRIVERS META QUEST ===");
        
        // Verificar ADB (Android Debug Bridge) para Quest
        bool adbInstalled = IsADBInstalled();
        UnityEngine.Debug.Log($"ADB (Android Debug Bridge): {(adbInstalled ? "✅ Instalado" : "❌ No encontrado")}");
        
        // Verificar Oculus drivers
        bool oculusDrivers = Directory.Exists(@"C:\Program Files\Oculus\Support\oculus-drivers");
        UnityEngine.Debug.Log($"Oculus USB Drivers: {(oculusDrivers ? "✅ Instalados" : "❌ No encontrados")}");
        
        UnityEngine.Debug.Log("\n📋 VERIFICACIONES RECOMENDADAS:");
        UnityEngine.Debug.Log("1. Quest 3 conectado via USB y detectado en Meta Horizon Link");
        UnityEngine.Debug.Log("2. Developer Mode habilitado en Quest 3");
        UnityEngine.Debug.Log("3. USB Debugging permitido");
        UnityEngine.Debug.Log("4. Link Cable funcionando (cable oficial Meta o compatible USB 3.0+)");
    }

    static bool IsADBInstalled()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = "version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
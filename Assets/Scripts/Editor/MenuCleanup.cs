using UnityEditor;
using UnityEngine;

/// <summary>
/// Script para limpiar menús duplicados y refrescar el sistema de menús de Unity
/// </summary>
public class MenuCleanup
{
    [MenuItem("Tools/TITA/🔧 Menu System/Refresh Unity Menus")]
    public static void RefreshUnityMenus()
    {
        Debug.Log("=== REFRESHING UNITY MENUS ===");
        
        // Forzar recompilación y refresh de menús
        AssetDatabase.Refresh();
        EditorUtility.RequestScriptReload();
        
        Debug.Log("✅ Unity menus refreshed. Menús duplicados deberían estar resueltos.");
        Debug.Log("Si persiste el error, reiniciar Unity Editor.");
    }
    
    [MenuItem("Tools/TITA/🔧 Menu System/List All TITA Menus")]
    public static void ListAllTitaMenus()
    {
        Debug.Log("=== LISTADO DE MENÚS TITA ===");
        
        Debug.Log("📋 Menús VR Principales:");
        Debug.Log("  • Setup Completo VR Explorador (nuevo, robusto)");
        Debug.Log("  • [LEGACY] Setup Explorador VR Específico (legacy)");
        Debug.Log("");
        
        Debug.Log("🔧 Herramientas VR Fix:");
        Debug.Log("  • Fix PlayerController Components");
        Debug.Log("  • Fix PlayerController Input Actions");
        Debug.Log("  • Resolver Conflictos SteamVR");
        Debug.Log("");
        
        Debug.Log("🔍 Diagnóstico VR:");
        Debug.Log("  • Diagnosticar VR");
        Debug.Log("  • Diagnosticar OpenXR Runtime");
        Debug.Log("  • Safe Diagnostic PlayerController");
        Debug.Log("");
        
        Debug.Log("🧪 Testing:");
        Debug.Log("  • Test All VR Fixes");
        Debug.Log("  • Compile Test - Verify No Errors");
        Debug.Log("");
        
        Debug.Log("📌 Recomendación: Usar 'Setup Completo VR Explorador' (nuevo) para setup inicial");
    }
    
    [MenuItem("Tools/TITA/🔧 Menu System/Clean Legacy Scripts")]
    public static void CleanLegacyScripts()
    {
        Debug.Log("=== CLEANING LEGACY SCRIPTS ===");
        
        bool foundLegacy = false;
        
        // Verificar scripts legacy que pueden causar conflictos
        string[] legacyPaths = {
            "Assets/Editor/ExplorerVRSetupTool.cs",
            "Assets/Editor/TechnicianVRSetupTool.cs"
        };
        
        foreach (string path in legacyPaths)
        {
            if (System.IO.File.Exists(path))
            {
                foundLegacy = true;
                Debug.LogWarning($"⚠️ Legacy script encontrado: {path}");
                Debug.LogWarning($"   Menú legacy: [LEGACY] Setup Explorador VR Específico");
            }
        }
        
        if (!foundLegacy)
        {
            Debug.Log("✅ No se encontraron scripts legacy conflictivos");
        }
        else
        {
            Debug.Log("");
            Debug.Log("📋 Scripts legacy mantienen funcionalidad específica del Explorador");
            Debug.Log("📋 Para setup general VR, usar: 'Setup Completo VR Explorador' (nuevo)");
        }
    }
}
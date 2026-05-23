# Menu Conflict Resolution - TITA VR Tools

## ❌ **Error Encontrado**
```
Cannot add menu item 'Tools/TITA/Setup Completo VR Explorador' for method 'VRSetupTool.SetupCompleteVR' because a menu item with the same name already exists.
```

## 🎯 **Causa**
Múltiples scripts Editor con el mismo nombre de menú:
- `Assets/Editor/ExplorerVRSetupTool.cs` (legacy, específico Explorador)
- `Assets/Scripts/Editor/VRSetupTool.cs` (nuevo, general)

## ✅ **Solución Aplicada**

### **1. Renombrado Menu Legacy**
```csharp
// ANTES:
[MenuItem("Tools/TITA/Setup Completo VR Explorador")]

// DESPUÉS:
[MenuItem("Tools/TITA/[LEGACY] Setup Explorador VR Específico")]
```

### **2. Mantenido Menu Nuevo**
```csharp
// MANTIENE:
[MenuItem("Tools/TITA/Setup Completo VR Explorador")]  // VRSetupTool.cs
```

### **3. Added Menu Utilities**
```csharp
[MenuItem("Tools/TITA/🔧 Menu System/Refresh Unity Menus")]
[MenuItem("Tools/TITA/🔧 Menu System/List All TITA Menus")]  
[MenuItem("Tools/TITA/🔧 Menu System/Clean Legacy Scripts")]
```

## 📋 **Menús TITA Organizados**

### **✅ VR Setup Principal (USAR ESTOS):**
```
Tools → TITA → Setup Completo VR Explorador (⭐ NUEVO, ROBUSTO)
Tools → TITA → Fix PlayerController Components
Tools → TITA → Fix PlayerController Input Actions
Tools → TITA → Resolver Conflictos SteamVR
```

### **🔍 Diagnóstico VR:**
```
Tools → TITA → Diagnosticar VR
Tools → TITA → Diagnosticar OpenXR Runtime
Tools → TITA → Safe Diagnostic PlayerController
```

### **🧪 Testing:**
```
Tools → TITA → Test All VR Fixes
Tools → TITA → Compile Test - Verify No Errors
```

### **📜 Legacy (Funcionalidad Específica):**
```
Tools → TITA → [LEGACY] Setup Explorador VR Específico
Tools → TITA → Setup VR Técnico (Meta Quest)
```

### **🔧 Menu System:**
```
Tools → TITA → 🔧 Menu System → Refresh Unity Menus
Tools → TITA → 🔧 Menu System → List All TITA Menus  
Tools → TITA → 🔧 Menu System → Clean Legacy Scripts
```

## 🚀 **Workflow Recomendado**

### **Para Setup VR Inicial:**
1. **Tools → TITA → Setup Completo VR Explorador** (⭐ NUEVO)
2. **Tools → TITA → Test All VR Fixes** (verificación)
3. **Conectar Quest 3 + Test**

### **Para Troubleshooting:**
1. **Tools → TITA → Diagnosticar VR**
2. **Tools → TITA → Resolver Conflictos SteamVR** (si hay problemas)
3. **Tools → TITA → Fix PlayerController Components** (si hay errores)

### **Si Hay Menu Conflicts:**
1. **Tools → TITA → 🔧 Menu System → Refresh Unity Menus**
2. **Reiniciar Unity Editor** (si persiste)

## 🎯 **Diferencias Legacy vs Nuevo**

### **ExplorerVRSetupTool.cs (Legacy)**
- ✅ **Específico**: Solo para personaje Explorador
- ✅ **Detallado**: Setup completo manos, avatar, animaciones
- ✅ **Funcional**: Mantiene funcionalidad específica
- ⚠️ **Limitado**: Solo funciona si hay objetos específicos en escena

### **VRSetupTool.cs (Nuevo)**
- ✅ **General**: Funciona con cualquier setup VR
- ✅ **Robusto**: Manejo de errores y casos edge
- ✅ **Automático**: Fix automático de problemas comunes
- ✅ **Compatible**: Todas las versiones Unity XR Toolkit

## 🔄 **Prevención Futura**

### **Guidelines para Scripts Editor:**
1. **Unique Menu Names**: Nunca duplicar nombres exactos
2. **Namespace Organization**: Usar subcategorías (🔧 Menu System)
3. **Legacy Marking**: Marcar scripts antiguos como [LEGACY]
4. **Version Compatibility**: Scripts nuevos deben ser compatibles

### **Testing Workflow:**
1. Después de agregar nuevos menús: **Refresh Unity Menus**
2. Verificar no hay conflictos: **List All TITA Menus**
3. Test funcionalidad: **Test All VR Fixes**

## ✅ **Estado Resuelto**

### **Console Esperado:**
```
✅ Unity menus refreshed. Menús duplicados deberían estar resueltos.
📋 Recomendación: Usar 'Setup Completo VR Explorador' (nuevo) para setup inicial
```

### **Unity Editor:**
- ❌ **Error de menu conflict**: Resuelto
- ✅ **Menús organizados**: Por categorías
- ✅ **Workflow claro**: Setup nuevo vs Legacy
- ✅ **Tools de maintenance**: Menu System utilities

---

**Status**: ✅ **MENU CONFLICTS RESUELTOS**  
**Recommendation**: Usar **Tools → TITA → Setup Completo VR Explorador** para setup VR general
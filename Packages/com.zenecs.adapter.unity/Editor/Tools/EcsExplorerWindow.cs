// #if UNITY_EDITOR
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using UnityEditor;
// using UnityEngine;
// using ZenECS.Adapter.Unity.Binding;
// using ZenECS.Core;
// using ZenECS.Core.Infrastructure;
// using ZenECS.EditorUtils;
// using ZenECS.EditorCommon; // ZenFoldoutHeader / ZenComponentFormGUI
//
// namespace ZenECS.EditorTools
// {
//     public sealed class EcsExplorerWindow : EditorWindow
//     {
//         [MenuItem("ZenECS/Tools/ECS Explorer")]
//         public static void Open() => GetWindow<EcsExplorerWindow>("ECS Explorer");
//
//         // --- UI labels/tooltips (English) ---
//         const string LABEL_ENTITY_ID = "Entity ID: ";
//         const string BTN_FIND = "Find";
//         const string BTN_CLEAR_FILTER = "Clear";
//         const string TIP_FIND = "Show only the entity with this ID (no system switching).";
//         const string TIP_CLEAR = "Exit single-entity view and show all entities.";
//
//         // --- Single-entity Find mode state ---
//         string _entityIdText = "";
//         int? _findEntityId = null; // current target ID (null => list mode)
//         bool _findMode = false; // single view mode on/off
//         Entity _foundEntity; // resolved entity
//         bool _foundValid = false; // found in world?
//
//         // --- Other UI/layout state ---
//         Vector2 _left, _right;
//         int _selSystem = -1;
//         readonly List<Entity> _cache = new(256);
//         double _nextRepaint;
//         private int _selSysEntityCount;
//
//         readonly Dictionary<int, bool> _entityFold = new(); // entityId → fold
//         readonly Dictionary<string, bool> _componentFold = new(); // $"{entityId}:{typeName}" → fold
//         bool _editMode = true;
//
//         void OnEnable()
//         {
//             EditorApplication.update += OnEditorUpdate;
//             EditorApplication.playModeStateChanged += OnPlayModeChanged;
//             AssemblyReloadEvents.beforeAssemblyReload += OnBeforeReload;
//         }
//
//         void OnDisable()
//         {
//             EditorApplication.update -= OnEditorUpdate;
//             EditorApplication.playModeStateChanged -= OnPlayModeChanged;
//             AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeReload;
//         }
//
//         void OnEditorUpdate()
//         {
//             if (EditorApplication.timeSinceStartup > _nextRepaint)
//             {
//                 _nextRepaint = EditorApplication.timeSinceStartup + 0.25;
//                 Repaint();
//             }
//         }
//
//         void OnBeforeReload()
//         {
//             _selSystem = -1;
//             _selSysEntityCount = 0;
//             _cache.Clear();
//             _entityFold.Clear();
//             _componentFold.Clear();
//             _findMode = false;
//             _findEntityId = null;
//             _foundValid = false;
//         }
//
//         void OnPlayModeChanged(PlayModeStateChange s)
//         {
//             if (s == PlayModeStateChange.ExitingPlayMode || s == PlayModeStateChange.EnteredEditMode)
//             {
//                 EcsRuntimeDirectory.Detach();
//                 _selSystem = -1;
//                 _selSysEntityCount = 0;
//                 _cache.Clear();
//                 _findMode = false;
//                 _findEntityId = null;
//                 _foundValid = false;
//                 Repaint();
//             }
//         }
//
//         void OnGUI()
//         {
//             var world = EcsRuntimeDirectory.World;
//             var systems = EcsRuntimeDirectory.RunningSystems; // running system only (not init/deinit)
//
//             // ====== Vertical Splitter Layout ======
//             using (new EditorGUILayout.HorizontalScope())
//             {
//                 // ---------- Left: Systems ----------
//                 using (var sv = new EditorGUILayout.ScrollViewScope(_left, GUILayout.Width(300)))
//                 {
//                     _left = sv.scrollPosition;
//
//                     EditorGUILayout.Space(4);
//                     using (new EditorGUILayout.HorizontalScope())
//                     {
//                         EditorGUILayout.LabelField("Systems", EditorStyles.boldLabel);
//                         GUILayout.FlexibleSpace();
//                         if (GUILayout.Button("Clear", GUILayout.Width(60)))
//                         {
//                             _selSystem = -1;
//                             _selSysEntityCount = 0;
//                             _cache.Clear();
//                         }
//                     }
//
//                     EditorGUILayout.Space(4);
//
//                     if (systems.Count == 0)
//                         EditorGUILayout.HelpBox("No systems registered. Ensure init & attach.", MessageType.Info);
//
//                     for (int i = 0; i < systems.Count; i++)
//                     {
//                         var s = systems[i];
//                         string name = s.GetType().Name;
//                         if (GUILayout.Toggle(_selSystem == i, name, "Button")) _selSystem = i;
//                     }
//                 }
//
//                 // ---------- Right: Entities ----------
//                 using (var sv = new EditorGUILayout.ScrollViewScope(_right))
//                 {
//                     _right = sv.scrollPosition;
//
//                     EditorGUILayout.Space(4);
//
//                     // --- Toolbar (Find / Clear Filter / Edit Mode) ---
//                     using (new EditorGUILayout.HorizontalScope())
//                     {
//                         if (_selSysEntityCount > 0)
//                         {
//                             EditorGUILayout.LabelField($"Entities ({_selSysEntityCount})", EditorStyles.boldLabel);
//                         }
//                         else
//                         {
//                             EditorGUILayout.LabelField("Entities", EditorStyles.boldLabel);
//                         }
//
//                         GUILayout.FlexibleSpace();
//                     }
//
//                     EditorGUILayout.Space(4);
//
//                     bool done = false;
//
//                     // --- Single-entity view takes precedence ---
//                     if (_findMode)
//                     {
//                         using (new EditorGUILayout.VerticalScope("box"))
//                         {
//                             if (_findEntityId.HasValue)
//                             {
//                                 if (world == null)
//                                 {
//                                     EditorGUILayout.HelpBox("World not attached.", MessageType.Warning);
//                                 }
//                                 else if (_foundValid)
//                                 {
//                                     EditorGUILayout.LabelField($"Entity #{_findEntityId.Value}",
//                                         EditorStyles.boldLabel);
//                                     GUILayout.Space(2);
//                                     DrawOneEntity(world, _foundEntity);
//                                 }
//                                 else
//                                 {
//                                     EditorGUILayout.HelpBox(
//                                         $"No entity with ID {_findEntityId.Value} in this World.",
//                                         MessageType.Info
//                                     );
//                                 }
//                             }
//                             else
//                             {
//                                 EditorGUILayout.HelpBox(
//                                     "Please enter a valid positive numeric Entity ID.",
//                                     MessageType.Warning
//                                 );
//                             }
//
//                             GUILayout.Space(4);
//                             if (GUILayout.Button("Back to List"))
//                             {
//                                 _entityIdText = "";
//                                 _findEntityId = null;
//                                 _foundValid = false;
//                                 _findMode = false;
//                             }
//                         }
//
//                         done = true; // stop here in single view mode
//                     }
//
//                     if (!done)
//                     {
//                         // --- Normal (list) mode below ---
//                         if (_selSystem < 0 || _selSystem >= systems.Count)
//                         {
//                             EditorGUILayout.HelpBox("Select a system to inspect.", MessageType.None);
//                             done = true;
//                         }
//                     }
//
//                     if (!done)
//                     {
//                         if (world == null)
//                         {
//                             EditorGUILayout.HelpBox("World not attached.", MessageType.Warning);
//                             done = true;
//                         }
//                     }
//
//                     if (!done)
//                     {
//                         var sys = systems[_selSystem];
//                         _cache.Clear();
//
//                         if (!ZenECS.Adapter.Unity.Infrastructure.WatchQueryRunner.TryCollectByWatch(sys, world, _cache))
//                             EditorGUILayout.HelpBox("No inspector. Implement IInspectableSystem or add [Watch].",
//                                 MessageType.Info);
//
//                         _selSysEntityCount = _cache.Count;
//
//                         foreach (var e in _cache.Distinct())
//                             DrawOneEntity(world, e);
//                     }
//                 }
//             }
//             
//             GUILayout.Space(4);
//             DrawFooter();
//         }
//
//         private void DrawFooter()
//         {
//             var world = EcsRuntimeDirectory.World;
//             var systems = EcsRuntimeDirectory.RunningSystems; // running system only (not init/deinit)
//
//             using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
//             {
//                 var systemCount = systems?.Count ?? 0;
//                 var entityCount = world?.GetAllEntities()?.Count ?? 0;
//
//                 GUILayout.Label($"Systems: {systemCount}");
//                 GUILayout.Space(12);
//                 GUILayout.Label($"Total Entities: {entityCount}");
//
//                 GUILayout.FlexibleSpace();
//
//                 GUILayout.Label(LABEL_ENTITY_ID, GUILayout.Width(55));
//                 _entityIdText = GUILayout.TextField(_entityIdText, GUILayout.Width(80));
//                 _entityIdText = new string(_entityIdText.Where(char.IsDigit).ToArray());
//
//                 // Find => enter single-entity view (no system switching)
//                 var contentFind = new GUIContent(BTN_FIND, TIP_FIND);
//                 if (GUILayout.Button(contentFind, GUILayout.Width(56)))
//                 {
//                     if (int.TryParse(_entityIdText, out var id) && id > 0)
//                     {
//                         _findEntityId = id;
//                         _foundValid = TryResolveEntityById(world, id, out _foundEntity);
//                         _findMode = true;
//                     }
//                     else
//                     {
//                         _findEntityId = null;
//                         _foundValid = false;
//                         _findMode = true; // still enter to show guidance
//                     }
//
//                     Repaint();
//                 }
//
//                 // Clear Filter => exit single-entity view
//                 var contentClear = new GUIContent(BTN_CLEAR_FILTER, TIP_CLEAR);
//                 if (GUILayout.Button(contentClear, GUILayout.Width(60)))
//                 {
//                     _entityIdText = "";
//                     _findEntityId = null;
//                     _foundValid = false;
//                     _findMode = false;
//                     Repaint();
//                 }
//
//                 _editMode = GUILayout.Toggle(_editMode, "Edit", "Button", GUILayout.Width(60));
//             }
//         }
//
//         // --- Try to resolve by ID without switching systems ---
//         bool TryResolveEntityById(object world, int id, out Entity e)
//         {
//             e = default;
//             if (world == null) return false;
//             var wType = world.GetType();
//
//             // (A) World.IsAlive(int) + new Entity(id)
//             var miIsAlive = wType.GetMethod("IsAlive", new[] { typeof(int) });
//             if (miIsAlive != null)
//             {
//                 try
//                 {
//                     if ((bool)miIsAlive.Invoke(world, new object[] { id }))
//                     {
//                         e = (Entity)Activator.CreateInstance(typeof(Entity), id);
//                         return true;
//                     }
//                 }
//                 catch
//                 {
//                     /* ignore */
//                 }
//             }
//
//             // (B) World.AllEntities() → IEnumerable<Entity> scan
//             var miAll = wType.GetMethod("GetAllEntities", Type.EmptyTypes);
//             if (miAll != null)
//             {
//                 try
//                 {
//                     var enumerable = miAll.Invoke(world, null) as System.Collections.IEnumerable;
//                     if (enumerable != null)
//                     {
//                         foreach (var obj in enumerable)
//                         {
//                             if (obj is Entity en && en.Id == id)
//                             {
//                                 e = en;
//                                 return true;
//                             }
//                         }
//                     }
//                 }
//                 catch
//                 {
//                     /* ignore */
//                 }
//             }
//
//             // (C) Add your direct API here if exists, e.g. TryGetEntity(int, out Entity)
//             // var miTryGet = wType.GetMethod("TryGetEntity",
//             //     new[] { typeof(int), typeof(Entity).MakeByRefType() });
//             // if (miTryGet != null) { ... }
//
//             return false;
//         }
//
//         // --- Render a single entity box (reused by list mode & single view mode) ---
//         void DrawOneEntity(World world, Entity e)
//         {
//             if (!_entityFold.ContainsKey(e.Id)) _entityFold[e.Id] = false;
//
//             using (new EditorGUILayout.VerticalScope("box"))
//             {
//                 // ===== Entity header =====
//                 var headRect = GUILayoutUtility.GetRect(10, EditorGUIUtility.singleLineHeight + 6f,
//                     GUILayout.ExpandWidth(true));
//                 bool openE = _entityFold[e.Id];
//
//                 ZenFoldoutHeader.DrawRow(ref openE, headRect, $"Entity #{e.Id}", "", rRight =>
//                 {
//                     var style = EditorStyles.miniButton;
//
//                     // label/icon candidates
//                     GUIContent addLong = EditorGUIUtility.TrTextContent("+");
//                     GUIContent selLong = EditorGUIUtility.TrTextContent("•");
//
//                     float BtnH(GUIContent gc)
//                     {
//                         if (gc == null) return 0f;
//                         var sz = style.CalcSize(gc);
//                         return Mathf.Ceil(Mathf.Max(EditorGUIUtility.singleLineHeight + 2f,
//                             sz.y + style.margin.vertical + 6f));
//                     }
//
//                     var syncTargetRegistry = EcsRuntimeDirectory.SyncTargetRegistry;
//                     var syncTarget = syncTargetRegistry?.Resolve(e);
//                     var hasView = syncTarget is IUnityViewBinder;
//
//                     GUIContent useAdd = addLong;
//                     float wAdd = 20;
//                     GUIContent useSel = selLong;
//                     float wSel = 20f;
//
//                     float hAdd = BtnH(useAdd);
//                     float hSel = BtnH(useSel);
//                     float hBtn = Mathf.Max(hAdd, hSel);
//                     float yBtn = rRight.y + Mathf.Max(0f, (rRight.height - hBtn) * 0.5f);
//
//                     float right = rRight.xMax;
//                     Rect rDel = new Rect(right - 20, yBtn, 20, hBtn);
//                     Rect rAdd = new Rect(right - (wAdd + 22.5f), yBtn, wAdd, hBtn);
//                     right = rAdd.x - (useSel != null ? 3f : 0f);
//
//                     using (new EditorGUI.DisabledScope(!_editMode))
//                     {
//                         if (GUI.Button(rDel, "x", style))
//                         {
//                             if (EditorUtility.DisplayDialog(
//                                     "Remove Entity",
//                                     $"Remove this entity?\n\nEntity #{e.Id}",
//                                     "Yes", "No"))
//                             {
//                                 world.DestroyEntity(e);
//                                 Repaint();
//                             }
//                         }
//
//                         if (GUI.Button(rAdd, useAdd, style))
//                         {
//                             var all = ZenECS.EditorCommon.ZenComponentPickerWindow.FindAllZenComponents().ToList();
//                             var disabled = new HashSet<Type>();
//                             foreach (var (tHave, _) in world.GetAllComponents(e)) disabled.Add(tHave);
//
//                             ZenComponentPickerWindow.Show(
//                                 all, disabled,
//                                 picked =>
//                                 {
//                                     var inst = ZenECS.Core.ZenDefaults.CreateWithDefaults(picked);
//                                     EcsExplorerApply.AddBoxed(world, e, inst);
//                                     Repaint();
//                                 },
//                                 rAdd,
//                                 $"Entity #{e.Id} Add Component",
//                                 ZenComponentPickerWindow.PickerOpenMode.UtilityFixedWidth
//                             );
//                         }
//                     }
//
//                     if (hasView)
//                     {
//                         Rect rSel = new Rect(right - (wSel), yBtn, wSel, hBtn);
//                         if (GUI.Button(rSel, useSel, style))
//                         {
//                             var t = ((IUnityViewBinder)syncTarget).Go.transform;
//                             UnityEditor.Selection.activeTransform = t;
//                             UnityEditor.EditorGUIUtility.PingObject(t);
//                         }
//                     }
//                 }, true, false);
//                 _entityFold[e.Id] = openE;
//
//                 if (!openE) return;
//
//                 // ===== Summary line =====
//                 var line = EditorGUIUtility.singleLineHeight;
//                 var r = GUILayoutUtility.GetRect(10, line, GUILayout.ExpandWidth(true));
//
//                 var compsEnum = world.GetAllComponents(e);
//                 var arr = compsEnum.ToArray();
//
//                 // Arrow toggle (open/close all visible components)
//                 var rArrow = new Rect(r.x + 3, r.y + 1, 18f, r.height - 2);
//                 var rLabel = new Rect(rArrow.xMax - 1f, r.y, r.width - (rArrow.width + 4f), r.height);
//                 bool allOpen = AreAllComponentsOpen_VisibleOnly(e, arr);
//
//                 EditorGUI.BeginChangeCheck();
//                 bool visNext = EditorGUI.Foldout(rArrow, allOpen, GUIContent.none, false);
//                 EditorGUIUtility.AddCursorRect(rArrow, MouseCursor.Link);
//                 if (EditorGUI.EndChangeCheck())
//                 {
//                     SetAllComponentsFold(world, e, visNext);
//                     Repaint();
//                     GUIUtility.ExitGUI();
//                 }
//
//                 EditorGUI.LabelField(rLabel, $"Components: {arr.Length}");
//                 DrawComponentsList(world, e, arr);
//             }
//         }
//
//         void DrawComponentsList(World world, Entity e, (Type type, object boxed)[] compsArray)
//         {
//             var line = EditorGUIUtility.singleLineHeight;
//
//             using (new EditorGUI.IndentLevelScope())
//             {
//                 foreach (var (t, boxed) in compsArray)
//                 {
//                     var ck = $"{e.Id}:{t.AssemblyQualifiedName}";
//                     if (!_componentFold.ContainsKey(ck)) _componentFold[ck] = false;
//
//                     bool hasFields = ZenComponentFormGUI.HasDrawableFields(t);
//
//                     using (new EditorGUILayout.VerticalScope("box"))
//                     {
//                         // ===== Component header =====
//                         var headRectC = GUILayoutUtility.GetRect(10, line + 6f, GUILayout.ExpandWidth(true));
//                         bool openC = _componentFold[ck];
//
//                         ZenFoldoutHeader.DrawRow(
//                             ref openC,
//                             headRectC,
//                             t.Name,
//                             t.Namespace,
//                             rRight =>
//                             {
//                                 var rReset = new Rect(rRight.xMax - 42.5f, rRight.y, 20, rRight.height);
//                                 var rRemove = new Rect(rRight.xMax - 20, rRight.y, 20, rRight.height);
//
//                                 using (new EditorGUI.DisabledScope(!_editMode))
//                                 {
//                                     using (new EditorGUI.DisabledScope(!hasFields))
//                                     {
//                                         if (GUI.Button(rReset, "R", EditorStyles.miniButton))
//                                         {
//                                             if (EditorUtility.DisplayDialog(
//                                                     "Reset Component",
//                                                     $"Reset to defaults?\n\nEntity #{e.Id} - {t.Name}Component",
//                                                     "Yes", "No"))
//                                             {
//                                                 var def = ZenECS.Core.ZenDefaults.CreateWithDefaults(t);
//                                                 EcsExplorerApply.SetBoxed(world, e, def);
//                                                 Repaint();
//                                             }
//                                         }
//                                     }
//
//                                     if (GUI.Button(rRemove, "X", EditorStyles.miniButton))
//                                     {
//                                         if (EditorUtility.DisplayDialog(
//                                                 "Remove Component",
//                                                 $"Remove this component?\n\nEntity #{e.Id} - {t.Name}Component",
//                                                 "Yes", "No"))
//                                         {
//                                             EcsExplorerApply.RemoveBoxed(world, e, t);
//                                             _componentFold.Remove(ck);
//                                             Repaint();
//                                         }
//                                     }
//                                 }
//                             },
//                             foldable: hasFields,
//                             false
//                         );
//
//                         _componentFold[ck] = hasFields && openC;
//
//                         // ===== body =====
//                         if (!hasFields || !_componentFold[ck]) continue;
//
//                         try
//                         {
//                             object obj = CopyBox(boxed, t);
//                             float bodyH = ZenComponentFormGUI.CalcHeightForObject(obj, t);
//                             bodyH = Mathf.Max(bodyH, EditorGUIUtility.singleLineHeight + 6f);
//
//                             var body = GUILayoutUtility.GetRect(10, bodyH, GUILayout.ExpandWidth(true));
//                             var bodyInner = new Rect(body.x + 4, body.y + 2, body.width - 8, body.height - 4);
//
//                             EditorGUI.BeginChangeCheck();
//                             ZenComponentFormGUI.DrawObject(bodyInner, obj, t);
//                             if (EditorGUI.EndChangeCheck() && _editMode)
//                                 EcsExplorerApply.SetBoxed(world, e, obj);
//                         }
//                         catch (KeyNotFoundException)
//                         {
//                         }
//                     }
//                 }
//             }
//         }
//
//         static int CountLines(string s)
//         {
//             if (string.IsNullOrEmpty(s)) return 1;
//             int c = 1;
//             for (int i = 0; i < s.Length; i++)
//                 if (s[i] == '\n')
//                     c++;
//             return c;
//         }
//
//         // ===== Safe new & shallow copy =====
//         static class SafeNew
//         {
//             public static object New(Type t)
//             {
//                 if (t.IsValueType) return Activator.CreateInstance(t);
//                 var ctor = t.GetConstructor(Type.EmptyTypes);
//                 if (ctor != null) return Activator.CreateInstance(t);
//                 return System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t);
//             }
//         }
//
//         static object CopyBox(object src, Type t)
//         {
//             if (src == null) return SafeNew.New(t);
//             if (t.IsValueType) return src;
//             var dst = SafeNew.New(t);
//             foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Instance))
//                 f.SetValue(dst, f.GetValue(src));
//             return dst;
//         }
//
//         // Expand/collapse helpers
//         void SetAllComponentsFold(World world, Entity e, bool open)
//         {
//             var comps = world.GetAllComponents(e);
//             foreach (var (t, _) in comps)
//             {
//                 if (!ZenComponentFormGUI.HasDrawableFields(t)) continue;
//                 var key = $"{e.Id}:{t.AssemblyQualifiedName}";
//                 _componentFold[key] = open;
//             }
//         }
//
//         bool AreAllComponentsOpen_VisibleOnly(Entity e, (Type type, object boxed)[] comps)
//         {
//             bool any = false;
//             foreach (var (t, _) in comps)
//             {
//                 if (!ZenComponentFormGUI.HasDrawableFields(t)) continue;
//                 any = true;
//                 string key = $"{e.Id}:{t.AssemblyQualifiedName}";
//                 if (!_componentFold.TryGetValue(key, out bool open) || !open)
//                     return false;
//             }
//
//             return any && true;
//         }
//
//         // ===== World API invoker (Set/Replace/Add/Remove) =====
//         static class EcsExplorerApply
//         {
//             static readonly Dictionary<Type, Action<World, Entity, object>> _setCache = new();
//             static readonly Dictionary<Type, Func<World, Entity, bool>> _hasCache = new();
//             static readonly Dictionary<Type, Action<World, Entity>> _removeCache = new();
//             static MethodInfo _miHas, _miSet, _miReplace, _miAdd, _miRemove;
//
//             public static void SetBoxed(World w, Entity e, object boxed)
//             {
//                 if (boxed == null) return;
//                 var t = boxed.GetType();
//
//                 var has = GetHas(w, t);
//                 _ = has?.Invoke(w, e);
//
//                 if (!_setCache.TryGetValue(t, out var setter))
//                 {
//                     setter = BuildSetter(w, t);
//                     _setCache[t] = setter;
//                 }
//
//                 if (setter != null)
//                 {
//                     setter(w, e, boxed);
//                     return;
//                 }
//
//                 var remover = GetRemove(w, t);
//                 remover?.Invoke(w, e);
//                 var add = GetAdd(w, t);
//                 if (add != null)
//                 {
//                     add(w, e, boxed);
//                     return;
//                 }
//
//                 Debug.LogWarning($"[EcsExplorer] No applicable API for {t.Name}. Expected Set<T>/Replace<T>/Add<T>.");
//             }
//
//             static Func<World, Entity, bool> GetHas(World w, Type t)
//             {
//                 if (_hasCache.TryGetValue(t, out var f)) return f;
//                 _miHas ??= typeof(World).GetMethods(BindingFlags.Instance | BindingFlags.Public)
//                     .FirstOrDefault(m =>
//                         m.Name == "Has" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1);
//                 if (_miHas != null)
//                 {
//                     var g = _miHas.MakeGenericMethod(t);
//                     Func<World, Entity, bool> fn = (ww, ee) => (bool)g.Invoke(ww, new object[] { ee });
//                     _hasCache[t] = fn;
//                     return fn;
//                 }
//
//                 _hasCache[t] = null;
//                 return null;
//             }
//
//             static Action<World, Entity> GetRemove(World w, Type t)
//             {
//                 if (_removeCache.TryGetValue(t, out var a)) return a;
//
//                 // ★ Mutator 경유: EcsEditorMutatorAPI.Remove<T>(World, Entity)
//                 _miRemove ??= typeof(ZenECS.EditorUtils.EcsEditorMutatorAPI)
//                     .GetMethods(BindingFlags.Public | BindingFlags.Static)
//                     .FirstOrDefault(m =>
//                         m.Name == nameof(EcsEditorMutatorAPI.Remove)
//                         && m.IsGenericMethodDefinition
//                         && m.GetParameters().Length == 2);
//
//                 if (_miRemove != null)
//                 {
//                     var g = _miRemove.MakeGenericMethod(t);
//                     Action<World, Entity> fn = (ww, ee) => g.Invoke(null, new object[] { ww, ee });
//                     _removeCache[t] = fn;
//                     return fn;
//                 }
//
//                 _removeCache[t] = null;
//                 return null;
//             }
//
//             static Action<World, Entity, object> BuildSetter(World w, Type t)
//             {
//                 // ★ Mutator 경유: EcsEditorMutatorAPI.Replace<T>(World, Entity, in T)
//                 _miReplace ??= typeof(ZenECS.EditorUtils.EcsEditorMutatorAPI)
//                     .GetMethods(BindingFlags.Public | BindingFlags.Static)
//                     .FirstOrDefault(m =>
//                         m.Name == nameof(EcsEditorMutatorAPI.Replace)
//                         && m.IsGenericMethodDefinition
//                         && m.GetParameters().Length == 3);
//                 if (_miReplace != null)
//                 {
//                     var g = _miReplace.MakeGenericMethod(t);
//                     Action<World, Entity, object> fn = (ww, ee, boxed) =>
//                         g.Invoke(null, new object[] { ww, ee, boxed });
//                     _setCache[t] = fn;
//                     return fn;
//                 }
//
//                 _setCache[t] = null;
//                 return null;
//             }
//
//             static Action<World, Entity, object> GetAdd(World w, Type t)
//             {
//                 // ★ Mutator 경유: EcsEditorMutatorAPI.Add<T>(World, Entity, in T)
//                 _miAdd ??= typeof(ZenECS.EditorUtils.EcsEditorMutatorAPI)
//                     .GetMethods(BindingFlags.Public | BindingFlags.Static)
//                     .FirstOrDefault(m =>
//                         m.Name == nameof(EcsEditorMutatorAPI.Add)
//                         && m.IsGenericMethodDefinition
//                         && m.GetParameters().Length == 3);
//
//                 if (_miAdd != null)
//                 {
//                     var g = _miAdd.MakeGenericMethod(t);
//                     Action<World, Entity, object> fn = (ww, ee, boxed) =>
//                         g.Invoke(null, new object[] { ww, ee, boxed });
//                     return fn;
//                 }
//
//                 return null;
//             }
//
//             public static void AddBoxed(World w, Entity e, object boxed)
//             {
//                 if (boxed == null) return;
//                 var t = boxed.GetType();
//                 var add = GetAdd(w, t);
//                 if (add != null)
//                 {
//                     add(w, e, boxed);
//                     return;
//                 }
//
//                 Debug.LogWarning($"[EcsExplorer] Add<{t.Name}> not available via EcsEditorMutatorAPI.");
//             }
//
//             public static void RemoveBoxed(World w, Entity e, Type t)
//             {
//                 var remover = GetRemove(w, t);
//                 if (remover != null)
//                 {
//                     remover(w, e);
//                     return;
//                 }
//
//                 Debug.LogWarning($"[EcsExplorer] Remove<{t.Name}> not available via EcsEditorMutatorAPI.");
//             }
//         }
//
//         public void SelectEntity(int entityId)
//         {
//             var world = EcsRuntimeDirectory.World;
//             _findEntityId = entityId;
//             _foundValid = TryResolveEntityById(world, entityId, out _foundEntity);
//             _findMode = true;
//             Repaint();
//         }
//     }
// }
// #endif
﻿using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using Battlehub.Utils;
using Battlehub.RTCommon;

namespace Battlehub.RTHandles
{
    public delegate void UnityEditorToolChanged();
    public class UnityEditorToolsListener
    {
        public static event UnityEditorToolChanged ToolChanged;

        #if UNITY_EDITOR
        private static UnityEditor.Tool m_tool;
        static UnityEditorToolsListener()
        {
            m_tool = UnityEditor.Tools.current;
        }

        #else
        void Use()
        {
            ToolChanged();
        }
        #endif

        public static void Update()
        {
            #if UNITY_EDITOR
            if (m_tool != UnityEditor.Tools.current)
            {
                if (ToolChanged != null)
                {
                    ToolChanged();
                }
                m_tool = UnityEditor.Tools.current;
            }
            #endif
        }
    }

    public interface IScenePivot
    {
        Vector3 Pivot
        {
            get;
            set;
        }

        Vector3 SecondaryPivot
        {
            get;
            set;
        }

        Vector3 CameraPosition
        {
            get;
            set;
        }

        bool IsOrthographic
        {
            get;
            set;
        }

        void Focus();
    }

    public class RuntimeSelectionCancelArgs
    {
        public bool Cancel
        {
            get;
            set;
        }
    }

    public interface IRuntimeSelectionComponent : IScenePivot
    {
        PositionHandle PositionHandle
        {
            get;
        }

        RotationHandle RotationHandle
        {
            get;
        }

        ScaleHandle ScaleHandle
        {
            get;
        }

        RectTool RectTool
        {
            get;
        }

        BoxSelection BoxSelection
        {
            get;
        }

  
        bool IsPositionHandleEnabled
        {
            get;
            set;
        }

        bool IsRotationHandleEnabled
        {
            get;
            set;
        }

        bool IsScaleHandleEnabled
        {
            get;
            set;
        }

        bool IsRectToolEnabled
        {
            get;
            set;
        }

        bool IsBoxSelectionEnabled
        {
            get;
            set;
        }

        bool CanSelect
        {
            get;
            set;
        }
        
        bool CanSelectAll
        {
            get;
            set;
        }


        float SizeOfGrid
        {
            get;
            set;
        }

        bool IsGridVisible
        {
            get;
            set;
        }

        bool IsGridEnabled
        {
            get;
            set;
        }
    }

    [DefaultExecutionOrder(-55)]
    public class RuntimeSelectionComponent : RTEComponent, IRuntimeSelectionComponent
    {
        [SerializeField]
        private OutlineManager m_outlineManager = null;
        [SerializeField]
        private PositionHandle m_positionHandle = null;
        [SerializeField]
        private RotationHandle m_rotationHandle = null;
        [SerializeField]
        private ScaleHandle m_scaleHandle = null;
        [SerializeField]
        private RectTool m_rectTool = null;
        [SerializeField]
        private BoxSelection m_boxSelection = null;
        [SerializeField]
        private GameObject m_selectionGizmoPrefab = null;
        [SerializeField]
        private SceneGrid m_grid = null;
        [SerializeField]
        private Transform m_pivot = null;
        [SerializeField]
        private Transform m_secondaryPivot = null;
        [SerializeField]
        private bool m_isPositionHandleEnabled = true;
        [SerializeField]
        private bool m_isRotationHandleEnabled = true;
        [SerializeField]
        private bool m_isScaleHandleEnabled = true;
        [SerializeField]
        private bool m_isRectToolEnabled = true;
        [SerializeField]
        private bool m_isBoxSelectionEnabled = true;
        [SerializeField]
        private bool m_canSelect = true;
        [SerializeField]
        private bool m_canSelectAll = true;
     
        protected Transform PivotTransform
        {
            get { return m_pivot; }
        }

        protected Transform SecondaryPivotTransform
        {
            get { return m_secondaryPivot; }
        }

        public virtual bool IsOrthographic
        {
            get { return Window.Camera.orthographic; }
            set { Window.Camera.orthographic = value; }
        }

        public virtual Vector3 CameraPosition
        {
            get { return Window.Camera.transform.position; }
            set
            {
                Window.Camera.transform.position = value;
                Window.Camera.transform.LookAt(Pivot);
            }
        }

        public virtual Vector3 Pivot
        {
            get { return m_pivot.transform.position; }
            set
            {
                m_pivot.transform.position = value;
                Window.Camera.transform.LookAt(Pivot);
            }
        }

        public virtual Vector3 SecondaryPivot
        {
            get { return m_secondaryPivot.transform.position; }
            set { m_secondaryPivot.transform.position = value; }
        }

        public BoxSelection BoxSelection
        {
            get { return m_boxSelection; }
        }

        public PositionHandle PositionHandle
        {
            get { return m_positionHandle; }
        }

        public RotationHandle RotationHandle
        {
            get { return m_rotationHandle; }
        }

        public ScaleHandle ScaleHandle
        {
            get { return m_scaleHandle; }
        }

        public RectTool RectTool
        {
            get { return m_rectTool; }
        }

        public bool IsPositionHandleEnabled
        {
            get { return m_isPositionHandleEnabled && m_positionHandle != null; }
            set
            {
                m_isPositionHandleEnabled = value;
                if (m_positionHandle != null)
                {
                    if (value)
                    {
                        m_positionHandle.Targets = GetTargets();
                    }
                    m_positionHandle.gameObject.SetActive(value && Editor.Tools.Current == RuntimeTool.Move && m_positionHandle.Target != null);
                }
            }
        }

        public bool IsRotationHandleEnabled
        {
            get { return m_isRotationHandleEnabled && m_rotationHandle != null; }
            set
            {
                m_isRotationHandleEnabled = value;
                if (m_rotationHandle != null)
                {
                    if (value)
                    {
                        m_rotationHandle.Targets = GetTargets();
                    }
                    m_rotationHandle.gameObject.SetActive(value && Editor.Tools.Current == RuntimeTool.Rotate && m_rotationHandle.Target != null);
                }
            }
        }

        public bool IsScaleHandleEnabled
        {
            get { return m_isScaleHandleEnabled && m_scaleHandle != null; }
            set
            {
                m_isScaleHandleEnabled = value; 
                if(m_scaleHandle != null)
                {
                    if (value)
                    {
                        m_scaleHandle.Targets = GetTargets();
                    }
                    m_scaleHandle.gameObject.SetActive(value && Editor.Tools.Current == RuntimeTool.Scale && m_scaleHandle.Target != null);
                }
            }
        }

        public bool IsRectToolEnabled
        {
            get { return m_isRectToolEnabled && m_rectTool != null; }
            set
            {
                m_isRectToolEnabled = value;
                if (m_rectTool != null)
                {
                    if (value)
                    {
                        m_rectTool.Targets = GetTargets();
                    }
                    m_rectTool.gameObject.SetActive(value && Editor.Tools.Current == RuntimeTool.Rect && m_rectTool.Target != null);
                }
            }
        }

        public bool IsBoxSelectionEnabled
        {
            get { return m_isBoxSelectionEnabled && m_boxSelection != null; }
            set
            {
                m_isBoxSelectionEnabled = value;
                if(m_boxSelection != null)
                {
                    m_boxSelection.enabled = value && Editor.ActiveWindow == Window;
                }
            }
        }

        public bool CanSelect
        {
            get { return m_canSelect; }
            set { m_canSelect = value; }
        }

        public bool CanSelectAll
        {
            get { return m_canSelectAll; }
            set { m_canSelectAll = value; }
        }

        public bool IsGridVisible
        {
            get
            {
                if(m_grid == null)
                {
                    return false;
                }
                return m_grid.gameObject.activeSelf;
            }
            set
            {
                if(m_grid == null)
                {
                    return;
                }
                m_grid.gameObject.SetActive(value);
            }
        }

        private bool m_isGridEnabled;
        public bool IsGridEnabled
        {
            get { return m_isGridEnabled; }
            set
            {
                if(m_isGridEnabled != value)
                {
                    m_isGridEnabled = value;
                    ApplyIsGridEnabled();
                }
            }
        }

        private void ApplyIsGridEnabled()
        {
            if (m_positionHandle != null)
            {
                m_positionHandle.SnapToGrid = IsGridEnabled;
            }

            if (m_scaleHandle != null)
            {
                m_scaleHandle.AbsouluteGrid = IsGridEnabled;
            }
        }

        public float SizeOfGrid
        {
            get
            {
                if(m_grid == null)
                {
                    return 0.5f;
                }
                return m_grid.SizeOfGrid;
            }
            set
            {
                if (m_grid == null)
                {
                    return;
                }

                m_grid.SizeOfGrid = value;
                ApplySizeOfGrid();
            }
        }

        private void ApplySizeOfGrid()
        {
            if (m_positionHandle != null)
            {
                m_positionHandle.GridSize = SizeOfGrid;
            }

            if (m_scaleHandle != null)
            {
                m_scaleHandle.GridSize = SizeOfGrid;
            }

            if(m_rectTool != null)
            {
                m_rectTool.GridSize = SizeOfGrid;
            }
        }

        protected override void AwakeOverride()
        {
            base.AwakeOverride();
            
            Window.IOCContainer.RegisterFallback<IScenePivot>(this);
            Window.IOCContainer.RegisterFallback<IRuntimeSelectionComponent>(this);

            if(m_outlineManager == null)
            {
                m_outlineManager = GetComponentInChildren<OutlineManager>(true);
                if(m_outlineManager != null)
                {
                    m_outlineManager.Camera = Window.Camera;
                }
            }

            if(m_boxSelection == null)
            {
                m_boxSelection = GetComponentInChildren<BoxSelection>(true);
            }
            if(m_positionHandle == null)
            {
                m_positionHandle = GetComponentInChildren<PositionHandle>(true);
                
            }
            if(m_rotationHandle == null)
            {
                m_rotationHandle = GetComponentInChildren<RotationHandle>(true);
            }
            if(m_scaleHandle == null)
            {
                m_scaleHandle = GetComponentInChildren<ScaleHandle>(true);
            }
            if(m_rectTool == null)
            {
                m_rectTool = GetComponentInChildren<RectTool>(true);
                
            }
            if(m_grid == null)
            {
                m_grid = GetComponentInChildren<SceneGrid>(true);
            }
            
            if (m_boxSelection != null)
            {
                if(m_boxSelection.Window == null)
                {
                    m_boxSelection.Window = Window;
                }

                m_boxSelection.Filtering += OnBoxSelectionFiltering;
                m_boxSelection.Selection += OnBoxSelection;
            }

            if (m_positionHandle != null)
            {
                if(m_positionHandle.Window == null)
                {
                    m_positionHandle.Window = Window;
                }

                m_positionHandle.gameObject.SetActive(true);
                m_positionHandle.gameObject.SetActive(false);

                m_positionHandle.BeforeDrag.AddListener(OnBeforeDrag);
                m_positionHandle.Drop.AddListener(OnDrop);
            }

            if (m_rotationHandle != null)
            {
                if(m_rotationHandle.Window == null)
                {
                    m_rotationHandle.Window = Window;
                }

                m_rotationHandle.gameObject.SetActive(true);
                m_rotationHandle.gameObject.SetActive(false);

                m_rotationHandle.BeforeDrag.AddListener(OnBeforeDrag);
                m_rotationHandle.Drop.AddListener(OnDrop);
            }

            if (m_scaleHandle != null)
            {
                if(m_scaleHandle.Window == null)
                {
                    m_scaleHandle.Window = Window;
                }
                m_scaleHandle.gameObject.SetActive(true);
                m_scaleHandle.gameObject.SetActive(false);

                m_scaleHandle.BeforeDrag.AddListener(OnBeforeDrag);
                m_scaleHandle.Drop.AddListener(OnDrop);
            }


            if (m_rectTool != null)
            {
                if (m_rectTool.Window == null)
                {
                    m_rectTool.Window = Window;
                }
                m_rectTool.gameObject.SetActive(true);
                m_rectTool.gameObject.SetActive(false);

                m_rectTool.BeforeDrag.AddListener(OnBeforeDrag);
                m_rectTool.Drop.AddListener(OnDrop);
            }

            if (m_grid != null)
            {
                if (m_grid.Window == null)
                {
                    m_grid.Window = Window;
                }
            }

            Editor.Selection.SelectionChanged += OnRuntimeSelectionChanged;
            Editor.Tools.ToolChanged += OnRuntimeToolChanged;

            if (m_pivot == null)
            {
                GameObject pivot = new GameObject("Pivot");
                pivot.transform.SetParent(transform, true);
                pivot.transform.position = Vector3.zero;
                m_pivot = pivot.transform;
            }

            if (m_secondaryPivot == null)
            {
                GameObject secondaryPivot = new GameObject("SecondaryPivot");
                secondaryPivot.transform.SetParent(transform, true);
                secondaryPivot.transform.position = Vector3.zero;
                m_secondaryPivot = secondaryPivot.transform;
            }

            ApplySizeOfGrid();
            ApplyIsGridEnabled();

            OnRuntimeSelectionChanged(null);
        }

        protected virtual void Start()
        {
            if (GetComponent<RuntimeSelectionInputBase>() == null)
            {
                gameObject.AddComponent<RuntimeSelectionInput>();
            }

            if (m_positionHandle != null && !m_positionHandle.gameObject.activeSelf)
            {
                m_positionHandle.gameObject.SetActive(true);
                m_positionHandle.gameObject.SetActive(false);
            }

            if (m_rotationHandle != null && !m_rotationHandle.gameObject.activeSelf)
            {
                m_rotationHandle.gameObject.SetActive(true);
                m_rotationHandle.gameObject.SetActive(false);
            }

            if (m_scaleHandle != null && !m_scaleHandle.gameObject.activeSelf)
            {
                m_scaleHandle.gameObject.SetActive(true);
                m_scaleHandle.gameObject.SetActive(false);
            }

            if (m_rectTool != null && !m_rectTool.gameObject.activeSelf)
            {
                m_rectTool.gameObject.SetActive(true);
                m_rectTool.gameObject.SetActive(false);
            }

            RuntimeTool tool = Editor.Tools.Current;
            Editor.Tools.Current = RuntimeTool.None;
            Editor.Tools.Current = tool;
        }

        protected override void OnDestroyOverride()
        {
            base.OnDestroyOverride();

            Window.IOCContainer.UnregisterFallback<IScenePivot>(this);
            Window.IOCContainer.UnregisterFallback<IRuntimeSelectionComponent>(this);

            if (m_boxSelection != null)
            {
                m_boxSelection.Filtering -= OnBoxSelectionFiltering;
                m_boxSelection.Selection -= OnBoxSelection;
            }

            Editor.Tools.ToolChanged -= OnRuntimeToolChanged;
            Editor.Selection.SelectionChanged -= OnRuntimeSelectionChanged;

            GameObject[] selectedObjects = Editor.Selection.gameObjects;
            if (selectedObjects != null)
            {
                for (int i = 0; i < selectedObjects.Length; ++i)
                {
                    GameObject go = selectedObjects[i];
                    if (go != null)
                    {
                        SelectionGizmo[] selectionGizmo = go.GetComponents<SelectionGizmo>();
                        for (int g = 0; g < selectionGizmo.Length; ++g)
                        {
                            if (selectionGizmo[g] != null && selectionGizmo[g].Window == Window)
                            {
                                Destroy(selectionGizmo[g]);
                            }
                        }
                    }
                }
            }

            if(m_positionHandle != null)
            {
                m_positionHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                m_positionHandle.Drop.RemoveListener(OnDrop);
            }

            if(m_rotationHandle != null)
            {
                m_rotationHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                m_rotationHandle.Drop.RemoveListener(OnDrop);
            }

            if(m_scaleHandle != null)
            {
                m_scaleHandle.BeforeDrag.RemoveListener(OnBeforeDrag);
                m_scaleHandle.Drop.RemoveListener(OnDrop);
            }

            if(m_rectTool != null)
            {
                m_rectTool.BeforeDrag.RemoveListener(OnBeforeDrag);
                m_rectTool.Drop.RemoveListener(OnDrop);
            }
        }

        protected override void OnWindowActivated()
        {
            base.OnWindowActivated();
            if (m_boxSelection != null)
            {
                m_boxSelection.enabled = true && IsBoxSelectionEnabled;
            }
        }

        protected override void OnWindowDeactivated()
        {
            base.OnWindowDeactivated();
            if(m_boxSelection != null)
            {
                m_boxSelection.enabled = false;
            }
        }

        private int GetDepth(Transform tr)
        {
            int depth = 0;

            while(tr.parent != null)
            {
                depth++;
                tr = tr.parent;
            }

            return depth;
        }

        private bool IsReachable(Transform t1, Transform t2)
        {
            Transform p1 = t1;
            while(p1 != null)
            {
                if(p1 == t2)
                {
                    return true;
                }

                p1 = p1.parent;
            }

            Transform p2 = t2;
            while(p2 != null)
            {
                if(p2 == t1)
                {
                    return true;
                }

                p2 = p2.parent;
            }

            return false;
        }

        private RaycastHit[] FilterHits(RaycastHit[] hits)
        {
            RaycastHit closestHit = hits.OrderBy(hit => hit.distance).FirstOrDefault();
            return hits.Where(h => IsReachable(h.transform, closestHit.transform)).ToArray();
        }

        private int GetNextIndex(RaycastHit[] hits)
        {
            int index = -1;
            if(hits == null || hits.Length == 0)
            {
                return index;
            }

            if(Editor.Selection.activeGameObject != null)
            {
                for (int i = 0; i < hits.Length; ++i)
                {
                    RaycastHit hit = hits[i];
                    if (Editor.Selection.IsSelected(hit.collider.gameObject))
                    {
                        index = i;
                    }
                }
            }
            
            index++;
            index %= hits.Length;
            return index;
        }

        public virtual void SelectGO(bool multiselect, bool allowUnselect)
        {
            if(!CanSelect)
            {
                return;
            }

            Ray ray = Window.Pointer;
            RaycastHit[] hits = Physics.RaycastAll(ray, float.MaxValue);
            if (hits.Length > 0)
            {
                hits = hits.Where(hit => CanSelectObject(hit.collider.gameObject)).OrderBy(hit => GetDepth(hit.transform)).ToArray();
                bool canSelect = hits.Length > 0;
                if (canSelect)
                {
                    hits = FilterHits(hits);
                    int nextIndex = GetNextIndex(hits);
                    GameObject hitGO = hits[nextIndex].collider.GetComponentInParent<ExposeToEditor>().gameObject;

                    if (multiselect)
                    {
                        List<Object> selection;
                        if (Editor.Selection.objects != null)
                        {
                            selection = Editor.Selection.objects.ToList();
                        }
                        else
                        {
                            selection = new List<Object>();
                        }

                        if (selection.Contains(hitGO))
                        {
                            selection.Remove(hitGO);
                            if (!allowUnselect)
                            {
                                selection.Insert(0, hitGO);
                            }
                        }
                        else
                        {
                            selection.Insert(0, hitGO);
                        }
                        Editor.Undo.Select(selection.ToArray(), hitGO);
                    }
                    else
                    {
                        Editor.Selection.activeObject = hitGO;
                    }
                }
                else
                {
                    if (!multiselect)
                    {
                        Editor.Selection.activeObject = null;
                    }
                }
            }
            else
            {
                if (!multiselect)
                {
                    Editor.Selection.activeObject = null;
                }
            }
        }

        public virtual void SnapToGrid()
        {
            GameObject[] selection = Editor.Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                return;
            }

            Transform activeTransform = selection[0].transform;

            Vector3 position = activeTransform.position;
            if (SizeOfGrid < 0.01)
            {
                SizeOfGrid = 0.01f;
            }
            position.x = Mathf.Round(position.x / SizeOfGrid) * SizeOfGrid;
            position.y = Mathf.Round(position.y / SizeOfGrid) * SizeOfGrid;
            position.z = Mathf.Round(position.z / SizeOfGrid) * SizeOfGrid;
            Vector3 offset = position - activeTransform.position;

            Editor.Undo.BeginRecord();
            for (int i = 0; i < selection.Length; ++i)
            {
                Editor.Undo.BeginRecordTransform(selection[i].transform);
                selection[i].transform.position += offset;
                Editor.Undo.EndRecordTransform(selection[i].transform);
            }
            Editor.Undo.EndRecord();

            if (m_rectTool != null && Editor.Tools.Current == RuntimeTool.Rect)
            {
                m_rectTool.RecalculateBoundsAndRebuild();
            }
        }

        public virtual void SelectAll()
        {
            if(!CanSelect || !CanSelectAll)
            {
                return;
            }
            Editor.Selection.objects = Editor.Object.Get(false).Select(exposed => exposed.gameObject).ToArray();
        }

        private void OnRuntimeToolChanged()
        {
            if (Editor.Selection.activeTransform == null)
            {
                return;
            }

            if (m_positionHandle != null)
            {
                if (Editor.Tools.Current == RuntimeTool.Move && IsPositionHandleEnabled)
                {
                    m_positionHandle.transform.position = Editor.Selection.activeTransform.position;
                    m_positionHandle.Targets = GetTargets();
                    m_positionHandle.gameObject.SetActive(m_positionHandle.Targets.Length > 0);
                }
                else
                {
                    m_positionHandle.gameObject.SetActive(false);
                }
            }
            if (m_rotationHandle != null)
            {   
                if (Editor.Tools.Current == RuntimeTool.Rotate && IsRotationHandleEnabled)
                {
                    m_rotationHandle.transform.position = Editor.Selection.activeTransform.position;
                    m_rotationHandle.Targets = GetTargets();
                    m_rotationHandle.gameObject.SetActive(m_rotationHandle.Targets.Length > 0);
                }
                else
                {
                    m_rotationHandle.gameObject.SetActive(false);
                }
            }
            if (m_scaleHandle != null)
            {
                if (Editor.Tools.Current == RuntimeTool.Scale && IsScaleHandleEnabled)
                {
                    m_scaleHandle.transform.position = Editor.Selection.activeTransform.position;
                    m_scaleHandle.Targets = GetTargets();
                    m_scaleHandle.gameObject.SetActive(m_scaleHandle.Targets.Length > 0);
                }
                else
                {
                    m_scaleHandle.gameObject.SetActive(false);
                }
            }
            if (m_rectTool != null)
            {
                if (Editor.Tools.Current == RuntimeTool.Rect && IsRectToolEnabled)
                {
                    m_rectTool.transform.position = Editor.Selection.activeTransform.position;
                    m_rectTool.Targets = GetTargets();
                    m_rectTool.gameObject.SetActive(m_rectTool.Targets.Length > 0);
                }
                else
                {
                    m_rectTool.gameObject.SetActive(false);
                }
            }

#if UNITY_EDITOR
            switch (Editor.Tools.Current)
            {
                case RuntimeTool.None:
                    UnityEditor.Tools.current = UnityEditor.Tool.None;
                    break;
                case RuntimeTool.Move:
                    UnityEditor.Tools.current = UnityEditor.Tool.Move;
                    break;
                case RuntimeTool.Rotate:
                    UnityEditor.Tools.current = UnityEditor.Tool.Rotate;
                    break;
                case RuntimeTool.Scale:
                    UnityEditor.Tools.current = UnityEditor.Tool.Scale;
                    break;
                case RuntimeTool.Rect:
                    UnityEditor.Tools.current = UnityEditor.Tool.Rect;
                    break;
                case RuntimeTool.View:
                    UnityEditor.Tools.current = UnityEditor.Tool.View;
                    break;
            }
#endif
        }

        private void OnBoxSelectionFiltering(object sender, FilteringArgs e)
        {
            if(!CanSelect)
            {
                return;
            }

            if (e.Object == null)
            {
                e.Cancel = true;
            }

            ExposeToEditor exposeToEditor = e.Object.GetComponent<ExposeToEditor>();
            if (!exposeToEditor)
            {
                e.Cancel = true;
            }
        }

        private void OnBoxSelection(object sender, BoxSelectionArgs e)
        {
            if(CanSelect)
            {
                m_editor.Selection.objects = e.GameObjects;
            }
        }

        private void OnRuntimeSelectionChanged(Object[] unselected)
        {
            if (unselected != null)
            {
                for (int i = 0; i < unselected.Length; ++i)
                {
                    GameObject unselectedObj = unselected[i] as GameObject;
                    if (unselectedObj != null)
                    {
                        SelectionGizmo[] selectionGizmo = unselectedObj.GetComponents<SelectionGizmo>();
                        for(int g = 0; g < selectionGizmo.Length; ++g)
                        {
                            if (selectionGizmo[g] != null && selectionGizmo[g].Window == Window)
                            {
                                //DestroyImmediate(selectionGizmo[g]);
                                selectionGizmo[g].Internal_Destroyed = true;
                                Destroy(selectionGizmo[g]);
                            }
                        }
                       
                        ExposeToEditor exposeToEditor = unselectedObj.GetComponent<ExposeToEditor>();
                        if (exposeToEditor)
                        {
                            if (exposeToEditor.Unselected != null)
                            {
                                exposeToEditor.Unselected.Invoke(exposeToEditor);
                            }
                        }
                    }
                }
            }

            GameObject[] selected = Editor.Selection.gameObjects;
            if (selected != null)
            {
                for (int i = 0; i < selected.Length; ++i)
                {
                    GameObject selectedObj = selected[i];
                    ExposeToEditor exposeToEditor = selectedObj.GetComponent<ExposeToEditor>();
                    if (exposeToEditor && !selectedObj.IsPrefab() && !selectedObj.isStatic)
                    {
                        SelectionGizmo selectionGizmo = selectedObj.GetComponent<SelectionGizmo>();
                        if (selectionGizmo == null || selectionGizmo.Internal_Destroyed || selectionGizmo.Window != Window)
                        {
                            if(!Editor.IsPlaymodeStateChanging || !Editor.IsPlaying)
                            {
                                if((selectedObj.hideFlags & HideFlags.DontSave) == 0)
                                {
                                    if(m_outlineManager == null)
                                    {
                                        selectionGizmo = selectedObj.AddComponent<SelectionGizmo>();
                                        if (m_selectionGizmoPrefab != null)
                                        {
                                            selectionGizmo.SelectionGizmoModel = Instantiate(m_selectionGizmoPrefab, selectionGizmo.transform);
                                            //selectionGizmo.SelectionGizmoModel.layer = Editor.CameraLayerSettings.RuntimeGraphicsLayer;
                                        }
                                    }                                   
                                } 
                            }
                        }
                        if(selectionGizmo != null)
                        {
                            selectionGizmo.Window = Window;
                        }
                        
                        if (exposeToEditor.Selected != null)
                        {
                            exposeToEditor.Selected.Invoke(exposeToEditor);
                        }
                    }
                }
            }

            if (Editor.Selection.activeGameObject == null || Editor.Selection.activeGameObject.IsPrefab())
            {
                SetHandlesActive(false);
            }
            else
            {
                SetHandlesActive(false);
                OnRuntimeToolChanged();
            }
        }

        private void SetHandlesActive(bool isActive)
        {
            if (m_positionHandle != null)
            {
                m_positionHandle.gameObject.SetActive(isActive);
            }
            if (m_rotationHandle != null)
            {
                m_rotationHandle.gameObject.SetActive(isActive);
            }
            if (m_scaleHandle != null)
            {
                m_scaleHandle.gameObject.SetActive(isActive);
            }
            if(m_rectTool != null)
            {
                m_rectTool.gameObject.SetActive(isActive);
            }
        }

        protected virtual bool CanSelectObject(GameObject go)
        {
            return go.GetComponentInParent<ExposeToEditor>();
        }

        protected virtual bool CanTransformObject(GameObject go)
        {
            if(go == null)
            {
                return false;
            }

            ExposeToEditor exposeToEditor = go.GetComponentInParent<ExposeToEditor>();
            if(exposeToEditor == null)
            {
                return true;
            }
            return exposeToEditor.CanTransform;
        }

        protected virtual Transform[] GetTargets()
        {
            if(Editor.Selection.gameObjects == null)
            {
                return null;
            }

            return Editor.Selection.gameObjects.Where(g => CanTransformObject(g)).Select(g => g.transform).OrderByDescending(g => Editor.Selection.activeTransform == g).ToArray();
        }

        public virtual void Focus()
        {

        }

        private bool m_wasUnitSnappingEnabled;
        private void OnBeforeDrag(BaseHandle handle)
        {
            if(IsGridEnabled)
            {
                m_wasUnitSnappingEnabled = Editor.Tools.UnitSnapping;
                Editor.Tools.UnitSnapping = true;
            }
            
        }

        private void OnDrop(BaseHandle handle)
        {
            if(IsGridEnabled)
            {
                Editor.Tools.UnitSnapping = m_wasUnitSnappingEnabled;
            }
        }
    }
}

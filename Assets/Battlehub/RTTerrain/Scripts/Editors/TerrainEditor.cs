﻿using Battlehub.RTCommon;
using Battlehub.RTEditor;
using Battlehub.RTHandles;
using Battlehub.UIControls;
using Battlehub.Utils;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.RTTerrain
{
    public class TerrainEditor : MonoBehaviour
    {
        public event Action TerrainChanged;

        public enum EditorType
        {
            Empty = 0,
            Paint_Terrain = 1,
            Selection_Handles = 2,
            Settings = 3,
        }

        public enum PaintTool
        {
            Raise_Or_Lower_Terrain = 0,
            Paint_Texture = 1,
            Stamp_Terrain = 2,
            Set_Height = 3,
            Smooth_Height = 4,
        }

        [SerializeField]
        private Toggle[] m_toggles = null;
        [SerializeField]
        private GameObject[] m_editors = null;
        [SerializeField]
        private EnumEditor m_paintToolSelector = null;
        [SerializeField]
        private GameObject[] m_paintTools = null;

        [SerializeField]
        private TerrainProjector m_terrainProjectorPrefab = null;

        private IRTE m_editor;
        private IWindowManager m_wm;

        public TerrainProjector Projector
        {
            get;
            private set;
        }

        private Terrain m_terrain;
        public Terrain Terrain
        {
            get { return m_terrain; }
            set
            {
                if (m_terrain != value)
                {
                    m_terrain = value;
                    if (TerrainChanged != null)
                    {
                        TerrainChanged();
                    }

                    EditorType editorType = EditorType.Empty;
                    for (int i = 1; i < m_toggles.Length; ++i)
                    {
                        if (m_toggles[i] != null && m_toggles[i].isOn)
                        {
                            editorType = (EditorType)i;
                        }
                    }

                    UpdateProjectorState(editorType);
                }
            }
        }

        private PaintTool m_selectedPaintTool;
        public PaintTool SelectedPaintTool
        {
            get { return m_selectedPaintTool; }
            set
            {
                if(m_selectedPaintTool != value)
                {
                    UpdateProjectorState(EditorType.Paint_Terrain);

                    for (int i = 0; i < m_paintTools.Length; ++i)
                    {
                        m_paintTools[i].SetActive(false);
                    }

                    m_selectedPaintTool = value;
                    m_paintTools[(int)m_selectedPaintTool].SetActive(true);
                }
            }
        }

        private void Awake()
        {
            m_editor = IOC.Resolve<IRTE>();
            m_editor.Tools.ToolChanging += OnEditorToolChanging;
            m_wm = IOC.Resolve<IWindowManager>();
            m_wm.WindowCreated += OnWindowCreated;
            m_wm.AfterLayout += OnAfterLayout;

            Projector = Instantiate(m_terrainProjectorPrefab, m_editor.Root);
            Projector.gameObject.SetActive(false);

            if(IOC.Resolve<ITerrainSelectionHandlesTool>() == null)
            {
                if(m_toggles[(int)EditorType.Selection_Handles])
                {
                    m_toggles[(int)EditorType.Selection_Handles].gameObject.SetActive(false);
                }
            }

            for(int i = 0; i < m_toggles.Length; ++i)
            {
                Toggle toggle = m_toggles[i];
                if(toggle != null)
                {
                    EditorType editorType = ToEditorType(i);
                    UnityEventHelper.AddListener(toggle, tog => tog.onValueChanged, v => OnToggleValueChanged(editorType, v));
                }
            }

            for(int i = 0; i < m_editors.Length; ++i)
            {
                m_editors[i].SetActive(false);
            }

            EditorType toolType = (m_editor.Tools.Custom is EditorType) ? (EditorType)m_editor.Tools.Custom : EditorType.Empty;
            Toggle selectedToggle = m_toggles[(int)toolType];
            if(selectedToggle != null)
            {
                selectedToggle.isOn = true;
            }
            else
            {
                GameObject emptyEditor = m_editors[(int)EditorType.Empty];
                if (emptyEditor)
                {
                    emptyEditor.gameObject.SetActive(true);
                }
            }

            if(m_paintToolSelector != null)
            {
                m_paintToolSelector.Init(this, this, Strong.PropertyInfo((TerrainEditor x) => x.SelectedPaintTool), null, "Tool:", null, null, null, false);
            }
            
            SubscribeSelectionChangingEvent(true);
        }

   
        private void OnDestroy()
        {
            if(m_wm != null)
            {
                m_wm.WindowCreated -= OnWindowCreated;
                m_wm.AfterLayout -= OnAfterLayout;
            }

            if (m_editor != null)
            {
                m_editor.Tools.ToolChanging -= OnEditorToolChanging;
            }

            if(Projector != null)
            {
                Destroy(Projector.gameObject);
            }

            for (int i = 0; i < m_toggles.Length; ++i)
            {
                Toggle toggle = m_toggles[i];
                UnityEventHelper.RemoveAllListeners(toggle, tog => tog.onValueChanged);
            }

            SubscribeSelectionChangingEvent(false);
        }

        private void OnToggleValueChanged(EditorType editorType,  bool value)
        {
            GameObject emptyEditor = m_editors[(int)EditorType.Empty];
            if (emptyEditor)
            {
                emptyEditor.gameObject.SetActive(!value);
            }

            GameObject editor = m_editors[(int)editorType];
            UpdateProjectorState(editorType);

            if (editor)
            {
                editor.SetActive(value);
                if (value)
                {
                    m_editor.Tools.Custom = editorType;
                }
            }
            
        }

        private void UpdateProjectorState(EditorType editorType)
        {
            if (Projector != null)
            {
                if (Terrain == null || editorType == EditorType.Empty || 
                                       editorType == EditorType.Settings || 
                                       editorType == EditorType.Selection_Handles || !m_toggles[(int)editorType].isOn)
                {
                    Projector.gameObject.SetActive(false);
                }
                else
                {
                    Projector.gameObject.SetActive(true);
                }
            }
        }

        private static EditorType ToEditorType(int value)
        {
            if (!Enum.IsDefined(typeof(EditorType), value))
            {
                return EditorType.Empty;
            }
            return (EditorType)value;
        }

        private void SubscribeSelectionChangingEvent(bool subscribe)
        {
            if (m_editor != null)
            {
                foreach (RuntimeWindow window in m_editor.Windows)
                {
                    SubscribeSelectionChangingEvent(subscribe, window);
                }
            }
        }

        private void SubscribeSelectionChangingEvent(bool subscribe, RuntimeWindow window)
        {
            if (window != null && window.WindowType == RuntimeWindowType.Scene)
            {
                IRuntimeSelectionComponent selectionComponent = window.IOCContainer.Resolve<IRuntimeSelectionComponent>();

                if (selectionComponent != null)
                {
                    if (subscribe)
                    {
                        selectionComponent.SelectionChanging += OnSelectionChanging;
                    }
                    else
                    {
                        selectionComponent.SelectionChanging -= OnSelectionChanging;
                    }
                }
            }
        }

        private void OnEditorToolChanging(RuntimeTool toolType, object customTool)
        {
            if (!(customTool is EditorType))
            {
                foreach (Toggle toggle in m_toggles)
                {
                    if (toggle != null)
                    {
                        toggle.isOn = false;
                    }
                }
            }
            else
            {
                EditorType editorType = (EditorType)customTool;
                m_editor.Tools.IsBoxSelectionEnabled = editorType == EditorType.Selection_Handles;
            }
        }

        private void OnSelectionChanging(object sender, RuntimeSelectionChangingArgs e)
        {
            IRuntimeSelectionComponent selectionComponent = (IRuntimeSelectionComponent)sender;
            if (selectionComponent.Selection != m_editor.Selection)
            {
                return;
            }

            if (m_editor.Tools.Custom is EditorType)
            {
                EditorType editorType = (EditorType)m_editor.Tools.Custom;
                if(editorType != EditorType.Empty && editorType != EditorType.Selection_Handles)
                {
                    IRuntimeSelectionComponent component = (IRuntimeSelectionComponent)sender;
                    RaycastHit[] hits = Physics.RaycastAll(component.Window.Pointer);
                    
                    if(Terrain != null && hits.Any(hit => hit.collider.gameObject == Terrain.gameObject))
                    {
                        e.Cancel = true;
                    }
                }
            }
        }

        private void OnAfterLayout(IWindowManager wm)
        {
            SubscribeSelectionChangingEvent(false);
            SubscribeSelectionChangingEvent(true);
        }

        private void OnWindowCreated(Transform windowTransform)
        {
            RuntimeWindow window = windowTransform.GetComponent<RuntimeWindow>();
            if(window != null && window.WindowType == RuntimeWindowType.Scene)
            {
                SubscribeSelectionChangingEvent(false, window);
                SubscribeSelectionChangingEvent(true, window);
            }
        }

    }
}

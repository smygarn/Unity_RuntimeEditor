﻿using Battlehub.RTCommon;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public interface IEditCmd
    {
        bool CanExec(string cmd);
        void Exec(string cmd);
    }

    public class EditCmd : MonoBehaviour, IEditCmd
    {
        private IRuntimeEditor m_editor;

        private void Awake()
        {
            m_editor = IOC.Resolve<IRuntimeEditor>();
        }

        public bool CanExec(string cmd)
        {
            cmd = cmd.ToLower();
            switch (cmd)
            {
                case "duplicate":
                    return m_editor.Selection.activeGameObject != null;
                case "delete":
                    return m_editor.Selection.activeGameObject != null;
                case "undo":
                    return m_editor.Undo.CanUndo;
                case "redo":
                    return m_editor.Undo.CanRedo;
                case "play":
                    return !m_editor.IsPlaying;
                case "stop":
                    return m_editor.IsPlaying;
                case "scenesettings":
                    return true;
            }
            return false;
        }

        public void Exec(string cmd)
        {
            cmd = cmd.ToLower();
            switch (cmd)
            {
                case "duplicate":
                    m_editor.Duplicate(m_editor.Selection.gameObjects);
                    break;
                case "delete":
                    m_editor.Delete(m_editor.Selection.gameObjects);
                    break;
                case "undo":
                    m_editor.Undo.Undo();
                    break;
                case "redo":
                    m_editor.Undo.Redo();
                    break;
                case "play":
                    m_editor.IsPlaying = true;
                    break;
                case "stop":
                    m_editor.IsPlaying = false;
                    break;
                case "scenesettings":
                    IWindowManager wm = IOC.Resolve<IWindowManager>();
                    wm.CreateDialogWindow("scenesettings", "Scene Settings",
                        (sender, args) => { }, (sender, args) => { }, 250, 160, 250, 160, false);
                    break;

            }

        }
    }
}

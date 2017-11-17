﻿#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Modules;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEditor.Experimental.EditorVR.Core;

#if INCLUDE_TEXT_MESH_PRO
using TMPro;
#endif

[assembly: OptionalDependency("TMPro.TextMeshProUGUI", "INCLUDE_TEXT_MESH_PRO")]

namespace UnityEditor.Experimental.EditorVR.Menus
{
    abstract class MainMenuSelectable : MonoBehaviour
    {
        protected Selectable m_Selectable;

        [SerializeField]
        HapticPulse m_ClickPulse;

        [SerializeField]
        HapticPulse m_HoverPulse;

#if INCLUDE_TEXT_MESH_PRO
        [SerializeField]
        protected TextMeshProUGUI m_Description;
#endif

#if INCLUDE_TEXT_MESH_PRO
        [SerializeField]
        protected TextMeshProUGUI m_Title;
#endif

        protected Color m_OriginalColor;

        public Type toolType { get; set; }

        public bool selected
        {
            set
            {
                if (value)
                {
                    m_Selectable.transition = Selectable.Transition.None;
                    m_Selectable.targetGraphic.color = m_Selectable.colors.highlightedColor;
                }
                else
                {
                    m_Selectable.transition = Selectable.Transition.ColorTint;
                    m_Selectable.targetGraphic.color = m_OriginalColor;
                }
            }
        }

        protected void Awake()
        {
            m_OriginalColor = m_Selectable.targetGraphic.color;
        }

        public void SetData(string name, string description)
        {
#if INCLUDE_TEXT_MESH_PRO
            m_Title.text = name;
            m_Description.text = description;
#endif
        }
    }
}
#endif


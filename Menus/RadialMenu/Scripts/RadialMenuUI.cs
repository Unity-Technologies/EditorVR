#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR.Extensions;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Menus
{
    sealed class RadialMenuUI : MonoBehaviour, IConnectInterfaces
    {
        const int k_SlotCount = 16;

        [SerializeField]
        Sprite m_MissingActionIcon;

        [SerializeField]
        RadialMenuSlot m_RadialMenuSlotTemplate;

        [SerializeField]
        Transform m_SlotContainer;

        List<RadialMenuSlot> m_RadialMenuSlots;
        Coroutine m_VisibilityCoroutine;
        RadialMenuSlot m_HighlightedButton;
        float m_PhaseOffset; // Starting rotation for slots
        bool m_SemiTransparent;
        Transform m_AlternateMenuOrigin;
        bool m_Visible;
        List<ActionMenuData> m_Actions;
        bool m_PressedDown;
        Vector2 m_ButtonInputDirection;

        public Transform alternateMenuOrigin
        {
            get { return m_AlternateMenuOrigin; }
            set
            {
                if (m_AlternateMenuOrigin == value)
                    return;

                m_AlternateMenuOrigin = value;
                transform.SetParent(m_AlternateMenuOrigin);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }

        public bool visible
        {
            get { return m_Visible; }
            set
            {
                if (m_Visible == value)
                    return;

                m_Visible = value;

                this.StopCoroutine(ref m_VisibilityCoroutine);

                gameObject.SetActive(true);
                if (value && actions.Count > 0)
                    m_VisibilityCoroutine = StartCoroutine(AnimateShow());
                else if (!value && m_RadialMenuSlots != null) // Only perform hiding if slots have been initialized
                    m_VisibilityCoroutine = StartCoroutine(AnimateHide());
                else if (!value)
                    gameObject.SetActive(false);
            }
        }

        public List<ActionMenuData> actions
        {
            get { return m_Actions; }
            set
            {
                if (value != null)
                {
                    m_Actions = value;

                    if (visible && actions.Count > 0)
                        this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateShow());
                }
                else if (visible && m_RadialMenuSlots != null) // only perform hiding if slots have been initialized
                    visible = false;
            }
        }

        public bool pressedDown
        {
            get { return m_PressedDown; }
            set
            {
                if (m_PressedDown != value)
                {
                    m_PressedDown = value;

                    foreach (var slot in m_RadialMenuSlots)
                    {
                        if (slot == m_HighlightedButton)
                            slot.pressed = true; // If the button is pressed AND this slot is the one being highlighted, set the pressed event to true
                        else
                            slot.pressed = false;
                    }

                    if (m_HighlightedButton == null)
                    {
                        // No button was selected on the Radial Menu. Close the radial menu, and deselect.
                        Selection.activeGameObject = null;
                        visible = false;
                    }
                }
            }
        }

        public Vector2 buttonInputDirection
        {
            set
            {
                if (Mathf.Approximately(value.magnitude, 0) && !Mathf.Approximately(m_ButtonInputDirection.magnitude, 0))
                {
                    foreach (var slot in m_RadialMenuSlots)
                        slot.highlighted = false;
                }
                else if (value.magnitude > 0)
                {
                    if (node == Node.RightHand)
                        value.y *= -1;

                    var angle = Mathf.Atan2(value.y, value.x) * Mathf.Rad2Deg;
                    angle -= m_PhaseOffset;

                    // Handle lower quadrant to put it into full 360 degree range
                    if (angle < 0f)
                        angle += 360f;

                    const float kSlotAngleRange = 360f / k_SlotCount;
                    var kPadding = m_HighlightedButton ? 0.4f : 0.01; // allow for immediate visibility of the menu if no button has been highlighted yet
                    var index = angle / kSlotAngleRange;
                    var t = index % 1f;

                    // Use padding to prevent unintended button switches
                    if (t >= kPadding && t <= 1f - kPadding)
                    {
                        m_HighlightedButton = m_RadialMenuSlots[(int)index];
                        foreach (var slot in m_RadialMenuSlots)
                        {
                            slot.highlighted = slot == m_HighlightedButton;
                        }
                    }
                }

                m_ButtonInputDirection = value;
            }
        }

        bool semiTransparent
        {
            set
            {
                m_SemiTransparent = value;

                if (!value)
                    m_HighlightedButton = null;

                for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
                {
                    // Only set the semiTransparent value on menu slots representing actions
                    m_RadialMenuSlots[i].semiTransparent = m_Actions.Count > i && m_SemiTransparent;
                }
            }
        }

        public Node node { private get; set; }

        public event Action buttonHovered;
        public event Action buttonClicked;

        void Update()
        {
            if (m_Actions != null)
            {
                // Action icons can update after being displayed
                for (int i = 0; i < m_Actions.Count; ++i)
                {
                    var action = m_Actions[i].action;
                    var radialMenuSlot = m_RadialMenuSlots[i];
                    if (radialMenuSlot.icon != action.icon)
                        radialMenuSlot.icon = action.icon;
                }
            }

            if (m_Visible) // don't override transparency if the menu is in the process of hiding itself
                semiTransparent = !m_RadialMenuSlots.Any(x => x.highlighted);
        }

        public void Setup()
        {
            m_RadialMenuSlots = new List<RadialMenuSlot>();
            Material slotBorderMaterial = null;
            gameObject.name = node + " Radial Menu";

            for (int i = 0; i < k_SlotCount; ++i)
            {
                var menuSlotGO = ObjectUtils.Instantiate(m_RadialMenuSlotTemplate.gameObject, m_SlotContainer, false);
                menuSlotGO.name = node + " Radial Menu Slot " + i;
                var menuSlot = menuSlotGO.GetComponent<RadialMenuSlot>();
                this.ConnectInterfaces(menuSlot);
                menuSlot.orderIndex = k_SlotCount - i - 1;
                m_RadialMenuSlots.Add(menuSlot);
                menuSlot.hovered += OnButtonHovered;

                if (slotBorderMaterial == null)
                    slotBorderMaterial = menuSlot.borderRendererMaterial;

                // Set a new shared material for the slots in a RadialMenu.
                // This isolates shader changes in a RadialMenu's border material to only the slots in a given RadialMenu
                menuSlot.borderRendererMaterial = slotBorderMaterial;
            }
            SetupRadialSlotPositions();
        }

        void SetupRadialSlotPositions()
        {
            const float rotationSpacing = 360f / k_SlotCount;
            for (int i = 0; i < k_SlotCount; ++i)
            {
                var slot = m_RadialMenuSlots[i];

                // We move in counter-clockwise direction
                // Account for the input & position phase offset, based on the number of actions, rotating the menu content to be bottom-centered
                m_PhaseOffset = node == Node.LeftHand ? -90f - rotationSpacing : 90f;
                slot.visibleLocalRotation = Quaternion.AngleAxis(m_PhaseOffset + rotationSpacing * i, node == Node.LeftHand ? Vector3.down : Vector3.up);
                slot.visible = false;
            }

            this.RestartCoroutine(ref m_VisibilityCoroutine, AnimateHide());
        }

        void UpdateRadialSlots()
        {
            var gradientPair = UnityBrandColorScheme.sessionGradient;

            for (int i = 0; i < m_Actions.Count; ++i)
            {
                // prevent more actions being added beyond the max slot count
                if (i >= k_SlotCount)
                    break;

                var actionMenuData = m_Actions[i];
                var action = actionMenuData.action;
                var slot = m_RadialMenuSlots[i];
                slot.gradientPair = gradientPair;
                slot.icon = action.icon ?? m_MissingActionIcon;

                var index = i; // Closure
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() =>
                {
                    var selectedSlot = m_RadialMenuSlots[index];
                    var buttonAction = m_Actions[index].action;
                    buttonAction.ExecuteAction();
                    selectedSlot.icon = buttonAction.icon ?? m_MissingActionIcon;
                });

                slot.tooltip = null;
                slot.tooltipText = actionMenuData.tooltipText;

                var tooltip = action as ITooltip;
                if (tooltip != null)
                    slot.tooltip = tooltip;
            }
        }

        IEnumerator AnimateShow()
        {
            UpdateRadialSlots();

            for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
            {
                var slot = m_RadialMenuSlots[i];

                slot.wasVisible = slot.visible;
                if (i < m_Actions.Count)
                    slot.visible = true;
                else
                    slot.visible = false;
            }

            var revealAmount = 0f;
            var hiddenSlotRotation = node == Node.LeftHand ? 90f + 360f / k_SlotCount : 90f;
            while (revealAmount < 1)
            {
                revealAmount += Time.deltaTime * 8;

                for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
                {
                    if (i < m_Actions.Count)
                    {
                        var slot = m_RadialMenuSlots[i];
                        if (slot.wasVisible)
                            continue;

                        var transform = slot.transform;
                        var localRotation = transform.localRotation.eulerAngles;
                        var destRotation = slot.visibleLocalRotation.eulerAngles.y;
                        if (node == Node.LeftHand && destRotation > 180f)
                            destRotation -= 360f;

                        localRotation.y = Mathf.Lerp(hiddenSlotRotation, destRotation, revealAmount * revealAmount);
                        transform.localRotation = Quaternion.Euler(localRotation);
                        slot.CorrectIconRotation();
                    }
                }

                yield return null;
            }

            for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
            {
                if (i < m_Actions.Count)
                {
                    var slot = m_RadialMenuSlots[i];
                    slot.transform.localRotation = slot.visibleLocalRotation;
                    slot.CorrectIconRotation();
                }
            }

            m_VisibilityCoroutine = null;
        }

        IEnumerator AnimateHide()
        {
            const float hiddenSlotRotation = 360f / k_SlotCount;

            for (var i = 0; i < m_RadialMenuSlots.Count; ++i)
                m_RadialMenuSlots[i].visible = false;

            var revealAmount = 1f;
            while (revealAmount > 0)
            {
                revealAmount -= Time.deltaTime * 8;

                for (int i = 0; i < m_RadialMenuSlots.Count; ++i)
                {
                    var slot = m_RadialMenuSlots[i];
                    var transform = slot.transform;
                    var localRotation = slot.transform.localRotation.eulerAngles;
                    localRotation.y = Mathf.Lerp(hiddenSlotRotation, slot.visibleLocalRotation.eulerAngles.y, revealAmount);
                    transform.localRotation = Quaternion.Euler(localRotation);
                }

                yield return null;
            }

            gameObject.SetActive(false);
            m_VisibilityCoroutine = null;
        }

        public void SelectionOccurred()
        {
            if (m_HighlightedButton != null)
                m_HighlightedButton.button.onClick.Invoke();

            if (buttonClicked != null)
                buttonClicked();
        }

        void OnButtonHovered()
        {
            if (buttonHovered != null)
                buttonHovered();
        }
    }
}
#endif

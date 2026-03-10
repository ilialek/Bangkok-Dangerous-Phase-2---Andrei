using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProceduralEditor
{
    public class EditorSelection<T>
    {
        private List<T> m_SelectedItems = new List<T>();
        private T m_SelectedPrimary = default;
        private bool m_MoveSelect;
        private bool m_Dragging;
        private Vector2 m_DragStartMousePos;

        private const float m_DragThreshold = 5.0f;

        // Getters
        public IReadOnlyList<T> Selectedtems => m_SelectedItems;
        public T PrimaryItem => m_SelectedPrimary;
        public bool Empty => m_SelectedItems.Count == 0;
        public bool IsAnySelected => m_SelectedItems.Count > 0;
        public bool IsSingleSelected => m_SelectedItems.Count == 1;
        public bool IsMultiSelect => m_SelectedItems.Count > 1;
        public bool IsDragging => m_Dragging;
        public bool IsMoveSelection => !m_Dragging && m_MoveSelect;
        public int SelectedCount => m_SelectedItems.Count;

        public bool IsSelected(T item) => m_SelectedItems.Contains(item);
        public bool IsSelectedAsSingle(T item) => m_SelectedItems.Count == 1 && m_SelectedItems.Contains(item);

        public void Setup()
        {
            m_Dragging = false;
            m_MoveSelect = true;
        }

        // Selection
        public void Deselect()
        {
            m_SelectedItems.Clear();
            m_SelectedPrimary = default;
            m_MoveSelect = false;
            m_Dragging = false;
        }

        public void SelectSingle(T item)
        {
            m_SelectedItems.Clear();
            m_SelectedItems.Add(item);
            m_SelectedPrimary = item;
            m_MoveSelect = false;
            m_Dragging = false;
        }

        public void RemoveFromSelection(int index)
        {
            m_SelectedItems.RemoveAt(index);
        }

        public int FindIndex(T item)
        {
            return m_SelectedItems.IndexOf(item);
        }

        public void SortSelection()
        {
            if (m_SelectedItems.Count == 0) return;

            m_SelectedItems.Sort();
            m_SelectedPrimary = m_SelectedItems[0];
        }

        public bool HandleKnotSelection(Event currentEvent, T item, Vector3 position)
        {
            if (!m_Dragging && !IsSelectedAsSingle(item) && currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.alt &&
                HandleUtility.DistanceToCircle(position, EditorTools.KnotSize) < 10.0f)
            {
                // Multi selection
                if (currentEvent.shift)
                {
                    // Range selection for integer types
                    if (typeof(T) == typeof(int) || typeof(T).IsEnum)
                    {
                        int anchorIndex = IsAnySelected ? Convert.ToInt32(m_SelectedPrimary) : Convert.ToInt32(item);

                        int currentIndex = Convert.ToInt32(item);
                        int start = Mathf.Min(anchorIndex, currentIndex);
                        int end = Mathf.Max(anchorIndex, currentIndex);

                        for (int j = start; j <= end; j++)
                        {
                            T value = (T)Convert.ChangeType(j, typeof(T));
                            if (!m_SelectedItems.Contains(value))  m_SelectedItems.Add(value);
                        }
                    }
                    else
                    {
                        // Toggle selection
                        ToggleSelected(item);
                    }
                }
                else if (currentEvent.control || currentEvent.command)
                {
                    // Toggle selection
                    ToggleSelected(item);
                }
                else
                {
                    // Single selection
                    m_SelectedItems.Clear();
                    m_SelectedItems.Add(item);
                }

                if (IsAnySelected)
                {
                    m_SelectedPrimary = m_SelectedItems[0];
                    m_MoveSelect = false;
                    m_DragStartMousePos = currentEvent.mousePosition;
                    currentEvent.Use();
                }

                return true;
            }

            return false;
        }

        public void ToggleSelected(T item)
        {
            if (m_SelectedItems.Contains(item)) m_SelectedItems.Remove(item);
            else m_SelectedItems.Add(item);
        }

        // Dragging & Moving
        public void HandleDrag(Event currentEvent)
        {
            // Check drag threshold
            if (currentEvent.type == EventType.MouseDrag && !m_Dragging && !m_MoveSelect && (currentEvent.mousePosition - m_DragStartMousePos).magnitude > m_DragThreshold)
            {
                m_Dragging = true;
            }
            else if (currentEvent.type == EventType.MouseUp && IsAnySelected && !m_MoveSelect)
            {
                m_Dragging = false;
                m_MoveSelect = true;
                currentEvent.Use();
            }
        }

        public void DragToMove()
        {
            m_Dragging = false;
            m_MoveSelect = true;
        }

        public void Validate()
        {
            if (Empty && m_Dragging)
            {
                m_Dragging = false;
            }

            if (m_SelectedItems.Count == 0) return;

            for (int i = m_SelectedItems.Count - 1; i >= 0; i--)
            {
                if (m_SelectedItems[i] == null)// || m_SelectedItems[i].RoadGuid.Key == "" || m_ProceduralManager.Knots[m_SelectedItems[i].KnotGuid] == null)
                {
                    m_SelectedItems.RemoveAt(i);
                }
            }

            if (Empty)
            {
                Deselect();
            }
        }
    }
}
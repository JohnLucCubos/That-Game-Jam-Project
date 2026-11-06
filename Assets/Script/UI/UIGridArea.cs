using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fills this RectTransform with a grid of UISlot cells, restricted to a shape.
/// Cells outside the shape are never created, so items can't be dropped there.
/// Works with UIItem, UISlotGroup, swaps, and spawners unchanged.
///
/// Runs before other UI scripts so a UISlotGroup on the same object
/// collects the generated cells.
/// </summary>
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(RectTransform))]
public class UIGridArea : MonoBehaviour
{
    public enum Shape
    {
        Rectangle,
        Circle
    }

    public enum CircleFit
    {
        // Cell is kept if its centre is inside the circle (rounder edge, cells overhang).
        CellCenter,
        // Cell is kept only if it's fully inside the circle (no overhang).
        FullCell
    }

    [Header("Grid")]
    [SerializeField, Min(1)] private int columns = 8;
    [SerializeField, Min(1)] private int rows = 8;
    [SerializeField] private Vector2 cellSize = new Vector2(64f, 64f);
    [SerializeField] private Vector2 spacing = Vector2.zero;

    [Header("Restriction")]
    [SerializeField] private Shape shape = Shape.Circle;
    // 0 = fit the grid (half of its smaller side).
    [SerializeField, Min(0f)] private float circleRadius = 0f;
    [SerializeField] private CircleFit circleFit = CircleFit.FullCell;

    [Header("Allowed Items")]
    // Item Ids this grid accepts. Leave empty to accept any item.
    [SerializeField] private List<string> acceptedItemIds = new List<string>();

    [Header("Cells")]
    // Optional cell prefab: needs a UISlot and a raycast-target Image.
    // Leave empty to generate plain cells.
    [SerializeField] private UISlot cellTemplate;
    // Only used for generated cells. Alpha 0 = invisible but still droppable.
    [SerializeField] private Color cellColor = new Color(1f, 1f, 1f, 0.08f);
    [SerializeField] private bool rebuildOnAwake = true;

    [SerializeField, HideInInspector] private List<UISlot> cells = new List<UISlot>();

    // Indexed [column, row]. Null where the cell is outside the shape.
    private UISlot[,] cellLookup;

    public IReadOnlyList<UISlot> Cells
    {
        get { return cells; }
    }

    private void Awake()
    {
        if (rebuildOnAwake || cells.Count == 0)
        {
            Rebuild();
        }
        else
        {
            RebuildLookupFromNames();
        }
    }

    /// <summary>
    /// Returns the cell at (column, row), or null if it's outside the grid or shape.
    /// </summary>
    public UISlot GetCell(int column, int row)
    {
        if (cellLookup == null)
        {
            return null;
        }

        if (column < 0 || column >= columns || row < 0 || row >= rows)
        {
            return null;
        }

        return cellLookup[column, row];
    }

    // Right-click the component header > Rebuild Grid to build in edit mode,
    // so you can parent items under cells before pressing Play.
    [ContextMenu("Rebuild Grid")]
    public void Rebuild()
    {
        ClearCells();
        cellLookup = new UISlot[columns, rows];

        float radius = GetRadius();

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Vector2 position = GetCellPosition(column, row);
                if (!IsInsideShape(position, radius))
                {
                    continue;
                }

                UISlot cell = CreateCell(column, row, position);
                cells.Add(cell);
                cellLookup[column, row] = cell;
            }
        }
    }

    // ---------- Shape ----------

    private float GetRadius()
    {
        if (circleRadius > 0f)
        {
            return circleRadius;
        }

        Vector2 gridSize = GetGridSize();
        return Mathf.Min(gridSize.x, gridSize.y) * 0.5f;
    }

    private bool IsInsideShape(Vector2 cellCenter, float radius)
    {
        switch (shape)
        {
            case Shape.Rectangle:
                return true;
            case Shape.Circle:
                return IsInsideCircle(cellCenter, radius);
            default:
                return true;
        }
    }

    private bool IsInsideCircle(Vector2 cellCenter, float radius)
    {
        switch (circleFit)
        {
            case CircleFit.CellCenter:
                return cellCenter.magnitude <= radius;
            case CircleFit.FullCell:
                // The corner farthest from the centre must be inside.
                Vector2 farCorner = new Vector2(
                    Mathf.Abs(cellCenter.x) + cellSize.x * 0.5f,
                    Mathf.Abs(cellCenter.y) + cellSize.y * 0.5f);
                return farCorner.magnitude <= radius;
            default:
                return true;
        }
    }

    // ---------- Layout ----------

    private Vector2 GetGridSize()
    {
        float width = columns * cellSize.x + (columns - 1) * spacing.x;
        float height = rows * cellSize.y + (rows - 1) * spacing.y;
        return new Vector2(width, height);
    }

    // Local position relative to this RectTransform's centre.
    // Row 0 is the top row.
    private Vector2 GetCellPosition(int column, int row)
    {
        Vector2 step = cellSize + spacing;
        float x = (column - (columns - 1) * 0.5f) * step.x;
        float y = ((rows - 1) * 0.5f - row) * step.y;
        return new Vector2(x, y);
    }

    // ---------- Cells ----------

    private UISlot CreateCell(int column, int row, Vector2 position)
    {
        UISlot cell;

        if (cellTemplate != null)
        {
            cell = Instantiate(cellTemplate, transform);
        }
        else
        {
            GameObject go = new GameObject("Cell", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.GetComponent<Image>().color = cellColor;
            cell = go.AddComponent<UISlot>();
        }

        cell.name = GetCellName(column, row);
        cell.ConfigureAsGridCell(acceptedItemIds);

        RectTransform cellRect = (RectTransform)cell.transform;
        cellRect.anchorMin = new Vector2(0.5f, 0.5f);
        cellRect.anchorMax = new Vector2(0.5f, 0.5f);
        cellRect.pivot = new Vector2(0.5f, 0.5f);
        cellRect.sizeDelta = cellSize;
        cellRect.anchoredPosition = position;

        return cell;
    }

    private void ClearCells()
    {
        foreach (UISlot cell in cells)
        {
            if (cell == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(cell.gameObject);
            }
            else
            {
                DestroyImmediate(cell.gameObject);
            }
        }

        cells.Clear();
    }

    // Used when cells were built in edit mode and rebuildOnAwake is off.
    private void RebuildLookupFromNames()
    {
        cellLookup = new UISlot[columns, rows];

        foreach (UISlot cell in cells)
        {
            if (cell == null)
            {
                continue;
            }

            // Re-apply in case the allowed list changed since the edit-mode build.
            cell.ConfigureAsGridCell(acceptedItemIds);

            string[] parts = cell.name.Split('_');
            if (parts.Length != 3)
            {
                continue;
            }

            if (int.TryParse(parts[1], out int column) && int.TryParse(parts[2], out int row))
            {
                if (column >= 0 && column < columns && row >= 0 && row < rows)
                {
                    cellLookup[column, row] = cell;
                }
            }
        }
    }

    private static string GetCellName(int column, int row)
    {
        return "Cell_" + column + "_" + row;
    }

    // ---------- Editor preview ----------

    private void OnDrawGizmosSelected()
    {
        if (shape != Shape.Circle)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;

        float radius = GetRadius();
        const int segments = 64;
        Vector3 previous = new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 next = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}

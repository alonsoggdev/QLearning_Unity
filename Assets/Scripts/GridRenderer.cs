using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject startPrefab;
    public GameObject goalPrefab;
    public GameObject giftPrefab;

    public float cellSize = 1.0f;

    public void RenderGrid(char[,] grid)
    {
        int rows = grid.GetLength(0);
        int cols = grid.GetLength(1);

        float width = cols * cellSize;
        float height = rows * cellSize;

        Vector3 offset = new Vector3(-width / 2f + cellSize / 2f, height / 2f - cellSize / 2f, 0);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                char cellType = grid[row, col];

                Vector3 position = new Vector3(col * cellSize, -row * cellSize, 0) + offset;

                GameObject prefabToInstantiate = GetPrefabForCell(cellType);
                if (prefabToInstantiate != null)
                {
                    GameObject cell = Instantiate(prefabToInstantiate, position, Quaternion.identity, this.transform);

                    cell.name = $"{row}-{col}";
                    cell.tag = $"{cellType}";
                }
            }
        }

        this.transform.localScale = Vector3.one * 0.8f;

    }

    GameObject GetPrefabForCell(char cellType)
    {
        switch (cellType)
        {
            case '0':
                return floorPrefab;
            case '1':
                return wallPrefab;
            case 'X':
                return startPrefab;
            case 'S':
                return goalPrefab;
            case 'G':
                return giftPrefab;
            default:
                return floorPrefab;
        }
    }
}

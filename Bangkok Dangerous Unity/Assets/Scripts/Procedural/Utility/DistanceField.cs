using System.Collections.Generic;
using UnityEngine;

public static class DistanceField
{
    public static float[,] GenerateDistanceField(float[,] inputGrid, float[,] mask, bool invert = false)
    {
        int width = inputGrid.GetLength(0);
        int height = inputGrid.GetLength(1);
        float[,] distanceField = new float[width, height];

        if (width != mask.GetLength(0) || height != mask.GetLength(1))
        {
            Debug.Log("Invalid size - Can not create distance field");
            return mask;
        }
        
        //Perform BFS to calculate distances
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (invert ? inputGrid[x, y] <= 0.0f : inputGrid[x, y] >= 1.0f)
                {
                    distanceField[x, y] = 0;
                    
                    //Start bfs from obstacle positions
                    queue.Enqueue(new Vector2Int(x, y));
                }
                else
                {
                    distanceField[x, y] = Mathf.Infinity;
                }
            }
        }
        
        //Directions for neighboring cells (left, right, up, down)
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0),  //Right
            new Vector2Int(-1, 0), //Left
            new Vector2Int(0, 1),  //Up
            new Vector2Int(0, -1)  //Down
        };

        //BFS Loop
        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;

                if (IsValid(neighbor, width, height) && float.IsPositiveInfinity(distanceField[neighbor.x, neighbor.y]))
                {
                    //Update distance to be the current cell's distance + 1
                    distanceField[neighbor.x, neighbor.y] = distanceField[current.x, current.y] + 1;
                    queue.Enqueue(neighbor);
                }
            }
        }

        //Normalize values
        float maxValue = float.MinValue;
        float minValue = float.MaxValue;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (distanceField[x, y] < minValue)
                    minValue = distanceField[x, y];
                if (distanceField[x, y] > maxValue)
                    maxValue = distanceField[x, y];
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mask[x, y] >= 1.0f)
                {
                    distanceField[x, y] = Mathf.InverseLerp(maxValue, minValue, distanceField[x, y]);
                }
                else
                {
                    distanceField[x, y] = 0.0f;
                }
            }
        }

        return distanceField;
    }

    private static bool IsValid(Vector2Int pos, int width, int height)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }
}
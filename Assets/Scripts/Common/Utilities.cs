using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public static class Utilities
{
    public static class HungarianAlgorithm
    {
        public static int[] Solve(float[,] costMatrix)
        {
            int rows = costMatrix.GetLength(0);
            int cols = costMatrix.GetLength(1);
            int dim = Mathf.Max(rows, cols);

            float[,] matrix = new float[dim, dim];
            for (int i = 0; i < dim; i++)
                for (int j = 0; j < dim; j++)
                    matrix[i, j] = (i < rows && j < cols) ? costMatrix[i, j] : 0;

            int[] labelByWorker = new int[dim];
            int[] labelByJob = new int[dim];
            float[] minSlackValueByJob = new float[dim];
            int[] minSlackWorkerByJob = new int[dim];
            int[] matchJobByWorker = new int[dim];
            for (int i = 0; i < dim; i++) matchJobByWorker[i] = -1;
            int[] matchWorkerByJob = new int[dim];
            for (int j = 0; j < dim; j++) matchWorkerByJob[j] = -1;

            float[] labelByWorkerRow = new float[dim];
            float[] labelByJobCol = new float[dim];

            for (int i = 0; i < dim; i++)
                for (int j = 0; j < dim; j++)
                    labelByWorkerRow[i] = Mathf.Max(labelByWorkerRow[i], matrix[i, j]);

            for (int w = 0; w < dim; w++)
            {
                float[] slack = new float[dim];
                for (int i = 0; i < dim; i++) slack[i] = float.PositiveInfinity;

                bool[] committedWorkers = new bool[dim];
                int[] parentWorkerByCommittedJob = new int[dim];
                for (int i = 0; i < dim; i++) parentWorkerByCommittedJob[i] = -1;

                int committedJob = -1, committedWorker = w;
                while (true)
                {
                    committedWorkers[committedWorker] = true;
                    float minSlack = float.PositiveInfinity;
                    int minSlackWorker = -1, minSlackJob = -1;

                    for (int j = 0; j < dim; j++)
                    {
                        if (parentWorkerByCommittedJob[j] == -1)
                        {
                            float slackValue = labelByWorkerRow[committedWorker] + labelByJobCol[j] - matrix[committedWorker, j];
                            if (slackValue < slack[j])
                            {
                                slack[j] = slackValue;
                                minSlackWorkerByJob[j] = committedWorker;
                            }
                            if (slack[j] < minSlack)
                            {
                                minSlack = slack[j];
                                minSlackWorker = minSlackWorkerByJob[j];
                                minSlackJob = j;
                            }
                        }
                    }

                    for (int i = 0; i < dim; i++)
                    {
                        if (committedWorkers[i]) labelByWorkerRow[i] -= minSlack;
                    }
                    for (int j = 0; j < dim; j++)
                    {
                        if (parentWorkerByCommittedJob[j] != -1) labelByJobCol[j] += minSlack;
                        else slack[j] -= minSlack;
                    }

                    parentWorkerByCommittedJob[minSlackJob] = minSlackWorker;
                    if (matchWorkerByJob[minSlackJob] == -1)
                    {
                        int committedJobIter = minSlackJob;
                        while (true)
                        {
                            int parentWorker = parentWorkerByCommittedJob[committedJobIter];
                            int temp = matchJobByWorker[parentWorker];
                            match(parentWorker, committedJobIter);
                            if (temp == -1) break;
                            committedJobIter = temp;
                        }
                        break;
                    }
                    else
                    {
                        committedWorker = matchWorkerByJob[minSlackJob];
                    }
                }
            }

            int[] result = new int[rows];
            for (int i = 0; i < rows; i++)
                result[i] = matchJobByWorker[i];

            return result;

            void match(int w, int j)
            {
                matchJobByWorker[w] = j;
                matchWorkerByJob[j] = w;
            }
        }
    }

    public static Vector3 GetClosestPointOnPath(Vector3 target, List<Vector3> pathPoints)
    {
        Vector3 closestPoint = pathPoints[0];
        float minDistanceSqr = float.MaxValue;

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Vector3 a = pathPoints[i];
            Vector3 b = pathPoints[i + 1];

            // Project target onto segment [a, b]
            Vector3 ab = b - a;
            float t = Vector3.Dot(target - a, ab) / ab.sqrMagnitude;
            t = Mathf.Clamp01(t);
            Vector3 projected = a + t * ab;

            float distSqr = (target - projected).sqrMagnitude;
            if (distSqr < minDistanceSqr)
            {
                minDistanceSqr = distSqr;
                closestPoint = projected;
            }
        }

        return closestPoint;
    }

    // Converts to a Vector3 to Vector2(x,z)
    public static Vector2 ToVector2XZ(Vector3 v) => new Vector2(v.x, v.z);

    // Converts to a Vector2 to Vector3(x, given y, z)
    public static Vector3 ToVector3(Vector2 v2, float y) => new Vector3(v2.x, y, v2.y); 
    
    public static float SnapToDirections(float inputAngle, int directionCount)
    {
        float step = 360f / directionCount;
        float snapped = Mathf.Round(inputAngle / step) * step;
        return (snapped % 360 + 360) % 360; // Normalize to 0–360
    }

    public static void FitNavMeshObstacleToMesh(MeshFilter meshFilter, NavMeshObstacle obstacle)
    {
        if (meshFilter == null || obstacle == null)
            return;

        // Get local bounds of the mesh
        Bounds meshBounds = meshFilter.sharedMesh.bounds;

        // Calculate world scale
        Vector3 worldScale = meshFilter.transform.lossyScale;

        // Calculate scaled size from mesh bounds
        Vector3 scaledSize = Vector3.Scale(meshBounds.size, worldScale);

        // Set NavMeshObstacle size
        obstacle.size = scaledSize;

        // Calculate local center in world space
        Vector3 worldCenter = meshFilter.transform.TransformPoint(meshBounds.center);

        // Convert world center to local space of the obstacle
        Vector3 localCenter = obstacle.transform.InverseTransformPoint(worldCenter);

        // Set NavMeshObstacle center
        obstacle.center = localCenter;
    }
}

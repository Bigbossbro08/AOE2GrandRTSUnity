using System;
using System.Collections.Generic;
using UnityEngine;

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

    // Converts to a Vector3 to Vector2(x,z)
    public static Vector2 ToVector2XZ(Vector3 v) => new Vector2(v.x, v.z);

    // Converts to a Vector2 to Vector3(x, given y, z)
    public static Vector3 ToVector3(Vector2 v2, float y) => new Vector3(v2.x, y, v2.y);
}

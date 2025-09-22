using System;

namespace App.BLL.Services
{
    /// <summary>
    /// Cosine similarity for skill vectors.
    /// </summary>
    public static class VectorMath
    {
        public static decimal CosineSimilarity(float[] a, float[] b)
        {
            decimal dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += (decimal)a[i] * (decimal)b[i];
                magA += (decimal)a[i] * (decimal)a[i];
                magB += (decimal)b[i] * (decimal)b[i];
            }

            if (magA == 0 || magB == 0)
            {
                // Return 0 similarity if either vector has zero magnitude
                return 0;
            }

            return dot / (decimal)(Math.Sqrt((double)magA) * Math.Sqrt((double)magB));
        }
    }
}
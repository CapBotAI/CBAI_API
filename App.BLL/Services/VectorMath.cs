using System;

namespace App.BLL.Services
{
    /// <summary>
    /// Cosine similarity for skill vectors.
    /// </summary>
    public static class VectorMath
    {
        public static double CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null) return 0.0;
            if (a.Length != b.Length) return 0.0;

            double dot = 0.0, magA = 0.0, magB = 0.0;
            for (int i = 0; i < a.Length; i++)
            {
                var va = (double)a[i];
                var vb = (double)b[i];
                dot += va * vb;
                magA += va * va;
                magB += vb * vb;
            }

            if (magA <= 0.0 || magB <= 0.0) return 0.0;
            return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
        }
    }
}
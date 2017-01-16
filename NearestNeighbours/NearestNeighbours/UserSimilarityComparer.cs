using System.Collections.Generic;

namespace NearestNeighbours
{
    public class UserSimilarityComparer : IComparer<UserSimilarity>
    {
        public int Compare(UserSimilarity x, UserSimilarity y)
        {
            return (int)(x.Similarity - y.Similarity) * 1000000000;
        }
    }
}

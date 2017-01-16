using System.Collections.Generic;

namespace NearestNeighbours
{
    public class User
    {
        public string UserId { get; set; }
        public HashSet<string> SongsIds { get; set; } = new HashSet<string>();
        public SortedSet<UserSimilarity> Similarities { get; set; } 
            = new SortedSet<UserSimilarity>(new UserSimilarityComparer());
    }
}

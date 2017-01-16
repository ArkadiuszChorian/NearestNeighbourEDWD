using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace NearestNeighbours
{
    class Program
    {
        private const int NumberOfAllUsers = 1014070;
        private const int NumberOfFirstUsers = 100;
        private const int NumberOfUsersToFindSimilarityWith = 100;
        private const int NumberOfTopUsersInSimilarityDictionary = 100;
        private const int MaxUserIdLength = 7;
        private static Dictionary<string, HashSet<string>> FirstUsersAndTheirSongs { get; set; }
        private static Dictionary<string, HashSet<string>> UsersAndTheirSongsOld { get; set; }
        private static Dictionary<string, User> UsersAndTheirSongs { get; set; }
        private static List<User> Users { get; set; } = new List<User>(NumberOfAllUsers);
        private static Stopwatch Stoper { get; set; } = new Stopwatch();
        //Max Songs 1040
        //Users = 1 014 070

        static void Main(string[] args)
        {
            FirstUsersAndTheirSongs = new Dictionary<string, HashSet<string>>(NumberOfFirstUsers);
            //UsersAndTheirSongsOld = new Dictionary<string, HashSet<string>>(NumberOfAllUsers);
            UsersAndTheirSongs = new Dictionary<string, User>(NumberOfAllUsers);

            Console.WriteLine("Reading data...");
            Stoper.Start();

            //ReadData();
            ReadDataOld();

            foreach (var userId in UsersAndTheirSongs.Keys)
            {
                Users.Add(UsersAndTheirSongs[userId]);
            }

            var any = Users.Count(user => user.SongsIds.Count > 1);
            var max = Users.Max(user => user.SongsIds.Count);

            Stoper.Stop();
            var readingDataElapsedSeconds = Stoper.Elapsed.Seconds;
            Console.WriteLine("Reading data finished in " + readingDataElapsedSeconds + " seconds");

            Console.WriteLine("Finding neighbours...");
            Stoper.Restart();

            FindSimilarities(NumberOfUsersToFindSimilarityWith);       

            Stoper.Stop();

            var resultDocument = BuildNearestNeigboursDocument(NumberOfUsersToFindSimilarityWith);
            var findingNeighboursElapsedSeconds = Stoper.Elapsed.Seconds;

            using (var streamWriter = new StreamWriter("times.txt"))
            {
                streamWriter.WriteLine("Reading data [seconds]              = " + readingDataElapsedSeconds);
                streamWriter.WriteLine("Finding nearest neigbours [seconds] = " + findingNeighboursElapsedSeconds);
            }

            using (var streamWriter = new StreamWriter("neigbours.txt"))
            {
                streamWriter.Write(resultDocument);
            }

            Console.Clear();
            Console.WriteLine("Calculation finished");
            Console.WriteLine("Reading data elapsed seconds       = " + readingDataElapsedSeconds);
            Console.WriteLine("Finding neighbours elapsed seconds = " + findingNeighboursElapsedSeconds);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    
        private static void ReadDataOld()
        {           
            try
            {
                using (var streamReader = new StreamReader("facts.csv"))
                {
                    var line = streamReader.ReadLine();

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        //Index:        0,          1,          2,   
                        //Header:       Song ID,    User ID,    Date ID

                        var factData = line.Split(',');
                        var userId = factData[1];
                        var songId = factData[0];                      

                        //if (FirstUsersAndTheirSongs.Count < NumberOfFirstUsers)
                        //{
                        //    if (!FirstUsersAndTheirSongs.ContainsKey(factData[1]))
                        //        FirstUsersAndTheirSongs.Add(factData[1], new HashSet<string>());

                        //    FirstUsersAndTheirSongs[factData[1]].Add(factData[0]);
                        //}

                        if (!UsersAndTheirSongs.ContainsKey(userId))
                        {
                            var user = new User {UserId = userId};
                            user.SongsIds.Add(songId);
                            UsersAndTheirSongs.Add(userId, user);
                        }
                        else
                        {
                            UsersAndTheirSongs[userId].SongsIds.Add(songId);
                        }                   

                        //UsersAndTheirSongs[factData[1]].Add(factData[0]);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }

        private static void ReadData()
        {
            try
            {
                using (var streamReader = new StreamReader("facts.csv"))
                {
                    var line = streamReader.ReadLine();

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        //Index:        0,          1,          2,   
                        //Header:       Song ID,    User ID,    Date ID

                        var factData = line.Split(',');

                        if (!Users.Exists(user => user.UserId == factData[1]))
                        {
                            var user = new User {UserId = factData[1]};
                            user.SongsIds.Add(factData[0]);
                            Users.Add(user);
                        }
                        else
                        {
                            var user = Users.Find(user2 => user2.UserId == factData[1]);
                            user.SongsIds.Add(factData[0]);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("The file could not be read:");
                Console.WriteLine(e.Message);
            }
        }

        private static double CalculateJaccardIndex(ICollection<string> set1, ICollection<string> set2)
        {
            var numberOfCommonElements = set1.Count(set2.Contains);

            return (double) numberOfCommonElements / (set1.Count + set2.Count - numberOfCommonElements);
        }

        //private static Dictionary<string, double> GetSimilarityWithAllUsers(string user)
        //{
        //    var userSongs = UsersAndTheirSongs[user];
        //    var similarityDictionary = new Dictionary<string, double>(UsersAndTheirSongs.Count);

        //    foreach (var userAndHisSongs in UsersAndTheirSongs)
        //    {
        //        var similarity = CalculateJaccardIndex(userSongs, userAndHisSongs.Value);
        //        similarityDictionary.Add(userAndHisSongs.Key, similarity);
        //    }

        //    return similarityDictionary;
        //}

        private static void FindSimilarities(int numberOfUsers)
        {
            var localStoper = new Stopwatch();

            for (var i = 0; i < numberOfUsers; i++)
            {
                Console.Clear();
                Console.WriteLine("Finding similarities for user number " + i);

                var user1 = Users[i];

                for (var j = i; j < Users.Count; j++)
                {
                    var user2 = Users[j];
                    localStoper.Restart();
                    var similarity = CalculateJaccardIndex(user1.SongsIds, user2.SongsIds);
                    localStoper.Stop();

                    if (user1.Similarities.Count < 100)
                    {
                        localStoper.Restart();
                        user1.Similarities.Add(new UserSimilarity { Similarity = similarity, UserId = user2.UserId });
                        localStoper.Stop();
                        if (i != j)
                        {
                            user2.Similarities.Add(new UserSimilarity { Similarity = similarity, UserId = user1.UserId });
                        }       
                    }
                    else
                    {
                        var user1MinimalSimilarity = user1.Similarities.Min;
                        var user2MinimalSimilarity = user2.Similarities.Min;

                        if (similarity > user1MinimalSimilarity.Similarity)
                        {
                            user1.Similarities.Remove(user1MinimalSimilarity);
                            user1.Similarities.Add(new UserSimilarity { Similarity = similarity, UserId = user2.UserId });
                        }

                        if (similarity > user2MinimalSimilarity.Similarity)
                        {
                            user2.Similarities.Remove(user1MinimalSimilarity);
                            user2.Similarities.Add(new UserSimilarity { Similarity = similarity, UserId = user1.UserId });
                        }
                    }
                }
            }
        }

        private static string BuildNearestNeigboursDocument(int numberOfUsers)
        {
            //const int numberOfCharacters = 1550000000;
            const int numberOfCharsInSingleUserSection = 1518;
            
            var stringBuilder = new StringBuilder(numberOfCharsInSingleUserSection * numberOfUsers);

            for (var i = 0; i < numberOfUsers; i++)
            {
                var user = Users[i];

                stringBuilder.AppendLine("User = " + user.UserId);

                foreach (var userSimilarity in user.Similarities)
                {
                    stringBuilder.AppendLine(userSimilarity.UserId.PadLeft(MaxUserIdLength) + " " + userSimilarity.Similarity.ToString("F3", CultureInfo.CreateSpecificCulture("en-US")));
                }

                stringBuilder.AppendLine();
            }
            
            return stringBuilder.ToString();
        }

        //private static string BuildNearestNeigboursDocumentOld(int numberOfUsers)
        //{
        //    //const int numberOfCharacters = 1550000000;
        //    const int numberOfCharsInSingleUserSection = 1518;

        //    var statusCounter = 0;
        //    var stringBuilder = new StringBuilder(numberOfCharsInSingleUserSection * numberOfUsers);
        //    var usersAndTheirSongs = numberOfUsers == UsersAndTheirSongs.Count ? UsersAndTheirSongs : FirstUsersAndTheirSongs;

        //    foreach (var userAndHisSongs in usersAndTheirSongs)
        //    {
        //        stringBuilder.AppendLine("User = " + userAndHisSongs.Key);

        //        var similarityDictionary = GetSimilarityWithAllUsers(userAndHisSongs.Key);
        //        var topNearestNeighbours = similarityDictionary.OrderByDescending(kvp => kvp.Value).Take(NumberOfTopUsersInSimilarityDictionary);

        //        foreach (var nearestNeighbour in topNearestNeighbours)
        //        {
        //            stringBuilder.AppendLine(nearestNeighbour.Key.PadLeft(MaxUserIdLength) + " " + nearestNeighbour.Value.ToString("F3", CultureInfo.CreateSpecificCulture("en-US")));
        //        }

        //        stringBuilder.AppendLine();
        //        statusCounter++;

        //        Console.Clear();
        //        Console.WriteLine("Number of users with calculated nearest neighbours: " + statusCounter);
        //    }

        //    return stringBuilder.ToString();
        //}
    }
}

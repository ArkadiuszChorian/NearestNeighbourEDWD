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
        private const int NumberOfTopUsersInSimilarityDictionary = 100;
        private const int MaxUserIdLength = 7;
        private static Dictionary<string, HashSet<string>> FirstUsersAndTheirSongs { get; set; }
        private static Dictionary<string, HashSet<string>> UsersAndTheirSongs { get; set; }
        private static Stopwatch Stoper { get; set; } = new Stopwatch();
        //Max Songs 1040
        //Users = 1 014 070

        static void Main(string[] args)
        {
            FirstUsersAndTheirSongs = new Dictionary<string, HashSet<string>>(NumberOfFirstUsers);
            UsersAndTheirSongs = new Dictionary<string, HashSet<string>>(NumberOfAllUsers);

            Console.WriteLine("Reading data...");
            Stoper.Start();

            ReadData();

            Stoper.Stop();
            var readingDataElapsedSeconds = Stoper.Elapsed.Seconds;
            Console.WriteLine("Reading data finished in " + readingDataElapsedSeconds + " seconds");

            Console.WriteLine("Finding neighbours...");
            Stoper.Restart();

            var resultDocument = BuildNearestNeigboursDocument(NumberOfFirstUsers);

            Stoper.Stop();
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

                        if (FirstUsersAndTheirSongs.Count < NumberOfFirstUsers)
                        {
                            if (!FirstUsersAndTheirSongs.ContainsKey(factData[1]))
                                FirstUsersAndTheirSongs.Add(factData[1], new HashSet<string>());

                            FirstUsersAndTheirSongs[factData[1]].Add(factData[0]);
                        }
                        
                        if (!UsersAndTheirSongs.ContainsKey(factData[1]))
                            UsersAndTheirSongs.Add(factData[1], new HashSet<string>());

                        UsersAndTheirSongs[factData[1]].Add(factData[0]);
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

        private static Dictionary<string, double> GetSimilarityWithAllUsers(string user)
        {
            var userSongs = UsersAndTheirSongs[user];
            var similarityDictionary = new Dictionary<string, double>(UsersAndTheirSongs.Count);

            foreach (var userAndHisSongs in UsersAndTheirSongs)
            {
                var similarity = CalculateJaccardIndex(userSongs, userAndHisSongs.Value);
                similarityDictionary.Add(userAndHisSongs.Key, similarity);
            }

            return similarityDictionary;
        }

        private static string BuildNearestNeigboursDocument(int numberOfUsers)
        {
            //const int numberOfCharacters = 1550000000;
            const int numberOfCharsInSingleUserSection = 1518;

            var statusCounter = 0;
            var stringBuilder = new StringBuilder(numberOfCharsInSingleUserSection * numberOfUsers);
            var usersAndTheirSongs = numberOfUsers == UsersAndTheirSongs.Count ? UsersAndTheirSongs : FirstUsersAndTheirSongs;

            foreach (var userAndHisSongs in usersAndTheirSongs)
            {                                      
                stringBuilder.AppendLine("User = " + userAndHisSongs.Key);

                var similarityDictionary = GetSimilarityWithAllUsers(userAndHisSongs.Key);
                var topNearestNeighbours = similarityDictionary.OrderByDescending(kvp => kvp.Value).Take(NumberOfTopUsersInSimilarityDictionary);

                foreach (var nearestNeighbour in topNearestNeighbours)
                {
                    stringBuilder.AppendLine(nearestNeighbour.Key.PadLeft(MaxUserIdLength) + " " + nearestNeighbour.Value.ToString("F3", CultureInfo.CreateSpecificCulture("en-US")));
                }

                stringBuilder.AppendLine();
                statusCounter++;
                
                Console.Clear();
                Console.WriteLine("Number of users with calculated nearest neighbours: " + statusCounter);
            }

            return stringBuilder.ToString();
        }         
    }
}

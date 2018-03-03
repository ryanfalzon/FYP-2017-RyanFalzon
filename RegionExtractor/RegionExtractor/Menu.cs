﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bio;
using Bio.Algorithms.Alignment.MultipleSequenceAlignment;
using System.IO;
using Neo4j.Driver.V1;

namespace RegionExtractor
{
    class Menu
    {
        // Private properties
        private string choice;
        private List<Sequence> data;
        private int funFamCount;

        // Constructor
        public Menu()
        {
            this.choice = "";
            funFamCount = 0;
        }

        // Method to output the menuma
        public void Show()
        {
            Console.WriteLine("\nMAIN MENU");
            Console.WriteLine("---------\n");
            Console.WriteLine("1) Generate Regions");
            Console.WriteLine("2) Transfer Functional Families To Graph Database");
            Console.Write("\nEnter Choice or X to Exit: ");
            choice = Console.ReadLine();
            CheckInput(choice);
        }

        // Method to determine user input in menu
        public void CheckInput(string choice)
        {
            // Some temp variables
            string database;
            string table;
            DatabaseConnection db;

            // Choice
            switch (choice)
            {
                case "1":
                    Console.Write("\nEnter Database Name: ");
                    database = Console.ReadLine();
                    Console.Write("Enter Table Name: ");
                    table = Console.ReadLine();
                    db = new DatabaseConnection(database, table);
                    if (db.Connect(true))
                    {
                        data = db.Query1();
                        if (db.Connect(false))
                        {
                            ExtractRegions();
                        }
                        else
                        {
                            Show();
                        }
                    }
                    else
                    {
                        Show();
                    }
                    break;

                case "2":
                    Console.Write("\nEnter Database Name: ");
                    database = Console.ReadLine();
                    Console.Write("Enter Table Name: ");
                    table = Console.ReadLine();
                    db = new DatabaseConnection(database, table);
                    if (db.Connect(true))
                    {
                        List<FunFam> funFams = db.Query2();
                        if (db.Connect(false))
                        {
                            ToCSV(funFams);
                        }
                        else
                        {
                            Show();
                        }
                    }
                    else
                    {
                        Show();
                    }
                    break;

                case "3":break;

                case "X":
                    System.Environment.Exit(1);
                    break;

                default:
                    Console.Write("\nInvalid Input. Press Any Key To Continue...");
                    Console.ReadLine();
                    Console.WriteLine();
                    Show();
                    break;
            }
        }

        // Method to extract the regions from the loaded dataset
        private void ExtractRegions()
        {
            
            // Some temporary variables
            List<Sequence> sequences = GetNextFunFam();
            List<int> lengths = new List<int>();
            List<Bio.Sequence> regions = new List<Bio.Sequence>();
            List<string> kmers = new List<string>();
            IList<Bio.Algorithms.Alignment.ISequenceAlignment> alignedRegions;
            PAMSAMMultipleSequenceAligner aligner = new PAMSAMMultipleSequenceAligner();
            string consensusTemp = "";

            // Iterate while their are more functional families to process
            while (sequences.Count > 0)
            {
                // Output headings for table
                Console.WriteLine("\nProtein ID\t\t\t\t|\tFunctional Family\t\t|\tRegion");
                Console.WriteLine("----------\t\t\t\t\t-----------------\t\t\t------");

                // Process current sequences
                foreach (Sequence s in sequences)
                {
                    Console.Write(s.Protein_id + "\t|\t" + s.Functional_family + "\t\t|\t");

                    // Check if sequence is not null
                    if (s.Full_sequence != "")
                    {
                        lengths.Add(s.getLength());
                        //regions.Add(s.Sequence_header);
                        //regions.Add(s.Full_sequence.Substring(s.RegionX, s.getLength()));

                        regions.Add(new Bio.Sequence(Alphabets.Protein, s.Full_sequence.Substring((s.RegionX - 1), s.getLength())));
                        Console.Write(s.Full_sequence.Substring((s.RegionX - 1), s.getLength()));
                    }
                    else
                    {
                        Console.Write("No sequence for this protein!");
                    }
                    Console.WriteLine();
                }

                // Output some statistics
                CalculateStatistics(lengths);
                //System.IO.File.WriteAllLines("regions - " + sequences.ElementAt(0).Functional_family + ".txt", regions);

                // Check if functional family has more than one sequence
                if(regions.Count > 1)
                {
                    // Calculate the multiple sequence alignment for the extracted regions
                    alignedRegions = aligner.Align(regions);
                    Console.WriteLine("\nMultiple Sequence Alignment:");
                    Console.WriteLine(alignedRegions[0]);

                    // Calculate the consensus sequence
                    consensusTemp = GetConsensus(AnalyzeMSA(alignedRegions[0].ToString()));
                    Console.WriteLine("Consensus Sequence:");
                    Console.WriteLine(consensusTemp + "\n");
                    alignedRegions.Clear();
                }
                else
                {
                    consensusTemp = regions[0].ToString();
                    Console.WriteLine("\nNo Need For Multiple Sequence Alignment or Consensus Resolver.\n");
                }

                // Get the k-mers for the consensus sequence
                /*kmers = StoreKmers(consensusTemp, 3, 0);
                foreach(string k in kmers)
                {
                    Console.WriteLine(k);
                }*/

                // Send data to graph database
                //ToGraph(sequences[0].Functional_family, kmers);

                // Reset temp variables
                lengths.Clear();
                sequences.Clear();
                regions.Clear();
                kmers.Clear();
                Console.WriteLine();

                // Get the next functional family
                sequences = GetNextFunFam();
            }
            Console.ReadLine();
        }

        // Method to get next set of proteins for a functional family
        private List<Sequence> GetNextFunFam()
        {
            // Some temporary variables
            List<Sequence> sequences = new List<Sequence>();
            string currentFunFam;

            // Check if data is available
            if(data.Count > 0)
            {
                currentFunFam = data.ElementAt(0).Functional_family;

                // Iterating until the functional family changes
                while ((data.Count > 0) && (data.ElementAt(0).Functional_family == currentFunFam))
                {
                    sequences.Add(data.ElementAt(0));
                    data.RemoveAt(0);
                }
            }
            
            // Return the list of sequences
            return sequences;
        }

        // Calculate the statistics
        static void CalculateStatistics(List<int> lengths)
        {
            int max = 0;
            int min = 100;
            double average = 0;
            double median = 0;
            double variance = 0;
            double standardDeviation = 0;

            // Iterate through all the lengths in the list to calculate max, min and average values for length
            foreach (int length in lengths)
            {
                // Check if current length is the longest
                if (length >= max)
                {
                    max = length;
                }
                // Check if current length is the smallest
                else if (length < min)
                {
                    min = length;
                }

                // Add the length for the average
                average += length;
            }
            average = average / lengths.Count;

            // Calculate the standard deviation
            foreach (int length in lengths)
            {
                variance += Math.Pow((length - average), 2);
            }
            standardDeviation = Math.Sqrt(variance);

            // Output statistics
            Console.WriteLine("\n\nStatistics");
            Console.WriteLine("----------");
            Console.Write("\nMaximum Length = " + max + "\nMinimum Length = " + min + "\nAverage Length = " + average + "\nMedian Length = ");

            // Check if length of lengths is even
            if ((lengths.Count % 2) == 0)
            {
                median = (lengths.ElementAt(Convert.ToInt32(Math.Floor(Convert.ToDouble(lengths.Count / 2)))) + lengths.ElementAt(Convert.ToInt32(Math.Ceiling(Convert.ToDouble(lengths.Count / 2))))) / 2;
            }
            else
            {
                median = lengths.ElementAt(lengths.Count / 2);
            }
            Console.Write(median + "\nVariance = " + variance + "\nStandard Devaition = " + standardDeviation + "\n\n");
        }

        // Method to transfer passed contents to a csv file
        private void ToCSV(List<FunFam> values)
        {
            // Initialize a csv holder
            var csv = new StringBuilder();

            // Add the headers
            csv.AppendLine(string.Format("{0},{1},{2}", "CATH FunFam ID", "CATH Family", "Functional Family"));

            // Iterate through all the passed values
            foreach(FunFam s in values)
            {
                var first = s.CathFunFamID;
                var second = s.CathFamily;
                var third = s.FunctionalFamily;
                var newLine = string.Format("{0},{1},{2}", first, second, third);
                csv.AppendLine(newLine);
            }

            // Write to file
            File.WriteAllText("functional_families.csv", csv.ToString());
        }

        // method to transfer contents of CSV to Neo4J
        private void ToGraph(string funFam, List<string> kmers)
        {
            /*using (var driver = GraphDatabase.Driver("bolt://localhost", AuthTokens.Basic("neo4j", "fyp_ryanfalzon")))
            using (var session = driver.Session())
            {
                session.Run("LOAD CSV WITH HEADERS FROM \"file:///functional_families.csv\" as " +
                    "funfam create(f1: FunFam { cath_funfam_id: funfam.CATH_FunFam_ID, cath_family: funfam.CATH_Family, functional_family: funfam.Functional_Family})");
                //session.Run("CREATE (a:Person {name:'Arthur', title:'King'})");
                //var result = session.Run("MATCH (a:Person) WHERE a.name = 'Arthur' RETURN a.name AS name, a.title AS title");

                /*foreach (var record in result)
                    Console.WriteLine($"{record["title"].As<string>()} {record["name"].As<string>()}");
            }*/

            // Temp variables
            int count = 0;
            string query = "CREATE (f:FunFam {id:\"" + funFamCount.ToString() + "\", name:\"" + funFam + "\"}) - [:HAS] -> ";

            // Add the kmers to the query
            for (int i = 0; i < kmers.Count; i++)
            {
                query += "(k" + count.ToString() + ":Kmer {id:\"" + count.ToString() + "\", name:\"" + kmers[i] + "\"})";
                count++;

                // Check if this is the last kmer
                if (i != (kmers.Count - 1))
                {
                    query += " - [:NEXT] -> ";
                }
            }

            funFamCount++;

            // Run the query
            using (var driver = GraphDatabase.Driver("bolt://localhost", AuthTokens.Basic("neo4j", "fyp_ryanfalzon")))
            using (var session = driver.Session())
            {
                session.Run(query);
            }
        }

        // Method to analyze the multiple sequence alignment produced
        private List<string> AnalyzeMSA(string msa)
        {
            // Some temporary variables
            List<string> seperatedRegions = new List<string>();
            string temp = "";
            int counter = 0;

            // Iterate through all the string
            while(counter < msa.Length)
            {
                // Check if current value is a .
                if(msa[counter] == '.')
                {
                    // Iterate until next line is available
                    while(msa[counter] != '\n')
                    {
                        counter++;
                    }
                    counter++;
                    seperatedRegions.Add(temp);
                    temp = "";
                }

                // Add current value
                else
                {
                    temp += msa[counter];
                    counter++;
                }
            }

            // Return answer
            return seperatedRegions;
        }

        // Method to calculate the consensus of a set of aligned sequences
        private string GetConsensus(List<string> msa)
        {
            // Some temporary variables
            SimpleConsensusResolver resolver = new SimpleConsensusResolver(Alphabets.Protein);
            List<List<byte>> coloumns = new List<List<byte>>();
            int thresholds = 0;
            string temp = "";

            //  Iterate through all the aligned sequences
            if (msa.Count > 1){
                for (int i = 0; i < msa[0].Length; i++)
                {
                    coloumns.Add(new List<byte>());
                    for (int j = 0; j < msa.Count; j++)
                    {
                        coloumns[i].Add((byte)msa[j][i]);
                    }
                }
            }

            // Get the thresholds
            //resolver.Threshold = GetThreshold(coloumns);
            List<int> thresholdColoumns = GetThreshold(coloumns);

            // Get the consesnus for the coloumns
            foreach(List<byte> coloumn in coloumns)
            {
                // Get the current consensus
                resolver.Threshold = thresholdColoumns[coloumns.IndexOf(coloumn)];
                temp += (char)(resolver.GetConsensus(coloumn.ToArray()));
            }

            // Return consensus
            return temp;
        }

        // Method to analayze the passed data to get an optimum threshold
        public List<int> GetThreshold(List<List<byte>> data)
        {
            // Some temporary variables
            List<byte> bytes = new List<byte>();
            List<int> byteCounter = new List<int>();
            List<int> thresholdColoumns = new List<int>();
            int max = 0;
            int temp = 0;

            //  Iterate through all the aligned sequences
            foreach(List<byte> list in data)
            {
                foreach(byte b in list)
                {
                    // Check if byte is already in list
                    if (!bytes.Contains(b))
                    {
                        bytes.Add(b);
                        byteCounter.Add(1);
                    }
                    else
                    {
                        byteCounter[bytes.IndexOf(b)] += 1;
                    }
                }

                // Analyze the gathered data so far
                foreach(int b in byteCounter)
                {
                    // Check if current value is greater than the maximum
                    if (b > max)
                    {
                        max = b;
                    }
                }

                // Output Some Statistics
                temp = Convert.ToInt32((Convert.ToDouble(max) / Convert.ToDouble(list.Count)) * 100);
                Console.WriteLine("Threshold For Current Coloumn: " + (char)bytes[byteCounter.IndexOf(max)] + " -> " + temp + "%");
                thresholdColoumns.Add(temp);

                // Reset variables
                bytes.Clear();
                byteCounter.Clear();
                max = 0;
                temp = 0;
            }

            /*// Get the mean and median threshold
            int thresholdMean = 0;
            int thresholdMedian = 0;
            foreach (int tc in thresholdColoumns)
            {
                thresholdMean += tc;
            }
            thresholdMean = thresholdMean / thresholdColoumns.Count();
            thresholdMedian = Convert.ToInt32(GetMedian(thresholdColoumns));
            Console.WriteLine("Threshold For MSA Using Mean: " + thresholdMean);
            Console.WriteLine("Threshold For MSA Using Median: " + thresholdMedian);
            return thresholdMedian;*/

            // Return the threshold for each coloumn
            return thresholdColoumns;
        }

        // Method to find the median value
        private double GetMedian(List<int> data)
        {
            int[] dataClone = data.ToArray();
            Array.Sort(dataClone);

            //get the median
            int size = dataClone.Length;
            int mid = size / 2;
            double median = (size % 2 != 0) ? (double)dataClone[mid] : ((double)dataClone[mid] + (double)dataClone[mid - 1]) / 2;
            return median;
        }


        // A recursive method to output all the possible kmers of a particular size
        private List<string> StoreKmers(string dna, int size, int counter)
        {
            List<string> kmers = new List<string>();

            // Continue outputting the kmers
            if (dna.Length != (counter + size))
            {
                kmers.Add(dna.Substring(counter, size));
                kmers.AddRange(StoreKmers(dna, size, (counter + 1)));
                return kmers;
            }
            else
            {
                return kmers;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions; // For using Regex

namespace Isidore.Load
{

    /// <summary>
    /// Provides means for passing data
    /// </summary>
    public partial class Data
    {
        /// <summary>
        /// Structure for storing Tecplot 3D data
        /// </summary>
        public struct Tecplot
        {
            /// <summary>
            /// Plot title
            /// </summary>
            public string Title;

            /// <summary>
            /// Variables names
            /// </summary>
            public string[] Variables;

            /// <summary>
            /// Ant auxiliary data included in the file header
            /// </summary>
            public string[] AuxiliaryData;

            /// <summary>
            /// 3D data contained in the file
            /// </summary>
            public double[][, ,] Data;
        }
    }

    /// <summary>
    /// A toolbox for loading data from files into memory
    /// </summary>
    public partial class Load
    {
        /// <summary>
        /// Tecplot full file name
        /// </summary>
        private static StreamReader textFile;

        /// <summary>
        /// Current line being parsed
        /// </summary>
        private static string thisLine;
        private static string tecplotDataLine;
        private static readonly object tecplotSync = new object();

        /// <summary>
        /// Regular expression for identifying and parsing any double quotes
        /// </summary>
        private static Regex reg = new Regex("\"([^\"]*)\""); 

        /// <summary>
        /// Processes a tecplot ASCII file and returns the data as a 
        /// 3D array.  Additional data is also contained in the 
        /// outputted structure
        /// </summary>
        /// <param name="fileStr"> Tecplot full file name </param>
        /// <returns> Tecplot data structure </returns>
        public static Data.Tecplot Tecplot(string fileStr)
        {
            
            // Checks for file
            if (!File.Exists(fileStr))
                throw new Exception("File does not exist.");

            // Legacy helper methods share reader state, so file-based entry
            // points serialize access to that state.
            lock (tecplotSync)
            {
                textFile = File.OpenText(fileStr);
                try
                {
                    Tuple<string, List<string>, List<string>, int[], bool> items = TecplotHeader();
                    List<string> variables = items.Item2;
                    int[] dataDims = items.Item4;
                    bool blockFormat = items.Item5;
                    double[][,,] data = blockFormat
                        ? assembleTecplotBlock(dataDims, variables.Count)
                        : assembleTecplotPoint(dataDims, variables.Count);
                    Data.Tecplot tplot = new Data.Tecplot();
                    tplot.Title = items.Item1;
                    tplot.Variables = items.Item2.ToArray();
                    tplot.AuxiliaryData = items.Item3.ToArray();
                    tplot.Data = data;
                    return tplot;
                }
                finally { textFile.Dispose(); }
            }
        }

        /// <summary>
        /// Processes 3D point formatted Tecplot data.
        /// Assumes stream is already pointed to data.
        /// </summary>
        /// <param name="dimSize"> Array size in each dimension </param>
        /// <param name="valNum"> Number of variables </param>
        /// <returns> 3D data array </returns>
        public static double[][,,] assembleTecplotBlock(int[] dimSize, 
            int valNum)
        {
            if (dimSize == null || dimSize.Length != 3 || valNum <= 0 ||
                dimSize[0] <= 0 || dimSize[1] <= 0 || dimSize[2] <= 0)
                throw new FormatException("Tecplot requires positive dimensions and at least one variable.");
            // Makes new data array
            double[][,,] data = new double[valNum][,,];
            for(int idx=0; idx<valNum; idx++)
                data[idx] = new double[dimSize[0],dimSize[1],dimSize[2]];

            // Indexers and counters
            int totEl = checked(dimSize[0]*dimSize[1]*dimSize[2]); // Total elements
            int expectedCount = checked(totEl * valNum);
            int cnt = 0; // Data indices counter
            int inc2 = dimSize[0]*dimSize[1]; // Third axis increment value
            
            // Parses data
            while(tecplotDataLine != null || !textFile.EndOfStream)
            {
                thisLine = tecplotDataLine ?? textFile.ReadLine();
                tecplotDataLine = null;
                int comment = thisLine.IndexOf('#');
                if (comment >= 0)
                    thisLine = thisLine.Substring(0, comment);
                string[] theseStr = thisLine.Split(new char[] { ' ', '\t', ',' },
                    StringSplitOptions.RemoveEmptyEntries);
                for(int idx = 0; idx < theseStr.Length; idx++)
                {
                    // Checks for empties
                    if(!String.IsNullOrEmpty(theseStr[idx]))
                    {
                        if (cnt >= expectedCount)
                            throw new FormatException("Tecplot contains more values than its declared dimensions.");
                        // Current data locations
                        int idx0 = cnt % dimSize[0]; // i
                        int idx1 = (cnt/dimSize[0]) % dimSize[1]; // j
                        int idx2 = (cnt/inc2) % dimSize[2]; // k
                        int idxV = cnt/totEl; // variable

                        // Assigns data
                        double thisVal = double.Parse(theseStr[idx].Replace('D', 'E').Replace('d', 'E'),
                            NumberStyles.Float, CultureInfo.InvariantCulture);
                        data[idxV][idx0, idx1, idx2] = thisVal;
                    
                        // Increment counters
                        cnt ++;
                    }
                }
                
            }

            if (cnt != expectedCount)
                throw new FormatException("Tecplot contains fewer values than its declared dimensions.");

            return data;
        }

        /// <summary>
        /// Processes 3D point formatted Tecplot data
        /// </summary>
        /// <param name="dimSize"> Array size in each dimension </param>
        /// <param name="valNum"> Number of variables </param>
        /// <returns> 3D data array </returns>
        public static double[][, ,] assembleTecplotPoint(int[] dimSize, 
            int valNum)
        {
            throw new NotImplementedException();
        }

       
        /// <summary>
        /// Reads the header of a Tecplot file.  Returning the plot's 
        /// title, variable names, auxiliary information, data size 
        /// in each dimension, and a block format tag 
        /// </summary>
        /// <returns> A tuple containing the data outlined in the 
        /// summary </returns>
        public static Tuple<string, List<string>, 
            List<string>, int[], bool> TecplotHeader()
        {

            string titleStr = null;
            List<string> varList = new List<string>();
            List<string> auxDataList = new List<string>();
            // Omitted ordered-zone dimensions have size one.
            int[] arrSize = new int[] { 1, 1, 1 };
            bool blockFormat = false;
            string auxStr = "auxdata";
            bool hitZone = false;
            bool readingVariables = false;
            tecplotDataLine = null;
            while (true)
            {
                thisLine = textFile.ReadLine();
                if (thisLine == null)
                    break;
                thisLine = thisLine.Trim();
                if (thisLine.Length == 0 || thisLine.StartsWith("#"))
                    continue;

                // Keep the first data line for the assembler rather than
                // requiring a blank line or an optional DT declaration.
                char first = thisLine[0];
                if (hitZone && (char.IsDigit(first) || first == '+' ||
                    first == '-' || first == '.'))
                {
                    tecplotDataLine = thisLine;
                    break;
                }
                if (thisLine.IndexOf(auxStr, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    int idx = thisLine.IndexOf(auxStr,
                        StringComparison.OrdinalIgnoreCase);
                    auxDataList.Add(thisLine.Substring(idx + auxStr.Length).Trim());
                    continue;
                }
                if (thisLine.StartsWith("title", StringComparison.OrdinalIgnoreCase))
                {
                    MatchCollection matches = reg.Matches(thisLine);
                    if (matches.Count == 0)
                        throw new FormatException("The Tecplot title must be quoted.");
                    titleStr = matches[0].Groups[1].Value;
                }
                if (thisLine.StartsWith("variables", StringComparison.OrdinalIgnoreCase))
                {
                    varList = retrieveVars();
                    readingVariables = true;
                    continue;
                }
                if (readingVariables && thisLine.StartsWith("\""))
                {
                    foreach (Match match in reg.Matches(thisLine))
                        varList.Add(match.Groups[1].Value);
                    continue;
                }
                readingVariables = false;
                if (thisLine.StartsWith("zone", StringComparison.OrdinalIgnoreCase))
                    hitZone = true;
                if (hitZone)
                {
                    string[] dimensionNames = new string[] { "i", "j", "k" };
                    for (int dimension = 0; dimension < 3; dimension++)
                    {
                        Match match = Regex.Match(thisLine, @"(?:^|[\s,])" +
                            dimensionNames[dimension] + @"\s*=\s*([+-]?\d+)", RegexOptions.IgnoreCase);
                        if (match.Success)
                            arrSize[dimension] = int.Parse(match.Groups[1].Value,
                                CultureInfo.InvariantCulture);
                    }
                    if (thisLine.IndexOf("block", StringComparison.OrdinalIgnoreCase) >= 0)
                        blockFormat = true;
                }
            }

            if (!hitZone || varList.Count == 0)
                throw new FormatException("Tecplot requires a zone and quoted variable names.");

            return Tuple.Create(titleStr, varList, auxDataList, arrSize, 
                blockFormat);
        }

        /// <summary>
        /// Returns an integer bound by the two markers
        /// </summary>
        /// <param name="str"> Text line to process </param>
        /// <param name="mark0"> Left bounding marker </param>
        /// <param name="mark1"> Right bounding marker </param>
        /// <returns> Integer bound by the markers </returns>
        private static int retrieveInt(string str, string mark0, 
            string mark1)
        {
            // Finds the position of the first and last markers
            int idx = str.IndexOf(mark0, 
                StringComparison.OrdinalIgnoreCase) + mark0.Length;
            string thisStr = str.Substring(idx);
            idx = thisStr.IndexOf(mark1, 
                StringComparison.OrdinalIgnoreCase);
            thisStr = thisStr.Substring(0, idx);

            return int.Parse(thisStr);
        }

        /// <summary>
        /// Returns all variable names from a Tecplot file
        /// </summary>
        /// <returns> All variable names </returns>
        private static List<string> retrieveVars()
        {

            List<string> varList = new List<string>();

            // Parses first line since least one variable 
            // name will be there
            MatchCollection matches = reg.Matches(thisLine);
            // Adds to varList
            foreach (object item in matches)
            {
                string varStr = item.ToString().Replace("\"", "");
                varList.Add(varStr);
            }

            return varList;
        }

        /// <summary>
        /// Reads the header of a Tecplot file.  Returning the plot's 
        /// title, variable names, auxiliary information, data size in 
        /// each dimension, and a block format tag 
        /// </summary>
        /// <param name="fileStr"> Tecplot full file name </param>
        /// <returns> A tuple containing the data outlined in the 
        /// summary </returns>
        public static Tuple<string, List<string>, List<string>, int[], bool> 
            TecplotHeader(string fileStr)
        {
            // Checks for file
            if (!File.Exists(fileStr))
                throw new Exception("File does not exist.");

            lock (tecplotSync)
            {
                textFile = File.OpenText(fileStr);
                try { return TecplotHeader(); }
                finally { textFile.Dispose(); }
            }
        }
    }
}

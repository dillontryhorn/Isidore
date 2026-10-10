using System;
using System.Linq;
using System.IO;

namespace Isidore.Load
{
    /// <summary>
    /// A toolbox for loading or reading ASCII text data
    /// </summary>
    public class Text
    {

        /// <summary>
        /// Loads a text file into memory, delimits via EOL
        /// </summary>
        /// <param name="fileStr">  File location and name</param>
        /// <returns> string array, one segment per line </returns>
        public static string[] Load(string fileStr)
        {
            // Checks for file
            if (!File.Exists(fileStr))
                throw new Exception("File does not exist.");

            // If available, loads text into an object & copies to a string
            StreamReader textFile = new StreamReader(fileStr);
            string text = textFile.ReadToEnd();
            textFile.Close();

            // Splits along any of these strings
            string[] Lines = Parse(text);
            return Lines;
        }

        /// <summary>
        /// Parses a text entry using fixed length demarcation
        /// </summary>
        /// <param name="line"> text line </param>
        /// <param name="lengths"> number of characters in each data 
        /// field </param>
        /// <returns> Array of strings divided along delimiters </returns>
        public static string[] Parse(string line, int[] lengths)
        {
            // Pads line if too small
            int totChar = lengths.Sum();
            if (totChar > line.Length)
                line = line.PadRight(totChar);

            // Separates string array
            string[] fields = new string[lengths.Length];
            int place = 0; // Indicates the current position in line
            for (int idx = 0; idx < lengths.Length; idx++)
            {
                fields[idx] = line.Substring(place, lengths[idx]);
                place += lengths[idx];
            }

            return fields;
        }

        /// <summary>
        /// Parses a text entry using a string list demarcation 
        /// (Can be more than one string).  If not provided, all newlines 
        /// and carriage combinations are used as a delimiters (i.e.
        /// Delimiters = new string[] { "\r\n", "\n\r", "\n", "\r" };
        /// </summary>
        /// <param name="Line"> text line </param>
        /// <param name="Delimiters"> strings to us as 
        /// delimiters </param>
        /// <returns> Array of strings divided along 
        /// delimiters </returns>
        public static string[] Parse(string Line, string[] Delimiters = 
            null)
        {
            string[] delimiters = Delimiters ?? 
                new string[] { "\r\n", "\n\r", "\n", "\r" };
            return Line.Split(delimiters, StringSplitOptions.None);
        }
    }
}

namespace Isidore.Load
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Provides information on a file format
    /// </summary>
    public partial class FileRef
    {
        /// <summary>
        /// Provides field demarcation for a NASTRAN formated name list
        /// </summary>
        public class NAS
        {
            /// <summary>
            /// Enumeration of different data entries accessed while 
            /// loading
            /// </summary>
            public enum dataType { 
                /// <summary>
                /// Indicates the line is neither GRID or CTETRA data
                /// </summary>
                None, 
                /// <summary>
                /// Indicates the line is GRID data
                /// </summary>
                GRID, 
                /// <summary>
                /// Indicates the line is CTETRA data
                /// </summary>
                CTETRA };

            /// <summary>
            /// Data format for .dat and .nas files
            /// </summary>
            public class Format
            {
                /// <summary>
                /// Fixed field data format cell lengths
                /// </summary>
                public static int[] fixedField = new 
                    int[] { 8, 8, 8, 8, 8, 8, 8, 8, 8, 8 };
                /// <summary>
                /// High precision data format cell lengths
                /// </summary>
                public static int[] highPrecis = new 
                    int[] { 8, 16, 16, 16, 16, 8 };

                /// <summary>
                /// Enumerates the four different data formats type 
                /// available
                /// </summary>
                public enum fType { 
                    /// <summary>
                    /// Indicates the NAS string is a free format
                    /// </summary>
                    Free, 
                    /// <summary>
                    /// Indicates the NAS string is a fixed format
                    /// </summary>
                    Fixed, 
                    /// <summary>
                    /// Indicates the NAS string is a high precision 
                    /// format
                    /// </summary>
                    HighPrec, 
                    /// <summary>
                    /// Indicates the NAS string is an integer format
                    /// </summary>
                    Integer };

                /// <summary>
                /// Determines which data format is used on this line
                /// </summary>
                /// <param name="data"> Data string </param>
                /// <returns> Data identification enumeration </returns>
                public static fType WhichFormat(string data)
                {
                    // grabs the mnemonic data from columns 0-7
                    string mnen = data.Substring(0, Math.Min(8, data.Length)).TrimEnd(' ');
                    if (data.IndexOf(',') >= 0) return fType.Free;
                    else if (mnen.EndsWith("*")) return fType.HighPrec;
                    else return fType.Fixed;
                }

                /// <summary>
                /// Provides the demarcation array or delimiter strings 
                /// for each data format
                /// </summary>
                /// <param name="formType"> Format type </param>
                /// <returns> Either an integer array of or a string 
                /// array of delimiters </returns>
                public dynamic Demark(fType formType)
                {
                    switch (formType)
                    {
                        case fType.Fixed:
                            return fixedField;
                        case fType.HighPrec:
                            return highPrecis;
                        default:
                            return new string[] { "," };
                    }
                }

                /// <summary>
                /// Takes a single data entry line and returns the 
                /// component fields
                /// </summary>
                /// <param name="line"> Data entry line </param>
                /// <returns> Each data field listed as a string 
                /// array</returns>
                public static string[] Separate(string line)
                {
                    fType type = WhichFormat(line);
                    Format form = new Format();
                    dynamic delim = form.Demark(type);
                    string[] dataEntry = Text.Parse(line, delim);
                    return dataEntry;
                }
            }

            /// <summary>
            /// Fields common across all data entries
            /// </summary>
            public class CommonField
            {
                /// <summary>
                /// Grid point identification number
                /// </summary>
                public const int Mnemonic = 0;
                /// <summary>
                /// Continue-to-next-line
                /// </summary>
                public const int Continue = 9;
            }

            /// <summary>
            /// GRID data entry field description
            /// </summary>
            public class GRID
            {
                /// <summary>
                /// Provides the field positions for each data entry
                /// </summary>
                public class DataField: CommonField
                {
                    /// <summary>
                    /// Grid point identification number
                    /// </summary>
                    public const int ID = 1;
                    /// <summary>
                    /// Coordinate system ID defining the point
                    /// </summary>
                    public const int CoorIn = 2;
                    /// <summary>
                    /// Cartesian grid point
                    /// </summary>
                    public static int[] Xi = new int[] { 3, 4, 5 };
                    /// <summary>
                    /// Coordinate system used in processing solutions 
                    /// </summary>
                    public const int CoorOut = 6;
                    /// <summary>
                    /// Grid permanent single point constraint
                    /// </summary>
                    public const int PSPC = 7;
                }

                /// <summary>
                /// Returns the Cartesian location of the grid point
                /// </summary>
                /// <param name="dataEntry"> Data entry string 
                /// array </param>
                /// <returns> An array representing the grid point 
                /// location </returns>
                public static double[] Location(string[] dataEntry)
                {
                    double x = parseAsDouble(dataEntry[DataField.Xi[0]]);
                    double y = parseAsDouble(dataEntry[DataField.Xi[1]]);
                    double z = parseAsDouble(dataEntry[DataField.Xi[2]]);
                    return new double[] { x, y, z };
                }

                /// <summary>
                /// Parses a string as a double including non-standard 
                /// exponential format
                /// </summary>
                /// <param name="str"> String to parse </param>
                /// <returns> Parsed number </returns>
                public static double parseAsDouble(string str)
                {
                    str = str.Trim().Replace('D', 'E').Replace('d', 'E');
                    double val;
                    if (double.TryParse(str, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out val))
                        return val;

                    // NASTRAN also omits E in signed exponents, including
                    // positive exponents. Do not mistake a leading sign for one.
                    int ind = Math.Max(str.LastIndexOf('-'), str.LastIndexOf('+'));
                    if (ind > 0)
                        str = str.Insert(ind, "E");
                    return double.Parse(str, NumberStyles.Float,
                        CultureInfo.InvariantCulture);
                }
            }

            /// <summary>
            /// CTETRA data entry field description.  
            /// CTETRA is a isoparametric tetrahedron.
            /// </summary>
            public class CTETRA
            {                
                /// <summary>
                /// Provides the field positions for each data entry
                /// </summary>
                public class DataField : CommonField
                {
                    /// <summary>
                    /// Grid point identification number
                    /// </summary>
                    public const int ID = 1;
                    /// <summary>
                    /// PSOLID property identification number. 
                    /// i.e. the part number
                    /// </summary>
                    public const int PSOLID = 2;
                    /// <summary>
                    /// Grid indices for the CTETRA patch, 
                    /// after continuation field removal.
                    /// Corresponds to fields 3,4,5,6,7,8,11,12,13,14;
                    /// </summary>
                    public static int[] GIDi = new 
                        int[]{3,4,5,6,7,8,9,10,11,12};
                }

                /// <summary>
                /// Converts a string containing a NAS patch to 
                /// an integer array containing the patch data.
                /// </summary>
                /// <param name="dataEntry"> NAD formatted data </param>
                /// <returns> Integers representing the patch </returns>
                public static int[] Patch(string[] dataEntry)
                {
                    int len = DataField.GIDi.Length; // Data length
                    int[] patch = new int[len]; // patch description
                    // Coverts and records each patch node
                    for (int idx = 0; idx < len; idx++)
                    {
                        patch[idx] = int.Parse(
                            dataEntry[DataField.GIDi[idx]]);
                    }
                    return patch;
                }
            }
        }
    }
}

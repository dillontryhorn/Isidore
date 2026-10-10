using System;
using System.Linq;
using System.Reflection;

namespace Isidore.Matlab
{
    /// <summary>
    /// Provides methods for processing data in Matlab (And other 
    /// applications)
    /// </summary>
    public class Net
    {
        /// <summary>
        /// Extracts a subfield value from a class or structure. Any 
        /// value will be returned as a 1D array, even for a single element. 
        /// Extracted values are limited to 1D arrays or less. An empty member
        /// path extracts the input value itself.
        /// </summary>
        /// <typeparam name="T"> Input data type </typeparam>
        /// <typeparam name="Tout"> Output data array </typeparam>
        /// <param name="ival"> Variable </param>
        /// <param name="fieldnames"> Subfield name string to 
        /// extract </param>
        /// <returns> Extracted value as a 1D array </returns>
        public static Tout[] GetValue<T, Tout>(T ival, string[] fieldnames)
        {

            if (fieldnames == null)
                throw new ArgumentNullException("fieldnames");
            object value = ival;
            for (int idx = 0; idx < fieldnames.Length; idx++)
            {
                if (value == null)
                    return new Tout[0];
                // The default string path is empty and requires no traversal.
                if (fieldnames.Length == 1 && fieldnames[idx] == "")
                    break;
                Type type = value.GetType();
                FieldInfo finfo = type.GetField(fieldnames[idx]);
                PropertyInfo pinfo = type.GetProperty(fieldnames[idx]);
                if (finfo == null && pinfo == null)
                    return new Tout[0];
                if (finfo != null)
                    value = finfo.GetValue(value);
                else
                    value = pinfo.GetValue(value);
            }

            // Handles arrays and variables separately to avoid errord
            if (value == null)
                return new Tout[0];
            var vType = value.GetType();
            if (vType.IsArray)
                return (Tout[])value;
            return new Tout[] { (Tout)value };
        }

        /// <summary>
        /// Extracts subfield values from a 1D array. Any value will be returned
        /// as a 1D array, even for a single element, so the returned array is 2D. 
        /// Extracted values are limited to 1D arrays or less.
        /// </summary>
        /// <typeparam name="T"> Input data type </typeparam>
        /// <typeparam name="Tout"> Output data array </typeparam>
        /// <param name="arr"> Variable </param>
        /// <param name="fieldName"> Subfield name string to 
        /// extract </param>
        /// <returns> Extracted value as a 2D array </returns>
        public static Tout[,] GetValue<T,Tout>(T[] arr, string fieldName = "")
        {

            // Splits fieldname string into an array of field strings
            string[] fieldnames = fieldName.Split('.');

            // Initialize a jagged array to handle unknown components
            var alen0 = arr.LongLength;
            Tout[][] jagArr = new Tout[alen0][];

            // Cycles through each element in the array & extracts field
            for (int idx0 = 0; idx0 < alen0; idx0++)
            {
                var ival = arr[idx0]; // This instance
                jagArr[idx0] = GetValue<T, Tout>(ival, fieldnames);
            }

            // Sets output array
            var vlen = jagArr.Length == 0 ? 0 : jagArr.Max(x => x.Length);
            var valArr = new Tout[alen0, vlen];

            // Populates the array
            for (int idx0 = 0; idx0 < alen0; idx0++)
                for (int idx1 = 0; idx1 < jagArr[idx0].Length; idx1++)
                    valArr[idx0, idx1] = jagArr[idx0][idx1];

            // Returns array
            return valArr;
        }

        /// <summary>
        /// Extracts subfield values from a 2D array. Any value will be returned
        /// as a 1D array, even for a single element, so the returned array is 3D. 
        /// Extracted values are limited to 1D arrays or less.
        /// </summary>
        /// <typeparam name="T"> Input data type </typeparam>
        /// <typeparam name="Tout"> Output data array </typeparam>
        /// <param name="arr"> Variable </param>
        /// <param name="fieldName"> Subfield name string to 
        /// extract </param>
        /// <returns> Extracted value as a 3D array </returns>
        public static Tout[,,] GetValue<T, Tout>(T[,] arr, string fieldName = "")
        {

            // Splits fieldname string into an array of field strings
            string[] fieldnames = fieldName.Split('.');

            // Initialize a jagged array to handle unknown components
            var alen0 = arr.GetLongLength(0);
            var alen1 = arr.GetLongLength(1);
            Tout[,][] jagArr = new Tout[alen0, alen1][];

            // Cycles through each element in the array & extracts field
            long vlen = 0; // Vector length
            for (int idx0 = 0; idx0 < alen0; idx0++)
                for (int idx1 = 0; idx1 < alen1; idx1++)
                {
                    // Copies extracted data
                    var ival = arr[idx0, idx1]; // This instance
                    jagArr[idx0, idx1] = GetValue<T, Tout>(ival, fieldnames);

                    // Checks vector length
                    var ilen = jagArr[idx0, idx1].LongLength;
                    vlen = (ilen > vlen) ? ilen : vlen;
                }

            // Sets output array
            var valArr = new Tout[alen0, alen1, vlen];

            // Populates the array
            for (int idx0 = 0; idx0 < alen0; idx0++)
                for (int idx1 = 0; idx1 < alen1; idx1++)
                    for (int idx2 = 0; idx2 < jagArr[idx0, idx1].Length; 
                        idx2++)
                        valArr[idx0, idx1, idx2] = jagArr[idx0, idx1][idx2];

            // Returns array
            return valArr;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Security.Policy;

namespace Isidore.Maths
{
    /// <summary>
    /// Retrieves the member value(s) from a field(s) for an object.
    /// </summary>
    public class Retrieve<T>
    {
        /// <summary>
        /// Retrieves the member value from a nested field for an object
        /// (e.g. {"Point" "Comp"}). If absent, returns the default 
        /// for the member type.
        /// </summary>
        /// <typeparam name="Tout"> Retrieved member's type </typeparam>
        /// <param name="inst"> Object instance </param>
        /// <param name="fieldNames"> Array of nested field names of the 
        /// retrieved member </param>
        /// <returns> Field member's value</returns>
        public static Tout Value<Tout>(T inst, string[] fieldNames)
        {
            {
                // If there is no instance returns the default for Tout
                if (inst == null)
                    return default;

                // Retrieved value
                object value = inst;

                // Descends through the member levels
                for (int idx = 0; idx < fieldNames.Length; idx++)
                {
                    if (value == null)
                        return default;
                    // Retrieves type, field, and property info
                    Type type = value.GetType();
                    FieldInfo finfo = type.GetField(fieldNames[idx]);
                    PropertyInfo pinfo = type.GetProperty(fieldNames[idx]);
                    if (finfo == null && pinfo == null)
                        return default;
                    if (finfo != null)
                        value = finfo.GetValue(value);
                    else
                        value = pinfo.GetValue(value);
                }

                return value == null ? default : (Tout)value;
            }
        }

        /// <summary>
        /// Retrieves the member value from a nested field for an object.
        /// If absent, returns the default for the member type.
        /// </summary>
        /// <typeparam name="Tout"> Retrieved member's type </typeparam>
        /// <param name="inst"> Object instance </param>
        /// <param name="fieldName">  Field names of the 
        /// retrieved member</param>
        /// <returns> Field member's value </returns>
        public static Tout Value<Tout>(T inst, string fieldName)
        {
            // Splits fieldname string into an array of field strings
            string[] fieldNames = fieldName.Split('.');

            return Value<Tout>(inst, fieldNames);
        }

        /// <summary>
        /// Retrieves the member value from a nested field for an array of 
        /// objects.  If absent, returns the default for the member type.
        /// </summary>
        /// <typeparam name="Tout"> Retrieved member's type </typeparam>
        /// <param name="inst"> Object instance </param>
        /// <param name="fieldName"> Field names of the 
        /// retrieved member </param>
        /// <returns> An array of field member's value </returns>
        public static Tout[] Value<Tout>(T[] inst, string fieldName) 
        {
            // Splits fieldname string into an array of field strings
            string[] fieldNames = fieldName.Split('.');

            // Cycles through array
            Tout[] iValue = new Tout[inst.Length];
            for (int idx = 0; idx < inst.Length; idx++)
                iValue[idx] = Value<Tout>(inst[idx], fieldNames);

            return iValue;
        }

        /// <summary>
        /// Retrieves the member value from a nested field for an array of 
        /// objects.  If absent, returns the default for the member type.
        /// </summary>
        /// <typeparam name="Tout"> Retrieved member's type </typeparam>
        /// <param name="inst"> Object instance </param>
        /// <param name="fieldName"> Field names of the 
        /// retrieved member </param>
        /// <returns> An array of field member's value </returns>
        public static Tout[,] Value<Tout>(T[,] inst, string fieldName)
        {
            // Splits fieldname string into an array of field strings
            string[] fieldNames = fieldName.Split('.');

            // Cycles through array
            int[] size = new int[] { inst.GetLength(0), inst.GetLength(1) };
            Tout[,] iValue = new Tout[size[0], size[1]];
            for (int idx0 = 0; idx0 < size[0]; idx0++)
                for (int idx1 = 0; idx1 < size[1]; idx1++)
                    iValue[idx0, idx1] = 
                        Value<Tout>(inst[idx0, idx1], fieldNames);

            return iValue;
        }
    }
}

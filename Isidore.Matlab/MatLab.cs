using System;
using System.IO;
using Isidore.Maths;

namespace Isidore.Matlab
{
    /// <summary>
    /// Facilitates a COM with MatLab
    /// </summary>
    public class MatLab
    {
        /// <summary>
        /// Passes a 2D array to MatLab, transposes array to match MatLab's 
        /// format
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="matlab"> MatLab COM instance </param>
        /// <param name="name"> Data variable name to use in MatLab </param>
        /// <param name="Arr"> Data array to pass to MatLab </param>
        public static void Put<T>(MLApp.MLApp matlab, string name, T[,] Arr)
        {
            // Transposes array
            T[,] tArr = Isidore.Maths.Arr.Transpose(Arr);
            // Pushes it to MatLab
            matlab.PutWorkspaceData(name, "base", tArr);
        }

        /// <summary>
        /// Passes a 1D array to MatLab
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="matlab"> MatLab COM instance </param>
        /// <param name="name"> Data variable name to use in MatLab </param>
        /// <param name="Arr"> Data array to pass to MatLab </param>
        public static void Put<T>(MLApp.MLApp matlab, string name, T[] Arr)
        {
            matlab.PutWorkspaceData(name, "base", Arr);
        }

        /// <summary>
        /// Passes a single value to MatLab
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="matlab"> MatLab COM instance </param>
        /// <param name="name"> Data variable name to use in MatLab </param>
        /// <param name="val"> Variable to pass to MatLab </param>
        public static void Put<T>(MLApp.MLApp matlab, string name, T val)
        {
            matlab.PutWorkspaceData(name, "base", val);
        }

        // This is cool but needs more work, there's a problem in that
        // the array has to be reshaped using
        // arr = reshape(arr1, [ size(end:-1:1)]);

        ///// <summary>
        ///// Passes a variable to MatLab.  If the variable is an array, it 
        ///// will be passed as type double
        ///// </summary>
        ///// <typeparam name="T"> Data type </typeparam>
        ///// <param name="matlab"> MatLab COM instance </param>
        ///// <param name="name"> Data variable name to use in 
        ///// MatLab </param>
        ///// <param name="val"> Variable to pass to MatLab </param>
        //public static void Put2<T>(MLApp.MLApp matlab, string name, T val)
        //{
        //    Type vtype = val.GetType();
        //    Type etype = vtype.GetElementType();
        //    if(vtype.IsArray)
        //    {
        //        // Converts the array to an Array type
        //        Array arr = val as Array;

        //        // Gets sizing information                    
        //        int rank = arr.Rank;
        //        int len = arr.Length;
        //        int[] size = new int[rank];
        //        for (int idx = 0; idx < rank; idx++)
        //            size[idx] = arr.GetLength(idx);

        //        // Converts the array to a 1-D double array
        //        var arr1 = arr.Cast<double>().ToArray();
        //        Array.Reverse(arr1); // Fixes problems rebuilding array
        //        // Pushes to Matlab
        //        matlab.PutWorkspaceData("arr1", "base", arr1);

        //        // Reshapes array
        //        var str = name +"=reshape(arr1,
        //          [" + string.Join(",", size) + "]);";
        //        matlab.Execute(str);
        //        matlab.Execute("clear arr1;");
        //    }
        //    else
        //        matlab.PutWorkspaceData(name, "base", val);
        //}

        /// <summary>
        /// Appends a data array to an existing array in MatLab, 
        /// transposes array to match MatLab's format
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="matlab"> MatLab COM instance </param>
        /// <param name="name"> Data variable name to use in 
        /// MatLab </param>
        /// <param name="Arr"> Data array to append to the "name" 
        /// array </param>
        public static void Append<T>(MLApp.MLApp matlab, 
            string name, T[,] Arr)
        {
            // Transposes array
            T[,] tArr = Isidore.Maths.Arr.Transpose(Arr);
            // Pushes it to MatLab
            matlab.Execute("isThere = exist('" + name + "','var');");
            double isThere = matlab.GetVariable("isThere", "base");
            if (isThere == 1)
            {
                matlab.PutWorkspaceData(name + "Tmp", "base", tArr);
                matlab.Execute(name + "=cat(3," + name + "," + 
                    name + "Tmp);");
                matlab.Execute("clear " + name + "Tmp;");
            }
            else
            {
                matlab.PutWorkspaceData(name, "base", tArr);
            }
            matlab.Execute("clear isThere");
        }

        /// <summary>
        /// Appends a single value to an existing array in MatLab
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="matlab"> MatLab COM instance </param>
        /// <param name="name"> Data variable name to use in 
        /// MatLab </param>
        /// <param name="Val"> Data variable to append to the "name" 
        /// array </param>
        public static void Append<T>(MLApp.MLApp matlab, string name,
            T Val)
        {
            matlab.Execute("isThere = exist('" + name + "','var');");
            double isThere = matlab.GetVariable("isThere", "base");
            if (isThere == 1)
            {
                matlab.PutWorkspaceData(name + "Tmp", "base", Val);
                matlab.Execute(name + "=[" + name + "," + name + "Tmp];");
                matlab.Execute("clear " + name + "Tmp;");
            }
            else
            {
                matlab.PutWorkspaceData(name, "base", Val);
            }
            matlab.Execute("clear isThere");
        }

        /// <summary>
        /// Passes a data array into MatLab, transposes 1st and 2nd 
        /// dimensions to match MatLab's format
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="matlab"> MatLab COM instance </param>
        /// <param name="name"> Data variable name to use in 
        /// MatLab </param>
        /// <param name="Arr3D"> Data array to append to the "name" 
        /// array </param>
        public static void Put<T>(MLApp.MLApp matlab, string name, 
            T[,,] Arr3D)
        {
            Func<T, double> convert = NumericConverter<T>();
            int xLen = Arr3D.GetLength(0);
            int yLen = Arr3D.GetLength(1);
            int fLen = Arr3D.GetLength(2);
            string varStr = string.Format("{0}=zeros({1:d},{2:d},{3:d});", 
                name, yLen, xLen, fLen);
            matlab.Execute(varStr);
            // Transposes each frame
            //for (int fI = 0; fI < fLen; fI++)
            //{
            //    double[,] frame = new double[yLen, xLen];
            //    for (int xI = 0; xI < xLen; xI++)
            //        for (int yI = 0; yI < yLen; yI++)
            //            frame[yI, xI] = Convert.ToDouble(
            //              Arr3D[xI, yI, fI]);
            //    //frame[yI, xI] = convert(Arr3D[xI, yI, fI]);
            //    // Pushes it to MatLab
            //    matlab.PutWorkspaceData(name + "f", "base", frame);
            //    string eStr = name + "(:,:," + fI + "+1)=" + name + "f;";
            //    matlab.Execute(eStr);
            //}
            for (int fI = 0; fI < fLen; fI++)
            {
                double[,] frame = new double[yLen, xLen];
                for (int xI = 0; xI < xLen; xI++)
                    for (int yI = 0; yI < yLen; yI++)
                        frame[yI, xI] = convert(Arr3D[xI, yI, fI]);
                // Pushes it to MatLab
                matlab.PutWorkspaceData(name + "f", "base", frame);
                string eStr = name + "(:,:," + fI + "+1)=" + name + "f;";
                matlab.Execute(eStr);
            }
            matlab.Execute("clear " + name + "f;");
        }

        /// <summary>
        /// Passes a data array into MatLab, transposes 1st and 2nd 
        /// dimensions to match MatLab's format
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="matlab"> MatLab COM instance </param>
        /// <param name="name"> Data variable name to use in 
        /// MatLab </param>
        /// <param name="Arr4D"> Data array to append to the "name" 
        /// array </param>
        public static void Put<T>(MLApp.MLApp matlab, string name,
            T[,,,] Arr4D)
        {
            Func<T, double> convert = NumericConverter<T>();
            int len0 = Arr4D.GetLength(0);
            int len1 = Arr4D.GetLength(1);
            int len2 = Arr4D.GetLength(2);
            int len3 = Arr4D.GetLength(3);
            string varStr = string.Format(
                "{0}=zeros({1:d},{2:d},{3:d},{4:d});",
                name, len1, len0, len2, len3);
            matlab.Execute(varStr);
            for (int i3 = 0; i3 < len3; i3++)
                for (int i2 = 0; i2 < len2; i2++)
                {
                    double[,] frame = new double[len1, len0];
                    for (int i1 = 0; i1 < len1; i1++)
                        for (int i0 = 0; i0 < len0; i0++)
                            frame[i1, i0] = convert(Arr4D[i0, i1, i2, i3]);
                    // Pushes it to MatLab
                    matlab.PutWorkspaceData(name + "0", "base", frame);
                    string eStr = name + "(:,:," + i2 + "+1," + i3 + "+1)=" 
                        + name + "0;";
                    matlab.Execute(eStr);
                }
            matlab.Execute("clear " + name + "0;");
        }

        /// <summary>
        /// Retrieves a MATLAB row or column vector as a 1D array.
        /// Empty arrays return an empty vector; non-vector matrices are rejected.
        /// </summary>
        /// <param name="matlab"> MatLab COM instance </param>
        /// <param name="variable"> Name of data in MatLab </param>
        /// <param name="workspace"> Workspace secession name </param>
        /// <returns> double type array </returns>
        public static double[] Get(MLApp.MLApp matlab, string variable, 
            string workspace)
        {
            double[,] m = matlab.GetVariable(variable, workspace);
            int d0 = m.GetLength(0);
            int d1 = m.GetLength(1);
            if (m.Length == 0)
                return new double[0];
            if (d0 != 1 && d1 != 1)
                throw new ArgumentException("The MATLAB variable must be a row or column vector.", "variable");

            double[] v = null;
            switch (d0 > d1)
            {
                case true:
                    v = new double[d0];
                    for (int Idx = 0; Idx < d0; Idx++)
                        v[Idx] = m[Idx, 0];
                    break;

                case false:
                    v = new double[d1];
                    for (int Idx = 0; Idx < d1; Idx++)
                        v[Idx] = m[0, Idx];
                    break;
            }
            return v;
        }

        // Higher-rank transfers are sent as double slices. Logical data has
        // the same 0/1 representation even though bool has no numeric cast.
        private static Func<T, double> NumericConverter<T>()
        {
            if (typeof(T) == typeof(bool))
                return value => (bool)(object)value ? 1.0 : 0.0;
            return Operator<T, double>.Convert;
        }

        /// <summary>
        /// Reads a 2D array from a .mat file
        /// </summary>
        /// <param name="fileName"> .mat file name</param>
        /// <param name="variable">  Variable name</param>
        /// <returns> Array of data extracted in MatLab </returns>
        public static double[,] Read(string fileName, string variable)
        {
            string fullPath = Path.GetFullPath(fileName).Replace("'", "''");
            string variableName = variable.Replace("'", "''");
            MLApp.MLApp matlab = new MLApp.MLApp();
            try
            {
                matlab.Execute("load('" + fullPath + "','" + variableName + "');");
                return matlab.GetVariable(variable, "base");
            }
            finally
            {
                matlab.Quit();
            }
        }
    }
}

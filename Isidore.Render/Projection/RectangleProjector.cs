using System;
using System.Linq;
using Isidore.Maths;

namespace Isidore.Render
{

    /// <summary>
    /// A projector that distributes rays over a rectangular grid
    /// </summary>
    public class RectangleProjector : Projector
    {
        # region Fields & Properties

        private double[] axis0pos, axis1pos, axis0ang, axis1ang;

        /// <summary>
        /// First dimension (X-Axis) position array in local space [m]
        /// </summary>
        public double[] Pos0 { get { return axis0pos; } }
        /// <summary>
        /// Second dimension (Y-Axis) position array in local space [m]
        /// </summary>
        public double[] Pos1 { get { return axis1pos; } }
        /// <summary>
        /// First dimension (X-Axis) pointing angle array in local space [rad]
        /// </summary>
        public double[] Ang0 { get { return axis0ang; } }
        /// <summary>
        /// Second dimension (Y-Axis) pointing angle array in local space [rad]
        /// </summary>
        public double[] Ang1 { get { return axis1ang; } }

        # endregion Fields & Properties
        # region Constructors

        /// <summary>
        /// Constructor using axis vector information
        /// </summary>
        /// <param name="axis0Pos"> Pixel position in the first axis
        /// (X-Axis) </param>
        /// <param name="axis1Pos"> Pixel position in the second axis
        /// (Y-Axis) </param>
        /// <param name="axis0Ang"> Pixel angle in the first axis
        /// (X-Axis) </param>
        /// <param name="axis1Ang"> Pixel angle in the second axis
        /// (Y-Axis) </param>
        public RectangleProjector(double[] axis0Pos, double[] axis1Pos, 
            double[] axis0Ang, double[] axis1Ang):base()
        {
            generateRays(axis0Pos, axis1Pos, axis0Ang, axis1Ang);
        }

        /// <summary>
        /// Constructor using axis scalar data
        /// </summary>
        /// <param name="numPix0"> Number of pixels in the first axis </param>
        /// <param name="numPix1"> Number of pixels in the second axis </param>
        /// <param name="lenPix0"> Size (Length) of pixels in the 
        /// first axis </param>
        /// <param name="lenPix1"> Size (Length) of pixels in the 
        /// second axis </param>
        /// <param name="pixelIFOV0"> IFOV of pixels in the 
        /// first axis </param>
        /// <param name="pixelIFOV1"> IFOV of pixels in the 
        /// second axis </param>
        public RectangleProjector(int numPix0 = 192, int numPix1 = 108,
            double lenPix0 = 0.01, double lenPix1 = 0.01,
            double pixelIFOV0 = 0, double pixelIFOV1 = 0) : base()
        {
            int len0 = numPix0;
            int len1 = numPix1;
            double[] pos0 = new double[len0];
            double[] ang0 = new double[len0];
            double[] pos1 = new double[len1];
            double[] ang1 = new double[len1];

            // axis 0: positive increments
            double axis0 = -0.5 * (len0 - 1);
            for (int idx = 0; idx < len0; idx++)
            {
                pos0[idx] = axis0 * lenPix0;
                ang0[idx] = axis0++ * pixelIFOV0;
            }

            // axis 1: positive increments
            double axis1 = -0.5 * (len1 - 1);
            for (int idx = 0; idx < len1; idx++)
            {
                pos1[idx] = axis1 * lenPix1;
                ang1[idx] = axis1++ * pixelIFOV1;
            }

            generateRays(pos0, pos1, ang0, ang1);
        }

        # endregion Constructors

        # region Methods

        /// <summary>
        /// Populates the ray structure from the base class.  
        /// World rays are not set here, but are set in 
        /// Projector.AdvanceToTime() routine.
        /// </summary>
        /// <param name="axis0Pos"> Axis 0 pixel positions </param>
        /// <param name="axis1Pos"> Axis 1 pixel positions </param>
        /// <param name="axis0Ang"> Axis 0 pixel angle </param>
        /// <param name="axis1Ang"> Axis 1 pixel angle </param>
        private void generateRays(double[] axis0Pos, double[] axis1Pos, double[] axis0Ang,
            double[] axis1Ang)
        {
            // Length check
            int len0 = axis0Pos.Length;
            int len1 = axis1Pos.Length;
            int alen0 = axis0Ang.Length;
            int alen1 = axis1Ang.Length;
            if(len0!=alen0)
                throw new Exception("The pixel position and angle vectors for axis 0 are not the same length.");
            if (len1 != alen1)
                throw new Exception("The pixel position and angle vectors for axis 1 are not the same length.");

            axis0pos = (double[])axis0Pos.Clone();
            axis1pos = (double[])axis1Pos.Clone();
            axis0ang = (double[])axis0Ang.Clone();
            axis1ang = (double[])axis1Ang.Clone();

            // Fills local rays
            raysLocal = new RenderRay[len0 * len1];
            int cnt = 0;
            for (int idx0 = 0; idx0 < len0; idx0++)
            {
                double xComp = Math.Tan(axis0Ang[idx0]);
                for (int idx1 = 0; idx1 < len1; idx1++)
                {
                    // Directional vector
                    double yComp = Math.Tan(axis1Ang[idx1]);
                    Vector locDir = new Vector(new double[] { xComp, yComp, 1.0 });
                    locDir.Normalize();

                    // Position point
                    Point locPt = new Point(new double[] { axis0pos[idx0], axis1pos[idx1], 0.0 });

                    // Local Ray
                    raysLocal[cnt++] = new RenderRay(locPt, locDir);
                }
            }
        }

        /// <summary>
        /// Retrieves the value from the field named in the intersect
        /// data given in the string input for the indexed ray in each
        /// elements ray tree
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="fieldName"> Name of the field to return the 
        /// value of </param>
        /// <param name="index"> Index of the property in the ray tree</param>
        /// <returns> An array of field values corresponding to each 
        /// projector ray </returns>
        new public T[,] GetIntersectValue<T>(string fieldName, int index)
        {
            T[,] iValue = new T[Pos0.Length, Pos1.Length];
            for (int idx0 = 0; idx0 < Pos0.Length; idx0++)
                for (int idx1 = 0; idx1 < Pos1.Length; idx1++)
                {
                    RayTree rayT = Ray(idx0, idx1);
                    if (index < rayT.Rays.Count)
                        iValue[idx0, idx1] = rayT.Rays[index].
                            IntersectData.GetFieldValue<T>(fieldName);
                }
            return iValue;
        }

        /// <summary>
        /// Retrieves the value from the field named in the intersect
        /// data given in the string input for the last ray in each
        /// elements ray tree
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="fieldName"> Name of the field to return the 
        /// value of </param>
        /// <returns> An array of field values corresponding to each 
        /// projector ray </returns>
        new public T[,] GetIntersectValue<T>(string fieldName)
        {
            T[,] iValue = new T[Pos0.Length, Pos1.Length];
            for (int idx0 = 0; idx0 < Pos0.Length; idx0++)
                for (int idx1 = 0; idx1 < Pos1.Length; idx1++)
                {
                    RayTree rayT = Ray(idx0, idx1);
                    iValue[idx0, idx1] = rayT.Rays.Last().
                        IntersectData.GetFieldValue<T>(fieldName);
                }
            return iValue;
        }

        /// <summary>
        /// Retrieves the value from the field named in the intersect
        /// data given in the string input for all rays in each
        /// elements ray tree
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="fieldName"> Name of the field to return the 
        /// value of </param>
        /// <returns> An array of field values corresponding to each 
        /// projector ray </returns>
        new public T[,][] GetIntersectValueTree<T>(string fieldName)
        {
            T[,][] iValue = new T[Pos0.Length, Pos1.Length][];
            for (int idx0 = 0; idx0 < Pos0.Length; idx0++)
                for (int idx1 = 0; idx1 < Pos1.Length; idx1++)
                {
                    RayTree rayT = Ray(idx0, idx1);
                    iValue[idx0, idx1] = new T[rayT.rays.Count];
                    for (int ridx = 0; ridx < rayT.rays.Count; ridx++)
                        iValue[idx0, idx1][ridx] = rayT.rays[ridx].
                        IntersectData.GetFieldValue<T>(fieldName);
                }
            
            return iValue;
        }

        /// <summary>
        /// Retrieves the property from the properties field in the intersect
        /// data for the last ray in each elements ray tree.  The type 
        /// determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <param name="index"> Index of the property in the ray tree</param>
        /// <returns> An array of property type data corresponding to each 
        /// projector ray </returns>
        new public Property[,] GetProperty<T>(int index)
        {
            Property[,] iP = new Property[Pos0.Length, Pos1.Length];
            for (int idx0 = 0; idx0 < Pos0.Length; idx0++)
                for (int idx1 = 0; idx1 < Pos1.Length; idx1++)
                {
                    RayTree rayT = Ray(idx0, idx1);
                    iP[idx0, idx1] = rayT.Rays[index].
                    IntersectData.GetProperty<T>();
                }
            return iP;
        }

        /// <summary>
        /// Retrieves the property from the properties field in the intersect
        /// data for the last ray in each elements ray tree.  The type 
        /// determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <returns> An array of property type data corresponding to each 
        /// projector ray </returns>
        new public Property[,] GetProperty<T>()
        {
            Property[,] iP = new Property[Pos0.Length, Pos1.Length];
            for (int idx0 = 0; idx0 < Pos0.Length; idx0++)
                for (int idx1 = 0; idx1 < Pos1.Length; idx1++)
                {
                    RayTree rayT = Ray(idx0, idx1);
                    iP[idx0,idx1] = rayT.Rays.Last().
                    IntersectData.GetProperty<T>();
                }
            return iP;
        }

        /// <summary>
        /// Retrieves the property from the properties field in the intersect
        /// data for all rays in each elements ray tree.  The type 
        /// determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <returns> An array of property type data corresponding to each 
        /// projector ray </returns>
        new public Property[,][] GetPropertyTree<T>()
        {
            Property[,][] iP = new Property[Pos0.Length, Pos1.Length][];

            for (int idx0 = 0; idx0 < Pos0.Length; idx0++)
                for (int idx1 = 0; idx1 < Pos1.Length; idx1++)
                {
                    RayTree rayT = Ray(idx0, idx1);
                    iP[idx0, idx1] = new Property[rayT.rays.Count];
                    for (int ridx = 0; ridx < rayT.rays.Count; ridx++)
                        iP[idx0, idx1][ridx] = rayT.rays[ridx].
                        IntersectData.GetProperty<T>();
                }

            return iP;
        }

        /// <summary>
        /// Retrieves the property field from the properties  in the 
        /// intersect data for the last ray in each elements ray tree.  
        /// The type propT determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <typeparam name="propT"> Property type to locate </typeparam>
        /// <param name="name"> Name of the property field to return </param>
        /// <param name="index"> Index of the property in the ray tree</param>
        /// <returns> An array of property field data corresponding to each 
        /// projector ray </returns>
        new public T[,] GetPropertyData<T, propT>(string name, int index)
        {
            Property[,] iP = GetProperty<propT>(index);

            T[,] iD = new T[Pos0.Length, Pos1.Length];
            for (int idx0 = 0; idx0 < Pos0.Length; idx0++)
                for (int idx1 = 0; idx1 < Pos1.Length; idx1++)
                    if (iP[idx0, idx1] != null)
                        iD[idx0, idx1] = iP[idx0, idx1].GetData<T>(name);

            return iD;
        }

        /// <summary>
        /// Retrieves the property field the properties in the 
        /// intersect data for the last ray in each elements ray tree.  
        /// The type propT determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <typeparam name="propT"> Property type to locate </typeparam>
        /// <param name="name"> Name of the property value to return </param>
        /// <returns> An array of property field data corresponding to each 
        /// projector ray </returns>
        new public T[,] GetPropertyData<T, propT>(string name)
        {
            Property[,] iP = GetProperty<propT>();

            T[,] iD = new T[Pos0.Length, Pos1.Length];
            for (int idx0 = 0; idx0 < Pos0.Length; idx0++)
                for (int idx1 = 0; idx1 < Pos1.Length; idx1++)
                    if (iP[idx0, idx1] != null)
                        iD[idx0, idx1] = iP[idx0, idx1].GetData<T>(name);

            return iD;
        }

        /// <summary>
        /// Retrieves the property field from the properties in the 
        /// intersect data for all rays in each elements ray tree.  
        /// The type propT determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <typeparam name="propT"> Property type to locate </typeparam>
        /// <param name="name"> Name of the property field to return </param>
        /// <returns> An array of property field data corresponding to each 
        /// projector ray </returns>
        new public T[,][] GetPropertyDataTree<T, propT>(string name)
        {
            Property[,][] iP = GetPropertyTree<propT>();

            T[,][] iD = new T[iP.GetLength(0), iP.GetLength(1)][];
            for (int idx0 = 0; idx0 < Pos0.Length; idx0++)
                for (int idx1 = 0; idx1 < Pos1.Length; idx1++)
                {
                    RayTree rayT = Ray(idx0, idx1);
                    iD[idx0, idx1] = new T[rayT.rays.Count];
                    for (int ridx = 0; ridx < rayT.rays.Count; ridx++)
                        if (iP[idx0, idx1][ridx] != null)
                            iD[idx0, idx1][ridx] =
                                iP[idx0, idx1][ridx].GetData<T>(name);
                }
            return iD;
        }


        /// <summary>
        /// Retrieves a ray tree in world space
        /// </summary>
        /// <param name="Idx0"> RayTree location in the first (X) axis </param>
        /// <param name="Idx1"> Ray location in the first (Y) axis </param>
        /// <returns> The ray in world space </returns>
        public RayTree Ray(int Idx0, int Idx1)
        {
            return Ray(Idx1 + Idx0 * Pos1.Length);
        }

        /// <summary>
        /// Retrieves a ray in projector/camera space
        /// </summary>
        /// <param name="Idx0"> Ray location in the first (X) axis </param>
        /// <param name="Idx1"> Ray location in the first (Y) axis </param>
        /// <returns> The ray in projector/camera space </returns>
        public RenderRay LocalRay(int Idx0, int Idx1)
        {
            return LocalRay(Idx1 + Idx0 * Pos1.Length);
        }

        /// <summary>
        /// Deep-copies the pixel axes as well as the projector's ray trees.
        /// </summary>
        protected override Item CloneImp()
        {
            RectangleProjector copy = (RectangleProjector)base.CloneImp();
            copy.axis0pos = (double[])axis0pos.Clone();
            copy.axis1pos = (double[])axis1pos.Clone();
            copy.axis0ang = (double[])axis0ang.Clone();
            copy.axis1ang = (double[])axis1ang.Clone();
            return copy;
        }

        # endregion Methods
    }
}

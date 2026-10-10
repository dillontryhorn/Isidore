using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Isidore.Maths;

namespace Isidore.Models
{
    /// <summary>
    /// The ReferencePointTurbulence class calculates the turbulence
    /// magnitude in a point in global space by interpolating between
    /// turbulence points listed by the closest reference point in the
    /// list.  If the reference point is of category 
    /// "interpolate" then a smooth step polynomial function is used
    /// to interpolate between its two reference points
    /// </summary>
    public class ReferencePointTurbulence
    {
        #region  Fields & Properties

        /// <summary>
        /// The list of turbulence points used in this scatter point
        /// turbulence
        /// </summary>
        public List<ReferencePoint> RefPts
        {
            get { return refPts; }
            set
            {
                refPts = value;
                // Rebuilds the K-D tree
                List<Point> ppts = new List<Point>();
                for (int idx = 0; idx < refPts.Count; idx++)
                    ppts.Add(refPts[idx]);
                ptKDtree = new KDTree(ppts);
            }
        }
        private List<ReferencePoint> refPts;

        /// <summary>
        /// The K-D tree for the list of turbulence points
        /// </summary>
        public KDTree PtKDtree { get { return ptKDtree; } }
        private KDTree ptKDtree;

        /// <summary>
        /// Smoothstep polynomial used for interpolation
        /// </summary>
        public SmoothStep Polynomial { get; set; }

        #endregion  Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="refPts"> List of reference points </param>
        /// <param name="polynomial"> Smooth Step polynomial (For reference 
        /// interpolation only </param>
        public ReferencePointTurbulence(List<ReferencePoint> refPts, 
            SmoothStep polynomial=null)
        {
            RefPts = refPts;
            this.Polynomial = polynomial?? new SmoothStep(2);
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Retrieves the turbulence magnitude values
        /// at the given locations
        /// </summary>
        /// <param name="pos"> Location points </param>
        /// <param name="now"> Current time </param>
        /// <returns> Turbulence values </returns>
        public double[] GetVal(List<Point> pos, double now)
        {
            double[] val = new double[pos.Count];
            for (int idx = 0; idx < pos.Count; idx++)
                val[idx] = GetVal(pos[idx], now);
            // This worked in C# but intermediately crashes in MATLAB
            //Parallel.For(0, pos.Count, idx =>
            //{
            //    val[idx] = GetVal(pos[idx], now);
            //});

            return val;
        }

        /// <summary>
        /// Retrieves the turbulence magnitude value
        /// at the given location
        /// </summary>
        /// <param name="pos"> Location point </param>
        /// <param name="now"> Current time </param>
        /// <returns> Turbulence value </returns>
        public double GetVal(Point pos, double now)
        {
            int refInd;
            double refDist;
            double[] dist;
            double val = GetVal(pos, now, out refInd, out refDist, out dist);
            return val;
        }

        /// <summary>
        /// Retrieves the turbulence magnitude value
        /// at the given location
        /// </summary>
        /// <param name="pos"> Location point </param>
        /// <param name="now"> Current time </param>
        /// <param name="refInd"> Closest reference point index </param>
        /// <param name="refDist"> Closest reference point separation </param>
        /// <param name="turbDist"> Distance to reference turbulence 
        /// points </param>
        /// <returns> Turbulence value </returns>
        public double GetVal(Point pos, double now, out int refInd,
            out double refDist, out double[] turbDist)
        {
            double val = 0;

            // Finds the closest reference point
            Tuple<int, double> nearest = ptKDtree.Nearest(pos);

            // Reference point index & distance
            refInd = nearest.Item1;
            refDist = nearest.Item2;

            // References point & category label
            ReferencePoint nearPt = refPts[refInd];
            ReferencePoint.Category ptCat = nearPt.CategoryLabel;

            // Retrieves the noise value depending on the point label
            if (ptCat == ReferencePoint.Category.Default)
            {
                val = GetVal(nearPt, pos, now, out turbDist);
            }
            else
            {
                ReferencePoint refPt0 = nearPt.ReferencePoints[0] as ReferencePoint;
                ReferencePoint refPt1 = nearPt.ReferencePoints[1] as ReferencePoint;
                double val0, val1;
                double[] turbDist0, turbDist1;
                val = GetInterpVal(refPt0, refPt1, pos, now,
                    out val0, out val1, out turbDist0, out turbDist1);
                turbDist = turbDist0.Concat(turbDist1).ToArray();
            }

            return val;
        }

        /// <summary>
        /// Calculates the turbulence value by interpolating between two 
        /// reference points.  Each value is weighted by a Smooth Step
        /// polynomial.
        /// </summary>
        /// <param name="refPt0"> First reference point </param>
        /// <param name="refPt1"> Second reference point </param>
        /// <param name="pos"> Location point </param>
        /// <param name="now"> Current time </param>
        /// <param name="val0"> First reference point turbulence 
        /// value </param>
        /// <param name="val1"> Second reference point turbulence
        /// value </param>
        /// <param name="turbDist0"> Distance to the first reference 
        /// turbulence points </param>
        /// <param name="turbDist1"> Distance to the first reference 
        /// turbulence points </param>
        /// <returns> Interpolated turbulence value </returns>
        public double GetInterpVal(ReferencePoint refPt0, ReferencePoint refPt1, 
            Point pos, double now, out double val0, out double val1, 
            out double[] turbDist0, out double[] turbDist1 )
        {
            // First reference noise value
            val0 = GetVal(refPt0, pos, now, out turbDist0);

            // Second reference noise value
            val1 = GetVal(refPt1, pos, now, out turbDist1);

            // Projects the point to the line between the 2 ref points
            Tuple<double, Point, double, Vector> d2l = Distance.Point2Line(pos, refPt0, refPt1);
            double distPos = d2l.Item3;

            // Distance between reference points
            double distLine = refPt0.Distance(refPt1);
            double loc = distPos / distLine;

            //// distance from the position to the reference points
            //var dist0 = pos.Distance(refPt0);
            //var dist1 = pos.Distance(refPt1);
            //var loc = dist0 / (dist0 + dist1);

            // Position in polynomial space
            double fac = Polynomial.Evaluate(loc);

            // Interpolated value
            double val = val0 + fac * (val1 - val0);

            return val;
        }

        /// <summary>
        /// Retrieves the turbulence magnitude value
        /// at the given location for a given reference point that
        /// determines which turbulence points to use in the noise
        /// calculations
        /// </summary>
        /// <param name="refPt"> Reference point use as the 
        /// base point </param>
        /// <param name="pos"> Location point </param>
        /// <param name="now"> Current time </param>
        /// <param name="turbDist"> Distance to reference turbulence 
        /// points </param>
        /// <returns> Turbulence value </returns>
        public double GetVal(ReferencePoint refPt, Point pos, double now, 
            out double[] turbDist)
        {
            double val = 0;

            // Finds the distance between the position and turbulence point
            // and the value of each turbulent point
            List<Point> tpts = refPt.ReferencePoints;
            turbDist = new double[tpts.Count];
            double[] idists = new double[tpts.Count];
            double idistSum = 0;
            int zeroDist = -1;
            for (int idx = 0; idx < tpts.Count; idx++)
            {
                TurbulencePointWFS tpt = tpts[idx] as TurbulencePointWFS;
                turbDist[idx] = pos.Distance(tpt);

                // Point overlap handler
                // Record index and break loop
                if (turbDist[idx] == 0)
                {
                    zeroDist = idx;
                    break;
                }

                // Inverse distance (used for influence)
                idists[idx] = 1.0 / turbDist[idx];
                idistSum += idists[idx];
            }

            // If there is a coincidence between the position and 
            // turbulence point, then it is the only contributor
            if (zeroDist >= 0)
            {
                TurbulencePointWFS thisPt = tpts[zeroDist] as TurbulencePointWFS;
                val = thisPt.GetVal(pos, now);
                return val;
            }

            // Calculates the influence of each point using reciprocal distance
            for (int idx = 0; idx < tpts.Count; idx++)
            {
                double frac = idists[idx] / idistSum;
                TurbulencePointWFS thisPt = tpts[idx] as TurbulencePointWFS;
                val += frac * thisPt.GetVal(pos, now);
            }

            return val;
        }

        /// <summary>
        /// Retrieves a reference point turbulence structure
        /// at the given location (Primarily used for debugging)
        /// </summary>
        /// <param name="pos"> Location point </param>
        /// <param name="now"> Current time </param>
        /// <returns> Turbulence value </returns>
        public RefPtTurbStruct GetStruct(Point pos, double now)
        {
            int refInd;
            double refDist;
            double[] dist;
            double val = GetVal(pos, now, out refInd, out refDist, out dist);

            RefPtTurbStruct valStruct = new RefPtTurbStruct(val, refInd, refDist, dist);
            return valStruct;
        }

        /// <summary>
        /// Retrieves the reference point turbulence structures
        /// at the given locations (Primarily used for debugging)
        /// </summary>
        /// <param name="pos"> Location points </param>
        /// <param name="now"> Current time </param>
        /// <returns> Turbulence values </returns>
        public RefPtTurbStruct[] GetStruct(List<Point> pos, double now)
        {
            RefPtTurbStruct[] val = new RefPtTurbStruct[pos.Count];
            for (int idx = 0; idx < pos.Count; idx++)
                val[idx] = GetStruct(pos[idx], now);

            return val;
        }

        #endregion Methods
    }

    /// <summary>
    /// Reference point turbulence structure.  Used primarily for debugging.
    /// </summary>
    public struct RefPtTurbStruct
    {
        /// <summary>
        /// Turbulence value
        /// </summary>
        public double Value { get; }

        /// <summary>
        /// Reference point index
        /// </summary>
        public int ReferenceIndex { get; }

        /// <summary>
        /// Distance to the reference point
        /// </summary>
        public double ReferencePointDistance { get; }

        /// <summary>
        /// Distance from the point to the turbulence points
        /// </summary>
        public double[] TurbulencePointDistance { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value"> Turbulence value </param>
        /// <param name="referenceIndex"> Reference point index </param>
        /// <param name="referencePointDistance"> Distance to the reference 
        /// point </param>
        /// <param name="turbulencePointDistance"> Distance from the point 
        /// to the turbulence points </param>
        public RefPtTurbStruct(double value, int referenceIndex,
            double referencePointDistance, double[] turbulencePointDistance)
        {
            Value = value;
            ReferenceIndex = referenceIndex;
            ReferencePointDistance = referencePointDistance;
            TurbulencePointDistance = turbulencePointDistance;
        }

    }
}

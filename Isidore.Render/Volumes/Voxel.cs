using System;
using System.Linq;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Represent a grid of voxels.  Since these are voxels, there are no
    /// spatial transformations.  Since a voxel is a child of the
    /// volume class, a single material is assigned to each voxel cell.
    /// </summary>
    public class Voxel : Volume
    {
        #region Fields & Properties

        /// <summary>
        /// The center points of each voxels
        /// </summary>
        public Point[] CenterPoint { get { return centerPoint; } }
        private Point[] centerPoint;

        /// <summary>
        /// Half-length of each voxel in each direction
        /// </summary>
        public double[] HalfLength { get { return halfLength; } }
        private double[] halfLength;

        /// <summary>
        /// N-dimension index of each voxel.  This is helpful for mapping the
        /// voxel array to 2D or 3D space.
        /// </summary>
        public int[,] Index { get { return index; } }
        private int[,] index;

        /// <summary>
        /// This voxel's overall bounding box center point
        /// </summary>
        public Point BoundingBoxCenter { get { return bbCenter; } }
        private Point bbCenter;

        /// <summary>
        /// This voxel's overall bounding box half lengths (In each dimension)
        /// </summary>
        public double[] BoundingBoxHalfLen { get { return bbHalfLen; } }
        private double[] bbHalfLen;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Voxel constructor
        /// </summary>
        /// <param name="CenterPoint"> Location of the center voxel's center
        /// point </param>
        /// <param name="GridResolution"> Number of voxels in each 
        /// axis </param>
        /// <param name="VoxelLength"> The length of each side of the 
        /// voxel </param>
        public Voxel(Point CenterPoint, int[] GridResolution,
            double[] VoxelLength)
        {
            // Checks that all inputs agree
            int rank = GridResolution.Length;
            if (rank != VoxelLength.Length)
                throw new System.ArgumentException(
                    "The rank of GridLength must equal that of VoxelLength.",
                    "Voxel");

            // This biases to positive values
            double[] halfRes = Operator.Convert<int, double>(GridResolution);
            halfRes = Operator.Add(halfRes, -1);
            halfRes = Operator.Multiply(halfRes, -0.5);

            double[] halfLen = Operator.Multiply(halfRes, VoxelLength);
            Point LowerCenter = new Point(halfLen) + CenterPoint;

            // Generates voxel center points and a reference index
            Tuple<Point[], int[,]> voxelData = SetVoxelPoints(LowerCenter, GridResolution,
                VoxelLength);
            centerPoint = voxelData.Item1;
            index = voxelData.Item2;

            // Voxel half length
            halfLength = Operator.Divide(VoxelLength, 2.0);

            // Full voxel bounding box
            Tuple<Point, double[]> boundingBox = SetBoundingBox(centerPoint, halfLength);
            bbCenter = boundingBox.Item1;
            bbHalfLen = boundingBox.Item2;
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Sets this instance's state vectors to time "now". Since this is a voxel,
        /// it doesn't perform any transformations.
        /// </summary>
        /// <param name="now"> Time to set this instance to </param>
        /// <param name="force"> Forces AdvanceToTime to run even if the time is the same </param>
        override public void AdvanceToTime(double now, bool force = false)
        {
            // Checks to see if time is different
            if (!force && now == CurrentTime)
                return;

            // Updates transform time line via virtual definition in Item class
            base.AdvanceToTime(now, force);
        }

        /// <summary>
        /// Performs an intersection check and operation with a ray.
        /// Voxels operates in global space.
        /// </summary>
        /// <param name="ray"> Ray to evaluate for intersection </param>
        /// <returns> Intersect flag (true = intersection) </returns>
        public override bool Intersect(ref RenderRay ray)
        {
            // If the shape isn't on, it should count as a miss
            if (!On)
                return false;

            // Bounding box check for intersection and the shorter travel
            // if either are false, returns
            Tuple<bool, double[]> bbInter = Intersect(ray, bbCenter, bbHalfLen);
            if (!bbInter.Item1 || bbInter.Item2[0] > ray.IntersectData.Travel)
                return false;

            // Near, far, ID for distance for a voxel
            bool hitOut = false;
            double nearOut = double.PositiveInfinity;
            int intersectIDOut = -1;
            bool hitIn = false;
            double nearIn = double.PositiveInfinity;
            double farIn = double.PositiveInfinity;
            int intersectIDIn = -1;

            // Evaluates each voxel
            for (int idx = 0; idx < centerPoint.Length; idx++)
            {
                // Checks for intersection
                Tuple<bool, double[]> interSect = Intersect(ray, centerPoint[idx], halfLength);

                // Continues if there is not intersect or if the far travel is
                // less than the minimum travel allows
                if (!interSect.Item1 || interSect.Item2[1] < ray.MinimumTravel)
                    continue;

                // Records intersection from outside the voxel
                if(interSect.Item2[0] < nearOut &&
                    interSect.Item2[0] >= ray.MinimumTravel)
                {
                    hitOut = true;
                    nearOut = interSect.Item2[0];
                    intersectIDOut = idx;
                }

                // Records intersection from within the voxel
                if(interSect.Item2[0] < ray.MinimumTravel
                    && interSect.Item2[1] < nearIn)
                {
                    hitIn = true;
                    nearIn = interSect.Item2[0];
                    farIn = interSect.Item2[1];
                    intersectIDIn = idx;
                }
            }

            // If there are no intersections, (even though there should be)
            // then returns
            if (!hitIn && !hitOut)
                return false;

            // Copies outside intersection point if there is one
            double travel = 0;
            int[] intersectID = new int[] { -1 };
            int[] inID;
            if (hitOut)
            {
                travel = nearOut;
                intersectID = new int[] { intersectIDOut };
            }
            else if(hitIn)
            {
                travel = farIn;
                intersectID = new int[] { intersectIDIn };
            }

            if (travel >= ray.IntersectData.Travel || travel > ray.MaximumTravel)
                return false;

            // Checks to see if there's volumetric data on the ray
            VolumeSpecificData vDataRay = ray.IntersectData.BodySpecificData as
                VolumeSpecificData;
            // If not, then vData will be null
            if (vDataRay == null)
                inID = new int[] { -1 };
            else
                inID = (int[])vDataRay.IntersectIndex.Clone();

            // Intersect Data
            Point intPt = ray.Propagate(travel);
            VolumeSpecificData vData = new VolumeSpecificData(
                inID, intersectID);
            IntersectData iData = new IntersectData(true, travel, intPt,
                this, vData);

            // Assigns intersect Data
            ray.IntersectData = iData;
            return true;
        }

        /// <summary>
        /// Performs an intersection test between a ray an a conventional
        /// AABB voxel definition of a center point and half lengths
        /// </summary>
        /// <param name="ray"> Ray to intersect </param>
        /// <param name="centerPoint"> Voxel center point </param>
        /// <param name="halfLength"> Voxel half length in each axis </param>
        /// <returns> Tuple containing </returns>
        public Tuple<bool, double[]> Intersect(Ray ray, Point centerPoint,
            double[] halfLength)
        {
            double[] t = new double[] { double.NegativeInfinity,
                double.PositiveInfinity };

            // For each voxel
            for (int idx = 0; idx < ray.Origin.Comp.Length; idx++)
                if (!InterAxis(centerPoint.Comp[idx], halfLength[idx],
                    ray.Origin.Comp[idx], ray.Dir.Comp[idx], ref t))
                    return new Tuple<bool, double[]>(false, t);

            return new Tuple<bool, double[]>(true, t);
        }

        /// <summary>
        /// Axis dependent intersection test.
        /// </summary>
        /// <param name="center"> Voxel center point in this dimension </param>
        /// <param name="halfLen"> Voxel half length in this dimension </param>
        /// <param name="rayOrig"> Ray origin in this dimension </param>
        /// <param name="rayDir"> Ray direction in each dimension </param>
        /// <param name="t"> Near and far travel distances </param>
        /// <returns> Indicates whether this axis is a positive intersection
        /// component </returns>
        private static bool InterAxis(double center, double halfLen,
            double rayOrig, double rayDir, ref double[] t)
        {
            // Uses formulation in rtr pg.743

            // Point separations
            double dpts = center - rayOrig;

            if (rayDir == 0.0)
                return Math.Abs(dpts) <= halfLen;

            double tNear = (dpts - halfLen) / rayDir;
            double tFar = (dpts + halfLen) / rayDir;

            if (tNear > tFar)
                Function.Swap(ref tNear, ref tFar);

            t[0] = tNear > t[0] ? tNear : t[0];
            t[1] = tFar < t[1] ? tFar : t[1];

            // Finish this code
            return (t[0] <= t[1]);
        }

        /// <summary>
        /// Determines if a point is inside any voxel.
        /// </summary>
        /// <param name="pt"> Point to evaluate </param>
        /// <returns> Returns a tuple with the evaluation (true if 
        /// inside the box) and the voxel index (-1 if outside all voxels).
        /// </returns>
        public Tuple<bool, int> Inside(Point pt)
        {
            // Addresses every voxel, checks for inclusion
            for (int idx = 0; idx < centerPoint.Length; idx++)
                if (Inside(pt, centerPoint[idx], HalfLength))
                    return new Tuple<bool, int>(true, idx);
            // If no overlap, then returns false
            return new Tuple<bool, int>(false, -1);
        }

        /// <summary>
        /// Determines if a point is inside an AABB
        /// </summary>
        /// <param name="Pt"> Point to evaluate </param>
        /// <param name="CenterPoint"> Box center point </param>
        /// <param name="HalfLength"> Box half lengths </param>
        /// <returns> Boolean indicating if the point is inside
        /// the AABB </returns>
        public static bool Inside(Point Pt, Point CenterPoint,
            double[] HalfLength)
        {
            int cLen = CenterPoint.Comp.Length;
            // For each dimension, rejects if outside bounds
            for (int idx = 0; idx < cLen; idx++)
                if (Pt.Comp[idx] > CenterPoint.Comp[idx] +
                    HalfLength[idx] || Pt.Comp[idx] <
                    CenterPoint.Comp[idx] - HalfLength[idx])
                    return false;
            // Otherwise, it's inside
            return true;
        }

        /// <summary>
        /// Creates a grid of voxel center points from the lowest most point,
        /// the number of points in each dimension, and the spacing between 
        /// each voxel
        /// </summary>
        /// <param name="LowerCenter"> Lowest most point in the grid </param>
        /// <param name="GridResolution"> Number of points in each 
        /// dimension </param>
        /// <param name="VoxelLength"> Length of voxel in each dimension 
        /// </param>
        /// <returns> Returns a voxel point array as a tuple: 1) center 
        /// points, 2) indices </returns>
        public static Tuple<Point[], int[,]> SetVoxelPoints(Point LowerCenter,
            int[] GridResolution, double[] VoxelLength)
        {
            // Total number of voxels
            int totCnt = GridResolution.Aggregate(1, (total, length) => total * length);
            int rank = GridResolution.Length;

            // Center point and index of each voxels
            Point[] centerPoint = new Point[totCnt];
            int[,] index = new int[totCnt, rank];

            // Finds the unique locations along each axis
            double[][] axisPos = new double[rank][];
            for (int rIdx = 0; rIdx < rank; rIdx++) // Increments through rank
            {
                axisPos[rIdx] = new double[GridResolution[rIdx]];
                for (int iIdx = 0; iIdx < GridResolution[rIdx]; iIdx++)
                    axisPos[rIdx][iIdx] = LowerCenter.Comp[rIdx] +
                        VoxelLength[rIdx] * iIdx;
            }

            // Adds point to center point list, records the N-Dim index to list
            for (int idx = 0; idx < totCnt; idx++)
            {
                // Maps array to index and position
                double[] thisPos = new double[rank]; // Position
                int runSeg = 1; // Provides a running segment size

                // For each dimension
                for (int dIdx = 0; dIdx < rank; dIdx++)
                {
                    // Defines index
                    int thisIdx = (idx / runSeg) % GridResolution[dIdx];

                    // Records index dimension
                    index[idx, dIdx] = thisIdx;

                    // Assigns position based on the index
                    thisPos[dIdx] = axisPos[dIdx][thisIdx];
                    runSeg *= GridResolution[dIdx];
                }

                // Adds point to list
                centerPoint[idx] = new Point(thisPos);
            }
            
            return new Tuple<Point[], int[,]>(centerPoint, index);
        }

        /// <summary>
        /// Creates an axis aligned bounding box (AABB) for a list of center
        /// points and a voxel half length array
        /// </summary>
        /// <param name="CenterPoints"> Voxel center point array </param>
        /// <param name="VoxelHalfLength"> Voxel half lengths </param>
        /// <returns> Returns a AABB descriptor as a tuple: 1) center point,
        /// 2) box half lengths </returns>
        public static Tuple<Point, double[]>SetBoundingBox
            (Point[] CenterPoints, double[] VoxelHalfLength)
        {

            int rank = VoxelHalfLength.Length;

            // These are the two bounding points
            Point maxPt = Point.NegativeInfinity(rank);
            Point minPt = Point.PositiveInfinity(rank);

            // Sets the bounding
            for(int idx=0; idx<CenterPoints.Length; idx++)
            {
                maxPt.UpperBound(CenterPoints[idx]);
                minPt.LowerBound(CenterPoints[idx]);
            }

            // Sets the center point
            Point centPos = 0.5 * (maxPt + minPt);

            // Sets half length of bounding box
            Point halfPos = centPos - minPt;
            double[] halfLen = Operator.Add(halfPos.Comp, VoxelHalfLength);

            return new Tuple<Point, double[]>(centPos, halfLen);
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Voxel Clone()
        {
            return (Voxel)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            Voxel newCopy = (Voxel)MemberwiseClone();

            // Deep copy
            DeepCopyOverride(ref newCopy);

            newCopy.AdvanceToTime(CurrentTime, true);

            return newCopy;
        }

        /// <summary>
        /// Implements deep copies of members that would
        /// otherwise be shallow copied.
        /// </summary>
        /// <param name="copy"> Clone copy </param>
        protected void DeepCopyOverride(ref Voxel copy)
        {
            // Base copy
            Volume baseCast = copy; // This is a shallow copy
            DeepCopyOverride(ref baseCast);

            //private Point[] centerPoint;
            copy.centerPoint = new Point[centerPoint.Length];
            for (int idx = 0; idx < centerPoint.Length; idx++)
                copy.centerPoint[idx] = centerPoint[idx].Clone();

            //private double[] halfLength;
            copy.halfLength = (double[])halfLength.Clone();

            //private int[,] index;
            copy.index = (int[,])index.Clone();

            copy.bbCenter = bbCenter.Clone();
            copy.bbHalfLen = (double[])bbHalfLen.Clone();
        }

        #endregion Methods
    }
}

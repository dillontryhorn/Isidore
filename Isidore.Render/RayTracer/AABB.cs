using System;
//using Isidore.Projectors;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Represent an axis aligned bounding box (AABB).
    /// Note that this item cannot be spatially
    /// transformed.  Once created, it cannot be changed.
    /// This item is not a Body child but is a Item child.
    /// </summary>
    public class AABB : Item
    {
        // Real-time rendering: pg 729-730, 742-744

        #region Fields & Properties

        /// <summary>
        /// The box center point
        /// </summary>
        public Point CenterPoint { get { return centerPoint; } }
        /// <summary>
        /// The box center point
        /// </summary>
        protected Point centerPoint;

        /// <summary>
        /// Half-length of the box in each direction
        /// </summary>
        public double[] HalfLength { get { return halfLength; } }
        /// <summary>
        /// Half-length of the box in each direction
        /// </summary>
        protected double[] halfLength;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor.  All inputs are references, not cloned.
        /// </summary>
        /// <param name="CenterPoint"> Box center point </param>
        /// <param name="SideLengths"> Lengths of each side </param>
        public AABB(Point CenterPoint = null, double[] SideLengths = null)
        {
            centerPoint = CenterPoint ?? Point.Zero(3);
            double[] sideLengths = SideLengths ?? new double[] { 1, 1, 1 };

            halfLength = Operator.Multiply(sideLengths, 0.5);
        }

        /// <summary>
        /// Constructor using vertices as inputs.  This is used by
        /// Mesh objects.
        /// </summary>
        /// <param name="vertices"> Vertices list </param>
        public AABB(Vertices vertices)
        {
            // Finds the min.max of the vertices list
            double[] maxVal = (double[])vertices[0].Position.Comp.Clone();
            double[] minVal = (double[])vertices[0].Position.Comp.Clone();
            for(int vidx=0; vidx<vertices.Count; vidx++)
            {
                double[] comp = vertices[vidx].Position.Comp;
                for (int cidx = 0; cidx < comp.Length; cidx++)
                {
                    maxVal[cidx] = (comp[cidx] > maxVal[cidx]) ? 
                        comp[cidx] : maxVal[cidx];
                    minVal[cidx] = (comp[cidx] < minVal[cidx]) ?
                        comp[cidx] : minVal[cidx];
                }
            }

            // Calculates half-lengths
            halfLength = Maths.Operator.Multiply(Maths.Operator.Subtract
                (maxVal, minVal), 0.5);

            // Finds center-point
            double[] cent = Maths.Operator.Add(minVal, halfLength);
            centerPoint = new Point(cent);
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Sets this instance's state vectors to time "now". Since this is a
        /// bounding box, it doesn't perform any transformations.
        /// </summary>
        /// <param name="now"> Time to set this instance to </param>
        /// <param name="force"> Forces AdvanceToTime to run even if the time 
        /// is the same </param>
        override public void AdvanceToTime(double now, bool force = false)
        {
            // Checks to see if time is different
            if (!force && now == CurrentTime)
                return;

            // Updates transform time line via virtual definition in Item class
            base.AdvanceToTime(now, force);
        }

        /// <summary>
        /// Performs an intersection test between a ray and this box.
        /// </summary>
        /// <param name="ray"> Ray to check for intersection </param>
        /// <returns> Tuple containing: 1) Intersection flag, 2) intersection
        /// points (Near, far). </returns>
        public Tuple<bool, double[]> Intersect(Ray ray)
        {
            return Intersect(ray, centerPoint, halfLength);
        }

        /// <summary>
        /// Performs an intersection test between a ray an a conventional
        /// AABB definition of a center point and half lengths
        /// </summary>
        /// <param name="ray"> Ray to intersect </param>
        /// <param name="centerPoint"> Box center point </param>
        /// <param name="halfLength"> Box half length in each axis </param>
        /// <returns> Tuple containing: 1) Intersection flag, 2) intersection
        /// point </returns>
        public static Tuple<bool, double[]> Intersect(Ray ray,
            Point centerPoint, double[] halfLength)
        {
            double[] t = new double[] { double.NegativeInfinity,
                double.PositiveInfinity };

            Point dPoint = centerPoint - ray.Origin;

            // For each voxel
            for (int idx = 0; idx < ray.Origin.Comp.Length; idx++)
            {
                // Orientation relevant data
                double dotPoint = dPoint.Comp[idx];
                double dotDir = ray.Dir.Comp[idx];

                if(!InterAxis(centerPoint.Comp[idx], halfLength[idx],
                    dotPoint, dotDir, ref t))
                    return new Tuple<bool, double[]>(false, t);
            }

            return new Tuple<bool, double[]>(true, t);
        }

        /// <summary>
        /// Axis dependent intersection test using a slab model.
        /// </summary>
        /// <param name="center"> Voxel center point in this 
        /// dimension </param>
        /// <param name="halfLen"> Voxel half length in this 
        /// dimension </param>
        /// <param name="compOrig"> Ray origin component 
        /// (sideDir*point) </param>
        /// <param name="compDir"> Ray direction component 
        /// (sideDir*direction) </param>
        /// <param name="t"> Near and far travel distances </param>
        /// <returns> Indicates whether this axis is a positive intersection
        /// component </returns>
        protected static bool InterAxis(double center, double halfLen,
            double compOrig, double compDir, ref double[] t)
        {
            // A parallel ray intersects this slab only when its origin is
            // inside it. Avoid dividing by zero at slab boundaries.
            if (compDir == 0.0)
                return Math.Abs(compOrig) <= halfLen;

            // Uses formulation in rtr pg.743 with some
            // modifications

            double tNear = (compOrig + halfLen) / compDir;
            double tFar = (compOrig - halfLen) / compDir;

            if (tNear > tFar)
                Function.Swap(ref tNear, ref tFar);

            t[0] = tNear > t[0] ? tNear : t[0];
            t[1] = tFar < t[1] ? tFar : t[1];

            // Finish this code
            return (t[0] <= t[1]);
        }

        /// <summary>
        /// Determines if the triangle represented by the points
        /// p0, p1, p2 overlaps with this box
        /// </summary>
        /// <param name="p0"> First triangle point </param>
        /// <param name="p1"> Second triangle point </param>
        /// <param name="p2"> Third triangle point </param>
        /// <returns> Overlap flag: true = overlap, 
        /// false = no overlap </returns>
        public bool TriangleOverlap(Point p0, Point p1, Point p2)
        {
            // Derived from Moller's AABB-triangle overlap 

            // Check 1: Overlapping bounding boxes
            // Vertices shift by this box center
            Point v0 = p0 - centerPoint;
            Point v1 = p1 - centerPoint;
            Point v2 = p2 - centerPoint;
            if (!BoxTriangleBoxOverlap(v0, v1, v2)) return false;

            // Check 2: Overlapping plane and AABB
            Vector edge0 = (Vector)(v1 - v0);
            Vector edge1 = (Vector)(v2 - v1);
            Vector normal = edge0.Cross(edge1);
            double d = -normal.Dot((Vector)v0);
            if (!PlaneOverlapLocal(normal, d)) return false;

            // Check 3: Axis check
            Vector edge2 = (Vector)(v0 - v2);
            double[][] v012 = new double[3][];
            v012[0] = v0.Comp; v012[1] = v1.Comp;v012[2] = v2.Comp;
            if (!TriangleAxisCheck(v012, edge0.Comp, 0)) return false;
            if (!TriangleAxisCheck(v012, edge1.Comp, 1)) return false;
            if (!TriangleAxisCheck(v012, edge2.Comp, 2)) return false;

            return true;
        }

        /// <summary>
        /// Performs the three axis checks for each edge.  This is used by
        /// TriangleOverlap
        /// </summary>
        /// <param name="v012"> Vertices components, each jagged array index
        /// is a separate vertex </param>
        /// <param name="edge"> Edge components </param>
        /// <param name="Axis"> Edge axis indicator </param>
        /// <returns> Flag: true = possible overlap, 
        /// false = no overlap </returns>
        private bool TriangleAxisCheck(double[][] v012, double[] edge, int Axis)
        {
            for (int idx = 0; idx < 3; idx++)
            {
                double a = edge[didx[idx][1]];
                double b = edge[didx[idx][0]];
                double p0 = a * sign[idx][0] *
                                v012[vidx[Axis][idx][0]][didx[idx][0]] +
                            b * sign[idx][1] *
                                v012[vidx[Axis][idx][0]][didx[idx][1]];
                double p1 = a * sign[idx][0] *
                                v012[vidx[Axis][idx][1]][didx[idx][0]] +
                            b * sign[idx][1] *
                                v012[vidx[Axis][idx][1]][didx[idx][1]];
                double max, min;
                if (p0 < p1) { min = p0; max = p1; }
                else { min = p1; max = p0; }
                double rad = Math.Abs(a) * halfLength[didx[idx][0]] +
                    Math.Abs(b) * halfLength[didx[idx][1]];

                if (min > rad || max < -rad) return false;
            }
            return true;
        }
        // Vertex index
        private int[][][] vidx = new int[][][]{
            new int[][]{ new int[]{ 0, 2 }, new int[] { 0, 2 },
                new int[] { 1, 2 } },
            new int[][]{ new int[]{ 0, 2 }, new int[] { 0, 2 },
                new int[] { 0, 1 } },
            new int[][]{ new int[]{ 0, 1 }, new int[] { 0, 1 },
                new int[] { 1, 2 } } };
        // Dimensional index
        private int[][] didx = new int[][] {
            new int[]{ 1, 2, }, new int[]{ 0, 2 }, new int[]{ 0, 1 } };
        // Dimensional sign
        private double[][] sign = new double[][] { new double[]{ 1, -1 },
            new double[]{ -1, 1 }, new double[]{ 1, -1 } };


        /// <summary>
        /// Determines if the bounding box of the triangle overlaps this box
        /// </summary>
        /// <param name="v0"> First triangle vertex position </param>
        /// <param name="v1"> Second triangle vertex position </param>
        /// <param name="v2"> Third triangle vertex position </param>
        /// <returns></returns>
        private bool BoxTriangleBoxOverlap(Point v0, Point v1, Point v2)
        {
            for(int idx=0; idx<v0.Comp.Length; idx++)
            {
                // Minimum bound
                double min = (v1.Comp[idx] < v0.Comp[idx]) ? v1.Comp[idx] : 
                    v0.Comp[idx];
                if (v2.Comp[idx] < min)
                    min = v2.Comp[idx];

                // Maximum bound
                double max = (v1.Comp[idx] > v0.Comp[idx]) ? v1.Comp[idx] :
                    v0.Comp[idx];
                if (v2.Comp[idx] > max)
                    max = v2.Comp[idx];

                // Overlap test
                if (min > halfLength[idx] || max < -halfLength[idx]) return false;
            }
            // At this point, the bounding box overlaps this AABB
            return true;
        }

        /// <summary>
        /// Determines if a plane overlaps with this box.
        /// </summary>
        /// <param name="Normal"> Plane's normal (Ax + By + Cz) </param>
        /// <param name="D"> Plane's D (Ax + By + Cz + D = 0) </param>
        /// <returns> Boolean flag: true = overlap </returns>
        public bool PlaneOverlap(Vector Normal, double D)
        {
            // The slab extrema below are relative to the box center.
            return PlaneOverlapLocal(Normal, D + Normal.Dot((Vector)centerPoint));
        }

        private bool PlaneOverlapLocal(Vector Normal, double D)
        {
            // Derived from Moller's AABB-triangle overlap code

            Vector min = Vector.Zero(Normal.Comp.Length);
            Vector max = Vector.Zero(Normal.Comp.Length);
            for(int idx=0; idx<Normal.Comp.Length; idx++)
            {
                if(Normal.Comp[idx]>0.0)
                {
                    min.Comp[idx] = -halfLength[idx];
                    max.Comp[idx] = halfLength[idx];
                }
                else
                {
                    min.Comp[idx] = halfLength[idx];
                    max.Comp[idx] = -halfLength[idx];
                }
            }

            if (Normal.Dot(min) + D > 0.0) return false;
            else if (Normal.Dot(max) + D >= 0.0) return true;
            else return false;
        }

        /// <summary>
        /// Determines if a plane overlaps with this box.
        /// </summary>
        /// <param name="plane"> Plane to test intersection </param>
        /// <returns> Boolean flag: true = overlap</returns>
        public bool PlaneOverlap(Plane plane)
        {
            return PlaneOverlap(plane.Normal, plane.D);
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public AABB Clone()
        {
            return (AABB)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            AABB newCopy = (AABB)MemberwiseClone();

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
        protected void DeepCopyOverride(ref AABB copy)
        {
            // Base copy
            Item baseCast = copy; // This is a shallow copy
            DeepCopyOverride(ref baseCast);

            // private Point centerPoint;
            copy.centerPoint = centerPoint.Clone();

            // private double[] halfLength;
            copy.halfLength = (double[])halfLength.Clone();
        }

        #endregion Methods
    }
}

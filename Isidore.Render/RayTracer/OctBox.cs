using System;
using System.Collections.Generic;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Represents an axis-aligned octree box.  Since this is an octbox,
    /// there are no spatial transforms.  
    /// </summary>
    public class OctBox : AABB
    {
        #region Fields & Properties

        /// <summary>
        /// Provides the index of each octree subspace.  Increments start
        /// at the lowest corner and shift across increasing dimensions
        /// </summary>
        public int[] Index { get { return index; } }
        /// <summary>
        /// Provides the index of each octree subspace.  Increments start
        /// at the lowest corner and shift across increasing dimensions
        /// </summary>
        protected internal int[] index;

        /// <summary>
        /// Children of this subspace
        /// </summary>
        public OctBox[] ChildBoxes { get { return childBoxes; } }
        /// <summary>
        /// Children of this subspace
        /// </summary>
        protected internal OctBox[] childBoxes;

        /// <summary>
        /// Parent octree element
        /// </summary>
        public OctBox Parent { get { return parent; } }
        /// <summary>
        /// Parent octree element
        /// </summary>
        protected internal OctBox parent;

        /// <summary>
        /// Switch indicating this instance can be used.  This value cascade
        /// down to all children.
        /// </summary>
        new public bool On
        {
            get
            {
                return base.On;
            }
            set
            {
                base.On = value;
                // Sets all children to the same value
                if (childBoxes != null)
                    foreach (OctBox child in childBoxes)
                        child.On = value;
            }
        }

        /// <summary>
        /// Indicates whether any of this instance's children are on.
        /// </summary>
        public bool AnyChildrenOn
        {
            get
            {
                if (childBoxes == null)
                    return false;
                for (int idx = 0; idx < childBoxes.Length; idx++)
                    if (childBoxes[idx].On) return true;
                return false;
            }
        }

        /// <summary>
        /// Indicates whether all of this instance's children are on.
        /// </summary>
        public bool AllChildrenOn
        {
            get
            {
                if (childBoxes == null)
                    return false;
                for (int idx = 0; idx < childBoxes.Length; idx++)
                    if (!childBoxes[idx].On) return false;
                return true;
            }
        }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="CenterPoint"> Octbox center point </param>
        /// <param name="SideLengths"> full side lengths of the octbox </param>
        /// <param name="Index"> Index to use </param>
        /// <param name="Parent"> Parent octbox element </param>
        public OctBox(Point CenterPoint = null, double[] SideLengths = null,
            int[] Index = null, OctBox Parent = null): 
            base(CenterPoint,SideLengths)
        {
            index = Index ?? new int[] { 0 };
            parent = Parent;
            childBoxes = null;
        }

        /// <summary>
        /// Constructor using vertices as inputs.  This is used by
        /// Mesh objects.
        /// </summary>
        /// <param name="vertices"> Vertices list </param>
        /// <param name="Index"> Index to use </param>
        /// <param name="Parent"> Parent octbox element </param>
        public OctBox(Vertices vertices, int[] Index = null, 
            OctBox Parent = null) :base(vertices)
        {
            index = Index ?? new int[] { 0 };
            parent = Parent;
            childBoxes = null;
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Creates a tree of subspace for this octree instance
        /// </summary>
        public virtual void Subdivide()
        {
            childBoxes = Subdivide(this);
        }

        /// <summary>
        /// Creates a tree of subspace for an octree instance
        /// </summary>
        /// <param name="octbox"> Octbox to subdivide </param>
        /// <returns> A copy of the octbox after subdivided </returns>
        public static OctBox[] Subdivide(OctBox octbox)
        {
            // Need to make this functionality a static so it can be
            // used by children

            int rank = octbox.CenterPoint.Comp.Length;
            // number of subspace voxels
            int childCnt = (int)Math.Pow(2, rank);

            // New lower corner, grid resolution pixel, & half-length
            double[] newHlen = Operator.Multiply(octbox.HalfLength, 0.5);
            int[] gridRes = Maths.Distribution.Uniform(rank, 2);
            Point lowCorn = octbox.CenterPoint - newHlen;

            // Uses the Voxel.SetVoxelPoints
            Tuple<Point[], int[,]> vData = Voxel.SetVoxelPoints(lowCorn, gridRes, 
                octbox.HalfLength);
            int place = octbox.Index.Length;
            int[] newIndex = new int[place + 1];
            octbox.Index.CopyTo(newIndex, 0);
            OctBox[] childBoxes = new OctBox[childCnt];
            for (int idx = 0; idx < childCnt; idx++)
            {
                newIndex[place] = idx;
                childBoxes[idx] = new OctBox(vData.Item1[idx], 
                    octbox.HalfLength, (int[])newIndex.Clone(), 
                    octbox);
            }

            return childBoxes;
        }

        /// <summary>
        /// Performs an intersection test between a ray and this octree 
        /// element.
        /// </summary>
        /// <param name="ray"> Ray to check for intersection </param>
        /// <returns> Intersection data </returns>
        new public OctBoxIntersect Intersect(Ray ray)
        {
            // If this element isn't open, then returns unchanged
            if (!On)
                return new OctBoxIntersect();

            // Runs this element's AABB intersection algorithm
            Tuple<bool, double[]> iSpace = base.Intersect(ray);
            return new OctBoxIntersect(iSpace.Item1, iSpace.Item2[0],
                iSpace.Item2[1], this);
        }

        /// <summary>
        /// Performs an intersection test between a ray and this octree 
        /// element's children.
        /// </summary>
        /// <param name="ray"> Ray to check for intersection </param>
        /// <returns> A list of intersection data </returns>
        public List<OctBoxIntersect> IntersectChildren( Ray ray)
        {
            List<OctBoxIntersect> list = new List<OctBoxIntersect>();

            // Accesses each child
            if (childBoxes != null)
                foreach (OctBox child in childBoxes)
                    list.Add(child.Intersect(ray));

            return list;
        }

        /// <summary>
        /// Constructs an octree for this instance and all children.
        /// </summary>
        /// <returns> List representing the octree </returns>
        public List<OctBox> MakeOctree()
        {
            // Adds this instance
            List<OctBox> octree = new List<OctBox> { this };
            // Now adds each child octree
            if(childBoxes != null)
                for(int cIdx = 0; cIdx < childBoxes.Length; cIdx++)
                {
                    List<OctBox> childTree = childBoxes[cIdx].MakeOctree();
                    childTree.ForEach(branch => octree.Add(branch));
                }
            return octree;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public OctBox Clone()
        {
            return (OctBox)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            OctBox newCopy = (OctBox)MemberwiseClone();

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
        protected void DeepCopyOverride(ref OctBox copy)
        {
            // Base copy
            AABB baseCast = copy; // This is a shallow copy
            DeepCopyOverride(ref baseCast);

            //  private int[] index;
            copy.index = index.Clone() as int[];
        }
        #endregion Methods
    }

    /// <summary>
    /// Intersection data structure for octboxes.
    /// </summary>
    public struct OctBoxIntersect
    {
        /// <summary>
        /// Hit intersection indicator
        /// </summary>
        public bool Hit;

        /// <summary>
        /// Near surface intersect travel distance
        /// </summary>
        public double NearTravel;

        /// <summary>
        /// Far surface intersect travel distance
        /// </summary>
        public double FarTravel;

        /// <summary>
        /// Octbox used in intersection
        /// </summary>
        public OctBox OctBox;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hit"> Intersect indicator </param>
        /// <param name="nearTravel"> Near surface intersect travel 
        /// distance </param>
        /// <param name="farTravel"> Far surface intersect travel 
        /// distance </param>
        /// <param name="octBox"> OctBox instance used in intersection </param>
        public OctBoxIntersect(bool hit = false, 
            double nearTravel = double.PositiveInfinity,
            double farTravel = double.PositiveInfinity,
            OctBox octBox = null)
        {
            Hit = hit;
            NearTravel = nearTravel;
            FarTravel = farTravel;
            OctBox = octBox;
        }
    }
}

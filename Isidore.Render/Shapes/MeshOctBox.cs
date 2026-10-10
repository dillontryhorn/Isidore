using System.Collections.Generic;
using System.Linq;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// MeshOctBox is a OctBox specific for Mesh Octrees.
    /// It has an extra field for recording overlapping facets.
    /// </summary>
    public class MeshOctBox : OctBox
    {
        #region Fields & Properties

        /// <summary>
        /// List of facets that overlap this OctBox
        /// </summary>
        public List<int> FacetOverlap;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="CenterPoint"> Octbox center point </param>
        /// <param name="SideLengths"> full side lengths of the octbox </param>
        /// <param name="Index"> Index to use </param>
        /// <param name="Parent"> Parent octbox element </param>
        public MeshOctBox(Point CenterPoint = null,
            double[] SideLengths = null,
            int[] Index = null, OctBox Parent = null) :
            base(CenterPoint, SideLengths, Index, Parent)
        { }

        /// <summary>
        /// Constructor using vertices as inputs.  This is used by
        /// Mesh objects.
        /// </summary>
        /// <param name="vertices"> Vertices list </param>
        /// <param name="Index"> Index to use </param>
        /// <param name="Parent"> Parent octbox element </param>
        public MeshOctBox(Vertices vertices, int[] Index = null,
            OctBox Parent = null) : base(vertices, Index, Parent)
        { }

        /// <summary>
        /// Constructor using an octbox.  The octbox is not deep copied.
        /// </summary>
        /// <param name="octbox"> Octbox to cast </param>
        public MeshOctBox(OctBox octbox): base(octbox.CenterPoint,
            Maths.Operator.Multiply(2.0, octbox.HalfLength), octbox.Index,
            octbox.Parent)
        { }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Determines if the triangle facet identified in the facetIndex 
        /// list overlaps with this box and stores it as FacetOverlap.  
        /// If facetIndex is empty, then all facets will be checked.
        /// </summary>
        /// <param name="mesh">  Triangle mesh </param>
        /// <param name="facetIndex"> Index list of the facets to 
        /// check </param>
        public void FindFacetOverlap(Mesh mesh, List<int> facetIndex = null)
        {
            FindFacetOverlap(mesh.GlobalVertices, mesh.Facets, facetIndex);
        }

        /// <summary>
        /// Determines if the triangle facet identified in the facetIndex 
        /// list overlaps with this box and stores it as FacetOverlap.  
        /// If facetIndex is empty, then all facets will be checked.
        /// </summary>
        /// <param name="vertices"> Mesh vertices list </param>
        /// <param name="facets"> facet list </param>
        /// <param name="facetIndex"> Index list of the facets to 
        /// check </param>
        public void FindFacetOverlap(Vertices vertices, List<int[]> facets,
            List<int> facetIndex = null)
        {
            // If null, every facet is used
            if (facetIndex == null)
                facetIndex = Enumerable.Range(0, facets.Count).ToList();

            // List of facet indices that overlap with this octbox
            List<int> list = new List<int>();
            // For each FacetIndex entry
            for (int idx = 0; idx < facetIndex.Count; idx++)
            {
                Point p0 = vertices[facets[facetIndex[idx]][0]].Position;
                Point p1 = vertices[facets[facetIndex[idx]][1]].Position;
                Point p2 = vertices[facets[facetIndex[idx]][2]].Position;
                if (TriangleOverlap(p0, p1, p2))
                    list.Add(facetIndex[idx]);
            }

            FacetOverlap = list;
        }

        /// <summary>
        /// Creates a tree of subspace for this octree instance
        /// </summary>
        new public void Subdivide()
        {
            OctBox[] cBox = Subdivide(this);
            childBoxes = new MeshOctBox[cBox.Length];
            for (int idx = 0; idx < cBox.Length; idx++)
                childBoxes[idx] = new MeshOctBox(cBox[idx]);
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public MeshOctBox Clone()
        {
            return (MeshOctBox)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            MeshOctBox newCopy = (MeshOctBox)MemberwiseClone();

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
        protected void DeepCopyOverride(ref MeshOctBox copy)
        {
            // Base copy
            OctBox baseCast = copy; // This is a shallow copy
            DeepCopyOverride(ref baseCast);

            // public List<int> FacetOverlap;
            List<int> newList = new List<int>();
            FacetOverlap.ForEach(n => newList.Add(n));
            copy.FacetOverlap = newList;
        }

        #endregion Methods
    }
}

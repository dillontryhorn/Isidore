using System;
using System.Collections.Generic;
using System.Linq;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Represents a triangular facet mesh.  Each fact must be a a triangle.
    /// Meshes are also used in the Polymesh class.
    /// </summary>
    public class Mesh : Shape
    {
        # region Fields & Properties

        /// <summary>
        /// Mesh vertex list in local space
        /// </summary>
        public Vertices LocalVertices { get { return localVertices; } }
        /// <summary>
        /// Mesh vertex list in local space
        /// </summary>
        protected Vertices localVertices;

        /// <summary>
        /// Mesh vertex list in global space
        /// </summary>
        public Vertices GlobalVertices { get { return globalVertices; } }
        /// <summary>
        /// Mesh vertex list in global space
        /// </summary>
        protected Vertices globalVertices;

        /// <summary>
        /// Facet list
        /// </summary>
        public List<int[]> Facets;

        /// <summary>
        /// Ray/Triangle intersect threshold
        /// </summary>
        public double intersectThreshold = 1.0e-9;

        /// <summary>
        /// Maximum number of facets per octbox in octree cell
        /// </summary>
        public int maxFacetPerOctBox;

        /// <summary>
        /// Maximum octree depth
        /// </summary>
        public int maxOctreeDepth;

        /// <summary>
        /// Ray/Triangle intersection edge1
        /// </summary>
        private Vector[] edge1;

        /// <summary>
        /// Ray/Triangle intersection edge2
        /// </summary>
        private Vector[] edge2;

        /// <summary>
        /// Ray/Triangle intersection normal (normal = edge1 x edge2)
        /// </summary>
        private Vector[] normal;

        /// <summary>
        /// Mesh octree
        /// </summary>
        protected MeshOctree octree;


        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="facets"> Facet list </param>
        /// <param name="vertices"> Vertex list </param>
        /// <param name="maxFacetPerOctBox"> Maximum facets per octree 
        /// box </param>
        /// <param name="maxOctreeDepth"> Maximum octree depth </param>
        public Mesh(List<int[]> facets, Vertices vertices,
            int maxFacetPerOctBox = 20, int maxOctreeDepth = 4)
        {
            Facets = facets;
            localVertices = vertices;
            this.maxFacetPerOctBox = maxFacetPerOctBox;
            this.maxOctreeDepth = maxOctreeDepth;
        }

        /// <summary>
        /// Constructor using lists as inputs
        /// </summary>
        /// <param name="facets"> Facet lists </param>
        /// <param name="vertexPositions"> List of vertice positions </param>
        /// <param name="vertexNormals"> List of vertice normals </param>
        /// <param name="vertexUV"> List of vertice UV coordinates </param>
        /// <param name="maxFacetPerOctBox"> Maximum facets per octree 
        /// box </param>
        /// <param name="maxOctreeDepth"> Maximum octree depth </param>
        public Mesh(List<int[]> facets = null,
            List<double[]> vertexPositions = null,
            List<double[]> vertexNormals = null,
            List<double[]> vertexUV = null, int maxFacetPerOctBox = 20,
            int maxOctreeDepth = 4)
        {
            // References facets
            Facets = facets;

            // Sets parameters
            this.maxFacetPerOctBox = maxFacetPerOctBox;
            this.maxOctreeDepth = maxOctreeDepth;

            // Makes Vertices list
            localVertices = new Vertices();
            for (int idx = 0; idx < vertexPositions.Count; idx++)
            {
                // Vertex position
                Vertex vert = new Vertex(new Point(vertexPositions[idx]));

                // Vertex normal
                if (vertexNormals != null && idx < vertexNormals.Count)
                    vert.Normal = new Normal(vertexNormals[idx]);

                // Vertex texture
                if (vertexUV != null && idx < vertexUV.Count)
                    vert.UV = vertexUV[idx];

                // Adds to list
                localVertices.Add(vert);
            }
        }

        /// <summary>
        /// Constructor using arrays for inputs (Useful for .NET interfaces)
        /// </summary>
        /// <param name="facets"> Facets X Vertice Array </param>
        /// <param name="vertexPositions"> Vertice X Spatial Dimension Array 
        /// (Usually N x 3) </param>
        /// <param name="vertexNormals"> Vertices X Spatial Dimension Array 
        /// (Usually N x 3) </param>
        /// <param name="vertexUV"> Vertices X 2 Array </param>
        /// <param name="maxFacetPerOctBox"> Maximum facets per octree 
        /// box </param>
        /// <param name="maxOctreeDepth"> Maximum octree depth </param>
        public Mesh(int[,] facets = null,
            double[,] vertexPositions = null,
            double[,] vertexNormals = null,
            double[,] vertexUV = null, int maxFacetPerOctBox = 20,
            int maxOctreeDepth = 4)
        {
            // Sets parameters
            this.maxFacetPerOctBox = maxFacetPerOctBox;
            this.maxOctreeDepth = maxOctreeDepth;

            // Converts the facet array to a facet list
            Facets = new List<int[]>();
            var flen1 = facets.GetLength(1);
            for (int fidx = 0;fidx < facets.GetLength(0); fidx++)
            {
                int[] facet = new int[flen1];
                for (int idx = 0; idx < flen1; idx++)
                    facet[idx] = facets[fidx, idx];
                Facets.Add(facet);
            }

            // Converts the vertice arrays to a vertex list
            localVertices = new Vertices();
            var vplen1 = vertexPositions.GetLength(1);
            var vnlen = vertexNormals == null ? 0 : vertexNormals.GetLength(0);
            var vnlen1 = vertexNormals == null ? 0 : vertexNormals.GetLength(1);
            var vtlen = vertexUV == null ? 0 : vertexUV.GetLength(0);
            var vtlen1 = vertexUV == null ? 0 : vertexUV.GetLength(1);
            for(int vidx = 0; vidx < vertexPositions.GetLength(0); vidx++)
            {
                // Vertex position
                double[] vPos = new double[vplen1];
                for(int idx = 0; idx < vplen1; idx++)
                    vPos[idx] = vertexPositions[vidx, idx];
                var vert = new Vertex(new Point(vPos));

                // Vertex normal
                if (vertexNormals != null && vidx < vnlen)
                {
                    double[] vNorm = new double[vnlen1];
                    for (int idx = 0; idx < vnlen1; idx++)
                        vNorm[idx] = vertexNormals[vidx, idx];
                    vert.Normal = new Normal(vNorm);
                }

                // Vertex texture
                if (vertexUV != null && vidx < vtlen)
                {
                    double[] vUV = new double[vtlen1];
                    for (int idx = 0; idx < vtlen1; idx++)
                        vUV[idx] = vertexUV[vidx, idx];
                    vert.UV = vUV;
                }

                // Adds to list
                localVertices.Add(vert);
            }
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Sets this instance's state vectors to time "now"
        /// </summary>
        /// <param name="now"> Time to set this instance to </param>
        /// <param name="force"> Forces AdvanceToTime to run even if the time
        /// is the same </param>
        public override void AdvanceToTime(double now = 0, bool force = false)
        {
            // Checks to see if time is different
            if (!force && now == CurrentTime)
                return;

            // Updates transform time line via virtual definition in Shape class
            base.AdvanceToTime(now, force);

            // Updates the vertex list
            globalVertices = localVertices.CopyTransform(
                TransformTimeLine.CurrentValue);

            // Fills intermediate values
            int len = Facets.Count;
            edge1 = new Vector[len];
            edge2 = new Vector[len];
            normal = new Vector[len];
            for (int idx = 0; idx < len; idx++)
            {
                edge1[idx] = (Vector)(globalVertices[Facets[idx][1]].Position -
                    globalVertices[Facets[idx][0]].Position);
                edge2[idx] = (Vector)(globalVertices[Facets[idx][2]].Position -
                    globalVertices[Facets[idx][0]].Position);
                normal[idx] = edge1[idx].Cross(edge2[idx]);
            }

            // Constructs Octree
            octree = new MeshOctree(globalVertices, Facets, maxFacetPerOctBox,
                maxOctreeDepth);
        }

        /// <summary>
        /// Performs an intersection check and operation with a ray.
        /// Mesh operates in global space.
        /// </summary>
        /// <param name="ray"> Ray to evaluate for intersection </param>
        /// <returns> Intersect flag (true = intersection) </returns>
        public override bool Intersect(ref RenderRay ray)
        {
            // If the shape isn't on, it should count as a miss
            if (!On)
                return false;

            // Checks the bounding box intersect
            OctBoxIntersect bbData = octree.meshoctboxes[0].Intersect(ray);
            if (!bbData.Hit)
                return false;

            // Finds which octboxes are intersected
            List<OctBoxIntersect> oData = octree.Intersect(ray);

            // Sorts the octboxes via near travel distances
            // (You could use  OrderByDescending for a z-buffer renderer)
            oData = oData.OrderBy(o => o.NearTravel).ToList();

            // Now we start evaluating individual facets
            // Facet evaluation list so we don't repeatedly evaluate a facet
            bool[] evaled = new bool[Facets.Count];

            // identifies this mesh as being intersected
            bool intersected = false;

            // Cycles through each octbox from nearest to farthest
            for (int oIdx = 0; oIdx < oData.Count; oIdx++)
            {
                if (oData[oIdx].NearTravel > ray.IntersectData.Travel)
                    break;

                MeshOctBox mBox = oData[oIdx].OctBox as MeshOctBox;

                // Cycles through each facet overlapping this box,
                // Records the closest intersection
                for (int fIdx = 0; fIdx < mBox.FacetOverlap.Count; fIdx++)
                {
                    int facetIdx = mBox.FacetOverlap[fIdx];

                    // Checks if this facet has already evaluated
                    if (!evaled[facetIdx])
                    {
                        evaled[facetIdx] = true;

                        // Returns intersect flag, travel, & barycentric UV
                        Tuple<bool, double, double[]> fData = RayTriangleIntersect(ray,
                            globalVertices[Facets[facetIdx][0]].Position,
                            edge1[facetIdx], edge2[facetIdx],
                            normal[facetIdx], intersectThreshold);

                        // If there's no intersection, or if it's too near
                        // or far, continues on with the next facet
                        if (!fData.Item1 ||
                            fData.Item2 >= ray.IntersectData.Travel ||
                            fData.Item2 > ray.MaximumTravel ||
                            fData.Item2 < ray.MinimumTravel)
                            continue;

                        // Determines if the intersect has an alpha tag
                        // Sets to true by default
                        bool alphaIn = true;
                        double[] textureUV = new double[] { double.NaN, 
                            double.NaN };
                        // Calculates the texture UV (for Alpha testing)
                        // (After checking if there are UV coordinates)
                        if ((UseAlpha || CalculateUV) &&
                            globalVertices[Facets[facetIdx][0]].UV != null &&
                            globalVertices[Facets[facetIdx][1]].UV != null &&
                            globalVertices[Facets[facetIdx][2]].UV != null)
                        {
                            textureUV = baryInterpolate(fData.Item3,
                                globalVertices[Facets[facetIdx][0]].UV,
                                globalVertices[Facets[facetIdx][1]].UV,
                                globalVertices[Facets[facetIdx][2]].UV);
                            alphaIn = getAlpha(textureUV[0], textureUV[1]);
                        }

                        // Uses alpha value to trigger recording
                        if (alphaIn)
                        {
                            // Propagation travel
                            double travel = fData.Item2;

                            // Intersect Point
                            Point iPt = ray.Propagate(travel);

                            // Normal
                            Normal norm = new Normal();
                            norm.Comp = baryInterpolate(fData.Item3,
                                globalVertices[Facets[facetIdx][0]].Normal.Comp,
                                globalVertices[Facets[facetIdx][1]].Normal.Comp,
                                globalVertices[Facets[facetIdx][2]].Normal.Comp);
                            if (norm.Mag() == 0)
                                norm = new Normal(normal[facetIdx].CopyNormalize());
                            else
                                norm.Normalize();

                            // Cosine angle of incidence
                            double cosAngInc = -ray.Dir.Dot(norm);

                            // if back face intersection option is off then returns if negative
                            if (!IntersectBackFaces && cosAngInc < 0)
                                continue;

                            // Bounds cosine angle to +/- 1
                            cosAngInc = Math.Min(Math.Abs(cosAngInc), 1);

                            // Shape specific data
                            ShapeSpecificData sData = new ShapeSpecificData(
                                norm, cosAngInc, textureUV[0], textureUV[1]);

                            // Intersect Data
                            IntersectData iData = new IntersectData(true,
                                travel, iPt, this, sData);

                            // Updates ray intersect data
                            ray.IntersectData = iData;

                            // marks this mesh as intersected
                            intersected = true;
                        }
                    }
                }

                // An overlapping facet can intersect beyond this box.
                // Continue until later boxes begin beyond the closest hit.
            }

            return intersected;
        }
        
        /// <summary>
        /// Performs an interpolation using barycentric coordinates
        /// </summary>
        /// <param name="uv"> Barycentric coordinates </param>
        /// <param name="comp0"> Components of first point </param>
        /// <param name="comp1"> Components of second point </param>
        /// <param name="comp2"> Components of third point </param>
        /// <returns> Interpolated coordinates </returns>
        private double[] baryInterpolate(double[] uv, double[] comp0, 
            double[] comp1, double[] comp2)
        {
            double w = 1.0 - uv[0] - uv[1];
            double[] iComp = new double[comp0.Length];
            for (int idx = 0; idx < comp0.Length; idx++)
                iComp[idx] = w * comp0[idx] + uv[0] * comp1[idx] + 
                    uv[1] * comp2[idx];
            return iComp;
        }

        /// <summary>
        /// Fast ray/triangle intersection which uses intermediate values.
        /// These values are vertex dependent and thus can be computed 
        /// a priori.
        /// </summary>
        /// <param name="ray"> Ray to test for intersection </param>
        /// <param name="p0"> First point of the triangle </param>
        /// <param name="edge1"> p1->p0 edge (edge2 = p1 - p0) </param>
        /// <param name="edge2"> p2->p0 edge (edge2 = p2 - p0) </param>
        /// <param name="normal"> Triangle normal, 
        /// (normal = edge1 x edge2) </param>
        /// <param name="intersectThreshold"> threshold value for testing 
        /// intersection </param>
        /// <returns> Tuple containing intersection flag, travel, 
        /// UV coordinates </returns>
        public static Tuple<bool, double, double[]> RayTriangleIntersect(
            Ray ray, Point p0, Vector edge1, Vector edge2, Vector normal,
            double intersectThreshold = 1.0e-9)
        {

            // Ray/Triangle intersection, RTR pg.750

            // Finds determinant
            Vector pvec = ray.Dir.Cross(edge2);
            double det = edge1.Dot(pvec);
            Vector tvec = (Vector)(ray.Origin - p0);
            double idet = 1.0 / det;
            
            Vector qvec = tvec.Cross(edge1);

            // Intersect
            double t, u, v;
            if (det > intersectThreshold)
            {
                u = tvec.Dot(pvec);
                if (u < 0.0 || u > det)
                    return new Tuple<bool, double, double[]>(false,
                        double.PositiveInfinity, new double[2]);

                v = ray.Dir.Dot(qvec);
                if (v < 0.0 || (u + v) > det)
                    return new Tuple<bool, double, double[]>(false,
                        double.PositiveInfinity, new double[2]);
            }
            else if (det < -intersectThreshold)
            {
                u = tvec.Dot(pvec);
                if (u > 0.0 || u < det)
                    return new Tuple<bool, double, double[]>(false,
                        double.PositiveInfinity, new double[2]);
                
                v = ray.Dir.Dot(qvec);
                if (v > 0.0 || (u + v) < det)
                    return new Tuple<bool, double, double[]>(false,
                        double.PositiveInfinity, new double[2]);
            }
            else
                return new Tuple<bool, double, double[]>(false,
                    double.PositiveInfinity, new double[2]);


            // At this point, it's an intersection
            t = idet * edge2.Dot(qvec);
            u *= idet;
            v *= idet;

            // Returns intersection tag
            return new Tuple<bool, double, double[]>(true, t,
                new double[] { u, v });
        }

        /// <summary>
        /// Fast ray/triangle intersection.
        /// </summary>
        /// <param name="ray"> Ray to test for intersection </param>
        /// <param name="p0"> First point of the triangle </param>
        /// <param name="p1"> Second point of the triangle </param>
        /// <param name="p2"> Third point of the triangle </param>
        /// <param name="intersectThreshold"> threshold value for testing 
        /// intersection </param>
        /// <returns> Tuple containing intersection flag, travel, 
        /// UV coordinates </returns>
        public static Tuple<bool, double, double[]> RayTriangleIntersect(
            Ray ray, Point p0, Point p1, Point p2, 
            double intersectThreshold = 1.0e-9)
        {

            // Ray/Triangle intersection, RTR pg.750

            // Point0 edges
            Vector edge1 = (Vector)(p1 - p0);
            Vector edge2 = (Vector)(p2 - p0);
            Vector normal = edge1.Cross(edge2);

            return RayTriangleIntersect(ray, p0, edge1, edge2, normal,
                intersectThreshold);
        }

        /// <summary>
        /// Transforms the local vertex list of this mesh.  Note that
        /// this is a permanent transform.
        /// </summary>
        /// <param name="trans"> Transformation instance </param>
        /// <param name="inverse"> Switch for using the inverse 
        /// transform </param>
        public void LocalTransform(Transform trans, bool inverse = false)
        {
            localVertices.Transform(trans, inverse);
        }

        /// <summary>
        /// Returns the three position points making the triangle facet
        /// listed at "index" on the Facet list in global space.
        /// </summary>
        /// <param name="index"> Index of facet on list </param>
        /// <param name="p0"> First facet point in global space </param>
        /// <param name="p1"> Second facet point in global space </param>
        /// <param name="p2"> Third Facet point in global space </param>
        public void GlobalTriangleIndex(int index, out Point p0, 
            out Point p1, out Point p2)
        {
            p0 = globalVertices[Facets[index][0]].Position;
            p1 = globalVertices[Facets[index][1]].Position;
            p2 = globalVertices[Facets[index][2]].Position;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Mesh Clone()
        {
            return (Mesh)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            Mesh newCopy = (Mesh)MemberwiseClone();

            // Deep copy
            DeepCopyOverride(ref newCopy);

            // If the current time has been set, then this should set
            // the interpolated members in the copy
            if (!double.IsNaN(CurrentTime))
                newCopy.AdvanceToTime(CurrentTime, true);

            return newCopy;
        }

        /// <summary>
        /// Implements deep copies of members that would
        /// otherwise be shallow copied.
        /// </summary>
        /// <param name="copy"> Clone copy </param>
        protected void DeepCopyOverride(ref Mesh copy)
        {
            // Base copy
            Shape baseCast = copy; // This is a shallow copy
            DeepCopyOverride(ref baseCast);

            //protected Vertices localVertices;
            copy.localVertices = localVertices.Clone();

            //protected Vertices globalVertices;
            if (globalVertices != null)
                copy.globalVertices = globalVertices.Clone();

            //public List<int[]> Facets;
            copy.Facets = Facets.Select(f => (int[])f.Clone()).ToList();

            //private Vector[] edge1;
            if (edge1 != null)
                copy.edge1 = edge1.Select(e => e.Clone()).ToArray();

            //private Vector[] edge2;
            if (edge2 != null)
                copy.edge2 = edge2.Select(e => e.Clone()).ToArray();

            //private Vector[] normal;
            if (normal != null)
                copy.normal = normal.Select(n => n.Clone()).ToArray();

            //private MeshOctree octree;
            if (octree != null)
                copy.octree = octree.Clone();
    }

        #endregion Methods
    }
}

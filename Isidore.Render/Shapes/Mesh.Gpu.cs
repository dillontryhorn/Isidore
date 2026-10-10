using System;
using System.Collections.Generic;
using Isidore.Maths;

namespace Isidore.Render
{
    public partial class Mesh
    {
        // Count actual octree candidates before selecting automatic execution.
        // Direct Intersect calls retain CPU traversal.
        private const long GpuMinimumWork = 1000000;
        private const long GpuColdMinimumWork = 64L * 1024 * 1024;
        private const long GpuCompilationMinimumWork = 4L * 1024 * 1024;
        private const int MaximumFacetReferences = 32 * 1024 * 1024;

        // Internal verification diagnostic for regression/benchmark evidence:
        // a completed dispatch alone does not mean its candidates were usable.
        internal int LastGpuVerifiedRays { get; private set; }
        internal long LastGpuCandidateTriangles { get; private set; }
        internal int LastGpuFacetReferences { get; private set; }

        internal bool TryGpuIntersect(Projector projector)
        {
            LastGpuVerifiedRays = 0;
            LastGpuCandidateTriangles = 0;
            LastGpuFacetReferences = 0;
            bool clonesProperties;
            if (GetType() != typeof(Mesh) || !On || projector == null ||
                projector.Rays == null || Facets == null || Facets.Count == 0 ||
                globalVertices == null || edge1 == null || edge2 == null ||
                normal == null || octree == null ||
                edge1.Length != Facets.Count || edge2.Length != Facets.Count ||
                normal.Length != Facets.Count ||
                !Finite(intersectThreshold) || intersectThreshold < 0 ||
                !PureTexture(UseAlpha ? Alpha : null) ||
                !GpuMaterialsSafe(out clonesProperties) ||
                !GpuAcceleration.MeetsWorkloadThreshold((long)projector.Rays.Length * Facets.Count,
                    GpuMinimumWork, GpuColdMinimumWork))
                return false;
            // Highly selective leaves favor the existing parallel CPU path:
            // host broadphase/verification can cost more than the triangle work.
            // Check current tree density before packing, without fallback history
            // that public geometry edits could invalidate. Force mode remains
            // available for callers who want to measure their own workload.
            if (GpuAcceleration.Mode == GpuMode.Automatic &&
                octree.meshoctboxes[0].ChildBoxes != null && !GpuDenseLeafExists())
                return false;

            // Public geometry and ray arrays are mutable. Repack their current
            // contents for every body turn, using the same cached edges as the
            // CPU rather than silently rebuilding a different triangle.
            bool commitStarted = false;
            try
            {
                List<RenderRay> active = new List<RenderRay>();
                List<int> ranges = new List<int>();
                List<int> facetReferences = new List<int>();
                int[] visited = new int[Facets.Count];
                bool sharedRootRange = octree.meshoctboxes.Count == 1 &&
                    octree.meshoctboxes[0].ChildBoxes == null;
                bool rootRangePacked = false;
                int rootRangeEnd = 0;
                long candidateWork = 0;
                int[] slots = new int[projector.Rays.Length];
                for (int index = 0; index < slots.Length; index++)
                {
                    slots[index] = -1;
                    RayTree tree = projector.Rays[index];
                    if (tree == null) return false;
                    if (!tree.Open) continue;
                    RenderRay ray = tree.NextOpenRay();
                    if (!GpuRaySafe(ray, clonesProperties)) return false;
                    // Preserve the cheap CPU octree-root rejection before
                    // sending triangle work to the device.
                    OctBoxIntersect root = octree.meshoctboxes[0].Intersect(ray);
                    if (!root.Hit) continue;
                    int start = facetReferences.Count;
                    if (sharedRootRange && root.NearTravel <= ray.IntersectData.Travel)
                    {
                        // An unsplit mesh has one identical candidate pool for
                        // every eligible ray. Share that range in the CSR pool.
                        if (!rootRangePacked)
                        {
                            foreach (int facet in octree.meshoctboxes[0].FacetOverlap)
                            {
                                if (facet < 0 || facet >= Facets.Count) return false;
                                if (visited[facet] != 0) continue;
                                visited[facet] = 1;
                                if (facetReferences.Count == MaximumFacetReferences) return false;
                                facetReferences.Add(facet);
                            }
                            rootRangeEnd = facetReferences.Count;
                            rootRangePacked = true;
                        }
                        start = 0;
                        ranges.Add(start);
                        ranges.Add(rootRangeEnd);
                        candidateWork += rootRangeEnd;
                    }
                    else
                    {
                        int stamp = active.Count + 1;
                        foreach (OctBoxIntersect box in octree.Intersect(ray))
                        {
                            if (double.IsNaN(box.NearTravel)) return false;
                            if (box.NearTravel > ray.IntersectData.Travel) continue;
                            foreach (int facet in ((MeshOctBox)box.OctBox).FacetOverlap)
                            {
                                if (facet < 0 || facet >= Facets.Count) return false;
                                if (visited[facet] == stamp) continue;
                                visited[facet] = stamp;
                                if (facetReferences.Count == MaximumFacetReferences) return false;
                                facetReferences.Add(facet);
                            }
                        }
                        ranges.Add(start);
                        ranges.Add(facetReferences.Count);
                        candidateWork += facetReferences.Count - start;
                    }
                    slots[index] = active.Count;
                    active.Add(ray);
                }
                LastGpuCandidateTriangles = candidateWork;
                LastGpuFacetReferences = facetReferences.Count;
                if (!GpuAcceleration.MeetsWorkloadThreshold(candidateWork, GpuMinimumWork, GpuColdMinimumWork))
                    return false;

                // Only serialize the larger vertex buffers after the actual
                // broadphase workload qualifies. Small selective batches leave
                // the driver cold and avoid scanning all mesh vertices.
                double[,] triangles;
                if (!GpuAcceleration.ShouldUseKernel(MeshGpuSource, "mesh_candidates", candidateWork,
                    GpuMinimumWork, GpuColdMinimumWork, GpuCompilationMinimumWork)) return false;
                if (!GpuTriangleBuffer(out triangles)) return false;

                double[,] rays = new double[active.Count, 9];
                for (int index = 0; index < active.Count; index++)
                {
                    RenderRay ray = active[index];
                    for (int axis = 0; axis < 3; axis++)
                    {
                        rays[index, axis] = ray.Origin.Comp[axis];
                        rays[index, axis + 3] = ray.Dir.Comp[axis];
                    }
                    rays[index, 6] = ray.MinimumTravel;
                    rays[index, 7] = ray.MaximumTravel;
                    rays[index, 8] = ray.IntersectData.Travel;
                }
                int[] candidates = new int[active.Count];
                double[,] result = new double[active.Count, 4];
                if (!GpuAcceleration.TryExecute(MeshGpuSource, "mesh_candidates", active.Count,
                    GpuArgument.Input(rays), GpuArgument.Input(triangles),
                    GpuArgument.Input(ranges.ToArray()), GpuArgument.Input(facetReferences.ToArray()),
                    GpuArgument.Output(candidates), GpuArgument.Output(result),
                    GpuArgument.Scalar(intersectThreshold)))
                    return false;

                // Commit in the existing body/ray order. Materials still run
                // on the CPU; a rejected raw winner must search farther facets.
                commitStarted = true;
                for (int index = 0; index < projector.Rays.Length; index++)
                {
                    RayTree tree = projector.Rays[index];
                    if (!tree.Open) continue;
                    RenderRay ray = tree.NextOpenRay();
                    int slot = slots[index];
                    bool hit;
                    if (slot < 0 || !ReferenceEquals(ray, active[slot]) ||
                        !GpuSnapshotMatches(ray, rays, slot) || candidates[slot] == -2)
                        hit = Intersect(ref ray);
                    else if (candidates[slot] < 0)
                    {
                        hit = false;
                        LastGpuVerifiedRays++;
                    }
                    else
                    {
                        int facet = candidates[slot];
                        Tuple<bool, double, double[]> exact = RayTriangleIntersect(ray,
                            globalVertices[Facets[facet][0]].Position, edge1[facet],
                            edge2[facet], normal[facet], intersectThreshold);
                        bool verified = exact.Item1 && Finite(exact.Item2) &&
                            Math.Abs(exact.Item2 - result[slot, 0]) <= result[slot, 3] &&
                            GpuFacetReachable(ray, facet, exact.Item2);
                        hit = verified && RecordFacetIntersection(ref ray, facet, exact);
                        if (hit) LastGpuVerifiedRays++;
                        if (!hit) hit = Intersect(ref ray);
                    }
                    if (hit) ApplyMaterials(ref ray);
                    ray.Status = RayStatus.Propagated;
                }
                return true;
            }
            catch (OutOfMemoryException)
            {
                if (commitStarted) throw;
                // Host packing can fail before the runtime's own bounded
                // allocation check. Nothing has been committed at that point.
                return false;
            }
        }

        private bool GpuDenseLeafExists()
        {
            bool dense = false;
            foreach (MeshOctBox box in octree.meshoctboxes)
            {
                if (box == null) return false;
                if (box.ChildBoxes == null)
                {
                    if (box.FacetOverlap == null) return false;
                    if (box.FacetOverlap.Count >= 128) dense = true;
                }
            }
            return dense;
        }

        private bool GpuTriangleBuffer(out double[,] triangles)
        {
            triangles = new double[Facets.Count, 9];
            for (int f = 0; f < Facets.Count; f++)
            {
                int[] facet = Facets[f];
                if (facet == null || facet.Length != 3 ||
                    edge1[f] == null || edge2[f] == null || normal[f] == null ||
                    edge1[f].GetType() != typeof(Vector) || edge2[f].GetType() != typeof(Vector) ||
                    normal[f].GetType() != typeof(Vector) ||
                    !GpuNumbers(edge1[f].Comp, 3) || !GpuNumbers(edge2[f].Comp, 3)) return false;
                for (int v = 0; v < 3; v++)
                {
                    if (facet[v] < 0 || facet[v] >= globalVertices.Count) return false;
                    Vertex vertex = globalVertices[facet[v]];
                    if (vertex == null || vertex.GetType() != typeof(Vertex) ||
                        vertex.Position == null || vertex.Normal == null ||
                        vertex.Position.GetType() != typeof(Point) || vertex.Normal.GetType() != typeof(Normal) ||
                        !GpuNumbers(vertex.Position.Comp, 3) || !GpuNumbers(vertex.Normal.Comp, 3) ||
                        (vertex.UV != null && !GpuNumbers(vertex.UV, 2))) return false;
                }
                for (int axis = 0; axis < 3; axis++)
                {
                    triangles[f, axis] = globalVertices[facet[0]].Position.Comp[axis];
                    triangles[f, axis + 3] = edge1[f].Comp[axis];
                    triangles[f, axis + 6] = edge2[f].Comp[axis];
                }
            }
            return true;
        }

        private bool GpuFacetReachable(RenderRay ray, int facet, double travel)
        {
            // Public facet/vertex edits can leave the CPU octree stale. The
            // candidate must belong to a leaf the original traversal can visit.
            foreach (OctBoxIntersect box in octree.Intersect(ray))
                if (box.NearTravel <= Math.Min(ray.IntersectData.Travel, travel) &&
                    ((MeshOctBox)box.OctBox).FacetOverlap.Contains(facet))
                    return true;
            return false;
        }

        private static bool GpuSnapshotMatches(RenderRay ray, double[,] snapshot, int slot)
        {
            if (ray.MinimumTravel != snapshot[slot, 6] ||
                ray.MaximumTravel != snapshot[slot, 7] ||
                ray.IntersectData.Travel != snapshot[slot, 8]) return false;
            for (int axis = 0; axis < 3; axis++)
                if (ray.Origin.Comp[axis] != snapshot[slot, axis] ||
                    ray.Dir.Comp[axis] != snapshot[slot, axis + 3]) return false;
            return true;
        }

        private static bool GpuRaySafe(RenderRay ray, bool clonesProperties)
        {
            if (ray == null || ray.GetType() != typeof(RenderRay) ||
                ray.Origin == null || ray.Dir == null || ray.IntersectData == null ||
                ray.Origin.GetType() != typeof(Point) || ray.Dir.GetType() != typeof(Vector) ||
                !GpuNumbers(ray.Origin.Comp, 3) || !GpuNumbers(ray.Dir.Comp, 3) ||
                !Finite(ray.MinimumTravel) || double.IsNaN(ray.MaximumTravel) ||
                double.IsNaN(ray.IntersectData.Travel)) return false;
            if (!clonesProperties) return true;
            if (ray.Properties == null) return false;
            foreach (Property property in ray.Properties)
                if (!GpuPropertySafe(property)) return false;
            return true;
        }

        private bool GpuMaterialsSafe(out bool clonesProperties)
        {
            clonesProperties = false;
            if (Materials == null) return true;
            foreach (MaterialStack stack in Materials)
            {
                if (stack == null) return false;
                foreach (Material material in stack)
                {
                    if (material == null) return false;
                    Type type = material.GetType();
                    // Even an off custom material may override Apply and
                    // deliberately ignore the base class's On flag.
                    if (type != typeof(PropertyValue) && type != typeof(TextureValue) &&
                        type != typeof(OPD) && type != typeof(Reflector) &&
                        type != typeof(Reflective) && type != typeof(Transparency)) return false;
                    if (!material.On) continue;
                    if (!PureTexture(material.Alpha)) return false;
                    if (type == typeof(PropertyValue))
                    {
                        if (!GpuPropertySafe(((PropertyValue)material).Property)) return false;
                    }
                    else if (type == typeof(TextureValue))
                    {
                        if (((TextureValue)material).baseTexture == null ||
                            !PureTexture(((TextureValue)material).baseTexture)) return false;
                    }
                    else if (type == typeof(OPD))
                    {
                        OPD opd = (OPD)material;
                        if (opd.Length == null || opd.Length.GetType() != typeof(Length) ||
                            !PureTexture(opd.ScaleTexture)) return false;
                    }
                    else if (type == typeof(Reflector))
                    {
                        Reflector reflector = (Reflector)material;
                        if (!GpuPropertySafe(reflector.Reflectance) ||
                            !PureTexture(reflector.Scalar)) return false;
                    }
                    else if (type == typeof(Reflective))
                    {
                        if (!GpuPropertySafe(((Reflective)material).Reflectance)) return false;
                        clonesProperties = true;
                    }
                    else if (type == typeof(Transparency))
                    {
                        if (!GpuPropertySafe(((Transparency)material).RefractiveIndex)) return false;
                        clonesProperties = true;
                    }
                    else return false;
                }
            }
            return true;
        }

        private static bool GpuPropertySafe(Property property)
        {
            if (property == null) return false;
            Type type = property.GetType();
            if (type == typeof(Scalar) || type == typeof(Length) || type == typeof(Temperature) ||
                type == typeof(Irradiance) || type == typeof(Wavelength)) return true;
            Spectrum<double, double> spectrum;
            if (type == typeof(Reflectance)) spectrum = ((Reflectance)property).Value;
            else if (type == typeof(RefractiveIndex)) spectrum = ((RefractiveIndex)property).Value;
            else if (type == typeof(SpectralIrradiance)) spectrum = ((SpectralIrradiance)property).Value;
            else if (type == typeof(PowerSpectrum)) spectrum = ((PowerSpectrum)property).Value;
            else return false;
            return spectrum == null || spectrum.GetType() == typeof(Spectrum<double, double>);
        }

        private static bool PureTexture(Texture texture)
        {
            return texture == null || texture.GetType() == typeof(MapTexture);
        }

        private static bool Finite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool GpuNumbers(double[] values, int dimensions)
        {
            if (values == null || values.Length != dimensions) return false;
            foreach (double value in values)
                if (!Finite(value) || Math.Abs(value) > 1e50 ||
                    (value != 0 && Math.Abs(value) < 1e-50)) return false;
            return true;
        }

        // Error bounds use absolute expanded products, including the original
        // origin/vertex magnitudes in a subtractive cancellation. Boundary,
        // determinant and distance ties request full CPU traversal. GPU values
        // only select a candidate; CPU recomputation produces every public value.
        private const string MeshGpuSource = @"
double mesh_dot(double3 a, double3 b) {
    double s=0.0; s+=a.x*b.x; s+=a.y*b.y; s+=a.z*b.z; return s;
}
double3 mesh_cross(double3 a, double3 b) {
    return (double3)(a.y*b.z-a.z*b.y, a.z*b.x-a.x*b.z, a.x*b.y-a.y*b.x);
}
double3 mesh_cross_bound(double3 a, double3 b) {
    return (double3)(a.y*b.z+a.z*b.y, a.z*b.x+a.x*b.z, a.x*b.y+a.y*b.x);
}
__kernel void mesh_candidates(__global const double* rays,
    __global const double* triangles, __global const int* ranges,
    __global const int* facetReferences, __global int* candidates,
    __global double* result, double threshold) {
    int r=get_global_id(0);
    double3 origin=vload3(0,rays+9*r);
    double3 direction=vload3(0,rays+9*r+3);
    double minimum=rays[9*r+6], maximum=rays[9*r+7], previous=rays[9*r+8];
    double best=INFINITY, bestError=0.0, bestU=0.0, bestV=0.0;
    int winner=-1, uncertain=0;
    const double margin=2.8421709430404007e-14; // 128 double ulps at one.
    for(int entry=ranges[2*r]; entry<ranges[2*r+1]; entry++) {
        int f=facetReferences[entry];
        double3 p=vload3(0,triangles+9*f);
        double3 e1=vload3(0,triangles+9*f+3), e2=vload3(0,triangles+9*f+6);
        double3 pv=mesh_cross(direction,e2);
        double det=mesh_dot(e1,pv), d=fabs(det);
        double de=margin*mesh_dot(fabs(e1),mesh_cross_bound(fabs(direction),fabs(e2)));
        if(d+de<=threshold) continue;
        if(d-de<=threshold) { uncertain=1; continue; }
        double3 tv=origin-p, q=mesh_cross(tv,e1), tvBound=fabs(origin)+fabs(p);
        double rawU=mesh_dot(tv,pv), rawV=mesh_dot(direction,q);
        double sign=det>0.0 ? 1.0 : -1.0;
        double u=sign*rawU, v=sign*rawV;
        double ue=margin*mesh_dot(tvBound,mesh_cross_bound(fabs(direction),fabs(e2)));
        double ve=margin*mesh_dot(fabs(direction),mesh_cross_bound(tvBound,fabs(e1)));
        if(u < -ue || u>d+ue+de || v < -ve || u+v>d+ue+ve+de) continue;
        double numerator=mesh_dot(e2,q), reciprocal=1.0/det;
        double t=reciprocal*numerator;
        double ne=margin*mesh_dot(fabs(e2),mesh_cross_bound(tvBound,fabs(e1)));
        double error=(ne+fabs(t)*de)/(d-de)+margin*fabs(t);
        if(!isfinite(t) || !isfinite(error)) { uncertain=1; continue; }
        if(t+error<minimum || t-error>maximum || t-error>=previous) continue;
        if(u<=ue || v<=ve || d-u-v<=de+ue+ve ||
            t-error<=minimum || t+error>=maximum || t+error>=previous) {
            uncertain=1; continue;
        }
        if(winner>=0 && fabs(t-best)<=error+bestError) uncertain=1;
        if(t<best) { winner=f; best=t; bestError=error;
            bestU=rawU*reciprocal; bestV=rawV*reciprocal; }
    }
    candidates[r]=uncertain ? -2 : winner;
    result[4*r]=best; result[4*r+1]=bestU; result[4*r+2]=bestV; result[4*r+3]=bestError;
}";
    }
}

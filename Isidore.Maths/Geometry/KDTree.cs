using System;
using System.Collections.Generic;

namespace Isidore.Maths
{
    /// <summary>
    /// KDTree implements a K-D range tree introduced by Bentley (1975).  
    /// This implementation is derived from Numerical Recipes an tries to 
    /// remain consistent with its code and nomenclature.
    /// </summary>
    public class KDTree
    {
        #region Fields & Properties

        /// <summary>
        /// Reference to the points forming the K-D tree
        /// </summary>
        public readonly List<Point> ptss;

        /// <summary>
        /// Box node array corresponding to the k-D tree
        /// </summary>
        public readonly BoxNode[] boxes;

        /// <summary>
        /// Sorted point index
        /// </summary>
        public readonly int[] ptindx;

        /// <summary>
        /// Reverse sorted point index (Used in locate)
        /// </summary>
        private int[] rptindx;

        /// <summary>
        /// Number of spatial dimensions
        /// </summary>
        private int DIM;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructs a K-D tree from a list of points
        /// </summary>
        /// <param name="pts"> List of points </param>
        public KDTree(List<Point> pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");
            if (pts.Count == 0)
                throw new ArgumentException("A K-D tree requires at least one point.", "pts");
            if (pts[0] == null || pts[0].Comp == null || pts[0].Comp.Length == 0)
                throw new ArgumentException("Points must have at least one coordinate.", "pts");
            DIM = pts[0].Comp.Length;
            for (int idx = 0; idx < pts.Count; idx++)
                if (pts[idx] == null || pts[idx].Comp == null || pts[idx].Comp.Length != DIM)
                    throw new ArgumentException("All points must have the same number of coordinates.", "pts");
            ptss = pts;
            int npts = ptss.Count;
            ptindx = new int[npts];
            rptindx = new int[npts];

            // Makes a stack large enough for 2^50 points
            int[] taskmom = new int[50];
            int[] taskdim = new int[50];

            // Initialize point index
            for (int k = 0; k < npts; k++)
                ptindx[k] = k;

            // Calculates and allocates for the total number of box nodes
            double M = Math.Pow(2.0, Math.Ceiling(Math.Log(npts, 2)));
            int nboxes = Math.Max(1, (int)Math.Min(M - 1, 2 * npts - 0.5 * M - 1));
            boxes = new BoxNode[nboxes];

            // Copies each point's location components into a 
            // contiguous array
            double[] coords = new double[DIM * npts];
            for (int j = 0, kk = 0; j < DIM; j++, kk += npts)
                for (int k = 0; k < npts; k++) coords[kk + k] = 
                        pts[k].Comp[j];

            // Initialize the root box and add it to the
            // subdivision task list
            Point lo = Point.NegativeInfinity(DIM);
            Point hi = Point.PositiveInfinity(DIM);
            boxes[0] = new BoxNode(lo, hi, 0, 0, 0, 0, npts - 1);
            int jbox = 0;
            // Box index
            taskmom[1] = 0;
            // Dimension index
            taskdim[1] = 0;
            // Leaves already hold up to two points. Small trees need no split.
            int nowtask = npts > 2 ? 1 : 0;
            // Main loop
            while (nowtask > 0)
            {
                int tmom = taskmom[nowtask];
                int tdim = taskdim[nowtask--];
                int ptlo = boxes[tmom].ptlo;
                int pthi = boxes[tmom].pthi;
                // Point index left of the subdivision
                int hp = ptlo;
                // Coordinate index for the current dimension
                int cp = tdim * npts;
                // Number of point contained in the subdivision
                int np = pthi - ptlo + 1;
                // Index of the last left point (boundary point)
                int kk = (np - 1) / 2;
                // Sorts index
                selecti(kk, hp, ref ptindx, np, cp, ref coords);

                // Create 2 daughters and add them to the subdivision 
                // task list
                hi = boxes[tmom].hi.Clone();
                lo = boxes[tmom].lo.Clone();
                hi.Comp[tdim] = coords[tdim * npts + ptindx[ptlo + kk]];
                lo.Comp[tdim] = coords[tdim * npts + ptindx[ptlo + kk]];
                boxes[++jbox] = new BoxNode(boxes[tmom].lo, hi, tmom, 0, 0,
                    ptlo, ptlo + kk);
                boxes[++jbox] = new BoxNode(lo, boxes[tmom].hi, tmom, 0, 0,
                    ptlo + kk + 1, pthi);
                boxes[tmom].dau1 = jbox - 1;
                boxes[tmom].dau2 = jbox;
                if (kk > 1)
                {
                    taskmom[++nowtask] = jbox - 1;
                    taskdim[nowtask] = (tdim + 1) % DIM;
                }
                if (np - kk > 3)
                {
                    taskmom[++nowtask] = jbox;
                    taskdim[nowtask] = (tdim + 1) % DIM;
                }
            }
            // Populates the reverse point index
            for (int j = 0; j < npts; j++) rptindx[ptindx[j]] = j;
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Sorts the array from largest to shortest by permuting the index.
        /// This implementation differs from Numerical Recipes in that it
        /// uses references instead of pointers
        /// </summary>
        /// <param name="k"> Boundary point (Last point on the 
        /// left) </param>
        /// <param name="indxLoc"> Index of index of the point on the left 
        /// end of the subdivision </param>
        /// <param name="indx"> Point index </param>
        /// <param name="n"> Number of points in the subdivision </param>
        /// <param name="arrLoc"> Index of the coordinate array for the 
        /// current dimension </param>
        /// <param name="arr"> Coordinate array </param>
        private void selecti(int k, int indxLoc, ref int[] indx, int n,
            int arrLoc, ref double[] arr)
        {
            int l = 0;
            int ir = n - 1;
            for (;;)
            {
                // Active partition has one or two elements
                if (ir <= l + 1)
                {
                    // For two elements
                    if (ir == l + 1 && arr[arrLoc + indx[indxLoc + ir]] <
                        arr[arrLoc + indx[indxLoc + l]])
                        SWAP(ref indx, indxLoc + l, indxLoc + ir);
                    return;
                }
                else
                {
                    // Finds the median of left, center and elements &
                    // rearranges by size
                    int mid = (l + ir) / 2;
                    SWAP(ref indx, indxLoc + mid, indxLoc + l + 1);
                    if (arr[arrLoc + indx[indxLoc + l]] >
                        arr[arrLoc + indx[indxLoc + ir]])
                        SWAP(ref indx, indxLoc + l, indxLoc + ir);
                    if (arr[arrLoc + indx[indxLoc + l + 1]] >
                        arr[arrLoc + indx[indxLoc + ir]])
                        SWAP(ref indx, indxLoc + l + 1, indxLoc + ir);
                    if (arr[arrLoc + indx[indxLoc + l]] >
                        arr[arrLoc + indx[indxLoc + l + 1]])
                        SWAP(ref indx, indxLoc + l, indxLoc + l + 1);

                    // Initializes partition pointers
                    int i = l + 1;
                    int j = ir;
                    int ia = indx[indxLoc + l + 1];
                    // Partitioning element
                    double a = arr[arrLoc + ia];
                    // The innermost loop
                    for (;;)
                    {
                        // Scans up to find arr[i] > a
                        do i++; while (arr[arrLoc + indx[indxLoc + i]] < a);
                        // Scans down to find arr[j] < a
                        do j--; while (arr[arrLoc + indx[indxLoc + j]] > a);
                        // Partitioning is complete when the points cross
                        if (j < i) break;
                        SWAP(ref indx, indxLoc + i, indxLoc + j);
                    }
                    // Inserts partitioning element index
                    indx[indxLoc + l + 1] = indx[indxLoc + j];
                    indx[indxLoc + j] = ia;
                    // Keeps the active partition that 
                    // contains the kth element
                    if (j >= k) ir = j - 1;
                    if (j <= k) l = i;
                }
            }
        }

        /// <summary>
        /// Swaps two elements in an integer array
        /// </summary>
        /// <param name="array"> Integer array </param>
        /// <param name="idx1"> 1st index to swap </param>
        /// <param name="idx2"> 2nd index to swap </param>
        private static void SWAP(ref int[] array, int idx1, int idx2)
        {
            int temporary = array[idx1];
            array[idx1] = array[idx2];
            array[idx2] = temporary;
        }

        /// <summary>
        /// Determines the distance between two points in the tree.
        /// Co-located points return infinity to avoid self-referencing
        /// </summary>
        /// <param name="jpt"> Fist point </param>
        /// <param name="kpt"> Second point </param>
        /// <returns> Separation distance </returns>
        private double disti(int jpt, int kpt)
        {
            if (jpt == kpt) return double.PositiveInfinity;
            return ptss[jpt].Distance(ptss[kpt]);
        }

        /// <summary>
        /// Determines the smallest box node the contains the point
        /// </summary>
        /// <param name="pt"> Input point </param>
        /// <returns> Index of the smallest box node </returns>
        private int locate(Point pt)
        {
            // Start with the root box node
            int nb = 0;
            int jdim = 0;
            // Descends down the tree
            while (boxes[nb].dau1 > 0)
            {
                int d1 = boxes[nb].dau1;
                if (pt.Comp[jdim] <= boxes[d1].hi.Comp[jdim]) nb = d1;
                else nb = boxes[nb].dau2;
                // Increments the dimension cyclically
                jdim = ++jdim % DIM;
            }
            return nb;
        }

        /// <summary>
        /// Returns the box node index containing the given point
        /// </summary>
        /// <param name="jpt"> Input point </param>
        /// <returns> Index of the containing box node </returns>
        private int locate(int jpt)
        {
            // The reverse index tells where the point is in the point index
            int jh = rptindx[jpt];
            int nb = 0;
            while (boxes[nb].dau1 > 0)
            {
                int d1 = boxes[nb].dau1;
                if (jh <= boxes[d1].pthi) nb = d1;
                else nb = boxes[nb].dau2;
            }
            return nb;
        }

        /// <summary>
        /// For a given point, returns the nearest point in the K-D tree.
        /// This implementation differs from Numerical Recipes in that
        /// this one returns the separation distance as well.
        /// </summary>
        /// <param name="pt"> Arbitrary point </param>
        /// <returns> Tuple containing: 1) index of the nearest point,
        /// 2) the separation distances between points </returns>
        public Tuple<int, double> Nearest(Point pt)
        {
            int nrst = 0;
            // Stack for boxes waiting to be opened
            int[] task = new int[50];
            double dnrst = double.PositiveInfinity;

            // Stage 1: Finds the nearest K-D tree point in the same
            // box node as pt
            // Finds which box pt is in
            int k = locate(pt);
            // Finds the nearest point
            for (int i = boxes[k].ptlo; i <= boxes[k].pthi; i++)
            {
                double d = ptss[ptindx[i]].Distance(pt);
                if (d < dnrst)
                {
                    nrst = ptindx[i];
                    dnrst = d;
                }
            }

            // Stage 2: Transverses the tree opening only possible boxes
            task[1] = 0;
            int ntask = 1;
            while (ntask > 0)
            {
                k = task[ntask--];
                // Distance to the closest box
                if (boxes[k].Distance(pt) < dnrst)
                    if (boxes[k].dau1 > 0)
                    {
                        task[++ntask] = boxes[k].dau1;
                        task[++ntask] = boxes[k].dau2;
                    }
                    // Check the couple of plots in the box
                    else
                    {
                        for (int i = boxes[k].ptlo; i <= boxes[k].pthi; i++)
                        {
                            double d = ptss[ptindx[i]].Distance(pt);
                            if (d < dnrst)
                            {
                                nrst = ptindx[i];
                                dnrst = d;
                            }
                        }
                    }
            }
            return new Tuple<int, double>(nrst, dnrst);
        }

        /// <summary>
        /// Returns a specified number of point within a given range of an 
        /// arbitrary point.  This implementation differs from Numerical 
        /// Recipes in that this one returns the separation distances.
        /// It also organizes the points in ascending distances.
        /// Therefore, limiting the number of points to effectively the 
        /// N nearest points.
        /// </summary>
        /// <param name="pt"> Input point </param>
        /// <param name="r"> Search radius </param>
        /// <param name="nmax"> Number of point to return. -1 or unspecified 
        /// will return all points </param>
        /// <returns> A tuple containing: 1) index of all points within,
        ///  2) distance between tree points and the given point </returns>
        public Tuple<int[], double[]> LocateNear(Point pt, double r,
            int nmax = -1)
        {
            if (nmax < 0) nmax = ptss.Count;
            List<int> list = new List<int>();
            List<double> dists = new List<double>();
            List<Tuple<int, double>> dList = new List<Tuple<int, double>>();
            int k, i, nb, nbold, ntask, jdim, d1, d2;
            int[] task = new int[50];
            nb = jdim = 0;

            if (r < 0.0) throw new Exception("Radius must be nonnegative.");
            // Find the smallest box that contains the ball of radius r.
            while (boxes[nb].dau1 > 0)
            {
                nbold = nb;
                d1 = boxes[nb].dau1;
                d2 = boxes[nb].dau2;
                // Only need to check the dimension that 
                // divides the daughters
                // A ball touching the split plane can include points from
                // either daughter, since the radius comparison is inclusive.
                if (pt.Comp[jdim] + r < boxes[d1].hi.Comp[jdim])
                    nb = d1;
                else if (pt.Comp[jdim] - r > boxes[d2].lo.Comp[jdim])
                    nb = d2;
                jdim = ++jdim % DIM;
                // Neither daughter in within range
                if (nb == nbold) break;
            }
            // Now traverse the tree below the starting box only as needed
            task[1] = nb;
            ntask = 1;

            while (ntask > 0)
            {
                k = task[ntask--];
                // Box and ball are disjoint.
                if (boxes[k].Distance(pt) > r) continue;
                if (boxes[k].dau1 > 0)
                {
                    // Expands box further when possible
                    task[++ntask] = boxes[k].dau1;
                    task[++ntask] = boxes[k].dau2;
                }
                else
                {
                    // Otherwise processes the points in the box
                    for (i = boxes[k].ptlo; i <= boxes[k].pthi; i++)
                    {
                        double dist = ptss[ptindx[i]].Distance(pt);
                        if (dist <= r /*&& list.Count < nmax*/)
                        {
                            // Inserts to maintain ascending distance
                            if (list.Count == 0)
                            {
                                list.Add(ptindx[i]);
                                dists.Add(dist);
                            }
                            else
                            {
                                int place = dists.FindIndex(d => d > dist);
                                // If point is farthest away, appends list
                                if (place < 0)
                                {
                                    list.Add(ptindx[i]);
                                    dists.Add(dist);
                                }
                                // Otherwise, inserts it in between 
                                // bounding points
                                else
                                {
                                    list.Insert(place, ptindx[i]);
                                    dists.Insert(place, dist);
                                }
                            }
                        }
                    }
                }
            }

            // Trims list to given length
            if (list.Count > nmax)
            {
                int len = dists.Count - nmax;
                dists.RemoveRange(nmax, len);
                list.RemoveRange(nmax, len);
            }

            return new Tuple<int[], double[]>(list.ToArray(), 
                dists.ToArray());
        }

        #endregion Methods
    }

    /// <summary>
    /// Node boxes in a binary tree containing points.
    /// </summary>
    public struct BoxNode
    {
        #region Fields & Properties

        /// <summary>
        /// The lower corner of the box
        /// </summary>
        public readonly Point lo;

        /// <summary>
        /// the upper corner of the box
        /// </summary>
        public readonly Point hi;

        /// <summary>
        /// Mother box index
        /// </summary>
        public int mom;

        /// <summary>
        /// 1st daughter box index
        /// </summary>
        public int dau1;

        /// <summary>
        /// 2nd daughter box index
        /// </summary>
        public int dau2;

        /// <summary>
        /// Index of the point of the lower corner of the box
        /// </summary>
        public readonly int ptlo;

        /// <summary>
        /// Index of the point of the upper corner of the box
        /// </summary>
        public readonly int pthi;

        #endregion Fields & Properties
        #region Constructor

        /// <summary>
        /// Box node structure used in constructing the K-D tree
        /// </summary>
        /// <param name="lo"> The lower corner of the box </param>
        /// <param name="hi"> The upper corner of the box </param>
        /// <param name="mom"> Index of mother node </param>
        /// <param name="dau1"> Index of 1st daughter node </param>
        /// <param name="dau2"> Index of 2nd daughter node </param>
        /// <param name="ptlo"> Index of the point of the lower corner
        /// of the box </param>
        /// <param name="pthi"> Index of the point of the higher corner 
        /// of the box </param>
        public BoxNode(Point lo, Point hi, int mom, int dau1, int dau2,
            int ptlo, int pthi)
        {
            this.lo = lo;
            this.hi = hi;
            this.mom = mom;
            this.dau1 = dau1;
            this.dau2 = dau2;
            this.ptlo = ptlo;
            this.pthi = pthi;
        }

        #endregion Constructor
        #region Methods

        /// <summary>
        /// Calculates the distance between a point and the closest point 
        /// of a box.  If the point is inside the box, a zero is returned.
        /// </summary>
        /// <param name="p"> Input point </param>
        /// <returns> Distance to the closest corner point </returns>
        public double Distance(Point p)
        {
            double dd = 0;
            for (int i = 0; i < p.Comp.Length; i++)
            {
                if (p.Comp[i] < lo.Comp[i])
                    dd += Math.Pow(p.Comp[i] - lo.Comp[i], 2);
                if (p.Comp[i] > hi.Comp[i])
                    dd += Math.Pow(p.Comp[i] - hi.Comp[i], 2);
            }
            return Math.Sqrt(dd);
        }

        #endregion Methods
    }
}

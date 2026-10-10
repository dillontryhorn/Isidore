using System;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Ray trees record the rays traced through a scene from a projector 
    /// to a terminating surface
    /// </summary>
    public class RayTree
    {
        # region Fields & Properties

        /// <summary>
        /// Component rays
        /// </summary>
        protected internal RenderRays rays;

        /// <summary>
        /// The maximum casted ray depth accepted
        /// </summary>
        public int DepthLimit = 10;

        /// <summary>
        /// Component rays
        /// </summary>
        public RenderRays Rays { get { return rays; } }

        /// <summary>
        /// Flags if there are any open rays in the tree
        /// </summary>
        public bool Open
        { get
            { if (open)
                    return CheckForOpenRays();
                else
                    return open;
            }
        }
        private bool open = true;

        # endregion Fields & Properties
        # region Constructor

        /// <summary>
        /// Constructs a RayTree from a single RenderRay
        /// </summary>
        /// <param name="ray"> Ray to act as the zero ray </param>
        public RayTree(RenderRay ray = null)
        {
            // Copies ray
            rays = new RenderRays { ray ?? new RenderRay() };
        }

        # endregion Constructor
        # region Methods

        /// <summary>
        /// Resets the ray tree by removing all casted rays
        /// </summary>
        public void Reset()
        {
            // Removes all rays after the initial ray
            if (rays.Count > 1)
                rays.RemoveRange(1, rays.Count - 1);
            if (rays.Count > 0)
            {
                rays[0].Status = RayStatus.Open;
                rays[0].IntersectData = new IntersectData();
            }
            open = rays.Count > 0;
        }

        /// <summary>
        /// Adds a ray to this ray tree
        /// </summary>
        /// <param name="ray"> Ray to add to the tree </param>
        public void Add(RenderRay ray)
        {
            if(ray.Rank > DepthLimit)
            {
                Console.WriteLine("RayTree:: Ray's rank is beyond this tree's limit. Ray was not added.");
                return;
            }
            else
            {
                rays.Add(ray);
                open = true;
            }

        }

        /// <summary>
        /// Adds a list of RenderRays to this RayTree
        /// </summary>
        /// <param name="moreRays"> List of RenderRays </param>
        public void Add(RenderRays moreRays)
        {
            // Uses a lambda expression to call the other Add method
            moreRays.ForEach(ray => Add(ray));
        }

        /// <summary>
        /// Searches each ray and shifts child ray to the tree.  Sets open rays to closed.
        /// </summary>
        public void Update()
        {
            // For each ray
            for (int ridx = 0; ridx < rays.Count; ridx++)
            {
                // 
                // Checks for propagation
                if (rays[ridx].Status == RayStatus.Propagated)
                {
                    // References the ray
                    RenderRay ray = rays[ridx];

                    // Closes the ray
                    ray.Status = RayStatus.Closed;

                    // Checks for any casted rays
                    if (ray.IntersectData.CastedRays != null)
                        // Closes any casted ray that above the maximum ray depth
                        for (int idx = 0; idx < ray.IntersectData.CastedRays.Count; 
                            idx++)
                        {
                            // References the current ray
                            RenderRay castedRay = ray.IntersectData.CastedRays[idx];

                            // If ray depth is above the limit, sets open to false
                            if (castedRay.Rank <= DepthLimit)
                                castedRay.Status = 0;
                            else
                                castedRay.Status = RayStatus.Closed;

                            // Adds ray to tree
                            rays.Add(castedRay);
                        }
                }
            }
            // Checks for open rays
            CheckForOpenRays();
        }

        /// <summary>
        /// Returns a reference to the next open ray in the tree
        /// Propagated rays are treated as open rays
        /// </summary>
        /// <returns> The next open render ray in the tree </returns>
        public RenderRay NextOpenRay()
        {
            // Uses a lambda predicate to return the first open ray in the tree
            return rays.Find(ray => ray.Status != RayStatus.Closed);
        }

        /// <summary>
        /// Checks to see if any rays are open
        /// This includes propagated rays
        /// </summary>
        /// <returns> Tags that rays are open </returns>
        public bool CheckForOpenRays()
        {
            open = false;
            for(int idx=0;idx<rays.Count;idx++)
                if(rays[idx].Status == RayStatus.Open || 
                    rays[idx].Status == RayStatus.Propagated)
                {
                    open = true;
                    break;
                }
            return open;
        }

        /// <summary>
        /// Checks to see if any rays are open but propagated
        /// </summary>
        /// <returns> Tags that rays are open </returns>
        public bool CheckForPropagatedRays()
        {
            open = false;
            for (int idx = 0; idx < rays.Count; idx++)
                if (rays[idx].Status == RayStatus.Propagated)
                {
                    open = true;
                    break;
                }
            return open;
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        public RayTree Clone()
        {
            RayTree newCopy = (RayTree)MemberwiseClone();

            if (rays != null)
                newCopy.rays = rays.Clone();

            return newCopy;
        }
        # endregion Methods
    }
}

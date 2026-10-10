using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Isidore.Render
{
    /// <summary>
    /// Body is an abstract class used for defining an physical boundary object
    /// that can be intersected or influence a ray.  This class is used as an 
    /// intermediate between the scene class and the two types of interfacing 
    /// objects: shapes which as surface representations, and volumes which 
    /// are volumetrics.
    /// </summary>
    public abstract class Body : Item
    {
        # region Fields & Properties

        // There are no members of the body class,
        // they are handled by the children

        # endregion Fields & Properties
        # region Abstract Methods

        /// <summary>
        /// Performs an intersection check and operation with a ray.
        /// It is assumed that the ray and object is in world space.  If this 
        /// is not the case, it should be addressed in that class's 
        /// Intersect method. One can use an iterative process to account for
        /// simulation special relativity.        /// </summary>
        /// <param name="ray"> Ray to evaluate/update for intersection </param>
        /// <returns> Intersect flag (true = intersection) </returns>
        public abstract bool Intersect(ref RenderRay ray);

        /// <summary>
        /// Applies material effects to the intersect data structure
        /// </summary>
        /// <param name="ray"> Render ray instance </param>
        public abstract void ApplyMaterials(ref RenderRay ray);

        # endregion Abstract Methods
        # region Methods

        /// <summary>
        /// Performs an intersection test for all the next open ray within a 
        /// projector's ray tree
        /// </summary>
        /// <param name="proj"> Projector instance </param>
        /// <returns> Array of IntersectData matching the projector rays </returns>
        public void OneCoreIntersect(ref Projector proj)
        {
            Mesh mesh = this as Mesh;
            if (mesh != null && mesh.TryGpuIntersect(proj))
                return;
            Projector projClone = proj;
            // Cycles through every ray tree
            for (int idx = 0; idx < proj.Rays.Length; idx++)
                Intersect(ref projClone.Rays[idx]);
        }

        /// <summary>
        /// Performs an intersection test for all the next open ray within a 
        /// projector's ray tree
        /// </summary>
        /// <param name="proj"> Projector instance </param>
        /// <returns> Array of IntersectData matching the projector rays </returns>
        public void MultiCoreIntersect(ref Projector proj)
        {
            Mesh mesh = this as Mesh;
            if (mesh != null && mesh.TryGpuIntersect(proj))
                return;
            Projector projClone = proj;
            Parallel.ForEach(Partitioner.Create(0, proj.Rays.Length), range => {
                for (int idx = range.Item1; idx < range.Item2; idx++)
                    Intersect(ref projClone.Rays[idx]);
            });
        }

        /// <summary>
        /// Performs an intersection test for all the next open ray within a 
        /// ray tree
        /// </summary>
        /// <param name="raytree"> Ray tree instance </param>
        /// <returns> IntersectData matching the projector rays </returns>
        protected void Intersect(ref RayTree raytree)
        {
            // Checks to see the ray tree is still open
            if (!raytree.Open)
                return;

            // Grabs next open ray in ray tree
            RenderRay thisRay = raytree.NextOpenRay();

            // Checks physical intersection
            bool intersect = Intersect(ref thisRay);

            // Checks material intersection and applies materials
            // Updates ray properties & casted rays
            if (intersect)
                ApplyMaterials(ref thisRay);

            // Marks this ray as being propagated
            thisRay.Status = RayStatus.Propagated;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Body Clone()
        {
            return (Body)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            Body newCopy = (Body)MemberwiseClone();

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
        protected void DeepCopyOverride(ref Body copy)
        {
            // Base copy
            Item baseCast = copy; // This is a shallow copy
            base.DeepCopyOverride(ref baseCast);
        }

        #endregion Methods
    }

    /// <summary>
    /// List of Body instances
    /// </summary>
    public class Bodies : List<Body>
    {
        /// <summary>
        /// Maximum value of the array
        /// (This is the default value of item.IDs)
        /// </summary>
        private int maxVal = -1;

        /// <summary>
        /// Adds an object to the list.  Automatically checks indices
        /// for null value (-1) or duplicates.
        /// </summary>
        /// <param name="item"> The object to add to the list. </param>
        new public void Add(Body item)
        {
            // Checks & corrects the ID
            // If ID > maxVal, updates maxID
            if (item.ID > maxVal)
            {
                maxVal = item.ID;
            }
            else
            {
                // If null (-1), then increments & assigns maxVal
                if (item.ID == -1)
                    item.ID = ++maxVal;
                // Otherwise Checks for duplicates
                else
                    //int[] idArr = this.Select(tag => tag.ID).ToArray();
                    for (int idx = 0; idx < Count; idx++)
                        if (item.ID == this[idx].ID)
                        {
                            item.ID = ++maxVal;
                            continue;
                        }
            }

            // Adds to list
            base.Add(item);
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        public Bodies Clone()
        {

            Bodies newList = new Bodies();

            ForEach(n => newList.Add(n.Clone()));

            return newList;
        }
    }
}

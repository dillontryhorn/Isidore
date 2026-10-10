using System.Collections.Generic;

namespace Isidore.Render
{
    /// <summary>
    ///  Body is an abstract class used for defining an item that can be 
    /// intersected by a ray that is a representation of a volumetric
    /// bulk media that the ray cast propagates through where the subspace
    /// has varying degrees of material influence.  This is opposed to the
    /// shape class that represents surfaces that may have layered materials.
    /// </summary>
    public abstract class Volume : Body
    {
        # region Fields & Properties

        /// <summary>
        /// Materials associated with each subspace of the volume.  Each
        /// material in the array is paired to the subspace index.  If there
        /// is only one material, then every material will be applied to
        /// every subspace.
        /// </summary>
        public List<Material> Materials = null;

        #endregion Fields & Properties
        #region Abstract Methods

        // No Abstract members

        #endregion Abstract Methods
        #region Methods

        /// <summary>
        /// Applies material effects to the intersect data structure
        /// </summary>
        /// <param name="ray"> Render ray instance </param>
        override public void ApplyMaterials(ref RenderRay ray)
        {
            if (Materials != null)
                for (int idx = 0; idx < Materials.Count; idx++)
                    Materials[idx].Apply(ref ray);

            // Checks if any rays have been casted 
            if(ray.IntersectData.CastedRays.Count == 0)
            {
                // If not, then adds a ray that is shifted
                // to the intersect point
                IntersectData iData = ray.IntersectData;

                // Checks that the specific data class is a Volume subclass
                VolumeSpecificData vData = iData.BodySpecificData as VolumeSpecificData;
                if (vData == null)
                    return;

                // New ray
                RenderRay cRay = new RenderRay(iData.IntersectPt.Clone(),
                    ray.Dir, ray.Time, ray.Rank + 1, RayType.Transmitted,
                    ray.Properties.Clone());

                // Adds a casted ray
                ray.IntersectData.CastedRays.Add(cRay);
            }
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Volume Clone()
        {
            return (Volume)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            Volume newCopy = (Volume)MemberwiseClone();

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
        protected void DeepCopyOverride(ref Volume copy)
        {
            // Base copy
            Item baseCast = copy; // This is a shallow copy
            base.DeepCopyOverride(ref baseCast);

            // List<MaterialStack> Materials
            List<Material> matCopy = new List<Material>();
            Materials.ForEach(mat => matCopy.Add(mat.Clone()));
        }

        #endregion Methods
    }

    /// <summary>
    /// List of Volume instances
    /// </summary>
    public class Volumes : List<Volume>
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
        new public void Add(Volume item)
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
        public Volumes Clone()
        {

            Volumes newList = new Volumes();

            ForEach(n => newList.Add(n.Clone()));

            return newList;
        }
    }
}

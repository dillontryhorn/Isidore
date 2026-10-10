using System.Collections.Generic;

namespace Isidore.Render
{
    /// <summary>
    /// Shape is an abstract class used for defining an item that can be 
    /// intersected by a ray that is a representation of a solid surface
    /// that might include models for layered materials.  This is opposed
    /// the volume class which is used to represent bulk media volumetrics.
    /// </summary>
    public abstract class Shape : Body
    {
        # region Fields & Properties

        /// <summary>
        /// Materials associated with surface shape. Each list entry
        /// corresponds to a different physical trait. Each Material layer
        /// stack entry is the next layer from the air interface.
        /// </summary>
        public List<MaterialStack> Materials = new List<MaterialStack>();

        /// <summary>
        /// Allows intersection to occur on the back side of shape manifolds
        /// </summary>
        public virtual bool IntersectBackFaces
        {
            get { return intersectBackFaces; }
            set { intersectBackFaces = value; }
        }
        private bool intersectBackFaces = true;

        /// <summary>
        /// Flag for where to calculate the intersection UV coordinates
        /// </summary>
        public virtual bool CalculateUV
        {
            get { return calculateUV; }
            set { calculateUV = value; }
        }
        private bool calculateUV = true;

        /// <summary>
        /// Switches Alpha Mapping on
        /// </summary>
        public virtual bool UseAlpha
        {
            get { return useAlpha; }
            set { useAlpha = value; }
        }

        /// <summary>
        /// Backing value for <see cref="UseAlpha"/>.
        /// </summary>
        public bool useAlpha = true;

        /// <summary>
        /// Alpha texture mapping: determines whether a surface region is physically interactive
        /// </summary>
        public Texture Alpha = null;

        # endregion Fields & Properties
        # region Abstract Methods

        // None

        # endregion Abstract Methods
        # region Methods

        /// <summary>
        /// Applies material effects to the intersect data structure
        /// </summary>
        /// <param name="ray"> Render ray instance </param>
        override public void ApplyMaterials(ref RenderRay ray)
        {
            if(Materials != null)
                for (int idx = 0; idx < Materials.Count; idx++)
                    Materials[idx].Apply(ref ray);
        }

        /// <summary>
        /// Returns the alpha value (0,1) for a provided U,V coordinate.
        /// The alpha map determines whether a intersect is valid
        /// </summary>
        /// <param name="U"> U barycentric coordinate </param>
        /// <param name="V"> V barycentric coordinate </param>
        /// <returns> A boolean indicator of the alpha intersect </returns>
        protected bool getAlpha(double U, double V)
        {
            // If Alpha mapping it turned off
            // then every intersect is valid
            if (!UseAlpha || Alpha == null)
                return true;
            else
                return Alpha.GetVal(U, V) > 0;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Shape Clone()
        {
            return (Shape)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            //var newCopy = (Shape)base.CloneImp();
            Shape newCopy = (Shape)MemberwiseClone();

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
        protected void DeepCopyOverride(ref Shape copy)
        {
            // Base copy
            Item baseCast = copy; // This is a shallow copy
            base.DeepCopyOverride(ref baseCast);

            // List<MaterialStack> Materials
            if (Materials != null)
            {
                // Can't use lambda expression directly
                List<MaterialStack> matCopy = new List<MaterialStack>();
                Materials.ForEach(mat => matCopy.Add(mat.Clone()));
                copy.Materials = matCopy;
            }

            // Texture<bool> Alpha
            if (Alpha != null)
                copy.Alpha = Alpha.Clone();
        }

        #endregion Methods
    }

    /// <summary>
    /// List of Shape instances
    /// </summary>
    public class Shapes : List<Shape>
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
        new public void Add(Shape item)
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
        public Shapes Clone()
        {

            Shapes newList = new Shapes();

            ForEach(n => newList.Add(n.Clone()));

            return newList;
        }
    }
}

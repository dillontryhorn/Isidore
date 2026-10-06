using System;
using System.Collections.Generic;

namespace Isidore.Render
{

    /// <summary>
    /// Materials are part of every body and determines how the ray 
    /// interacts with it.  Its effect is often described in the
    /// context of a physical property, i.e. Property.
    /// </summary>
    public abstract class Material : ICloneable
    {
        #region Fields & Properties

        /// <summary>
        /// Current time of the material
        /// </summary>
        public double CurrentTime { get { return currentTime; } }

        /// <summary>
        /// Current time of the material
        /// </summary>
        internal double currentTime = 0.0;

        /// <summary>
        /// Previous states maintained by the material
        /// </summary>
        public List<Material> PreviousStates { get { return previousStates; } }

        /// <summary>
        /// Previous states of the material.  This is used to revert if 
        /// there's an intersect point closer that this one or when a
        /// render ray is in negative relative time
        /// </summary>
        internal List<Material> previousStates;

        /// <summary>
        /// Turns on this texture
        /// </summary>
        public bool On = true;

        /// <summary>
        /// Material's alpha map for determining where the material exists
        /// over the associated surface.  Any x &gt; 0 is considered
        /// an valid intersection.
        /// </summary>
        public Texture Alpha = null;

        /// <summary>
        /// Period (in frames or time) to retain previous states
        /// </summary>
        internal Tuple<int, double> recallableRange = new Tuple<int, double>(5, 0.1);

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public Material()
        {
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Interface for applying and processing the ray intersect data by
        /// this material
        /// </summary>
        /// <param name="ray"> ray accessing the material </param>
        /// <returns> This material has interacted with the ray </returns>
        public virtual bool Apply(ref RenderRay ray)
        {
            // Checks if material is on
            if (!On)
                return false;

            // For shape bodies, checks if there is any alpha consideration
            // This might be moved to the material stack class eventually
            ShapeSpecificData sData = ray.IntersectData.BodySpecificData as 
                ShapeSpecificData;
            if (sData != null)
            {
                // Checks if alpha map has a non-zero value (and is not null)
                if (Alpha != null && Alpha.GetVal(sData.U, sData.V) > 0)
                    return false;
            }

            // Processes the intersection data 
            return ProcessIntersectData(ref ray);
        }

        /// <summary>
        /// Determines how the intersection data is altered by the material
        /// </summary>
        /// <param name="ray"> Render ray instance </param>
        /// <returns> Indicates material interaction  </returns>
        public abstract bool ProcessIntersectData(ref RenderRay ray);

        /// <summary>
        /// Deep-copy (Non-referenced) clone
        /// </summary>
        /// <returns> Cloned copy </returns>
        public Material Clone()
        {
            return CloneImp();
        }

        /// <summary>
        /// Deep-copy (Non-referenced) clone casted as an object class
        /// </summary>
        /// <returns> Object class clone </returns>
        object ICloneable.Clone()
        {
            return CloneImp();
        }

        /// <summary>
        /// Clone implementation. Uses MemberwiseClone to clone, and 
        /// inheriting classes will implement the cloning of
        /// specific data types 
        /// </summary>
        /// <returns> Clone copy </returns>
        protected virtual Material CloneImp()
        {
            // Shallow copy
            Material newCopy = (Material)MemberwiseClone();
            // recallable Range should be cloned with a shallow copy

            // Deep copy
            if (previousStates != null)
            {
                List<Material> newStates = new List<Material>();
                previousStates.ForEach(mat => newStates.Add(mat.Clone()));
                newCopy.previousStates = newStates;
            }
            if (Alpha != null)
                newCopy.Alpha = Alpha.Clone();

            return newCopy;
        }

        #endregion Methods
    }

    /// <summary>
    /// Stacks of material layers.  The first material in the list that
    /// is on and has texture coverage at the ray's UV coordinate
    /// will be used as the material
    /// </summary>
    public class MaterialStack : List<Material>
    {
        /// <summary>
        /// Constructor using a single initial material
        /// </summary>
        /// <param name="mat"> Material </param>
        public MaterialStack(Material mat)
        {
            Add(mat);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public MaterialStack() { }

        /// <summary>
        /// Applies the material layer list to the ray's incident data
        /// </summary>
        /// <param name="ray"> Ray to apply the material to </param>
        public void Apply(ref RenderRay ray)
        {
            for (int idx = 0; idx < Count; idx++)
            {
                // Checks if material texture is present
                bool inter = this[idx].Apply(ref ray);
                if (inter)
                    continue;
            }
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        public MaterialStack Clone()
        {

            MaterialStack newList = new MaterialStack();

            ForEach(n => newList.Add(n.Clone()));

            return newList;
        }
    }
}

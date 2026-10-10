namespace Isidore.Render
{
    /// <summary>
    /// PropertyValues provides a mechanism to assign adds its property
    /// to a ray.  It doesn't cast any child rays.  This is useful for
    /// providing information on surface conditions such as temperature.
    /// </summary>
    public class PropertyValue : Material
    {
        #region Fields & Properties

        /// <summary>
        /// Reflectance of this reflector material.
        /// </summary>
        public Property Property { get; set; }


        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// PropertyValue constructor.  The property is referenced
        /// (Not cloned)
        /// </summary>
        /// <param name="property"> Property instance </param>
        public PropertyValue(Property property)
        {
            Property = property;
        }

        #endregion Constructor
        #region Methods

        /// <summary>
        /// Finds the reflectance for the angle of incidence provided
        /// by the intersection data created from a ray/shape intersection
        /// and the wavelength property attached to the ray.  
        /// If there is no wavelength property, this material will not 
        /// interact.  This material will only interact with Shape subclasses.
        /// </summary>
        /// <param name="ray"> ray used for generating the intersect data
        /// instance</param>
        /// <returns> Indicates material interaction  </returns>
        override public bool ProcessIntersectData(ref RenderRay ray)
        {
            // Checks that the specific data class is a Shape subclass
            ShapeSpecificData sData = ray.IntersectData.BodySpecificData as 
                ShapeSpecificData;
            // If not, "as" will return a null, so this returns unaltered
            if (sData == null)
                return false;

            // This saves some typing
            IntersectData iData = ray.IntersectData;

            ray.IntersectData.Properties.Add(Property);

            // returns interaction notification
            return true;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public PropertyValue Clone()
        {
            return (PropertyValue)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Material CloneImp()
        {
            // Shallow copies from base
            PropertyValue newCopy = (PropertyValue)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (Property != null)
                newCopy.Property = Property.Clone();

            return newCopy;
        }
        #endregion Methods
    }
}

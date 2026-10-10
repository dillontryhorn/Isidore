namespace Isidore.Render
{

    /// <summary>
    /// TextureValue is a material that returns the value of its base texture
    /// member.  If casted rays are allowed, they will start at the 
    /// intersection point with the identical direction as its parent.
    /// </summary>
    public class TextureValue : Material
    {
        #region Fields & Properties

        /// <summary>
        /// The base texture used for calculating a value
        /// </summary>
        public Texture baseTexture { get; set; }

        /// <summary>
        /// Anchors texture to the local coordinate system
        /// (Used for procedural textures)
        /// </summary>
        public bool anchorTextureToBody { get; set; }

        /// <summary>
        /// Adds a time shift dimension to the texture coordinate
        /// </summary>
        public bool useTimeShift { get; set; }

        /// <summary>
        /// The length of time to use as a the base unit for the time dimension
        /// </summary>
        public double timeSegmentLength { get; set; }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor.  The base texture is referenced (Not cloned)
        /// </summary>
        /// <param name="baseTexture"> Texture used to provide a value 
        /// for the material </param>
        /// <param name="anchorTextureToBody"> Flag for anchoring the texture
        /// to the body local coordinates instead of the global 
        /// coordinates </param>
        /// <param name="useTimeShift"> Adds a time shift dimension to the 
        /// texture coordinate </param>
        /// <param name="timeSegmentLength"> Sets the length of time used as
        /// the base unit for the time dimension </param>
        public TextureValue(Texture baseTexture = null,
            bool anchorTextureToBody = true, bool useTimeShift=false, 
            double timeSegmentLength = 1.0)
        {
            this.baseTexture = baseTexture;
            this.anchorTextureToBody = anchorTextureToBody;
            this.useTimeShift = useTimeShift;
            this.timeSegmentLength = timeSegmentLength;
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Determines how the intersection data is altered by the material
        /// </summary>
        /// <param name="ray"> Render ray instance </param>
        /// <returns> Indicates material interaction  </returns>
        public override bool ProcessIntersectData(ref RenderRay ray)
        {
            // Reference to the ray's intersection data
            IntersectData iData = ray.IntersectData;

            // Handles map textures by using UV coordinates
            if(baseTexture.UseUV)
            {
                ShapeSpecificData sData = iData.BodySpecificData as ShapeSpecificData;
                double U = sData.U;
                double V = sData.V;
                double v = baseTexture.GetVal(U, V);
                Scalar thisV = new Scalar(v);
                iData.Properties.Add(thisV);
                return true;
            }

            // Otherwise, uses the location

            // Finds the intersect point
            Maths.Point iPt = iData.IntersectPt;

            // If the texture is anchored to the local coordinate, casts
            // the intersect point to the local frame of reference.
            if(anchorTextureToBody)
            {
                Maths.Transform trans = iData.Body.Local2World;
                // Overwrites the reference with a clone
                iPt = iPt.CopyTransform(trans, false);
            }

            // Texture coordinate.  Either uses the ray's intersection point
            // or adds a time shift to the end of the coordinates
            double[] coord;
            if(useTimeShift)
            {
                int cLen = iPt.Comp.Length;
                coord = new double[cLen + 1];
                // Copies intersection coordinates
                for (int idx = 0; idx < cLen; idx++)
                    coord[idx] = iPt.Comp[idx];
                // Adds time shift
                double timeShift = ray.Time / timeSegmentLength;
                coord[cLen] = timeShift;
            }
            else
                coord = iPt.Comp;

            // Retrieves the texture value at the intersection point
            double val = baseTexture.GetVal(coord);

            // Converts the value into a scalar property and adds it to the array
            Scalar thisVal = new Scalar(val);
            iData.Properties.Add(thisVal);

            // returns interaction notification
            return true;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public TextureValue Clone()
        {
            return (TextureValue)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Material CloneImp()
        {
            // Shallow copies from base
            TextureValue newCopy = (TextureValue)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (baseTexture != null)
                newCopy.baseTexture = baseTexture.Clone();

            return newCopy;
        }

        #endregion Methods
    }
}

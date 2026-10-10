using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// ProceduralTexture is a Texture child that applies Noise to a Material
    /// </summary>
    public class ProceduralTexture : Texture
    {
        #region Fields & Properties

        /// <summary>
        /// Noise used in this procedural texture
        /// </summary>
        public Noise noise { get; set; }


        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor.  Initializes a procedural texture.
        /// </summary>
        /// <param name="noiseFunc"> Noise to use procedurally  
        /// (Default = fBm with default parameter settings.) </param>
        public ProceduralTexture(Noise noiseFunc = null)
        {
            noise = noiseFunc ?? new fBmNoise();
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Returns value of texture for a given coordinate
        /// </summary>
        /// <param name="x"> X coordinate </param>
        /// <param name="y"> Y coordinate </param>
        /// <param name="z"> Z coordinate </param>
        /// <returns> Texture value at that location </returns>
        public override double GetVal(double x, double y, double z)
        {
            double[] coord = new double[] { x, y, z };
            return GetVal(coord);
        }

        /// <summary>
        /// Returns value of texture for a given coordinates
        /// </summary>
        /// <param name="x"> X coordinate </param>
        /// <param name="y"> Y coordinate </param>
        /// <returns> Texture value at that location </returns>
        public override double GetVal(double x, double y)
        {
            double[] coord = new double[] { x, y };
            return GetVal(coord);
        }

        /// <summary>
        /// Returns value of texture for a given coordinate
        /// </summary>
        /// <param name="coords"> Spatial coordinate vector </param>
        /// <returns> Texture value at that location </returns>
        public override double GetVal(double[] coords)
        {
            Point ptCoords = new Point(coords);
            return GetVal(ptCoords);
        }

        /// <summary>
        /// Returns value of texture for a given point
        /// </summary>
        /// <param name="coords"> Spatial coordinate vector </param>
        /// <returns> Texture value at that location </returns>
        public double GetVal(Point coords)
        {
            double inoise = noise.GetVal(coords);
            return inoise;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public ProceduralTexture Clone()
        {
            return CloneImp() as ProceduralTexture;
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Texture CloneImp()
        {
            // Shallow copies from base
            ProceduralTexture newCopy = base.CloneImp() as ProceduralTexture;

            // Deep-copies all data this is referenced by default
            newCopy.noise = noise.Clone();

            return newCopy;
        }

        #endregion Methods
    }
}

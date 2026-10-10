using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// PerturbingTexture is a Texture child that uses a procedural texture
    /// to perturb the image of a second texture
    /// </summary>
    public class PerturbedTexture : Texture
    {
        #region Fields & Properties

        /// <summary>
        /// Texture providing the perturbation magnitude
        /// </summary>
        public ProceduralTexture perturbingTexture { get; set; }

        /// <summary>
        /// Texture providing the source values to be perturbed
        /// </summary>
        public ProceduralTexture sourceTexture { get; set; }

        /// <summary>
        /// Directional vector to apply the perturbation
        /// </summary>
        public Vector perturbDirection
        {
            get { return perturbDir; }
            set { perturbDir = value.CopyNormalize(); }
        }
        /// <summary>
        /// Direction vector to apply the perturbation
        /// </summary>
        private Vector perturbDir;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Perturbed Texture Constructor
        /// </summary>
        /// <param name="sourceTexture"> Texture providing the source values 
        /// to be perturbed </param>
        /// <param name="perturbingTexture"> Texture providing the perturbation 
        /// magnitude</param>
        /// <param name="perturbDirection"> Directional vector to apply the 
        /// perturbation </param>
        public PerturbedTexture(ProceduralTexture sourceTexture,
            ProceduralTexture perturbingTexture = null, 
            Vector perturbDirection = null)
        {
            this.sourceTexture = sourceTexture;
            this.perturbingTexture = perturbingTexture ??
                new ProceduralTexture();
            this.perturbDirection = perturbDirection ??
                Vector.Unit(3, 1);
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
            // Casts coordinates as a point
            // Gets the perturbation 
            double val = GetVal(new Point(coords));

            return val;
        }

        /// <summary>
        /// Returns value of texture for a given point
        /// </summary>
        /// <param name="coords"> Spatial coordinate vector </param>
        /// <returns> Texture value at that location </returns>
        public double GetVal(Point coords)
        {
            // Gets the perturbation magnitude
            double mag = perturbingTexture.GetVal(coords);

            // Finds the perturbed coordinates
            Vector perturbsCoord = mag * perturbDirection;

            // Adds the perturbation to the base coordinates
            Point newCoord = coords.Clone();
            for (int idx = 0; idx < newCoord.Comp.Length; idx++)
                newCoord.Comp[idx] += perturbDirection.Comp[idx] * mag;

            // Gets value at perturbed coordinate
            double val = sourceTexture.GetVal(newCoord);

            return val;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public PerturbedTexture Clone()
        {
            return CloneImp() as PerturbedTexture;
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Texture CloneImp()
        {
            // Shallow copies from base
            PerturbedTexture newCopy = base.CloneImp() as PerturbedTexture;

            // Deep-copies all data this is referenced by default
            newCopy.perturbDir = perturbDir.Clone();
            newCopy.perturbingTexture = perturbingTexture.Clone();
            newCopy.sourceTexture = sourceTexture.Clone();

            return newCopy;
        }

        #endregion Methods
    }
}

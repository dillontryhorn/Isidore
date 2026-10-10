using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Texture class child that associates a 2D array map with a surface.
    /// It is intended to be used with alpha, bump, but not temp texture maps
    /// </summary>
    public class MapTexture:Texture
    {
        #region Fields & Properties

        /// <summary>
        /// Texture map (bitmap)
        /// </summary>
        public double[,] map;

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor.  Assigns a map of zeros based on width and height
        /// </summary>
        /// <param name="dim0"> Height of material texture </param>
        /// <param name="dim1"> Width of material texture</param>
        public MapTexture(int dim0 = 1, int dim1 = 1)
        {
            map = new double[dim0, dim1];
            useUV = true;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="tMap"> Texture map </param>
        public MapTexture(double[,] tMap)
        {
            SetMap(tMap);
            useUV = true;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="texture"> Map texture to copy </param>
        public MapTexture(MapTexture texture)
        {
            SetMap(texture.map);
            useUV = true;
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Sets map and updates associated values
        /// </summary>
        /// <param name="tMap"> texture map </param>
        public void SetMap(double[,] tMap)
        {
            map = (double[,])tMap.Clone();
        }

        /// <summary>
        /// Resets map to value tVal.
        /// </summary>
        /// <param name="tVal"> Value to set the map to </param>
        public void SetMap(double tVal)
        {
            int dim0 = map.GetLength(0);
            int dim1 = map.GetLength(1);
            map = Distribution.Uniform(dim0, dim1, tVal);
        }

        /// <summary>
        /// Scales the values of the map
        /// </summary>
        /// <param name="tScale"> Scale factor </param>
        public void ScaleMap(double tScale)
        {
            map = Operator.Multiply(tScale, map);
        }

        /// <summary>
        /// Returns the map value for UV coordinates
        /// </summary>
        /// <param name="u"> U coordinate </param>
        /// <param name="v"> V coordinate </param>
        /// <returns> Map value </returns>
        public override double GetVal(double u, double v)
        {
            // Retrieves image location
            int[] loc = UV2pixel(u, v);

            // Retrieves value
            double val = map[loc[0], loc[1]];
            return val;
        }

        /// <summary>
        /// Returns the map value for UV coordinates.
        /// In image mapping, there is no Z coordinate to interpolate.
        /// </summary>
        /// <param name="u"> U coordinate </param>
        /// <param name="v"> V coordinate </param>
        /// <param name="z"> Z coordinate </param>
        /// <returns> Map value </returns>
        public override double GetVal(double u, double v, double z)
        {
            return GetVal(u, v);
        }

        /// <summary>
        /// Returns the map value for the UV coordinates.
        /// </summary>
        /// <param name="coords"> UV (XY) or XYZ coordinate space array </param>
        /// <returns> Map value </returns>
        public override double GetVal(double[] coords)
        {

            if (coords.Length == 2)
                return GetVal(coords[0], coords[1]);
            else if (coords.Length == 3)
                return GetVal(coords[0], coords[1], coords[2]);
            else
                throw new System.ArgumentException(
                    "Textures are limited to 2 or 3 dimensions.", "coords");
        }

        /// <summary>
        /// Sets the map value for UV coordinates
        /// </summary>
        /// <param name="val"> value to set the UV coordinate to </param>
        /// <param name="u"> U coordinate </param>
        /// <param name="v"> V coordinate </param>
        public void SetVal(double val, double u, double v)
        {
            // Retrieves pixel coordinates
            int[] loc = UV2pixel(u, v);
            
            // Sets value
            map[loc[0], loc[1]] = val;
        }

        /// <summary>
        /// Converts coordinates from UV to pixel space
        /// </summary>
        /// <param name="u"> U coordinate </param>
        /// <param name="v"> V coordinate </param>
        /// <returns> pixel coordinates </returns>
        public int[] UV2pixel(double u, double v)
        {
            // Image size
            int len0 = map.GetLength(0);
            int len1 = map.GetLength(1);

            // UV pixel location on the map
            // (Images use a left-hand coordinate system)
            double dloc0 = u * len0;
            double dloc1 = (1 - v) * len1;

            // Fixes via Truncation
            int[] loc = new int[2];
            loc[0] = (int)dloc0;
            loc[1] = (int)dloc1;

            // Shift upper bound to the map pixel limit
            --len0;
            --len1;

            // Bounds coordinates to the image size
            loc[0] = (loc[0] > len0) ? len0 : loc[0];
            loc[1] = (loc[1] > len1) ? len1 : loc[1];
            loc[0] = (loc[0] < 0) ? 0 : loc[0];
            loc[1] = (loc[1] < 0) ? 0 : loc[1];

            return loc;
        }

        /// <summary>
        /// Resets the map to a single zero pixel
        /// </summary>
        public void reset()
        {
            map = new double[1, 1];
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public MapTexture Clone()
        {
            return (MapTexture)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Texture CloneImp()
        {
            // Shallow copies from base
            MapTexture newCopy = (MapTexture)base.CloneImp();

            // Deep-copies all data this is referenced by default
            if (map != null)
                newCopy.map = map.Clone() as double[,];

            return newCopy;
        }

        #endregion Methods
    }
}

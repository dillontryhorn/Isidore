using System;
using Isidore.Maths;

// Noise generate the synthetic values for procedural texturing
// By using an OOP approach, we are able to keep a general solution while
// maintaining individual noise realizations.

namespace Isidore.Render
{

    /// <summary>
    /// Enumerations of included noise distribution functions
    /// </summary>
    public enum NoiseDistribution
    {
        /// <summary>
        /// Normal distribution flag (No change)
        /// </summary>
        Normal,
        /// <summary>
        /// Long-normal distribution flag (Log space)
        /// </summary>
        LogNormal
    };
    
    /// <summary>
    /// The Noise class is used to access noise functions.  It is mainly used
    /// as a base class for other noise models.  Noise classes are used by the
    /// ProceduraTexture class for texture synthesis
    /// </summary>
    public class Noise : ICloneable
    {
        #region Fields & Properties

        /// <summary>
        /// The noise function used in generating noise
        /// </summary>
        public NoiseFunction noiseFunc;

        /// <summary>
        /// Additional spatial offset applied to the noise coordinates
        /// </summary>
        public Vector shift { get; set; }

        /// <summary>
        /// Factor multiplied to the noise (Prior to offset)
        /// </summary>
        public double multiplier { get; set; }

        /// <summary>
        /// Offset added to the noise (After multiplication)
        /// </summary>
        public double offset { get; set; }

        /// <summary>
        /// Distribution function applied to the noise (After all other steps) 
        /// </summary>
        public Func<double, double> distFunc { get; set; }

        #endregion Fields & Properties
        #region Constructor

        /// <summary>
        /// Constructor for a basic noise object for accessing a noise function
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        /// <param name="distFunc"> Distribution function (After all other
        /// steps) </param>
        public Noise(NoiseFunction noiseFunc = null, Vector shift = null, 
            double multiplier = 1.0, double offset = 0.0, 
            Func<double,double> distFunc = null)
        {
            
            this.noiseFunc = noiseFunc ?? new PerlinNoiseFunction();
            this.shift = shift ?? new Vector(1000.1, 2000.2, 3000.3);
            this.multiplier = multiplier;
            this.offset = offset;
            this.distFunc = distFunc ?? DistFunc();
        }

        /// <summary>
        /// Constructor for a basic noise object using the NoiseParameters
        /// class to set the noise parameters
        /// </summary>
        /// <param name="noiseFunc"> The noise function to access </param>
        /// <param name="noiseParams"> The noise parameters for the 
        /// noise </param>
        public Noise(NoiseFunction noiseFunc, NoiseParameters noiseParams) :
            this(noiseFunc, noiseParams.shift, noiseParams.multiplier,
                noiseParams.offset, noiseParams.distFunc)
        {
        }

        #endregion Constructor
        #region Methods

        /// <summary>
        /// Returns the noise value associated with the given coordinates
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Noise value </returns>
        public virtual double GetVal(Point coord)
        {
            return GetVal(coord, shift);
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinate
        /// array
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Noise value </returns>
        public virtual double[] GetVal(Point[] coord)
        {
            return GetVal(coord, shift);
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinates
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <returns> Noise value </returns>
        public virtual double GetVal(Point coord, Vector shift)
        {
            return GetVal(coord, shift, multiplier);
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinate
        /// array
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <returns> Noise value </returns>
        public virtual double[] GetVal(Point[] coord, Vector shift)
        {
            return GetVal(coord, shift, multiplier);
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinates
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <returns> Noise value </returns>
        public virtual double GetVal(Point coord, Vector shift,
            double multiplier)
        {
            return GetVal(coord, shift, multiplier, offset);
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinate
        /// array
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <returns> Noise value </returns>
        public virtual double[] GetVal(Point[] coord, Vector shift,
            double multiplier)
        {
            return GetVal(coord, shift, multiplier, offset);
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinates
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        /// <returns> Noise value </returns>
        public virtual double GetVal(Point coord, Vector shift,
            double multiplier, double offset)
        {
            return GetVal(coord, shift, multiplier, offset, distFunc);
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinate
        /// array
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        /// <returns> Noise value </returns>
        public virtual double[] GetVal(Point[] coord, Vector shift,
            double multiplier, double offset)
        {
            return GetVal(coord, shift, multiplier, offset, distFunc);
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinates
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        /// <param name="distFunc"> Distribution function (After all other
        /// steps) </param>
        /// <returns> Noise value </returns>
        public virtual double GetVal(Point coord, Vector shift, 
            double multiplier, double offset,
            Func<double, double> distFunc)
        {
            // Combines coordinate & shift vectors
            Point theseCoord;
            if (coord.Comp.Length > shift.Comp.Length)
            {
                theseCoord = coord.Clone() as Point;
                for (int idx = 0; idx < shift.Comp.Length; idx++)
                    theseCoord.Comp[idx] += shift.Comp[idx];
            }  
            else
            {
                theseCoord = (Point)shift.Clone();
                for (int idx = 0; idx < coord.Comp.Length; idx++)
                    theseCoord.Comp[idx] += coord.Comp[idx];
            }

            // Noise value at these coordinates
            double val = GetBaseVal(theseCoord);

            // Scales and shift the noise
            val *= multiplier;
            val += offset;

            // Applies distribution function
            val = distFunc(val);

            return val;
        }

        /// <summary>
        /// Returns the noise value associated with the given coordinates
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        /// <param name="distFunc"> Distribution function (After all other
        /// steps) </param>
        /// <returns> Noise value </returns>
        public virtual double[] GetVal(Point[] coord, Vector shift,
            double multiplier, double offset, 
            Func<double, double> distFunc)
        {
            double[] vals = new double[coord.Length];
            for (int idx = 0; idx < coord.Length; idx++)
            {
                vals[idx] = GetVal(coord[idx], shift, multiplier, offset, 
                    distFunc);
            }
            return vals;
        }

        /// <summary>
        /// Returns the noise values with no modification
        /// </summary>
        /// <param name="coord"> Noise coordinates </param>
        /// <returns> Noise value </returns>
        public virtual double GetBaseVal(Point coord)
        {
            return noiseFunc.GetVal(coord);
        }

        /// <summary>
        /// Returns a function for the supplied distributions 
        /// </summary>
        /// <param name="Distribution"> Enumeration of the supplied 
        /// distribution function</param>
        /// <returns> The function for the requested function </returns>
        public static Func<double, double> DistFunc(
            NoiseDistribution Distribution = NoiseDistribution.Normal)
        {
            // Function generated
            Func<double, double> thisDist;

            // Switch for function generation
            switch (Distribution)
            {
                // Normal (i.e. no change)
                case NoiseDistribution.Normal:
                    thisDist = x => x;
                    break;
                // Log-normal (i.e., log space)
                case NoiseDistribution.LogNormal:
                    thisDist = x => Math.Exp(x);
                    break;
                default:
                    throw new InvalidOperationException("unknown item type");
            }

            return thisDist;
        }

        /// <summary>
        /// Deep-copy (Non-referenced) clone
        /// </summary>
        /// <returns> Cloned copy </returns>
        public Noise Clone()
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
        protected virtual Noise CloneImp()
        {
            // Shallow copy
            Noise newCopy = (Noise)MemberwiseClone();

            // Deep copy
            newCopy.noiseFunc = noiseFunc.Clone();
            newCopy.shift = shift.Clone() as Vector;
            newCopy.distFunc = distFunc.Clone() as Func<double, double>;

            return newCopy;
        }

        #endregion Methods
    }

    /// <summary>
    /// Noise parameters and default values used by the noise class 
    /// </summary>
    public class NoiseParameters
    {
        /// <summary>
        /// Additional spatial offset applied to the noise coordinates
        /// </summary>
        public Vector shift { get; set; }

        /// <summary>
        /// Factor multiplied to the noise (Prior to offset)
        /// </summary>
        public double multiplier { get; set; }

        /// <summary>
        /// Offset added to the noise (After multiplication)
        /// </summary>
        public double offset { get; set; }

        /// <summary>
        /// Distribution function applied to the noise (After all other steps)
        /// </summary>
        public Func<double, double> distFunc { get; set; }

        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="shift"> Additional spatial offset applied to the 
        /// noise coordinates (Default is for three dimension.) </param>
        /// <param name="multiplier"> Factor multiplied to the noise 
        /// (Prior to offset) </param>
        /// <param name="offset"> Offset added to the noise (After 
        /// multiplication) </param>
        /// <param name="distFunc"> Distribution function (After all other
        /// steps) </param>
        public NoiseParameters(Vector shift = null,
            double multiplier = 1.0, double offset = 0.0, 
            Func<double, double> distFunc = null)
        {
            this.shift = shift ?? new Vector(1000.1, 2000.2, 3000.3);
            this.multiplier = multiplier;
            this.offset = offset;
            this.distFunc = distFunc ?? Noise.DistFunc();
        }

        /// <summary>
        /// Returns a clone of this noise parameters class via deep copy
        /// </summary>
        /// <returns> The cloned copy </returns>
        public NoiseParameters Clone()
        {
            // New instance
            NoiseParameters NewInst = MemberwiseClone() as NoiseParameters;

            // Deep copy
            NewInst.shift = shift.Clone() as Vector;
            NewInst.distFunc = distFunc.Clone() as Func<double, double>;

            return NewInst;
        }

    }
}

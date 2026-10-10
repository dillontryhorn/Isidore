using System;
using System.Linq;
using Isidore.Maths;
using Isidore.Render;

namespace Isidore.Models
{
    /// <summary>
    /// The TurbulencePointWFS is a child of Point that produces 
    /// multi-fractal turbulence using point used with wavefront sensor
    /// collected statistics
    /// </summary>
    public class TurbulencePointWFS : ReferencePoint
    {
        #region  Fields & Properties

        /// <summary>
        /// Turbulence magnitude noise
        /// </summary>
        public Noise NoiseMagnitude { get; set; }

        /// <summary>
        /// Turbulence direction noise
        /// </summary>
        public Noise NoiseDirection { get; set; }

        /// <summary>
        /// Turbulence speed noise
        /// </summary>
        public Noise NoiseSpeed { get; set; }

        /// <summary>
        /// Turbulence time step
        /// </summary>
        public double TimeStep
        {
            get { return timeStep; }
            set
            {
                if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                    throw new ArgumentOutOfRangeException("TimeStep",
                        "The time step must be finite and greater than zero.");
                timeStep = value;
            }
        }
        private double timeStep;

        /// <summary>
        /// Rate of charge in coherence
        /// </summary>
        public double CoherenceRate { get; set; }

        /// <summary>
        /// Transform to noise space
        /// </summary>
        public Transform NoiseTransform { get { return noiseTransform; } }
        private Transform noiseTransform;

        /// <summary>
        /// The turbulence point position in noise space
        /// </summary>
        public KeyFrame<Point> NoisePosition { get { return noisePosition; } }
        private KeyFrame<Point> noisePosition;


        /// <summary>
        /// Returns the last time state
        /// </summary>
        public double LastTime
        {
            get { return noisePosition.Times.Last(); }
        }

        #endregion  Fields & Properties
        #region Constructor

        /// <summary>
        /// TurbulencePointWFS Constructor
        /// </summary>
        /// <param name="position"> Position in local </param>
        /// <param name="noiseMagnitude"> Magnitude noise instance </param>
        /// <param name="noiseDirection"> Direction noise instance </param>
        /// <param name="noiseSpeed"> Speed noise instance </param>
        /// <param name="noiseTransform"> Transform to noise space </param>
        /// <param name="timestep"> Time step size </param>
        /// <param name="coherenceRate"> Coherence rate </param>
        public TurbulencePointWFS(Point position = null, 
            Noise noiseMagnitude=null, Noise noiseDirection=null, 
            Noise noiseSpeed=null, Transform noiseTransform=null, 
            double timestep = 1e-6, double coherenceRate = 0): 
            base(position ?? new Point())
        {
            NoiseMagnitude = noiseMagnitude ?? new fBmNoise();
            NoiseDirection = noiseDirection ?? new fBmNoise();
            NoiseSpeed = noiseSpeed ?? new fBmNoise();
            this.noiseTransform = noiseTransform ?? new Transform();
            TimeStep = timestep;
            CoherenceRate = coherenceRate;

            noisePosition = new KeyFrame<Point>(
                Zero(Comp.Length), 0);
        }

        #endregion Constructor
        #region Methods

        /// <summary>
        /// Returns the turbulence magnitude
        /// </summary>
        /// <param name="coord"> Coordinate (local) </param>
        /// <param name="now"> Current time </param>
        /// <returns></returns>
        public double GetVal(Point coord, double now)
        {
            if (double.IsNaN(now) || double.IsInfinity(now))
                throw new ArgumentOutOfRangeException("now", "Evaluation time must be finite.");
            // Adds points beyond the last time
            //if (noisePosition.Times.Length == 0 || now > LastTime)
            if (now > LastTime)
                ExtendTimeline(now);

            // Interpolates to the noise position
            Point iPt = noisePosition.InterpolateToTime(now);

            // Adds in the coordinate
            iPt += coord;

            // Adds the coherence shift to the N+1 dimension
            double[] icoord = new double[iPt.Comp.Length + 1];
            iPt.Comp.CopyTo(icoord,0);
            icoord[iPt.Comp.Length] = CoherenceRate * now;
            Point icoordPt = new Point(icoord);

            // Calculates the noise magnitude
            double mag = NoiseMagnitude.GetVal(icoordPt);

            return mag;
        }

        /// <summary>
        /// Extends the time limit
        /// </summary>
        /// <param name="newLimit"> New time limit </param>
        private void ExtendTimeline(double newLimit)
        {
            // Adds each additional time up-to & including the new limit
            while (LastTime < newLimit)
            {
                double nextTime = LastTime + TimeStep;
                if (double.IsInfinity(nextTime) || nextTime <= LastTime)
                    throw new InvalidOperationException("The time step cannot advance this timeline.");
                // Using N + t space to determine what the next
                // radial length and angle should be
                double speed = 0, Dir = 0;
                if (LastTime > 0)
                {
                    double[] coord = Comp.Concat(new double[] { LastTime }).ToArray();
                    Point noisePt = new Point(coord);
                    speed = NoiseSpeed.GetVal(noisePt);
                    Dir = NoiseDirection.GetVal(noisePt);
                }

                // Converts velocity into an offset point in sensor space
                // using the elapsed time.  The negative pushes the
                // turbulence in the right hemispherical direction
                double L = -speed * TimeStep; // Distance traveled 
                double dX = L * Math.Cos(Dir); // X position
                double dY = L * Math.Sin(Dir); // Y position
                Vector dPos = new Vector(dX, dY, 0); // Displacement in sensor space

                // Transforms the offset point to local turbulence space
                dPos.Transform(noiseTransform);

                // Adds this delta to the last position in the random walk
                Point walkPt = noisePosition.Values.Last() + dPos.Comp;

                // Adds the position to the key-frame
                noisePosition.AddKeys(walkPt, nextTime);
            }
        }

        /// <summary>
        /// Clones this copy by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public TurbulencePointWFS Clone()
        {
            return CloneImp() as TurbulencePointWFS;
        }

        /// <summary>
        /// Deep-copy clone of this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new protected ReferencePoint CloneImp()
        {
            // Shallow copies from base
            TurbulencePointWFS newCopy = base.Clone() as TurbulencePointWFS;

            // Deep-copies all data this is referenced by default
            newCopy.NoiseMagnitude = NoiseMagnitude.Clone();
            newCopy.NoiseDirection = NoiseDirection.Clone();
            newCopy.NoiseSpeed = NoiseSpeed.Clone();
            newCopy.noiseTransform = noiseTransform.Clone();
            newCopy.noisePosition = new KeyFrame<Point>(
                noisePosition.Values.Select(point => point.Clone()).ToArray(),
                (double[])noisePosition.Times.Clone(), noisePosition.Interpolation);
            newCopy.noisePosition.Animate = noisePosition.Animate;

            return newCopy;
        }

        #endregion Methods
    }
}

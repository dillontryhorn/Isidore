using System.Linq;

namespace Isidore.Maths
{
    /// <summary>
    /// KeyFrameTrans applies key framing to the transform class
    /// </summary>
    public class KeyFrameTrans : KeyFrame<Transform>
    {

        # region Fields & Properties

        // All inherited from KeyFrame base

        # endregion Fields & Properties
        # region Constructors

        /// <summary>
        /// Full Constructor 
        /// </summary>
        /// <param name="transforms"> Transforms List </param>
        /// <param name="timeStamps"> Matching time stamps </param>
        /// <param name="interpolation"> Interpolation method:
        /// "linear" </param>
        /// <param name="animate"> Turns on/off key framing animation 
        /// (0 = first data point) </param>
        public KeyFrameTrans(Transform[] transforms = null, 
            double[] timeStamps = null, string interpolation = "linear",
            bool animate = true):
            base(transforms == null ? new Transform[] { new Transform() } :
                (Transform[])transforms.Clone(), timeStamps == null ?
                new double[] { 0.0 } : (double[])timeStamps.Clone(), interpolation)
        {
            Animate = animate;
        }

        /// <summary>
        /// Constructor accepting a single transform. Use for static objects
        /// or for real time positioning
        /// </summary>
        /// <param name="transform"> Transform to use as the animated 
        /// transform </param>
        /// <param name="timeStamp"> Time stamp of transform </param>
        public KeyFrameTrans(Transform transform, double timeStamp = 0.0) :
            this(new Transform[] { transform.Clone() }, 
                new double[] { timeStamp }) { }

        # endregion Constructor
        # region Methods

        /// <summary>
        /// Adds the transform to the animation array at the appropriate 
        /// place in the time line
        /// </summary>
        /// <param name="transform"> Transformation </param>
        /// <param name="timeStamp"> Time associated with this 
        /// transformation </param>
        new public void AddKeys(Transform transform, double timeStamp)
        {
            base.AddKeys(transform, timeStamp);
        }
        
        /// <summary>
        /// Removes the transform and time at index "idx"
        /// </summary>
        /// <param name="idx"> Index of transform to remove </param>
        new public void RemoveKeys(int idx)
        {
            base.RemoveKeys(idx);
        }

        /// <summary>
        /// Merges two animation transforms by interpolating to unique 
        /// time points and applying the new transform to the existing 
        /// transform
        /// </summary>
        /// <param name="kf2"> Second key frame to merge with this 
        /// one </param>
        public void MergeKeys(KeyFrameTrans kf2)
        {
            base.MergeKeys(kf2);
        }

        /// <summary>
        /// Applies the inputted transform to every key frame in the path
        /// </summary>
        /// <param name="transform"> Transform to apply </param>
        public void ApplyTransform(Transform transform)
        {
            for (int idx = 0; idx < values.Length; idx++)
                values[idx] = transform * values[idx];
            RefreshCurrentValue();
        }

        /// <summary>
        /// Interpolates between time steps.  If the time point is outside 
        /// the time line, the closest transform is returned
        /// </summary>
        /// <param name="timePt"> time point to interpolate to </param>
        /// <returns> Interpolated transform </returns>
        public override Transform InterpolateToTime(double timePt)
        {
            // Because Transform isn't cover by expressions,
            // we need to explicitly express the interpolation

            if (values.Length == 0)
                throw new System.InvalidOperationException("There are no keys to interpolate.");
            if (double.IsNaN(timePt))
                throw new System.ArgumentException("The interpolation time must not be NaN.", "timePt");
            // Avoid redundancy by saving the current time's transform
            if (timePt == currentTime && Animate == currentAnimate)
                return currentValue;
            currentTime = timePt;
            currentAnimate = Animate;

            // Outside bounds handler
            if (timePt <= times[0] || !Animate)
                currentValue = values[0];
            else if (timePt >= times.Last())
                currentValue = values.Last();
            else
            {
                // Finds closest matches
                int idx = 0;
                do { } while (times[++idx] <= timePt);
                idx--;

                // If on-point, don't interpolate
                if (timePt == times[idx])
                    currentValue = values[idx];
                else // Linear interpolation
                {
                    // Interpolation factor
                    double fac = (timePt - times[idx]) /
                         (times[idx + 1] - times[idx]);

                    // Calls SLERP
                    currentValue = Maths.Interpolate.Linear(values[idx],
                        values[idx + 1], fac);
                }
            }
            return currentValue;
        }

        /// <summary>
        /// Returns a clone of this KeyFramePath by performing a deep copy
        /// </summary>
        /// <returns> KeyFramePath clone </returns>
        new public KeyFrameTrans Clone()
        {
           return new KeyFrameTrans(values, times, Interpolation, Animate);
        }

        # endregion Methods
    }
}

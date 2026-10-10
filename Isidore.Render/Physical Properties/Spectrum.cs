using System;
using System.Collections.Generic;

namespace Isidore.Render
{
    /// <summary>
    /// Lowest level spectrum interface.  Sample and Value types can be different types
    /// </summary>
    public interface ISpectrum
    {
        /// <summary>
        /// Sample array data type
        /// </summary>
        Type TypeSample { get; }

        /// <summary>
        /// Value data type
        /// </summary>
        Type TypeValue { get; }

        /// <summary>
        /// Sample data object (value)
        /// </summary>
        object Sample { get; set; }

        /// <summary>
        /// Value data object (value)
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// Cloning by deep copy
        /// </summary>
        /// <returns> Deep copy </returns>
        ISpectrum Clone();
    }

    /// <summary>
    /// Spectrum interface
    /// </summary>
    /// <typeparam name="SampleType"> Sample data array type </typeparam>
    /// <typeparam name="ValueType"> Value data array type </typeparam>
    public interface ISpectrum<SampleType, ValueType> : ISpectrum
    {
        /// <summary>
        /// Sample data array
        /// </summary>
        new SampleType[] Sample { get; set; }

        /// <summary>
        /// Values data array
        /// </summary>
        new ValueType[] Value { get; set; }

        /// <summary>
        /// Cloning by deep copy
        /// </summary>
        /// <returns> Deep copy </returns>
        new ISpectrum Clone();

    }

    /// <summary>
    /// Spectrum class is used to record any 1D data array with a sample spacing
    /// </summary>
    /// <typeparam name="SampleType"> Sample data array type </typeparam>
    /// <typeparam name="ValueType"> Value data array type </typeparam>
    public class Spectrum<SampleType, ValueType> : ISpectrum<SampleType, ValueType>
    {
        #region Fields & Properties

        /// <summary>
        /// The position in the spectrum of individual values
        /// </summary>
        protected SampleType[] sample;

        /// <summary>
        /// The quantity of the spectrum at each discrete location
        /// </summary>
        protected ValueType[] value;

        /// <summary>
        /// Sample data type
        /// </summary>
        public Type TypeSample
        {
            get { return typeof(SampleType); }
        }

        /// <summary>
        /// Value data type
        /// </summary>
        public Type TypeValue
        {
            get { return typeof(ValueType); }
        }

        /// <summary>
        /// Spectrum Sample interface
        /// </summary>
        object ISpectrum.Sample
        {
            get { return sample; }
            set { sample = (SampleType[])value; }
        }

        /// <summary>
        /// Spectrum Value interface
        /// </summary>
        object ISpectrum.Value
        {
            get { return value; }
            set { this.value = (ValueType[])value; }
        }

        /// <summary>
        /// Sample data type array interface
        /// </summary>
        SampleType[] ISpectrum<SampleType, ValueType>.Sample
        {
            get { return sample; }
            set { sample = value; }
        }

        /// <summary>
        /// Value data type array interface
        /// </summary>
        ValueType[] ISpectrum<SampleType, ValueType>.Value
        {
            get { return value; }
            set { this.value = value; }
        }

        /// <summary>
        /// The accessors for individual values positions in the spectrum
        /// </summary>
        public SampleType[] Sample
        {
            get { return sample; }
            set { sample = value; }
        }

        /// <summary>
        /// The accessor for individual data points in the spectrum
        /// </summary>
        public ValueType[] Value
        {
            get { return value; }
            set { this.value = value; }
        }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Spectrum Constructor
        /// </summary>
        /// <param name="Sample"> Spectrum sample points </param>
        /// <param name="Value"> Spectrum values at each sample </param>
        public Spectrum(SampleType[] Sample = null, ValueType[] Value = null)
        {
            if (Sample == null && Value == null)
                return;
            if (Sample == null || Value == null)
                throw new ArgumentException("Sample and Value must both be supplied.");
            // Checks that Sample and Value are the same length, throws if not
            if (Sample.Length != Value.Length)
            {
                throw new System.ArgumentException("Sample and Value must be the same length.", "Sample");
            }

            // Deep copies each array to avoid unintended referencing
            sample = (SampleType[])Sample.Clone();
            value = (ValueType[])Value.Clone();
        }

        /// <summary>
        /// Single value spectrum constructor (delta function)
        /// </summary>
        /// <param name="Sample"> Spectrum sample point </param>
        /// <param name="Value"> Spectrum value at the sample </param>
        public Spectrum(SampleType Sample, ValueType Value):this(
            new SampleType[] { Sample}, new ValueType[] { Value})
        {
        }

        /// <summary>
        /// Spectrum copy constructor
        /// </summary>
        /// <param name="spectrum"> Spectrum instance to copy </param>
        public Spectrum(Spectrum<SampleType, ValueType> spectrum):this(
            spectrum.sample, spectrum.value)
        {
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        public ISpectrum Clone()
        {
            // Copy types to spectrum
            Spectrum<SampleType, ValueType> newSpec = new Spectrum<SampleType, ValueType>(this.sample, this.value);
            return newSpec;
        }

        //public ValueType Retrieve(SampleType value, )

        #endregion Methods
    }

    /// <summary>
    /// A list of spectrums
    /// </summary>
    public class Spectrums : List<ISpectrum>
    {
        /// <summary>
        /// Deep copy clones every spectrum in this list
        /// </summary>
        /// <returns> Clone of base </returns>
        public Spectrums Clone()
        {
            Spectrums newList = new Spectrums();
            ForEach(item =>
                {
                    newList.Add(item.Clone());
                });

            return newList;
        }
    }
}

using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Xml.Linq;
using Isidore.Maths;

namespace Isidore.Render
{

    /// <summary>
    /// IntersectData provides an standard for reporting shape and surface interactions.
    /// All data is assumed to be in world coordinates.  There is no transform.
    /// </summary>
    public class IntersectData
    {
        # region Fields & Properties

        /// <summary>
        /// Reports if the ray intersected any shape
        /// </summary>
        public bool Hit;

        /// <summary>
        /// Travel distance to intersection
        /// </summary>
        public double Travel;

        /// <summary>
        /// Intersection point
        /// </summary>
        public Point IntersectPt;

        /// <summary>
        /// Reference to the body intersected
        /// (Assigned in Body base class).
        /// </summary>
        public Body Body;

        /// <summary>
        /// Intersection data that is specific to each subclass
        /// of the body class
        /// </summary>
        public BodySpecificData BodySpecificData;

        /// <summary>
        /// Reference to the shape intersected
        /// </summary>
        public Properties Properties = new Properties();

        /// <summary>
        /// Rays casted by a Surface class object
        /// </summary>
        public RenderRays CastedRays = new RenderRays();

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hit"> Indicates the ray intersects a shape </param>
        /// <param name="travel"> Distance traveled until intersection </param>
        /// <param name="intersectPt"> Point of intersection (World space) </param>
        /// <param name="body"> Reference to the body intersected </param>
        /// <param name="bodyData"> Intersection data specific to the body
        /// instance intersection</param>
        public IntersectData(bool hit = false, 
            double travel = double.PositiveInfinity,
            Point intersectPt = null, Body body = null,
            BodySpecificData bodyData = null)
        {
            Hit = hit;
            Travel = travel;
            IntersectPt = intersectPt ?? Point.NaN();
            Body = body;
            BodySpecificData = bodyData ?? new BodySpecificData();
        }

        # endregion Constructors
        # region Methods

        /// <summary>
        /// Retrieves the value from the field named in the string input
        /// </summary>
        /// <typeparam name="T"> Data type of the field being 
        /// returned </typeparam>
        /// <param name="fieldName"> Name of the field to return the 
        /// value of </param>
        /// <returns> Field value </returns>
        public T GetFieldValue<T>(string fieldName = "Hit")
        {
            // Checks to see if it's at the top level
            FieldInfo finfo = GetType().GetField(fieldName);
            if (finfo != null)
            {
                object value = finfo.GetValue(this);
                return (T)value;
            }

            // If not, checks the body
            if (Body != null)
            {
                finfo = Body.GetType().GetField(fieldName);
                if (finfo != null)
                {
                    object value = finfo.GetValue(Body);
                    return (T)value;
                }
            }

            // If not, checks the body specific data
            if (BodySpecificData != null)
            {
                finfo = BodySpecificData.GetType().GetField(fieldName);
                if (finfo != null)
                {
                    object value = finfo.GetValue(BodySpecificData);
                    return (T)value;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Retrieves the value from the field or property named in the 
        /// string input
        /// </summary>
        /// <typeparam name="T"> Data type of the field being 
        /// returned </typeparam>
        /// <param name="fieldName"> Name of the member to return the 
        /// value of (Can include subfields) </param>
        /// <returns> Field/property value </returns>
        public T GetValue<T>(string fieldName = "Hit")
        {
            // Splits fieldname string into an array of field strings
            string[] fieldnames = fieldName.Split('.');

            // Retrieves top level field info
            Type type = GetType();
            FieldInfo finfo = type.GetField(fieldnames[0]);
            PropertyInfo pinfo = type.GetProperty(fieldnames[0]);

            // If the field is not present then returns
            if (finfo == null && pinfo == null)
                return default(T);

            // Retreives the top level field value as an object
            object value;
            if(finfo != null)
                value = finfo.GetValue(this);
            else
                value = pinfo.GetValue(this);

            // Repeats the operation through each sub-field 
            for (int idx = 1; idx < fieldnames.Length; idx++)
            {
                if (value == null)
                    return default(T);

                type = value.GetType();                
                finfo = type.GetField(fieldnames[idx]);
                pinfo = type.GetProperty(fieldnames[idx]);
                if (finfo == null && pinfo == null)
                    return default(T);
                if (finfo != null)
                    value = finfo.GetValue(value);
                else
                    value = pinfo.GetValue(value);
            }

            return value == null ? default(T) : (T)value;
        }

        /// <summary>
        /// Retrieves a reference to the property entry that matches the 
        /// given type
        /// </summary>
        /// <typeparam name="T"> Type to return </typeparam>
        /// <returns> A reference to the property of type T </returns>
        public Property GetProperty<T>()
        {
            for(int idx =0; idx<Properties.Count; idx++)
                if(Properties[idx].GetType().Equals(typeof(T)))
                    return Properties[idx];

            return null;
        }

        /// <summary>
        /// Retrieves a reference to value in the field of the property entry 
        /// that matches the given type
        /// </summary>
        /// <typeparam name="T"> Type of data to return </typeparam>
        /// <typeparam name="propT"> Property type to locate </typeparam>
        /// <param name="fieldName"> Name of the field to return the 
        /// value of </param>
        /// <returns> The fields value in the property entry </returns>
        public T GetPropertyData<T,propT>(string fieldName)
        {
            Type propType = typeof(propT);

            // Locates the property
            Property prop = null;
            int cnt = 0;
            while (prop == null && cnt < Properties.Count)
            {
                if (Properties[cnt].GetType().Equals(propType))
                    prop = Properties[cnt];
                cnt++;
            }

            // Returns if nothing is found
            if (prop == null)
                return default(T);

            // Otherwise, searches for the field
            // Checks to see if it's at the top level
            T val = prop.GetData<T>(fieldName);

            return val;
        }

        /// <summary>
        /// Returns a clone of this intersect data class via deep copy
        /// </summary>
        /// <returns> The cloned copy </returns>
        public IntersectData Clone()
        {
            IntersectData copy = CloneFields();
            if (CastedRays != null)
                copy.CastedRays = CastedRays.Clone();
            return copy;
        }

        internal IntersectData CloneFields()
        {
            IntersectData NewInst = new IntersectData();
            NewInst.Hit = Hit;
            NewInst.Travel = Travel;
            NewInst.IntersectPt = IntersectPt.Clone();
            NewInst.Body = Body;

            if (BodySpecificData != null)
                NewInst.BodySpecificData = BodySpecificData.Clone();

            if (Properties != null)
                NewInst.Properties = Properties.Clone();
            NewInst.CastedRays = CastedRays;
            return NewInst;
        }

        # endregion Methods
    }
}

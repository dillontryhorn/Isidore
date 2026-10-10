using System.Collections.Generic;
using System.Linq;
using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Projectors propagate rays over space.  RaysLocal are the rays in local
    /// projector space while RaysWorld are the same rays in world space.
    /// Only RaysLocal can be set, and only RayWorld are used within the renderer.
    /// </summary>
    /// 
    public class Projector : Item
    {
        # region Fields & Properties 

        /// <summary>
        /// Projection pixel ray in local projector space
        /// </summary>
        public RenderRay[] LocalRays { get { return raysLocal; } set { raysLocal = value; } }
        /// <summary>
        /// Projection pixel rays in local projector space
        /// </summary>
        protected internal RenderRay[] raysLocal;

        /// <summary>
        /// Projection pixel rays in world space.  Consider this true ray
        /// </summary>
        public RayTree[] Rays { get { return rays; } set { rays = value; } }
        /// <summary>
        /// Projected pixel rays in world space
        /// </summary>
        protected internal RayTree[] rays;

        /// <summary>
        /// The depth limit of each ray tree
        /// </summary>
        public int RayTreeDepthLimit;

        #endregion Fields & Properties
        #region Constructor

        /// <summary>
        /// General constructor.  Rays array should be in local projector 
        /// space. 
        /// </summary>
        /// <param name="Rays"> Array representing each pixel's optical 
        /// axis </param>
        /// <param name="TimeLine"> Animated transformation time line </param>
        /// <param name="RayTreeDepthLimit"> Depth limit allowed for a ray 
        /// tree </param>
        /// <param name="Name"> A descriptive name for this projector </param>
        public Projector(RenderRay[] Rays = null, KeyFrameTrans 
            TimeLine = null, int RayTreeDepthLimit = 10,string Name = "")
        {
            // Clones all rays but not ray trees
            if (Rays != null)
            {
                raysLocal = new RenderRay[Rays.Length];
                for (int idx = 0; idx < Rays.Length; idx++)
                    raysLocal[idx] = Rays[idx].Clone();
            }

            // Clones time line.  If null, makes a new static path
            TransformTimeLine = TimeLine ?? new KeyFrameTrans();
            this.RayTreeDepthLimit = RayTreeDepthLimit;
            this.Name = Name;
        }

        # endregion Constructor
        # region Methods

        /// <summary>
        /// Sets the projector to its state at time "now" by updating the rays
        /// in world space </summary>
        /// <param name="now"> Time of which to set the state of the 
        /// object </param>
        /// <param name="force"> Forces AdvanceToTime to run even if the time
        /// is the same </param>
        override public void AdvanceToTime(double now, bool force = false)
        {
            // Returns if this projector is already set to now
            if (!force && now == CurrentTime)
                return;

            // Sets projection local2world transform
            base.AdvanceToTime(now, force); 

            // Constructs new ray tree array by clone copying the local ray
            rays = new RayTree[raysLocal.Length];
            for (int idx = 0; idx < raysLocal.Length; idx++)
            {
                // Copies ray tree
                rays[idx] = new RayTree(
                    raysLocal[idx].CopyTransform(Local2World));
                // Sets tree depth limit
                rays[idx].DepthLimit = RayTreeDepthLimit;
                // Assigns ray initialization time
                rays[idx].rays[0].Time = now;
            }
        }

        /// <summary>
        /// Adds a clone of the property instance to every render ray
        /// </summary>
        /// <param name="property"> Property to clone and add </param>
        public void AddProperty(Property property)
        {
            for (int idx = 0; idx < raysLocal.Length; idx++)
                raysLocal[idx].Properties.Add(property.Clone());
        }


        /// <summary>
        /// Retrieves a ray in projector/camera space.  
        /// Used by child projectors.
        /// </summary>
        /// <param name="Idx"> Render Ray's index </param>
        /// <returns> The ray in projector/camera space </returns>
        public RenderRay LocalRay(int Idx)
        {
            return LocalRays[Idx];
        }

        /// <summary>
        /// Searches ray tree array for open raytrees
        /// </summary>
        /// <returns> True if any ray tree is open </returns>
        public bool AnyOpenRayTrees()
        {
            foreach (RayTree rayTree in rays)
                if (rayTree.Open)
                    return true;
            return false;
        }

        /// <summary>
        /// Retrieves a ray tree for tracing in world space. 
        /// Used by child projectors.
        /// </summary>
        /// <param name="Idx"> Raytree's index </param>
        /// <returns> The ray in world space </returns>
        public RayTree Ray(int Idx)
        {
            return Rays[Idx];
        }

        /// <summary>
        /// Updates the projector by updating all raytrees,
        /// which are associated with each projector pixel
        /// </summary>
        public void UpdateRayTrees()
        {
            foreach (RayTree rayTree in rays)
                rayTree.Update();
        }

        /// <summary>
        /// Retrieves the value from the field named in the intersect
        /// data given in the string input for the indexed ray in each
        /// elements ray tree
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="fieldName"> Name of the field to return the 
        /// value of </param>
        /// <param name="index"> Index of the property in the ray tree</param>
        /// <returns> An array of field values corresponding to each 
        /// projector ray </returns>
        virtual public T[] GetIntersectValue<T>(string fieldName, 
            int index)
        {
            T[] iValue = new T[rays.Length];
            for (int idx = 0; idx < rays.Length; idx++)
                if(index < rays[idx].Rays.Count)
                    iValue[idx] = rays[idx].Rays[index].
                        IntersectData.GetFieldValue<T>(fieldName);
            return iValue;
        }

        /// <summary>
        /// Retrieves the value from the field named in the intersect
        /// data given in the string input for the last ray in each
        /// elements ray tree
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="fieldName"> Name of the field to return the 
        /// value of </param>
        /// <returns> An array of field values corresponding to each 
        /// projector ray </returns>
        virtual public T[] GetIntersectValue<T>(string fieldName)
        {
            T[] iValue = new T[rays.Length];
            for (int idx = 0; idx < rays.Length; idx++)
                iValue[idx] = rays[idx].Rays.Last().
                    IntersectData.GetFieldValue<T>(fieldName);
            return iValue;
        }


        /// <summary>
        /// Retrieves the value from the field named in the intersect
        /// data given in the string input for all rays in each
        /// elements ray tree
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <param name="fieldName"> Name of the field to return the 
        /// value of </param>
        /// <returns> An array of field values corresponding to each 
        /// projector ray </returns>
        virtual public T[][] GetIntersectValueTree<T>(string fieldName)
        {
            T[][] iValue = new T[rays.Length][];
            for (int idx = 0; idx < rays.Length; idx++)
            {
                iValue[idx] = new T[rays[idx].rays.Count];
                for (int ridx = 0; ridx < rays[idx].rays.Count; ridx++)
                    iValue[idx][ridx] = rays[idx].rays[ridx].
                        IntersectData.GetFieldValue<T>(fieldName);
            }
            return iValue;
        }

        /// <summary>
        /// Retrieves the property from the properties field in the intersect
        /// data for the indexed ray in each elements ray tree.  The type 
        /// determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <param name="index"> Index of the property in the ray tree</param>
        /// <returns> An array of property type data corresponding to each 
        /// projector ray </returns>
        virtual public Property[] GetProperty<T>(int index)
        {
            Property[] iP = new Property[rays.Length];
            for (int idx = 0; idx < rays.Length; idx++)
                iP[idx] = rays[idx].Rays[index].
                    IntersectData.GetProperty<T>();

            return iP;
        }

        /// <summary>
        /// Retrieves the property from the properties field in the intersect
        /// data for the last ray in each elements ray tree.  The type 
        /// determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <returns> An array of property type data corresponding to each 
        /// projector ray </returns>
        virtual public Property[] GetProperty<T>()
        {
            Property[] iP = new Property[rays.Length];
            for (int idx = 0; idx < rays.Length; idx++)
                iP[idx] = rays[idx].Rays.Last().
                    IntersectData.GetProperty<T>();

            return iP;
        }

        /// <summary>
        /// Retrieves the property from the properties field in the intersect
        /// data for all rays in each elements ray tree.  The type 
        /// determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type </typeparam>
        /// <returns> An array of property type data corresponding to each 
        /// projector ray </returns>
        virtual public Property[][] GetPropertyTree<T>()
        {
            Property[][] iP = new Property[rays.Length][];
            for (int idx = 0; idx < rays.Length; idx++)
            {
                iP[idx] = new Property[rays[idx].rays.Count];
                for (int ridx = 0; ridx < rays[idx].rays.Count; ridx++)
                    iP[idx][ridx] = rays[idx].rays[ridx].
                        IntersectData.GetProperty<T>();
            }

            return iP;
        }

        /// <summary>
        /// Retrieves the property field data from the properties in the 
        /// intersect data for the indexed ray in each elements ray tree.  
        /// The type propT determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <typeparam name="propT"> Property type to locate </typeparam>
        /// <param name="name"> Name of the property field to return </param>
        /// <param name="index"> Index of the property in the ray tree</param>
        /// <returns> An array of property field data corresponding to each 
        /// projector ray </returns>
        virtual public T[] GetPropertyData<T,propT>(string name, int index)
        {
            Property[] iP = GetProperty<propT>(index);

            T[] iD = new T[iP.Length];
            for (int idx = 0; idx < rays.Length; idx++)
                if (iP[idx] != null)
                    iD[idx] = iP[idx].GetData<T>(name);

            return iD;
        }

        /// <summary>
        /// Retrieves the property field data from the properties in the 
        /// intersect data for the last ray in each elements ray tree.  
        /// The type propT determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <typeparam name="propT"> Property type to locate </typeparam>
        /// <param name="name"> Name of the property field to return </param>
        /// <returns> An array of property field data corresponding to each 
        /// projector ray </returns>
        virtual public T[] GetPropertyData<T, propT>(string name)
        {
            Property[] iP = GetProperty<propT>();

            T[] iD = new T[iP.Length];
            for (int idx = 0; idx < rays.Length; idx++)
                if (iP[idx] != null)
                    iD[idx] = iP[idx].GetData<T>(name);

            return iD;
        }

        /// <summary>
        /// Retrieves the property field data from the properties in the 
        /// intersect data for all rays in each elements ray tree.  
        /// The type propT determines the entry returned.
        /// </summary>
        /// <typeparam name="T"> Data type to retrieve </typeparam>
        /// <typeparam name="propT"> Property type to locate </typeparam>
        /// <param name="name"> Name of the property field to return </param>
        /// <returns> An array of property type data corresponding to each 
        /// projector ray </returns>
        virtual public T[][] GetPropertyDataTree<T, propT>(string name)
        {
            Property[][] iP = GetPropertyTree<propT>();

            T[][] iD = new T[iP.Length][];
            for (int idx = 0; idx < rays.Length; idx++)
            {
                iD[idx] = new T[rays[idx].rays.Count];
                for (int ridx = 0; ridx < rays[idx].rays.Count; ridx++)
                    if (iP[idx][ridx] != null)
                        iD[idx][ridx] = iP[idx][ridx].GetData<T>(name);
            }
            return iD;
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        new public Projector Clone()
        {
            return (Projector)CloneImp();
        }

        /// <summary>
        /// Deep-copy clones this instance
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        protected override Item CloneImp()
        {
            Projector newCopy = (Projector)MemberwiseClone();

            // Deep copy
            DeepCopyOverride(ref newCopy);

            return newCopy;
        }

        /// <summary>
        /// Implements deep copies of members that would
        /// otherwise be shallow copied.
        /// </summary>
        /// <param name="copy"> Clone copy </param>
        protected void DeepCopyOverride(ref Projector copy)
        {
            // Base copy
            Item baseCast = copy; // This is a shallow copy
            base.DeepCopyOverride(ref baseCast);

            //protected internal RenderRay[] raysLocal;
            if (raysLocal != null)
            {
                copy.raysLocal = new RenderRay[raysLocal.Length];
                for (int idx = 0; idx < raysLocal.Length; idx++)
                    copy.raysLocal[idx] = raysLocal[idx].Clone();
            }

            //protected internal RayTree[] rays;
            if (rays != null)
            {
                copy.rays = new RayTree[rays.Length];
                for (int idx = 0; idx < rays.Length; idx++)
                    copy.rays[idx] = rays[idx].Clone();
            }

        }

        # endregion Methods
    }

    /// <summary>
    /// List for projector instances
    /// </summary>
    public class Projectors : List<Projector>
    {
        /// <summary>
        /// Maximum value of the array
        /// (This is the default value of item.IDs)
        /// </summary>
        private int maxVal = -1;

        /// <summary>
        /// Adds an object to the list.  Automatically checks indices
        /// for null value (-1) or duplicates.
        /// </summary>
        /// <param name="item"> The object to add to the list. </param>
        new public void Add(Projector item)
        {
            // Checks & corrects the ID
            // If ID > maxVal, updates maxID
            if (item.ID > maxVal)
            {
                maxVal = item.ID;
            }
            else
            {
                // If null (-1), then increments & assigns maxVal
                if (item.ID == -1)
                    item.ID = ++maxVal;
                // Otherwise Checks for duplicates
                else
                    for (int idx = 0; idx < Count; idx++)
                        if (item.ID == this[idx].ID)
                        {
                            item.ID = ++maxVal;
                            continue;
                        }
            }

            // Adds to list
            base.Add(item);
        }

        /// <summary>
        /// Clones this instance by performing a deep copy
        /// </summary>
        /// <returns> Clone copy of this instance </returns>
        public Projectors Clone()
        {

            Projectors newList = new Projectors();

            ForEach(n => newList.Add(n.Clone()));

            return newList;
        }
    }
}

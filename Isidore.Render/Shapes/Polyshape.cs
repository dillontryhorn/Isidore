using Isidore.Maths;

namespace Isidore.Render
{
    /// <summary>
    /// Represents a polyshape, which is a list of shapes.  This shape will
    /// apply the materials of the intersected component shape.  If it is
    /// empty, then it will use its own material.  This was a single bulk
    /// material can be applied to all components.
    /// </summary>
    public class Polyshape : Shape
    {

        # region Fields & Properties

        /// <summary>
        /// Shapes constituting this Polyshape
        /// </summary>
        public Shapes Shapes;

        /// <summary>
        /// Animation transform for this polyshape
        /// </summary>
        public override KeyFrameTrans TransformTimeLine
        {
            get { return base.TransformTimeLine; }
            set
            {
                // Sets this item's time line
                base.TransformTimeLine = value;
                // Sets each child's time line
                if (Shapes!=null)
                    for (int idx = 0; idx < Shapes.Count; idx++)
                        Shapes[idx].TransformTimeLine = value.Clone();
            }
        }

        /// <summary>
        /// Allows intersection to occur on the back side of shape manifolds
        /// </summary>
        public override bool IntersectBackFaces
        {
            get { return base.IntersectBackFaces; }
            set
            {
                // Sets this item's value
                base.IntersectBackFaces = value;
                // Sets each child's value
                if (Shapes != null)
                    for (int idx = 0; idx < Shapes.Count; idx++)
                        Shapes[idx].IntersectBackFaces = value;
            }
        }

        /// <summary>
        /// Flag for where to calculate the intersection UV coordinates
        /// </summary>
        public override bool CalculateUV
        {
            get { return base.CalculateUV; }
            set
            {
                // Sets this item's value
                base.CalculateUV = value;
                // Sets each child's value
                if (Shapes != null)
                    for (int idx = 0; idx < Shapes.Count; idx++)
                        Shapes[idx].CalculateUV = value;
            }
        }

        /// <summary>
        /// Switches Alpha Mapping on
        /// </summary>
        public override bool UseAlpha
        {
            get { return base.UseAlpha; }
            set
            {
                // Sets this item's value
                base.UseAlpha = value;
                // Sets each child's value
                if (Shapes != null)
                    for (int idx = 0; idx < Shapes.Count; idx++)
                        Shapes[idx].UseAlpha = value;
            }
        }

        #endregion Fields & Properties
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="shape"> Shape to place in the first entry in the 
        /// the polyshape </param>
        public Polyshape(Shape shape = null)
        {
            Shapes = new Shapes();
            if(shape != null)
                Shapes.Add(shape);
        }

        #endregion Constructors
        #region Methods

        /// <summary>
        /// Sets this instance's state vectors to time "now".  Applies
        /// this time and forcing flag to each entry on the list.
        /// </summary>
        /// <param name="now"> Time to set this instance to </param>
        /// <param name="force"> Forces AdvanceToTime to run even if the time
        /// is the same </param>
        override public void AdvanceToTime(double now = 0, bool force = false)
        {
            //Shapes.ForEach(shape => shape.AdvanceToTime(now, force));
            // Used a for loop so I can step into the loop
            for (int idx = 0; idx < Shapes.Count; idx++)
                Shapes[idx].AdvanceToTime(now, force);

            // At this point, this only provides transforms data
            base.AdvanceToTime(now, force);
        }

        /// <summary>
        /// Performs an intersection check and operation with a ray.
        /// Accesses the Intersect methods of the shapes in the list.
        /// </summary>
        /// <param name="ray"> Ray to evaluate for intersection </param>
        /// <returns> Intersect flag (true = intersection) </returns>
        public override bool Intersect(ref RenderRay ray)
        {
            // Checks to see if the object is on
            bool intersect = false;
            if (On)
                // Steps through component shapes
                for (int idx = 0; idx < Shapes.Count; idx++)
                {
                    bool thisIntersect = Shapes[idx].Intersect(ref ray);
                    if (!intersect && thisIntersect)
                        intersect = true;
                }

            return intersect;
        }

        /// <summary>
        /// Applies material effects to the intersect data structure. Uses
        /// the polyshape's materials if the component shape doesn't have any.
        /// </summary>
        /// <param name="ray"> Render ray instance </param>
        public override void ApplyMaterials(ref RenderRay ray)
        {
            Shape shape = (Shape)ray.IntersectData.Body;
            // Uses the intersected shape if it has materials
            // Otherwise uses the polyshape
            if (shape.Materials.Count > 0)
                shape.ApplyMaterials(ref ray);
            else
                base.ApplyMaterials(ref ray);
        }

        /// <summary>
        /// Adds a Shape to the end of the Shapes List.
        /// </summary>
        /// <param name="shape"> Shape to add to the list </param>
        public void Add(Shape shape)
        {
            Shapes.Add(shape);
        }

        /// <summary>
        /// Removes the element at the specified index of the Shapes List
        /// </summary>
        /// <param name="index"> The zero-based index of the element to remove </param>
        public void RemoveAt(int index)
        {
            Shapes.RemoveAt(index);
        }

        /// <summary>
        /// Deep-copies the parent state without broadcasting its transform,
        /// then copies components with their own animations intact.
        /// </summary>
        protected override Item CloneImp()
        {
            Polyshape copy = (Polyshape)MemberwiseClone();
            copy.Shapes = new Shapes();
            Shape shapeCopy = copy;
            DeepCopyOverride(ref shapeCopy);
            copy.Shapes = Shapes.Clone();
            if (!double.IsNaN(CurrentTime))
                copy.AdvanceToTime(CurrentTime, true);
            return copy;
        }

        #endregion Methods
    }
}

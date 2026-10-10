using System;
using System.Collections.Generic;
using System.Linq;
using Isidore.Maths;

namespace Isidore.Load
{
    /// <summary>
    /// Loading class
    /// </summary>
    public partial class Load
    {
        
        /// <summary>
        /// NASTRAN Dat file loader
        /// </summary>
        /// <param name="fileName"> NAS Dat filename </param>
        /// <returns> Structure containing NAS data </returns>
        public static Data.NAS NAS(String fileName)
        {
            String[] lines = Text.Load(fileName);
            return NAS(lines);
        }

        /// <summary>
        /// NASTRAN Dat data formatter
        /// </summary>
        /// <param name="lines"> Lines containing NAS data </param>
        /// <returns> Structure containing NAS data </returns>
        public static Data.NAS NAS(String[] lines)
        {
            // Retrieves all data entry types useful for the 
            // geometric model
            Type enumType = typeof(FileRef.NAS.dataType); // 
            string[] entryNames = Enum.GetNames(enumType);
            
            // Records points as a list, will convert at end
            List<int> gridID = new List<int>();
            List<double[]> gridList = new List<double[]>();
            List<int> nodeModelID = new List<int>();
            List<int> nodeID = new List<int>();
            List<int[]> nodeList = new List<int[]>();
            List<string> modelList = new List<string>();

            // String for indicating a model name
            string modelStr = "$*  NX Mesh Collector: ";  

            // Looks for grid points and ctetra points, can add more  
            for (int idx = 0; idx < lines.Length; idx++ )
            {
                // Checks for any model information
                if (lines[idx].StartsWith(modelStr))
                {
                    modelList.Add(lines[idx].Substring(modelStr.Length).
                        TrimEnd(' '));
                    continue;
                }

                // Skips comment lines
                if(lines[idx].StartsWith("$")) continue;

                // Checks to see if type matches a geometry entry
                FileRef.NAS.dataType dataType = new 
                    FileRef.NAS.dataType();
                for (int eIdx = 1; eIdx < entryNames.Length; eIdx++ )
                {
                    // If text matches
                    if (lines[idx].StartsWith(entryNames[eIdx])) 
                    {
                        // Assigns dataType the value of the text, 
                        // this method maintains increments > 1
                        dataType = (FileRef.NAS.dataType)
                            Enum.Parse(enumType, entryNames[eIdx]); 
                        eIdx = entryNames.Length;
                    }
                }
                
                // Returns if entry isn't on the useful geometric list
                if (dataType == FileRef.NAS.dataType.None) continue;
                
                // Separates data entry into text string
                String[] fields = 
                    FileRef.NAS.Format.Separate(lines[idx]);

                // Checks for line continuations
                int last = fields.Length-1;
                while(fields[last].StartsWith("+") || 
                    fields[last].StartsWith("*"))
                {
                    if (idx + 1 >= lines.Length)
                        throw new FormatException("Missing NASTRAN continuation line.");
                    idx++; // Increments to next line
                    String[] nextFields = 
                        FileRef.NAS.Format.Separate(lines[idx]);

                    List<string> fieldsList = fields.ToList();
                    List<string> nextList = nextFields.ToList();
                    fieldsList.RemoveAt(last);
                    nextList.RemoveAt(0);
                    fieldsList.AddRange(nextList);
                    fields = fieldsList.ToArray();
                    last = fields.Length - 1;
                }

                // Grid point extraction
                if (dataType == FileRef.NAS.dataType.GRID)
                {
                    gridID.Add(int.Parse(
                        fields[FileRef.NAS.GRID.DataField.ID]));
                    gridList.Add(
                        FileRef.NAS.GRID.Location(fields));
                }

                // CTETRA patch extraction
                if (dataType == FileRef.NAS.dataType.CTETRA)
                {
                    nodeID.Add(int.Parse(
                        fields[FileRef.NAS.CTETRA.DataField.ID]));
                    nodeModelID.Add(int.Parse(
                        fields[FileRef.NAS.CTETRA.DataField.PSOLID]));
                    nodeList.Add(FileRef.NAS.CTETRA.Patch(fields));
                }

            }

            // Data for geometric data
            Data.NAS GeomModel = new Data.NAS();
            GeomModel.Grid.ID = gridID.ToArray();
            GeomModel.Grid.Position = 
                Function.Jagged2Array(gridList.ToArray());
            GeomModel.Node.ID = nodeID.ToArray();
            GeomModel.Node.ModelID = nodeModelID.ToArray();
            GeomModel.Node.Vertices = 
                Function.Jagged2Array(nodeList.ToArray());
            GeomModel.Node.ModelList = modelList.ToArray();

            return GeomModel;
        }


# region References

# endregion
    }
}

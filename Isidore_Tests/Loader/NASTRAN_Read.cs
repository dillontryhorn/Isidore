using System;
using System.IO;
using Isidore.Load;

namespace Isidore_Tests
{
    class NASTRAN_Read
    {

        public static bool Run()
        {
            // Test fixtures are copied next to the executable so callers do
            // not need to launch the test from a particular directory.
            String fileName = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Inputs", "NASTRAN Files", "Sphere-000.dat");

            // Loads data
            Data.NAS geomData = Load.NAS(fileName);

            // MatLab Exchange
            MLApp.MLApp matlab = new MLApp.MLApp();
            String strAppDir = new FileInfo(System.Windows.Forms.Application.ExecutablePath).DirectoryName;
            String res = matlab.Execute("clear;");
            res = matlab.Execute("cd('" + strAppDir + "');");
            matlab.PutWorkspaceData("gridID", "base", geomData.Grid.ID);
            matlab.PutWorkspaceData("gridPos", "base", geomData.Grid.Position);
            matlab.PutWorkspaceData("nodesID", "base", geomData.Node.ID);
            matlab.PutWorkspaceData("nodeModelID", "base", geomData.Node.ModelID);
            matlab.PutWorkspaceData("nodeVertexID", "base", geomData.Node.Vertices);
            matlab.Execute("figure;plot3(gridPos(:,1),gridPos(:,2),gridPos(:,3),'.')");
            matlab.Execute("save NASTRAN_Read.mat");

            return true;
        }
    }
}

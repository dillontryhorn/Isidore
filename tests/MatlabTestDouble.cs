// Used only by RegressionTests.csproj. Production retains the real MATLAB COM reference.
using System;
using System.Collections.Generic;

namespace MLApp
{
    public class MLApp
    {
        public static MLApp LastCreated;
        public static object NextReadValue;
        public static bool ThrowOnRead;
        public readonly Dictionary<string, object> Workspace = new Dictionary<string, object>();
        public readonly List<string> Commands = new List<string>();
        public readonly List<Tuple<string, object>> Transfers = new List<Tuple<string, object>>();
        public bool QuitCalled;

        public MLApp() { LastCreated = this; }

        public void PutWorkspaceData(string name, string workspace, object value)
        {
            Workspace[name] = value;
            Transfers.Add(Tuple.Create(name, value));
        }

        public string Execute(string command)
        {
            Commands.Add(command);
            if (command.StartsWith("isThere = exist('"))
            {
                string name = command.Split('\'')[1];
                Workspace["isThere"] = Workspace.ContainsKey(name) ? 1.0 : 0.0;
            }
            return "";
        }

        public dynamic GetVariable(string name, string workspace)
        {
            if (ThrowOnRead)
                throw new InvalidOperationException("Simulated COM read failure.");
            object value;
            return Workspace.TryGetValue(name, out value) ? value : NextReadValue;
        }

        public void Quit() { QuitCalled = true; }
    }
}

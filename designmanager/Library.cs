using LS_API;
using Symyx.ClientUtilities.Interop;
using Symyx.LSDesignMgr.Interop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Security.Permissions;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace DesignManager
{
    public class Library
    {
        private LSAPI _lsAPI;
        private string _initLocation;
        private string _location;

        public string name { get; private set; }
        public uint color { get; private set; }
        public int rows { get; private set; }
        public int cols { get; private set; }
        public string dispenseMode { get; private set; } = null;
        public string tip { get; set; } = null;
        public double size { get; private set; }
        public bool cap { get; set; }
        public string initCapState { get; private set; } = "None";
        public string capRackLocation { get; private set; }
        public string type { get; private set; }
        public int id { get; set; }
        public bool isSource { get; private set; } = false;

        public Library(LSAPI lsAPI, string name, int rows, int cols, double size, bool cap, string capRackLocation)
        {
            _lsAPI = lsAPI;
            this.name = name;
            this.rows = rows;
            this.cols = cols;
            this.size = size;
            this.cap = cap;
            this.capRackLocation = capRackLocation;
            this.type = Utils.SizeToType(this.size);
            this.color = 255;

            int resCode = _lsAPI.AddLibrary(this.name, nRows: this.rows, nCols: this.cols, color: this.color);
            if (resCode < 0)
            {
                Console.WriteLine("Failed to add lib: " + _lsAPI.GetErrorMessage(resCode));
            }
        }

        public void AddChemical(
            Chemical chemical,
            string unit,
            List<double> values,
            List<Tuple<int, int>> pos,
            int idx,
            List<string> tags)
        {
            Unit lsUnit;
            Enum.TryParse<Unit>(unit, out lsUnit);

            int resCode = _lsAPI.AddSourceMap(
                chemicalName: chemical.name,
                mapType: "Discrete",
                unit: lsUnit,
                value: 0,
                values: values,
                positions: pos,
                libName: this.name,
                tagList: string.Join(",", ProcessDummyTag(tags)),
                layerIdx: idx,
                mapIdx: 1);
            if (resCode < 0)
            {
                Console.WriteLine("" + resCode + " " + pos[0].Item1 + "," + pos[0].Item2);
                Console.WriteLine("Failed to add source map: " + _lsAPI.GetErrorMessage(resCode));
            }

            foreach (var value in values)
            {
                if (unit.Equals("ul"))
                {
                    chemical.Dispense(value * 1e-3);
                }
                else
                {
                    chemical.Dispense(value);
                }
            }
        }

        public void AddArraySource(
            Library sourceLib,
            string unit,
            Tuple<int, int> sourceStart,
            Tuple<int, int> sourceEnd,
            Tuple<int, int> destStart,
            Tuple<int, int> destEnd,
            List<double> values,
            int idx,
            List<string> tags)
        {
            Unit lsUnit;
            Enum.TryParse<Unit>(unit, out lsUnit);

            int resCode = _lsAPI.AddArrayMap(
                sourceLib: sourceLib.name,
                destLib: this.name,
                mapType: "Discrete",
                unit: lsUnit,
                sourceStart: new Point(sourceStart.Item1, sourceStart.Item2),
                sourceEnd: new Point(sourceEnd.Item1, sourceEnd.Item2),
                destStart: new Point(destStart.Item1, destStart.Item2),
                destEnd: new Point(destEnd.Item1, destEnd.Item2),
                amount: values[0],
                amountList: values,
                tagList: string.Join("|", ProcessDummyTag(tags)),
                layerIdx: idx);
            if (resCode < 0)
            {
                Console.WriteLine(resCode);
                Console.WriteLine("Failed to add array map: " + _lsAPI.GetErrorMessage(resCode));
            }

            sourceLib.SetAsSource();
        }

        public void AddParameter(
            Parameter parameter,
            List<Tuple<int, int>> pos,
            int idx,
            object value = null,
            List<object> values = null)
        {
            int resCode;
            if (values == null)
            {
                resCode = _lsAPI.AddParameterMap(
                    parameterName: parameter.name,
                    mapType: "Uniform",
                    unit: parameter.GetUnit(),
                    value: parameter.GetDefaultValue() == null ? value : parameter.GetDefaultValue(),
                    positions: pos,
                    libName: this.name,
                    tagList: string.Join("|", ProcessDummyTag(parameter.GetTags())),
                    layerIdx: idx,
                    mapIdx: 1);
            }
            else
            {
                resCode = _lsAPI.AddParameterMap(
                    parameterName: parameter.name,
                    mapType: "Discrete",
                    unit: parameter.GetUnit(),
                    value: values[0],
                    values: values,
                    positions: pos,
                    libName: this.name,
                    tagList: string.Join("|", ProcessDummyTag(parameter.GetTags())),
                    layerIdx: idx,
                    mapIdx: 1);
            }

            if (resCode < 0)
            {
                Console.WriteLine("" + resCode + " " + pos[0].Item1 + "," + pos[0].Item2);
                Console.WriteLine("Failed to add parameter map: " + _lsAPI.GetErrorMessage(resCode));
            }

            // extra logic to handle the moveplate action
            if (parameter.name.Equals("MovePlate"))
            {
                MoveTo((string)value);
            }

            // extra logic to handle capping and uncapping
            if (parameter.name.Equals("Cap"))
            {
                CapUncap(true);
            }
            if (parameter.name.Equals("Uncap"))
            {
                CapUncap(false);
            }
        }

        public void SetAsSource()
        {
            // mark this library as a source
            this.isSource = true;
            this.dispenseMode = "Stunner fix";
            this.tip = "PDT";
        }

        public void SetInitLocation(string location)
        {
            this._initLocation = location;
            this._location = location;
        }

        private void MoveTo(string location)
        {
            if (Positions.positions.ContainsKey(location))
            {
                this._location = location;
            }
            else
            {
                throw new ArgumentException(string.Format("move plate: {0} is an invalid deck position", location));
            }
        }

        private void CapUncap(bool mode)
        {
            // capping is mode = true, uncapping is mode = false
            if (mode == this.cap)
            {
                if (mode)
                {
                    throw new ArgumentException(string.Format("cap: attempting to cap already capped library {0}", this.name));
                }
                else
                {
                    throw new ArgumentException(string.Format("uncap: attempting to uncap already uncapped library {0}", this.name));
                }
            }

            if (this.initCapState.Equals("None"))
            {
                this.initCapState = mode ? "Uncapped" : "Capped";
            }

            this.cap = mode;
        }

        public string GetInitLocation()
        {
            return this._initLocation;
        }

        public string GetLocation()
        {
            return this._location;
        }

        private List<string> ProcessDummyTag(List<string> tags)
        {
            if (!tags.Contains("Dummy"))
            {
                tags.Add("Execute");
            }
            return tags;
        }
    }
}

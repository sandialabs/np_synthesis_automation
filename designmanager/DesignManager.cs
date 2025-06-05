using LS_API;
using Symyx.ClientUtilities.Interop;
using Symyx.LSDesignMgr.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using YamlDotNet.Core.Tokens;

namespace DesignManager
{
    public class DesignManager
    {
        private List<Chemical> _chemicals = new List<Chemical>();
        private List<Library> _libraries = new List<Library>();
        private List<Parameter> _parameters = new List<Parameter>();
        private Dictionary<string, bool> _positions = Positions.positions;
        private LSAPI _lsAPI;
        private string _name;
        private string _project;
        private int _mapIdx = 1;

        public int designID { get; private set; } = -1;

        public DesignManager(string name, string project)
        {
            _name = name;
            _project = project;
            _lsAPI = new LSAPI();

            int resCode = _lsAPI.CreateNewDesign(_name, _project, "", "", "", "", "", "");
            if (resCode < 0)
            {
                Console.WriteLine("Failed to create design: " + _lsAPI.GetErrorMessage(resCode));
            }
        }

        public void AddChemical(
            string name,
            uint color,
            string unit,
            string dispenseMode,
            string tip,
            bool backing = false,
            int valve = 0,
            string location = null,
            string capRackLocation = "",
            int row = 0,
            int col = 0,
            double size = 0,
            bool cap = false,
            double amt = 999999)
        {
            Chemical chemical = new Chemical(
                _lsAPI,
                name,
                color,
                unit,
                dispenseMode,
                tip,
                amt);
            chemical.location = location;

            if (backing)
            {
                chemical.SetBacking(valve);
            }
            else
            {
                chemical.SetDeck(row, col, size, cap, capRackLocation);
            }

            _chemicals.Add(chemical);
        }

        public void AddLibrary(
            string name,
            int rows,
            int cols,
            double size,
            bool cap,
            string location,
            string capRackLocation = "")
        {
            Library library = new Library(
                _lsAPI,
                name,
                rows,
                cols,
                size,
                cap,
                capRackLocation);
            library.SetInitLocation(location);
            _libraries.Add(library);
        }

        public void AddSourceMap(
            string libName,
            string sourceName,
            string unit,
            List<double> values,
            List<Tuple<int, int>> pos,
            List<string> tags)
        {
            Library library = GetLibrary(libName);
            Chemical chemical = GetChemical(sourceName);
            library.AddChemical(
                chemical,
                unit,
                values,
                pos,
                _mapIdx++,
                tags);
        }

        public void AddArrayMap(
            string libName,
            string sourceName,
            string unit,
            List<double> values,
            Tuple<int, int> sourceStart,
            Tuple<int, int> sourceEnd,
            Tuple<int, int> destStart,
            Tuple<int, int> destEnd,
            List<string> tags)
        {
            Library sourceLib = GetLibrary(sourceName);
            Library destLib = GetLibrary(libName);
            destLib.AddArraySource(
                sourceLib,
                unit,
                sourceStart,
                sourceEnd,
                destStart,
                destEnd,
                values,
                _mapIdx++,
                tags);
        }

        public void AddParameterMap(
            string libName,
            string parameterName,
            List<Tuple<int, int>> pos,
            object value = null,
            List<object> values = null)
        {
            // if the parameter doesn't exist yet, instantiate it so it's added to the design
            Parameter parameter = GetParameter(parameterName);
            if (parameter == null)
            {
                parameter = new Parameter(_lsAPI, parameterName);
                _parameters.Add(parameter);
            }

            Library library = GetLibrary(libName);
            library.AddParameter(
                parameter,
                pos,
                _mapIdx++,
                value: value,
                values: values);
        }

        public void WriteLSR(string fname)
        {
            bool ret = _lsAPI.SaveDesignToFile(fname);
            if (!ret)
            {
                Console.WriteLine("Failed to save file: " + fname);
            }
        }

        public void WriteDB()
        {
            this.designID = _lsAPI.SaveDesignToDatabase(true, true);
            Console.WriteLine(string.Format("Design id {0} saved to database", this.designID));

            // update library ids
            var savedLibraries = _lsAPI.GetLibraries();
            foreach (var library in _libraries)
            {
                foreach (var savedLibrary in savedLibraries)
                {
                    if (library.name.Equals(savedLibrary.Name))
                    {
                        library.id = savedLibrary.LibraryID;
                    }
                }
            }
        }

        public List<Chemical> GetChemicals()
        {
            return _chemicals;
        }

        public List<Library> GetLibraries()
        {
            return _libraries;
        }

        public Chemical GetChemical(string name)
        {
            foreach (var chemical in _chemicals)
            {
                if (chemical.name.Equals(name))
                {
                    return chemical;
                }
            }
            return null;
        }

        public Library GetLibrary(string name)
        {
            foreach (var library in _libraries)
            {
                if (library.name.Equals(name))
                {
                    return library;
                }
            }
            return null;
        }

        public Parameter GetParameter(string name)
        {
            foreach (var parameter in _parameters)
            {
                if (parameter.name.Equals(name))
                {
                    return parameter;
                }
            }
            return null;
        }

        public void WriteChemicalManager(string fname)
        {
            if (this.designID == -1)
            {
                Console.WriteLine("Warning: design has not been saved to the database. This will populate the chemical manager file with invalid libarary IDs");
            }
            ChemicalManager chemicalManager = new ChemicalManager(fname, this);
            chemicalManager.WriteXML();
        }
    }
}

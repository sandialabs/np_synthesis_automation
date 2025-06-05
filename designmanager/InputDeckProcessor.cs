using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Drawing;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Web;
using LS_API;
using YamlDotNet.Core.Tokens;
using System.ComponentModel;

namespace DesignManager
{
    public class InputDeckProcessor
    {
        private string _filepath;
        //private string _filepath = "C:\\LSAPI\\DesignManager\\test_input.yaml";
        private YamlStream _yaml;
        private DesignInput _designInput;
        private DesignManager _designManager;
        private static Dictionary<string, string> _paramNames = new Dictionary<string, string>()
        {
            { "cap", "Cap" },
            { "uncap", "Uncap" },
            { "delay", "Delay" },
            { "move", "MovePlate" },
            { "heat", "HeatingTemp" },
            { "temp_ramp", "SetTemperatureRampRate" },
            { "timer", "SetTimer" },
            { "wait", "WaitForTimer" },
            { "start_reaction_timer", "StartReactionTimer" },
            { "end_reaction_timer", "EndReactionTimer" },
            { "pause", "Pause" },
            { "stunner_application", "StunnerApplication" },
            { "start_stunner", "StartStunner" },
            { "stunner_sample_group", "StunnerSampleGroup" },
            { "stunner_sample_type", "StunnerSampleType" },
            { "stunner_analyte_name", "StunnerAnalyteName" },
            { "stunner_buffer_name", "StunnerBufferName" },
        };
        private static List<string> _stunnerParams = new List<string>() { "stunner_sample_group", "stunner_sample_type",
            "stunner_analyte_name", "stunner_buffer_name", "stunner_application" };

        public InputDeckProcessor(string filepath) 
        {
            this._filepath = filepath;
        }

        public void ReadYAML()
        {
            var input = new StreamReader(_filepath);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            _designInput = deserializer.Deserialize<DesignInput>(input);

        }

        public void ProcessYAML()
        {
            _designManager = new DesignManager(_designInput.design.name, _designInput.design.project);
            ProcessChemicals();
            ProcessLibraries();
            ProcessMaps();

            string basename = _designInput.design.name.Replace(" ", "_");
            _designManager.WriteDB();
            _designManager.WriteLSR(string.Format("{0}_{1}.lsr", basename, _designManager.designID));

            _designManager.WriteChemicalManager(string.Format("{0}_{1}_cm.xml", basename, _designManager.designID));

            ASPromptWriter promptWriter = new ASPromptWriter(string.Format("{0}_{1}_prompts.xml", basename, _designManager.designID), _designManager);
            promptWriter.WritePrompts();
        }

        private void ProcessChemicals()
        {
            foreach (var chemical in _designInput.chemicals)
            {
                uint color = (((uint)chemical.color[0]) << 16) | (((uint)chemical.color[1]) << 8) | (((uint)chemical.color[2]));
                if (chemical.type.Equals("backing"))
                {
                    _designManager.AddChemical(
                        chemical.name,
                        color,
                        chemical.unit,
                        chemical.dispense_type,
                        chemical.tip,
                        backing: true,
                        valve: chemical.valve_position);
                }
                else
                {
                    _designManager.AddChemical(
                        chemical.name,
                        color,
                        chemical.unit,
                        chemical.dispense_type,
                        chemical.tip,
                        backing: false,
                        location: Positions.DeckPosition(chemical.deck, chemical.position),
                        capRackLocation: Positions.DeckPosition(chemical.caprack_deck, chemical.caprack_position),
                        row: chemical.row,
                        col: chemical.col,
                        size: chemical.size,
                        cap: chemical.capped,
                        amt: chemical.amount);
                }
            }
        }

        private void ProcessLibraries()
        {
            foreach (var library in _designInput.libraries)
            {
                _designManager.AddLibrary(
                    library.name,
                    library.rows,
                    library.cols,
                    library.size,
                    library.capped,
                    Positions.DeckPosition(library.deck, library.position),
                    Positions.DeckPosition(library.caprack_deck, library.caprack_position));
            }
        }

        private void ProcessMaps()
        {
            foreach (var map in _designInput.maps)
            {
                // make sure the library exists
                Library library = _designManager.GetLibrary(map.library);
                if (library == null)
                {
                    throw new ArgumentException(string.Format("Library {0} is not recognized", map.library));
                }

                // handle different map types
                if (map.type.Equals("dispense"))
                {
                    if (!map.split)
                    {
                        List<Tuple<int, int>> pos = new List<Tuple<int, int>>();
                        List<double> values = new List<double>();
                        for (int r = 1; r < library.rows + 1; r++)
                        {
                            for (int c = 1; c < library.cols + 1; c++)
                            {
                                pos.Add(Tuple.Create(r, c));
                                values.Add(Convert.ToDouble(map.map[(r - 1) * library.cols + c - 1]));
                            }
                        }
                        _designManager.AddSourceMap(
                            map.library,
                            map.chemical,
                            map.unit,
                            values,
                            pos,
                            map.tags);
                    }
                    else
                    {
                        for (int r = 1; r < library.rows + 1; r++)
                        {
                            for (int c = 1; c < library.cols + 1; c++)
                            {
                                _designManager.AddSourceMap(
                                    map.library,
                                    map.chemical,
                                    map.unit,
                                    new List<double>() { Convert.ToDouble(map.map[(r - 1) * library.cols + c - 1]) },
                                    new List<Tuple<int, int>>() { Tuple.Create(r, c) },
                                    map.tags);
                            }
                        }
                    }
                }
                else if (map.type.Equals("array"))
                {
                    if (!map.split)
                    {
                        List<double> values = map.map.ConvertAll(x => Convert.ToDouble(x));
                        _designManager.AddArrayMap(
                            map.library,
                            map.source,
                            map.unit,
                            values,
                            Tuple.Create(map.source_start[0], map.source_start[1]),
                            Tuple.Create(map.source_end[0], map.source_end[1]),
                            Tuple.Create(map.dest_start[0], map.dest_start[1]),
                            Tuple.Create(map.dest_end[0], map.dest_end[1]),
                            map.tags);
                    }
                    else
                    {
                        throw new ArgumentException("split = true for array map is not currently supported");
                    }
                }
                else if (map.type.Equals("start_stunner"))
                {
                    // handle the start stunner command separately by only setting the cells that equal 1
                    string paramName = ConvertParamName(map.type, library.GetLocation());
                    List<double> castMap = map.map.ConvertAll(x => Convert.ToDouble(x));
                    List<Tuple<int, int>> pos = new List<Tuple<int, int>>();
                    int ind = 0;
                    for (int r = 1; r < library.rows + 1; r++)
                    {
                        for (int c = 1; c < library.cols + 1; c++)
                        {
                            if (castMap[ind] == 1.0)
                            {
                                pos.Add(Tuple.Create(r, c));
                            }
                            ind++;
                        }
                    }

                    _designManager.AddParameterMap(
                        map.library,
                        paramName,
                        pos,
                        value: 1.0);
                }
                else if (_stunnerParams.Contains(map.type))
                {
                    string paramName = ConvertParamName(map.type, library.GetLocation());
                    // parse null entries and create new values array containing only non-null entries
                    List<Tuple<int, int>> pos = new List<Tuple<int, int>>();
                    List<string> castMap = map.map.ConvertAll(x => Convert.ToString(x));
                    List<object> values = new List<object>();
                    int ind = 0;
                    for (int r = 1; r < library.rows + 1; r++)
                    {
                        for (int c = 1; c < library.cols + 1; c++)
                        {
                            if (!string.IsNullOrEmpty(castMap[ind]))
                            {
                                pos.Add(Tuple.Create(r, c));
                                values.Add(castMap[ind]);
                            }
                            ind++;
                        }
                    }

                    _designManager.AddParameterMap(
                        map.library,
                        paramName,
                        pos,
                        values: values);
                }
                else
                {
                    List<Tuple<int, int>> pos = new List<Tuple<int, int>>();
                    for (int r = 1; r < library.rows + 1; r++)
                    {
                        for (int c = 1; c < library.cols + 1; c++)
                        {
                            pos.Add(Tuple.Create(r, c));
                        }
                    }

                    string paramName = ConvertParamName(map.type, library.GetLocation());
                    // TODO: check for valid deck position if heat or stir
                    // explicitly convert to string or double depending on map type
                    if (Parameter.parameterDict[paramName].type.Equals("Text"))
                    {
                        map.value = (string)map.value;
                    }
                    else
                    {
                        double val;
                        double.TryParse((string)map.value, out val);
                        map.value = val;
                    }

                    _designManager.AddParameterMap(
                        map.library,
                        paramName,
                        pos,
                        value: map.value);
                }
            }
        }

        private string ConvertParamName(string name, string location)
        {
            if (_paramNames.ContainsKey(name))
            {
                return _paramNames[name];
            }
            else if (name.Equals("stir"))
            {
                if (location.Contains("13-14"))
                {
                    return "VortexRate";
                }
                else if (location.Contains("15-16"))
                {
                    return "StirRate";
                }
                else
                {
                    throw new ArgumentException(string.Format("{0} is an invalid deck position for stir", location));
                }
            }
            else
            {
                throw new ArgumentException(string.Format("{0} is not a valid map type", name));
            }
        }

        public class DesignInput
        {
            public DesignSpec design { get; set; }
            public List<ChemicalInput> chemicals { get; set; }
            public List<LibraryInput> libraries { get; set; }
            public List<MapInput> maps { get; set; }
        }

        public class DesignSpec
        {
            public string name { get; set; }
            public string project { get; set; }
        }

        public class ChemicalInput
        {
            public string name { get; set; }
            public List<int> color { get; set; }
            public string type { get; set; }
            public string dispense_type { get; set; }
            public string tip { get; set; }
            public int valve_position { get; set; }
            public string deck { get; set; }
            public int position { get; set; }
            public int row { get; set; }
            public int col { get; set; }
            public int amount { get; set; }
            public string unit { get; set; }
            public double size { get; set; }
            public bool capped { get; set; }
            public string caprack_deck { get; set; } = "None";
            public int caprack_position { get; set; } = 0;
        }

        public class LibraryInput
        {
            public string name { get; set; }
            public int rows { get; set; }
            public int cols { get; set; }
            public string deck { get; set; }
            public int position { get; set; }
            public double size { get; set; }
            public bool capped { get; set; }
            public string caprack_deck { get; set; } = "None";
            public int caprack_position { get; set; } = 0;
        }

        public class MapInput
        {
            public string type { get; set; }
            public string library { get; set; }
            public string chemical { get; set; }
            public string source { get; set; }
            public List<int> source_start { get; set; }
            public List<int> source_end { get; set; }
            public List<int> dest_start { get; set; }
            public List<int> dest_end { get; set; }
            public List<string> tags { get; set; }
            public string unit { get; set; }
            public bool split { get; set; }
            public List<object> map { get; set; }
            public object value { get; set; }
            public double duration { get; set; }
            public double delay { get; set; }
        }
    }
}

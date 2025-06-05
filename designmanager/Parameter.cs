using LS_API;
using Symyx.ClientUtilities.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignManager
{
    public class Parameter
    { 
        public struct ParameterValues
        {
            public string type;
            public bool canVary;
            public Unit defaultUnit;
            public Unit sourceUnit;
            public int decimals;
            public List<string> tagList;
            public object defaultValue;

            public ParameterValues(string type, bool canVary, Unit defaultUnit, Unit sourceUnit, int decimals,
                List<string> tagList, object defaultValue = null)
            {
                this.type = type;
                this.canVary = canVary;
                this.defaultUnit = defaultUnit;
                this.sourceUnit = sourceUnit;
                this.decimals = decimals;
                this.tagList = tagList;
                this.defaultValue = defaultValue;
            }
        }

        private LSAPI _lsAPI;
        public static Dictionary<string, ParameterValues> parameterDict = new Dictionary<string, ParameterValues>() // type, canVary, defaultUnit, sourceUnit, decimalPlaces
        {
            { "Cap", new ParameterValues("Number", true, Unit.unUNDEFINED, Unit.unUNDEFINED, 2, new List<string> { "Processing" }, 1.0) },
            { "Uncap", new ParameterValues("Number", true, Unit.unUNDEFINED, Unit.unUNDEFINED, 2, new List < string > { "Processing" }, 1.0) },
            { "Delay", new ParameterValues("Time", false, Unit.min, Unit.min, 2, new List < string > { "Processing" }) },
            { "MovePlate", new ParameterValues("Text", false, Unit.unUNDEFINED, Unit.unUNDEFINED, 2, new List < string > { "Processing" }) },
            { "StirRate", new ParameterValues("Stir Rate", false, Unit.rpm, Unit.rpm, 2, new List < string > { "Processing" }) },
            { "HeatingTemp", new ParameterValues("Temperature", false, Unit.degC, Unit.degC, 2, new List < string > { "Processing" }) },
            { "SetTemperatureRampRate", new ParameterValues("Rate", false, Unit.unDEGCPM, Unit.unDEGCPM, 2, new List < string > { "Processing" }) },
            { "HeatStir1Temp", new ParameterValues("Temperature", false, Unit.degC, Unit.degC, 2, new List < string > { "Processing" }) },
            { "HeatStir2Temp", new ParameterValues("Temperature", false, Unit.degC, Unit.degC, 2, new List < string > { "Processing" }) },
            { "HeatStir3Temp", new ParameterValues("Temperature", false, Unit.degC, Unit.degC, 2, new List < string > { "Processing" }) },
            { "HeatVortex1Temp", new ParameterValues("Temperature", false, Unit.degC, Unit.degC, 2, new List < string > { "Processing" }) },
            { "VortexRate", new ParameterValues("Angular Speed", false, Unit.rpm, Unit.rpm, 2, new List < string > { "Processing" }) },
            { "SetTimer", new ParameterValues("Time", false, Unit.min, Unit.min, 2, new List < string > { "Processing" }) },
            { "WaitForTimer", new ParameterValues("Number", false, Unit.unUNDEFINED, Unit.unUNDEFINED, 2, new List < string > { "Processing" }, 0.0) },
            { "StartReactionTimer", new ParameterValues("Number", false, Unit.unUNDEFINED, Unit.unUNDEFINED, 2, new List < string > { "Processing" }, 0.0) },
            { "EndReactionTimer", new ParameterValues("Number", false, Unit.unUNDEFINED, Unit.unUNDEFINED, 2, new List < string > { "Processing" }, 0.0) },
            { "StunnerApplication", new ParameterValues("Text", false, Unit.unUNDEFINED, Unit.ul, 0, new List < string > { "Analysis" }) },
            { "StunnerSampleGroup", new ParameterValues("Text", false, Unit.unUNDEFINED, Unit.ul, 0, new List < string > { "Analysis" }) },
            { "StunnerSampleType", new ParameterValues("Text", false, Unit.unUNDEFINED, Unit.ul, 0, new List < string > { "Analysis" }) },
            { "StunnerAnalyteName", new ParameterValues("Text", false, Unit.unUNDEFINED, Unit.ul, 0, new List < string > { "Analysis" }) },
            { "StartStunner", new ParameterValues("Number", true, Unit.unUNDEFINED, Unit.ul, 2, new List < string > { "Analysis" }, 1.0) },
            { "LunaticPlateType", new ParameterValues("Text", false, Unit.unUNDEFINED, Unit.ul, 0, new List < string > { "Analysis" }) },
            { "StunnerBufferName", new ParameterValues("Text", false, Unit.unUNDEFINED, Unit.ul, 0, new List < string > { "Analysis" }) },
            { "Pause", new ParameterValues("Text", false, Unit.unUNDEFINED, Unit.unUNDEFINED, 2, new List < string > { "Processing" }) },
        };
        private ParameterValues _parameterValues;

        public string name { get; private set; }

        public Parameter(LSAPI lsAPI, string name)
        {
            _lsAPI = lsAPI;
            this.name = name;
            _parameterValues = parameterDict[name];

            _lsAPI.AddParameter(
                name: name,
                type: _parameterValues.type,
                defaultUnit: _parameterValues.defaultUnit,
                sourceUnit: _parameterValues.sourceUnit,
                canVaryAcrossRows: _parameterValues.canVary,
                canVaryAcrossColumns: _parameterValues.canVary,
                decimalPlaces: _parameterValues.decimals);
        }

        public Unit GetUnit()
        {
            return _parameterValues.defaultUnit;
        }

        public object GetDefaultValue()
        {
            return _parameterValues.defaultValue;
        }

        public List<string> GetTags()
        {
            return _parameterValues.tagList;
        }
    }
}

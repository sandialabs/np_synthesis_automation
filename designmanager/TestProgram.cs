using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LS_API;
using Symyx.LSDesignMgr.Interop;
using Symyx.iLSDesignMgr.Interop;
using Symyx.ClientUtilities.Interop;
using Symyx.iLSSources.Interop;
using System.Windows.Controls.Primitives;

namespace DesignManager
{
    internal class TestProgram
    {
        [STAThread]
        static void Main(string[] args)
        {
            LSAPI LSApi = new LSAPI();
            int resCode;
            bool ret;

            string designName = "testdesign";
            string projName = "Testing";
            resCode = LSApi.CreateNewDesign(designName, projName, "", "", "", "", "", "");
            if (resCode < 0)
            {
                Console.WriteLine("Fail to create design: " + LSApi.GetErrorMessage(resCode));
            }

            resCode = LSApi.AddLibrary("8mL rxn plate", nRows: 4, nCols: 6, color: 255);
            if (resCode < 0)
            {
                Console.WriteLine("Fail to add lib: " + LSApi.GetErrorMessage(resCode));
            }

            LSApi.AddChemical("Water", 0x800000, Unit.ml);

            List<Tuple<int, int>> pos = new List<Tuple<int, int>>();
            for (int r = 1; r < 5; r++)
            {
                for (int c = 1; c < 7; c++)
                {
                    pos.Add(new Tuple<int, int>(r, c));
                }
            }

            //LSApi.AddParameter(
            //    name: "StirRate",
            //    type: "Stir Rate",
            //    defaultUnit: Unit.rpm,
            //    sourceUnit: Unit.rpm,
            //    canVaryAcrossRows: false,
            //    canVaryAcrossColumns: false,
            //    decimalPlaces: 2);

            //LSApi.AddParameter(
            //    name: "SetTimer",
            //    type: "Time",
            //    defaultUnit: Unit.min,
            //    sourceUnit: Unit.min,
            //    canVaryAcrossRows: false,
            //    canVaryAcrossColumns: false,
            //    decimalPlaces: 2);

            //LSApi.AddParameter(
            //    name: "WaitForTimer",
            //    type: "Number",
            //    defaultUnit: Unit.unUNDEFINED,
            //    sourceUnit: Unit.unUNDEFINED,
            //    canVaryAcrossRows: false,
            //    canVaryAcrossColumns: false,
            //    decimalPlaces: 2);

            LSApi.AddParameter(
                name: "StunnerSampleType",
                type: "Text",
                defaultUnit: Unit.unUNDEFINED,
                sourceUnit: Unit.ul,
                canVaryAcrossRows: true,
                canVaryAcrossColumns: true,
                decimalPlaces: 0);

            //LSApi.AddParameter(
            //    name: "Cap",
            //    type: "Text",
            //    defaultUnit: Unit.unUNDEFINED,
            //    sourceUnit: Unit.ul,
            //    canVaryAcrossRows: false,
            //    canVaryAcrossColumns: false,
            //    decimalPlaces: 2);

            //LSApi.AddParameter(
            //    name: "StartStunner",
            //    type: "Number",
            //    defaultUnit: Unit.unUNDEFINED,
            //    sourceUnit: Unit.ul,
            //    canVaryAcrossRows: false,
            //    canVaryAcrossColumns: false,
            //    decimalPlaces: 0);

            //resCode = LSApi.AddSourceMap(
            //    chemicalName: "Water",
            //    mapType: "Uniform",
            //    unit: Unit.ml,
            //    value: 4.0,
            //    positions: pos,
            //    libName: "8mL rxn plate",
            //    tagList: "SyringePump,SingleTip,LookAhead,Backsolvent",
            //    layerIdx: 1,
            //    mapIdx: 1);
            //if (resCode < 0)
            //{
            //    Console.WriteLine("Fail to add source map: " + LSApi.GetErrorMessage(resCode));
            //}

            //resCode = LSApi.AddParameterMap(
            //    parameterName: "StirRate",
            //    mapType: "Uniform",
            //    unit: Unit.rpm,
            //    value: 600,
            //    positions: pos,
            //    libName: "8mL rxn plate",
            //    tagList: "Processing",
            //    layerIdx: 2,
            //    mapIdx: 1);
            //if (resCode < 0)
            //{
            //    Console.WriteLine("Fail to add parameter map: " + LSApi.GetErrorMessage(resCode));
            //}

            //resCode = LSApi.AddParameterMap(
            //    parameterName: "SetTimer",
            //    mapType: "Uniform",
            //    unit: Unit.min,
            //    value: 1,
            //    positions: pos,
            //    libName: "8mL rxn plate",
            //    tagList: "Processing",
            //    layerIdx: 3,
            //    mapIdx: 1);
            //if (resCode < 0)
            //{
            //    Console.WriteLine("Fail to add parameter map: " + LSApi.GetErrorMessage(resCode));
            //}

            //resCode = LSApi.AddParameterMap(
            //    parameterName: "WaitForTimer",
            //    mapType: "Uniform",
            //    unit: Unit.unUNDEFINED,
            //    value: 0,
            //    positions: pos,
            //    libName: "8mL rxn plate",
            //    tagList: "Processing",
            //    layerIdx: 4,
            //    mapIdx: 1);
            //if (resCode < 0)
            //{
            //    Console.WriteLine("Fail to add parameter map: " + LSApi.GetErrorMessage(resCode));
            //}

            //resCode = LSApi.AddParameterMap(
            //    parameterName: "StirRate",
            //    mapType: "Uniform",
            //    unit: Unit.rpm,
            //    value: 0,
            //    positions: pos,
            //    libName: "8mL rxn plate",
            //    tagList: "Processing",
            //    layerIdx: 5,
            //    mapIdx: 1);
            //if (resCode < 0)
            //{
            //    Console.WriteLine("Fail to add parameter map: " + LSApi.GetErrorMessage(resCode));
            //}

            pos.Clear();
            for (int r = 1; r < 3; r++)
            {
                for (int c = 1; c < 3; c++)
                {
                    pos.Add(new Tuple<int, int>(r, c));
                }
            }

            List<object> values = new List<object>();
            for (int i = 0; i < pos.Count; i++)
            {
                Console.WriteLine(string.Format("{0},{1}", pos[i].Item1, pos[i].Item2));
                values.Add("B");
            }
            values[1] = "S";
            //resCode = LSApi.AddParameterMap(
            //    parameterName: "StunnerSampleType",
            //    mapType: "Uniform",
            //    unit: Unit.unUNDEFINED,
            //    value: "B",
            //    values: values,
            //    positions: pos,
            //    libName: "8mL rxn plate",
            //    tagList: "Analysis",
            //    layerIdx: 6,
            //    mapIdx: 1);
            //if (resCode < 0)
            //{
            //    Console.WriteLine("Fail to add parameter map: " + LSApi.GetErrorMessage(resCode));
            //}

            resCode = LSApi.AddParameterMap(
                parameterName: "StunnerSampleType",
                mapType: "Discrete",
                unit: Unit.unUNDEFINED,
                value: "B",
                values: values,
                positions: pos,
                libName: "8mL rxn plate",
                tagList: "Analysis",
                layerIdx: 1,
                mapIdx: 1);
            if (resCode < 0)
            {
                Console.WriteLine("Fail to add parameter map: " + LSApi.GetErrorMessage(resCode));
            }

            //values = new List<object>();
            //for (int i = 0; i < pos.Count; i++)
            //{
            //    values.Add(1.0);
            //}
            //resCode = LSApi.AddParameterMap(
            //    parameterName: "StartStunner",
            //    mapType: "Discrete",
            //    unit: Unit.unUNDEFINED,
            //    value: null,
            //    values: values,
            //    positions: pos,
            //    libName: "8mL rxn plate",
            //    tagList: "Analysis",
            //    layerIdx: 7,
            //    mapIdx: 1);
            //if (resCode < 0)
            //{
            //    Console.WriteLine("Fail to add parameter map: " + LSApi.GetErrorMessage(resCode));
            //}

            //LSDesign design = LSApi.CurrentDesign;
            //Console.WriteLine(design.LayerCount);
            //Console.WriteLine(((ILSDesign)design).get_MapCount(5));

            string fname = "test.lsr";
            ret = LSApi.SaveDesignToFile(fname);
            if (!ret)
            {
                Console.WriteLine("Fail to save file: " + fname);
            }

            //int designID = LSApi.SaveDesignToDatabase(true, true);
            //Console.WriteLine("Design id: " +  designID);

            //LSDesign newDes = LSApi.GetDesignFromFile(fname);
            //// load and save it again to get rid of empty last layer
            //ret = LSApi.SaveDesignToFile(fname);
            //if (!ret)
            //{
            //    Console.WriteLine("Fail to save file: " + fname);
            //}
        }
    }
}

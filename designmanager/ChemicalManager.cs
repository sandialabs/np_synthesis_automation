using LS_API;
using Symyx.iLSSources.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using System.IO;
using Symyx.ClientUtilities.Interop;

namespace DesignManager
{
    public class ChemicalManager
    {
        private string _fname;
        private DesignManager _designManager;

        public ChemicalManager(string fname, DesignManager designManager)
        {
            _fname = fname;
            _designManager = designManager;
        }

        public void WriteXML()
        {
            XElement sources = new XElement("Symyx.AutomationStudio.Core.ChemicalManager",
                new XElement("Symyx.AutomationStudio.Core.ChemicalManager.Chemicals"),
                new XElement("Symyx.AutomationStudio.Core.ChemicalManager.Libraries")
            );

            XElement dispenses = new XElement("ChemicalsAndAssociatedDispenseModes",
                new XElement("ChemicalManager")
            );

            XElement chemicals = sources.Element("Symyx.AutomationStudio.Core.ChemicalManager.Chemicals");
            XElement libraries = sources.Element("Symyx.AutomationStudio.Core.ChemicalManager.Libraries");
            foreach (Chemical chemical in _designManager.GetChemicals())
            {
                Tuple<XElement, XElement> elements = ParseChemical(chemical);
                chemicals.Add(elements.Item1);
                dispenses.Element("ChemicalManager").Add(elements.Item2);
            }

            foreach (Library library in _designManager.GetLibraries())
            {
                Tuple<XElement, XElement, XElement> elements = ParseLibrary(library);
                libraries.Add(elements.Item1);
                if (elements.Item2 != null)
                {
                    dispenses.Element("ChemicalManager").Add(elements.Item2);
                    chemicals.Add(elements.Item3);
                }
            }

            using (StreamWriter sw = File.CreateText(_fname))
            {
                sw.WriteLine(sources);
                sw.WriteLine(dispenses);
            }
        }

        private Tuple<XElement, XElement> ParseChemical(Chemical chemical)
        {
            XElement amountLeft, size, substratePosition, substrateType, type, valveResource, valvePosition, units;
            if (chemical.backing)
            {
                amountLeft = new XElement("AmountLeft", "-1");
                size = new XElement("Size", "-1");
                substratePosition = new XElement("SubstratePosition");
                substrateType = new XElement("SubstrateType");
                type = new XElement("Type", "stBackingSolvent");
                valveResource = new XElement("ValveResource", "Valco Instruments Stream Selection Valve");
                valvePosition = new XElement("ValvePosition", chemical.valve.ToString());
                units = new XElement("Units", "undefined");
            }
            else
            {
                amountLeft = new XElement("AmountLeft", chemical.startingAmount.ToString());
                size = new XElement("Size", chemical.size.ToString());
                substratePosition = new XElement("SubstratePosition", chemical.location);
                substrateType = new XElement("SubstrateType", chemical.type);
                type = new XElement("Type", "stNormal");
                valveResource = new XElement("ValveResource");
                valvePosition = new XElement("ValvePosition", "0");
                units = new XElement("Units", chemical.unit);
            }
            XElement element = new XElement("Symyx.AutomationStudio.Core.ChemicalManager.Chemical",
                new XElement("Name", chemical.name),
                amountLeft,
                new XElement("Color", chemical.color),
                new XElement("Column", chemical.col.ToString()),
                new XElement("Columns", "0"),
                new XElement("Empty", "False"),
                new XElement("Questionable", "False"),
                new XElement("Row", chemical.row.ToString()),
                new XElement("Rows", "0"),
                size,
                substratePosition,
                substrateType,
                type,
                valveResource,
                valvePosition,
                units
            );

            XElement dispenseMode = new XElement("Chemical",
                new XAttribute("Name", chemical.name),
                new XAttribute("Mode", string.Format("{0}|{1}", chemical.dispenseMode, chemical.tip))
            );

            return Tuple.Create(element, dispenseMode);
        }

        private Tuple<XElement, XElement, XElement> ParseLibrary(Library library)
        {
            XElement element = new XElement("Symyx.AutomationStudio.Core.ChemicalManager.Library",
                new XElement("LibraryID", library.id.ToString()),
                new XElement("Name", library.name),
                new XElement("NumOfRows", library.rows.ToString()),
                new XElement("NumOfCols", library.cols.ToString()),
                new XElement("SubstrateType", library.type),
                new XElement("SubstratePosition", library.GetInitLocation()),
                new XElement("Symyx.AutomationStudio.Core.ChemicalManager.Wells")
            );

            XElement dispenseMode;
            XElement chemical;
            if (library.isSource)
            {
                dispenseMode = new XElement("Chemical",
                    new XAttribute("Name", library.name),
                    new XAttribute("Mode", string.Format("{0}|{1}", library.dispenseMode, library.tip))
                );

                chemical = new XElement("Symyx.AutomationStudio.Core.ChemicalManager.Chemical",
                    new XElement("Name", library.name),
                    new XElement("AmountLeft", "-1"),
                    new XElement("Color", library.color),
                    new XElement("Column", "0"),
                    new XElement("Columns", "0"),
                    new XElement("Empty", "False"),
                    new XElement("Questionable", "False"),
                    new XElement("Row", "0"),
                    new XElement("Rows", "0"),
                    new XElement("Size", "-1"),
                    new XElement("SubstratePosition", library.GetInitLocation()),
                    new XElement("SubstrateType", library.type),
                    new XElement("Type", "stPlate"),
                    new XElement("ValveResource"),
                    new XElement("ValvePosition", "0"),
                    new XElement("Units", "undefined")
                );
            }
            else
            {
                dispenseMode = null;
                chemical = null;
            }
            return Tuple.Create(element, dispenseMode, chemical);
        }
    }
}

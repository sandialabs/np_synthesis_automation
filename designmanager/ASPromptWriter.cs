using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.IO;

namespace DesignManager
{
    public class ASPromptWriter
    {
        private string _fname;
        private DesignManager _designManager;
        private string _templateFile = "C:\\Users\\Unchained Labs\\Documents\\DCIEDD\\LSAPI\\DesignManager\\AS.prompts.WithNoDesignCreator.xml";
        private XDocument _prompts;

        public ASPromptWriter(string fname, DesignManager designManager)
        {
            _fname = fname;
            _designManager = designManager;
            _prompts = XDocument.Load(_templateFile);
        }

        public void WritePrompts()
        {
            SetFilterByTags();
            SetLibraryStates();
            SetSourceStates();
            SetCapRackPositions();
            _prompts.Save(_fname);
        }

        private void SetFilterByTags()
        {
            SetPromptValue("DesignLoadingOption", "FilterMapType", "@FilterByTag; FilterByMap; No");
            SetPromptValue("DesignLoadingOption", "TagToFilterMaps", "Execute");
        }

        private void SetLibraryStates()
        {
            string valStr = "";
            foreach (Library library in _designManager.GetLibraries())
            {
                if (valStr.Length != 0)
                {
                    valStr += ";";
                }
                valStr += string.Format("[{0}:{1}]", library.id, library.initCapState);
            }
            SetPromptValue("LibraryInitialStates", "LibraryInitialState", valStr);
        }

        private void SetSourceStates()
        {
            HashSet<string> states = new HashSet<string>();
            foreach (Chemical chemical in _designManager.GetChemicals())
            {
                if (!chemical.backing)
                {
                    states.Add(string.Format("[{0}:{1}]", chemical.location, chemical.initCapState));
                }
            }

            string valStr = String.Join(";", states);
            SetPromptValue("SourceInitialStates", "SourceInitialState", valStr);
        }

        private void SetCapRackPositions()
        {
            string valStr = "";
            foreach (Library library in _designManager.GetLibraries())
            {
                if (library.initCapState.Equals("None"))
                {
                    continue;
                }
                if (valStr.Length != 0)
                {
                    valStr += ";";
                }
                valStr += string.Format("[{0}:{1}]", library.id, library.capRackLocation);
            }

            foreach (Chemical chemical in _designManager.GetChemicals())
            {
                if (chemical.backing || chemical.initCapState.Equals("None"))
                {
                    continue;
                }
                if (valStr.Length != 0)
                {
                    valStr += ";";
                }
                valStr += string.Format("[{0}:{1}]", chemical.location, chemical.capRackLocation);
            }

            SetPromptValue("CapRackPositions", "PromptForCapRackPosition", valStr);
        }

        private void SetPromptValue(string category, string prompt, string value)
        {
            var element = _prompts
                .Root
                .Element("categories")
                .Descendants("category").Where(e => e.Attribute("name").Value.Equals(category)).Single()
                .Element("prompts")
                .Descendants("prompt").Where(e => e.Attribute("name").Value.Equals(prompt)).Single();

            element.Value = value;
        }
    }
}

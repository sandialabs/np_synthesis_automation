using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignManager
{
    internal class TestInput
    {
        [STAThread]
        static void Main(string[] args)
        {
            InputDeckProcessor processor = new InputDeckProcessor(args[0]);
            processor.ReadYAML();
            processor.ProcessYAML();
        }
    }
}

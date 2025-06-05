using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignManager
{
    internal static class Utils
    {
        public static string SizeToType(double size)
        {
            string type;
            switch (size)
            {
                case 125:
                    type = "Rack 1x2 125mL Vial";
                    break;
                case 20:
                    type = "Rack 2x4 20mL Vial";
                    break;
                case 8:
                    type = "Rack 4x6 8mL Vial";
                    break;
                case 4:
                    type = "Rack 4x6 4mL Vial";
                    break;
                case 2:
                    type = "Rack 6x8 2mL Vial";
                    break;
                case 1.2:
                    type = "Rack 8x12 1.2mL Vial";
                    break;
                case -1:
                    type = "Rack 8x12 Stunner";
                    break;
                default:
                    throw new ArgumentException(string.Format("vial size {0} is not recognized", size));
            }

            return type;
        }
    }
}

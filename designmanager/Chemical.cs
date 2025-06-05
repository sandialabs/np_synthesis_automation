using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using LS_API;
using Symyx.ClientUtilities.Interop;
using Symyx.LSDesignMgr.Interop;

namespace DesignManager
{
    public class Chemical
    {
        private LSAPI _lsAPI;
        private Unit _unit;

        public string name { get; private set; }
        public uint color { get; private set; }
        public bool backing { get; set; } = false;
        public int row { get; set; } = 0;
        public int col { get; set; } = 0;
        public int valve { get; set; } = 0;
        public string location { get; set; } = null;
        public double amount { get; private set; }
        public double startingAmount { get; private set; }
        public string dispenseMode { get; private set; }
        public string tip { get; private set; }
        public double size { get; set; }
        public bool cap { get; set; }
        public string initCapState { get; private set; } = "None";
        public string capRackLocation { get; private set; }
        public string type { get; private set; }
        public string unit { get; private set; }

        public Chemical(
            LSAPI lsAPI,
            string name,
            uint color,
            string unit,
            string dispenseMode,
            string tip,
            double amt = 999999)
        {
            _lsAPI = lsAPI;
            this.name = name;
            this.color = color;
            Enum.TryParse<Unit>(unit, out _unit);
            this.unit = unit;
            this.dispenseMode = dispenseMode;
            this.tip = tip;
            this.amount = amt;
            this.startingAmount = amt;

            _lsAPI.AddChemical(this.name, this.color, _unit);
        }

        public void Add(double amt)
        {
            this.amount += amt;
        }

        public void Dispense(double amt)
        {
            this.amount -= amt;
        }

        public void SetBacking(int valve)
        {
            this.backing = true;
            this.valve = valve;
            this.type = null;
        }

        public void SetDeck(int row, int col, double size, bool cap, string capRackLocation)
        {
            this.backing = false;
            this.row = row;
            this.col = col;
            this.size = size;
            this.cap = cap;
            this.capRackLocation = capRackLocation;

            this.type = Utils.SizeToType(this.size);
        }
    }
}

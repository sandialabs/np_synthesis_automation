using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace DesignManager
{
    internal class TestDesignManager
    {
        [STAThread]
        static void Main(string[] args)
        {
            DesignManager designManager = new DesignManager("testy", "Testing");
            designManager.AddChemical(
                "Water",
                0x800000,
                "ml",
                "Non-viscous liquid 1 pre wet w/o touchoff",
                "ADT",
                backing: true,
                valve: 10);

            designManager.AddChemical(
                "Citric Acid 0.1M",
                0x000000,
                "ml",
                "Non-viscous liquid 1 pre wet w/ touchoff",
                "PDT",
                backing: false,
                location: "Deck 9-10 Position 3",
                row: 1,
                col: 1,
                size: 125,
                cap: false,
                amt: 100);

            designManager.AddLibrary(
                "test plate",
                4,
                6,
                8,
                false,
                "Deck 9-10 Position 2");

            designManager.AddLibrary(
                "test2",
                8,
                12,
                1.2,
                false,
                "Deck 9-10 Position 3");

            List<string> tags = new List<string>
            {
                "SyringePump",
                "SingleTip",
                "LookAhead",
                "Backsolvent"
            };
            List<Tuple<int, int>> pos = new List<Tuple<int, int>>();
            for (int r = 1; r < 5; r++)
            {
                for (int c = 1; c < 7; c++)
                {
                    pos.Add(Tuple.Create(r, c));
                }
            }
            List<double> values = Enumerable.Repeat(4.0, pos.Count).ToList();
            designManager.AddSourceMap(
                "test plate",
                "Water",
                "ml",
                values,
                pos,
                tags);

            values = Enumerable.Repeat(100.0, pos.Count).ToList();
            designManager.AddSourceMap(
                "test plate",
                "Citric Acid 0.1M",
                "ul",
                values,
                pos,
                new List<string>());

            values = Enumerable.Repeat(5.0, pos.Count).ToList();
            values[0] = 0.0;
            values[1] = 7.0;
            designManager.AddArrayMap(
                "test2",
                "test plate",
                "ul",
                values,
                Tuple.Create(1, 1),
                Tuple.Create(4, 6),
                Tuple.Create(5, 7),
                Tuple.Create(8, 12),
                new List<string>());

            designManager.AddParameterMap(
                "test plate",
                "Cap",
                pos,
                value: 1.0);

            designManager.AddParameterMap(
                "test plate",
                "MovePlate",
                pos,
                value: "Deck 13-14 Heat Vortex 1");

            designManager.AddParameterMap(
                "test plate",
                "StirRate",
                pos,
                value: 100);

            designManager.WriteLSR("testy.lsr");
            designManager.WriteDB();

            designManager.WriteChemicalManager("testy.xml");
        }
    }
}

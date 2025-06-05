using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesignManager
{
    internal static class Positions
    {
        public static Dictionary<string, bool> positions = new Dictionary<string, bool>()
        {
            { "Deck 9-10 Position 1", false },
            { "Deck 9-10 Position 2", false },
            { "Deck 9-10 Position 3", false },
            { "Deck 11-12 Position 2", false },
            { "Deck 11-12 Position 3", false },
            { "Deck 13-14 Heat Vortex 1", false },
            { "Deck 13-14 Heat Vortex 2", false },
            { "Deck 13-14 Heat Vortex 3", false },
            { "Deck 15-16 Heat Stir 1", false },
            { "Deck 15-16 Heat Stir 2", false },
            { "Deck 15-16 Heat Stir 3", false },
            { "Stunner", false },
        };

        public static string DeckPosition(string deck, int position = 0)
        {
            if (deck.Equals("None") && position == 0)
            {
                return "";
            }

            string str;
            if (deck.Equals("Stunner"))
            {
                str = "Deck 11-12 Position 2";
            }
            else if (deck.Equals("13-14"))
            {
                str = string.Format("Deck {0} Heat Vortex {1}", deck, position);
            }
            else if (deck.Equals("15-16"))
            {
                str = string.Format("Deck {0} Heat Stir {1}", deck, position);
            }
            else
            {
                str = string.Format("Deck {0} Position {1}", deck, position);
            }

            if (!positions.ContainsKey(str))
            {
                throw new ArgumentException(string.Format("{0} is an invalid deck position", str));
            }

            return str;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace BuzzardWPF.Management
{
    internal class CartConfigFilter
    {
        public static List<string> GetCartConfigNamesForCart(string cartName)
        {
            var cartConfigNames = DMS_DataAccessor.Instance.CartConfigNames
                .Where(x => x.StartsWith(cartName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.Equals(cartName, "No_Cart", StringComparison.OrdinalIgnoreCase))
            {
                // Also add Unknown_Cart_Config
                cartConfigNames.Add(DMS_DataAccessor.Instance.CartConfigNames.First(
                                        x => x.StartsWith("Unknown", StringComparison.OrdinalIgnoreCase)));
            }

            return cartConfigNames;
        }

    }
}

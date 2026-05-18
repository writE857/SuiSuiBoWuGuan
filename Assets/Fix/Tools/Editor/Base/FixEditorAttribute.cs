using System;
using System.Collections.Generic;
using System.Linq;

namespace Fix.Editor
{
    public class FixEditorAttribute : Attribute
    {
        private static readonly string[] kMenuItemSeparators = new string[1]
        {
            "/"
        };

        public string menuItem;
        public bool validate;

        public FixEditorAttribute(string itemName)
            : this(itemName, false)
        {
        }

        public FixEditorAttribute(string itemName, bool isValidateFunction)
        {
            itemName = NormalizeMenuItemName(itemName);
            this.menuItem = itemName;
            this.validate = isValidateFunction;
        }


        private static string NormalizeMenuItemName(string rawName) =>
            string.Join(kMenuItemSeparators[0],
                rawName
                    .Split(kMenuItemSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(token => token.Trim())
                    .ToArray<string>());

        public static IEnumerable<string> Step(string menuItem)
        {
            return menuItem
                    .Split(kMenuItemSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(token => token.Trim());
        }
    }
}
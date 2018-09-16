using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator.Model
{
   public class EAPEnumeration : MetaEntity
    {
        public bool Bitfield { get; set; }
        private List<EAPEnumValue> enumValue = new List<EAPEnumValue>();

        public EAPEnumeration()
            : base()
        {
            Bitfield = false;
        }
        public List<EAPEnumValue> EnumValue
        {
            get { return enumValue; }
            set { enumValue = value; }
        }

        public bool AddEnumValue(EAPEnumValue attr)
        {
            for (int i = 0; i < enumValue.Count; i++)
            {
                if (EnumValue[i].Name == attr.Name)
                    return false;
            }
            EnumValue.Add(attr);

            return true;
        }
    }
}

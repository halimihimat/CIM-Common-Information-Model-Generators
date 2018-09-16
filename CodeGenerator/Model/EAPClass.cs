using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator.Model
{
   public class EAPClass : MetaEntity
    {
        public string Parent { get; set; }
        public string Abstract { get; set; }
        private List<EAPAttribute> attributes = new List<EAPAttribute>();

        public EAPClass()
            : base()
        {
        }

        public List<EAPAttribute> Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }

        public bool AddAttribute(EAPAttribute attr)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
            }

            Attributes.Add(attr);
            return true;
        }
    }
}

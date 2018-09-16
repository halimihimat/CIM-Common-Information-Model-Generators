using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator.Model
{
  public  class EAPEnumValue : MetaEntity
    {
        public string Value { get; set; }

        public EAPEnumValue() : base()
        {
        }
    }
}

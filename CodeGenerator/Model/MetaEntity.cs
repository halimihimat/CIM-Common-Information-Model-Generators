using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator.Model
{
   public class MetaEntity
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public bool Extension { get; set; }
        public bool Parse { get; set; }

        public MetaEntity()
        {
            Extension = false;
        }
    }
}

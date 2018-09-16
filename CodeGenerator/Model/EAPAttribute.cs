using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator.Model
{
    public class EAPAttribute : MetaEntity
    {
        public string Min { get; set; }
        public string Max { get; set; }
        public string MeasurementType { get; set; }
        public bool Available { get; set; }
        public string TypeCode { get; set; }
        private List<string> referenceType = new List<string>();
        public bool Settable { get; set; }

        public bool Visible { get; set; }
        public int Index { get; set; }
        public string Group { get; set; }
        public bool Searchable { get; set; }
        public bool Aggregated { get; set; }
        public string Deadband { get; set; }

        public string Default { get; set; }
        public string Cardinality { get; set; }
        public bool IsReference { get; set; }

        public bool IsListOfReferences { get; set; }

        public EAPAttribute onSameConnectorPairAtt { get; set; }
        public EAPAttribute(string name,string code)
        {
            this.Name = name;
            this.Code = code;
            this.TypeCode = "";
        }

        public EAPAttribute() : base()
        {

            Available = true;
            Settable = true;
            Visible = true;
            Searchable = false;
            Aggregated = false;
            Index = 0;
            Deadband = null;
            Default = null;
            Cardinality = null;
            TypeCode = "";
            MeasurementType = "";
        }

        public List<string> ReferenceType
        {
            get { return referenceType; }
            set { referenceType = value; }
        }
    }
}

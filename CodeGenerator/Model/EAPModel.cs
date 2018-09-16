using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeGenerator.Model
{
  public  class EAPModel
    {
        private Hashtable classes;
        //private Hashtable links = new Hashtable();
        //private List<EAPClass> eapClass = new List<EAPClass>();
        private Dictionary<string, EAPEnumeration> enums;
        //baseModelCode
        //public HashSet<ModelCode> BaseModelCode  { get; set; }

        public Dictionary<string, string> GroupClasNames { get; set; }

        public String ModelName { get; set; }

        public Hashtable Classes
        {
            get { return classes; }
            set { classes = value; }
        }

        public EAPModel(String nameModel)
        {
            ModelName = nameModel;
            classes = new Hashtable();
            enums = new Dictionary<string, EAPEnumeration>();
            //BaseModelCode = new HashSet<ModelCode>();
            GroupClasNames = new Dictionary<string, string>();
        }

        public Dictionary<string, EAPEnumeration> Enums
        {
            get { return enums; }
            set { enums = value; }
        }
    }

    class EAPModels
    {
        public List<EAPModel> Models { get; set; }

        public EAPModels()
        {
            Models = new List<EAPModel>();
        }

    }
}

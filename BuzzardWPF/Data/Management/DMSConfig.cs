using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuzzardWPF.Data
{
    public class DMSConfig
    {
        public string CartName
        {
            get;
            set;
        }
        public string SeparationType
        {
            get;
            set;
        }
        public string InstrumentName
        {
            get;
            set;
        }
        public string Operator
        {
            get;
            set;
        }
        public bool ShouldCopyDataToDMS
        {
            get;
            set;
        }
        public string UploadPath
        {
            get;
            set;
        }
    }
}

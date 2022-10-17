using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MANSR_VIEWER
{
    class PrefecturesToString
    {
        string prefecture;
        int indicator;
        string area;
        string type;
        string site_name;

        public string Prefecture
        {
            get
            {
                return prefecture;
            }

            set
            {
                prefecture = value;
            }
        }
        public string Area
        {
            get
            {
                return area;
            }

            set
            {
                area = value;
            }
        }
        public string Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }
        public int Indicator
        {
            get
            {
                return indicator;
            }

            set
            {
                indicator = value;
            }
        }
        public string Site_name
        {
            get
            {
                return site_name;
            }

            set
            {
                site_name = value;
            }
        }
    }
}

using System.Collections.ObjectModel;
using BuzzardWPF.Searching;

namespace BuzzardWPF.Data
{
    public class BuzzardConfig
    {
        private ObservableCollection<SearchConfig> m_configurations;

        public BuzzardConfig()
        {
            m_configurations = new ObservableCollection<SearchConfig>();
        }

        public ObservableCollection<SearchConfig> Configurations
        {
            get
            {
                return m_configurations;
            }
            set
            {
                m_configurations = value;
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BuzzardWPF.Windows
{
	/// <summary>
	/// Interaction logic for DateRangeSelector.xaml
	/// </summary>
	public partial class DateRangeSelector 
		: UserControl, INotifyPropertyChanged
	{
		#region Events
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion


		#region Attributes
		private DateTime? m_startDate;
		private DateTime? m_endDate;

		private DateTime? m_minDate;
		private DateTime? m_maxDate;
		#endregion


		#region Initialization
		public DateRangeSelector()
		{
			InitializeComponent();
			this.DataContext = this;
		}
		#endregion


		#region Properties
		public DateTime? StartDate
		{
			get { return m_startDate; }
			set
			{
				if (m_startDate != value)
				{
					m_startDate = value;
					OnPropertyChanged("StartDate");
				}
			}
		}

		public DateTime? EndDate
		{
			get { return m_endDate; }
			set
			{
				if (m_endDate != value)
				{
					m_endDate = value;
					OnPropertyChanged("EndDate");
				}
			}
		}

		public DateTime? MinDate
		{
			get { return m_minDate; }
			set
			{
				if (m_minDate != value)
				{
					m_minDate = value;
					OnPropertyChanged("MinDate");
				}
			}
		}

		public DateTime? MaxDate
		{
			get { return m_maxDate; }
			set
			{
				if (m_maxDate != value)
				{
					m_maxDate = value;
					OnPropertyChanged("MaxDate");
				}
			}
		}
		#endregion


		#region Methods
		private void OnPropertyChanged(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}
		#endregion
	}
}

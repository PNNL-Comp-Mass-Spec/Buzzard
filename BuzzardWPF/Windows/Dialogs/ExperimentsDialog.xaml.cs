using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using LcmsNetDataClasses;
using LcmsNetDataClasses.Logging;


namespace BuzzardWPF.Windows
{
	/// <summary>
	/// Interaction logic for ExperimentsDialog.xaml
	/// </summary>
	public partial class ExperimentsDialog 
		: Window
	{
		public ExperimentsDialog()
		{
			InitializeComponent();
		}

		public classExperimentData SelectedExperiment
		{
			get { return m_viewer.SelectedExperiment; }
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (SelectedExperiment == null)
			{
				classApplicationLogger.LogMessage(
					0,
					"An experiment must be selected in order to proceed.");
				return;
			}

			DialogResult = true;
		}
	}
}

using System.Windows.Forms;
using System.Reflection;
using System;

namespace Buzzard
{
	public partial class formSplashScreen : Form
	{

		#region "Delegates"
			/// <summary>
			/// Delegate for updating status without cross-thread issues
			/// </summary>
			/// <param name="newStatus"></param>
			private delegate void delegateUpdateStatus(string newStatus);
		#endregion

		#region "Properties"
		public string Status
			{
				set
				{
//					labelStatus.Text = value;					
					UpdateStatus(value);
				}
			}
		#endregion

		#region "Methods"
			public formSplashScreen()
			{
				InitializeComponent();
                
                Assembly assem          = Assembly.GetEntryAssembly();
                AssemblyName assemName  = assem.GetName();
                Version ver             = assemName.Version;
                
                mlabel_version.Text += ver.ToString();                                       
			}

			private void UpdateStatus(string newStatus)
			{
				if (labelStatus.InvokeRequired)
				{
					delegateUpdateStatus d = new delegateUpdateStatus(UpdateStatus);
					labelStatus.Invoke(d, new object[] { newStatus });
				}
				else
				{
					labelStatus.Text = newStatus;
                    labelStatus.Refresh();
                    Application.DoEvents();
				}
			}

            public void SetEmulatedLabelVisibility(string cartName, bool visible)
            {
                if (visible)
                {
                    mlabel_emulated.ForeColor   = System.Drawing.Color.Red;
                    mlabel_emulated.Text        = cartName + "\n [EMULATED] ";                    
                }
                else
                {
                    mlabel_emulated.Text        = cartName; 
                }
            }
		#endregion
	}
}

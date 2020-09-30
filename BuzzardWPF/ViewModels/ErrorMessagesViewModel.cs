using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class ErrorMessagesViewModel : ReactiveObject
    {
        #region Attributes

        private List<string> errorMessages;

        #endregion

        #region Initialization

        public ErrorMessagesViewModel()
        {
            errorMessages = new List<string>();
        }

        #endregion

        #region Properties

        public List<string> ErrorMessages
        {
            get => errorMessages;
            set => this.RaiseAndSetIfChanged(ref errorMessages, value);
        }

        #endregion

    }
}

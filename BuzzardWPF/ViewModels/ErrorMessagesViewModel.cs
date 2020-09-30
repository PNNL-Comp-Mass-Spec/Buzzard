using System.Collections.Generic;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class ErrorMessagesViewModel : ReactiveObject
    {
        // Ignore Spelling: Creatable

        #region Initialization

        /// <summary>
        /// This constructor is used by ErrorMessagesView.xaml when the DataContext is defined by
        /// d:DataContext="{d:DesignInstance {x:Type viewModels:ErrorMessagesViewModel}, IsDesignTimeCreatable=True}"
        /// </summary>
        // ReSharper disable once UnusedMember.Global
        public ErrorMessagesViewModel() : this(new List<string>()) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="errorMessages"></param>
        public ErrorMessagesViewModel(List<string> errorMessages)
        {
            ErrorMessageList = errorMessages;
        }

        #endregion

        #region Properties

        public List<string> ErrorMessageList
        {
            get;
            set;
        }

        #endregion

    }
}

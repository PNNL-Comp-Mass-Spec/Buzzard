using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Windows;
using BuzzardWPF.Views;
using DynamicData;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class ErrorMessagesViewModel : ReactiveObject, IDisposable
    {
        /// <summary>
        /// This constructor is used by ErrorMessagesView.xaml when the DataContext is defined by
        /// d:DataContext="{d:DesignInstance {x:Type viewModels:ErrorMessagesViewModel}, IsDesignTimeCreatable=True}"
        /// </summary>
        [Obsolete("For WPF Design-time use only", true)]
        public ErrorMessagesViewModel() : this(() => { }) { }

        /// <summary>
        /// Create new view model
        /// </summary>
        /// <param name="windowClosedAction">Action to perform when the window is closed</param>
        public ErrorMessagesViewModel(Action windowClosedAction)
        {
            errorMessageList = new SourceList<string>();

            errorMessageListDisposable = errorMessageList.Connect().ObserveOn(RxApp.MainThreadScheduler).Bind(out var messages).Subscribe();
            ErrorMessageList = messages;
            windowClosed = windowClosedAction;
        }

        private readonly SourceList<string> errorMessageList;
        private readonly IDisposable errorMessageListDisposable;
        private ErrorMessagesView view;
        private readonly Action windowClosed;

        public ReadOnlyObservableCollection<string> ErrorMessageList { get; set; }

        public void AddMessages(List<string> messages)
        {
            errorMessageList.AddRange(messages);
        }

        public void ClearMessages()
        {
            errorMessageList.Clear();
        }

        public void Dispose()
        {
            errorMessageList?.Dispose();
            errorMessageListDisposable?.Dispose();
        }

        public void ShowWindow()
        {
            // Use the Dispatcher to avoid apartment threading error
            // "The calling thread must be STA, because many UI components require this"
            Application.Current.Dispatcher.Invoke(ShowErrorMessagesWork);
        }

        private void ShowErrorMessagesWork()
        {
            if (view != null)
            {
                view.Topmost = true;
                return;
            }

            view = new ErrorMessagesView
            {
                DataContext = this,
                ShowActivated = true,
                Topmost = true
            };

            if (Application.Current.MainWindow != null && !Application.Current.MainWindow.Equals(view))
            {
                view.Owner = Application.Current.MainWindow;
            }

            view.Closed += (sender, args) =>
            {
                view = null;
                windowClosed();
            };

            view.Show();
        }
    }
}

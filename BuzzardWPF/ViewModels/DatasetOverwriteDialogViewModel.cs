using System.Reactive;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class DatasetOverwriteDialogViewModel : ReactiveObject
    {
        #region Attributes
        private string m_fileToRenamePath;
        private string m_fileInWayPath;
        private bool m_doSameToOtherConflicts;
        private bool m_skipRename;
        #endregion

        #region Initialize
        public DatasetOverwriteDialogViewModel()
        {
            DoSameToOtherConflicts = false;
            FileToRenamePath = null;
            FileInWayPath = null;
            SkipDatasetRename = false;

            ReplaceDatasetCommand = ReactiveCommand.Create(ReplaceDataset);
            SkipDatasetCommand = ReactiveCommand.Create(SkipDataset);
        }
        #endregion

        #region Properties

        public ReactiveCommand<Unit, Unit> ReplaceDatasetCommand { get; }
        public ReactiveCommand<Unit, Unit> SkipDatasetCommand { get; }

        public FileFolderInfoViewerViewModel SourcePathData { get; }
        public FileFolderInfoViewerViewModel DestinationPathData { get; }

        public string FileToRenamePath
        {
            get { return m_fileToRenamePath; }
            set
            {
                if (m_fileToRenamePath != value)
                {
                    m_fileToRenamePath = value;
                    this.RaisePropertyChanged("FileToRenamePath");

                    SourcePathData.PathName = value;
                }
            }
        }

        public string FileInWayPath
        {
            get { return m_fileInWayPath; }
            set
            {
                if (m_fileInWayPath != value)
                {
                    m_fileInWayPath = value;
                    this.RaisePropertyChanged("FileInWayPath");

                    DestinationPathData.PathName = value;
                }
            }
        }

        public bool DoSameToOtherConflicts
        {
            get { return m_doSameToOtherConflicts; }
            set
            {
                if (m_doSameToOtherConflicts != value)
                {
                    m_doSameToOtherConflicts = value;
                    this.RaisePropertyChanged("DoSameToOtherConflicts");
                }
            }
        }

        public bool SkipDatasetRename
        {
            get { return m_skipRename; }
            private set
            {
                if (m_skipRename != value)
                {
                    m_skipRename = value;
                    this.RaisePropertyChanged("SkipDatasetRename");
                }
            }
        }

        public bool Success { get; private set; }

        #endregion

        #region Event Handlers
        private void ReplaceDataset()
        {
            SkipDatasetRename = false;
            Success = true;
        }

        private void SkipDataset()
        {
            SkipDatasetRename = true;
            Success = true;
        }
        #endregion
    }
}

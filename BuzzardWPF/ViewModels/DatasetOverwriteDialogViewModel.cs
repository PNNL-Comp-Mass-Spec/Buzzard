﻿using System;
using System.Reactive;
using ReactiveUI;

namespace BuzzardWPF.ViewModels
{
    public class DatasetOverwriteDialogViewModel : ReactiveObject
    {
        #region Attributes
        private bool m_doSameToOtherConflicts;
        private bool m_skipRename;
        #endregion

        #region Initialize

        /// <summary>
        /// Constructor for valid design-time data context
        /// </summary>
        [Obsolete("For WPF design-time view only", true)]
        public DatasetOverwriteDialogViewModel() : this(null, null)
        {
        }

        public DatasetOverwriteDialogViewModel(string sourceFilePath, string existingTargetFilePath)
        {
            DoSameToOtherConflicts = false;
            SourcePathData.PathName = sourceFilePath;
            ExistingTargetPathData.PathName = existingTargetFilePath;
            SkipDatasetRename = false;

            ReplaceDatasetCommand = ReactiveCommand.Create(ReplaceDataset);
            SkipDatasetCommand = ReactiveCommand.Create(SkipDataset);
        }
        #endregion

        #region Properties

        public ReactiveCommand<Unit, Unit> ReplaceDatasetCommand { get; }
        public ReactiveCommand<Unit, Unit> SkipDatasetCommand { get; }

        public FileFolderInfoViewerViewModel SourcePathData { get; } = new FileFolderInfoViewerViewModel();
        public FileFolderInfoViewerViewModel ExistingTargetPathData { get; } = new FileFolderInfoViewerViewModel();

        public bool DoSameToOtherConflicts
        {
            get => m_doSameToOtherConflicts;
            set => this.RaiseAndSetIfChanged(ref m_doSameToOtherConflicts, value);
        }

        public bool SkipDatasetRename
        {
            get => m_skipRename;
            private set => this.RaiseAndSetIfChanged(ref m_skipRename, value);
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

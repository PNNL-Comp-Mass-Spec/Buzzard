﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BuzzardWPF.Views
{
    /// <summary>
    /// Interaction logic for DatasetOverwriteDialogWindow.xaml
    /// </summary>
    public partial class DatasetOverwriteDialogWindow : Window
    {
        public DatasetOverwriteDialogWindow()
        {
            InitializeComponent();
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}

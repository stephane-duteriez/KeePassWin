﻿using KeePass.Win.ViewModels;
using System;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace KeePass.Win.Views
{
    public sealed partial class PasswordDialog : ContentDialog, IContentDialogResult
    {
        private ResultState _result = ResultState.None;

        public PasswordDialog(IFile db, Func<IFile, PasswordDialogViewModel> modelCreator)
        {
            this.InitializeComponent();

            Model = modelCreator(db);
        }

        private enum ResultState
        {
            None, Open, Cancel
        }

        private PasswordDialogViewModel Model { get; }

        public void Enter() => _result = ResultState.Open;

        public void Escape() => _result = ResultState.Cancel;

        private void CancelClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) => Escape();

        private void UnlockDatabaseClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) => Enter();

        public async Task<PasswordDialogViewModel> GetModelAsync()
        {
            await ShowAsync();

            if (_result == ResultState.Cancel || _result == ResultState.None)
            {
                return null;
            }
            else
            {
                return Model;
            }
        }
    }
}

﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BuzzardWPF.Utility;

namespace BuzzardWPF.Data.DMS
{
    [Serializable]
    public class UserIDPIDCrossReferenceEntry : IEquatable<UserIDPIDCrossReferenceEntry>, INotifyPropertyChangedExt
    {
        public UserIDPIDCrossReferenceEntry(int userId, string pid)
        {
            UserID = userId;
            PID = pid;
        }

        public UserIDPIDCrossReferenceEntry()
        {
        }

        private int userID;
        private string pid;

        public int UserID
        {
            get => userID;
            set => this.RaiseAndSetIfChanged(ref userID, value);
        }

        public string PID
        {
            get => pid;
            set => this.RaiseAndSetIfChanged(ref pid, value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool Equals(UserIDPIDCrossReferenceEntry other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return userID == other.userID && pid == other.pid;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((UserIDPIDCrossReferenceEntry) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                // ReSharper disable NonReadonlyMemberInGetHashCode

                return (userID * 397) ^ (pid?.GetHashCode() ?? 0);

                // ReSharper restore NonReadonlyMemberInGetHashCode
            }
        }
    }
}
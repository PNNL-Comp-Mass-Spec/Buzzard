using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace BuzzardWPF.Management
{
    public interface IStoredSettingsMonitor : IReactiveObject
    {
        bool SettingsChanged { get; set; }
    }

    public static class StoredSettingsMonitorExtensions
    {
        /// <summary>
        /// If the newValue is not equal to the backingField value (using default EqualityComparer), sets backingField and raises OnPropertyChanged
        /// </summary>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="obj"></param>
        /// <param name="backingField">Backing field for the property</param>
        /// <param name="newValue">New property value</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>final value of backingField</returns>
        public static TRet RaiseAndSetIfChangedMonitored<TRet>(this IStoredSettingsMonitor obj,
            ref TRet backingField, TRet newValue, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            {
                return newValue;
            }

            obj.RaiseAndSetIfChanged(ref backingField, newValue, propertyName);
            obj.SettingsChanged = true;
            return newValue;
        }

        /// <summary>
        /// If the newValue is not equal to the backingField value (using default EqualityComparer), sets backingField and raises OnPropertyChanged
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="backingField">Backing field for the property</param>
        /// <param name="newValue">New property value</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>true if value changed</returns>
        public static bool RaiseAndSetIfChangedMonitoredBool<T>(this IStoredSettingsMonitor obj,
            ref T backingField, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, newValue))
            {
                return false;
            }

            obj.RaiseAndSetIfChanged(ref backingField, newValue, propertyName);
            obj.SettingsChanged = true;
            return true;
        }

        /// <summary>
        /// If the newValue is not equal to the backingField value (using default EqualityComparer), sets backingField and raises OnPropertyChanged
        /// </summary>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="obj"></param>
        /// <param name="backingField">Backing field for the property</param>
        /// <param name="newValue">New property value</param>
        /// <param name="postChangeAction">Action executed if the property changed</param>
        /// <param name="propertyName">Name of the property</param>
        /// <returns>true if value changed</returns>
        public static TRet RaiseAndSetIfChanged<TRet>(this IReactiveObject obj,
            ref TRet backingField, TRet newValue, Action<TRet> postChangeAction, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<TRet>.Default.Equals(backingField, newValue))
            {
                return newValue;
            }

            obj.RaiseAndSetIfChanged(ref backingField, newValue, propertyName);
            postChangeAction?.Invoke(newValue);
            return newValue;
        }
    }
}

using System;
using System.ComponentModel;

namespace Blacker.MangaScraper.ViewModel
{

    /// <summary>
    /// Base ViewModel class
    /// </summary>
    abstract class BaseViewModel : INotifyPropertyChanged
    {
        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void InvokePropertyChanged(string propertyName)
        {
            var e = new PropertyChangedEventArgs(propertyName);

            PropertyChangedEventHandler changed = PropertyChanged;

            if (changed != null)
                changed(this, e);
        }

        #endregion  // Implementation of INotifyPropertyChanged

        public System.Windows.Window Owner { get; protected set; }
    }
}
